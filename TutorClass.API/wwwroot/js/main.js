// Ponto de entrada: configura a API, carrega preferências de acessibilidade, liga o roteador e a casca.
import { configure, session, isSignedIn, onSessionReset, resetSession, role } from './core/session.js';
import { setExpiredHandler } from './core/api.js';
import { loadPrefs, announce, focusHeading } from './core/a11y.js';
import { createRouter, match } from './core/router.js';
import { h, mount } from './core/dom.js';
import { renderChrome, refreshUnread, setupSkipLinks, closeNav, currentPath } from './shell.js';
import { ROUTES } from './routes.js';
import { findModule } from './modules.js';
import { notice, clearNotice } from './ui/toast.js';
import { closeActiveDialog } from './ui/dialog.js';
import { loading, errorState } from './ui/components.js';

import { setupBackgroundVideo } from './ui/bgVideo.js';
import { setupVLibras } from './ui/vlibras.js';
import { isNativeApp, setupAppStrip } from './ui/appDownload.js';

loadPrefs();
setupBackgroundVideo();
setupVLibras();
if (isNativeApp()) document.body.classList.add('native-app');
setupAppStrip();

// Mantém a hierarquia de títulos sem saltos (1.3.1): títulos de cartão assumem o nível seguinte ao título anterior.
function fixHeadings(root) {
  let prev = 0;
  for (const el of [...root.querySelectorAll('h1,h2,h3,h4')]) {
    if (el.closest('dialog') || !el.getClientRects().length) continue;
    const level = Number(el.tagName[1]);
    if (prev && level > prev + 1) {
      const fixed = document.createElement('h' + (prev + 1));
      for (const a of el.attributes) fixed.setAttribute(a.name, a.value);
      fixed.append(...el.childNodes); el.replaceWith(fixed); prev = prev + 1;
    } else prev = level;
  }
}
let headingTimer;
new MutationObserver(() => { clearTimeout(headingTimer); headingTimer = setTimeout(() => fixHeadings(document.getElementById('main')), 30); })
  .observe(document.getElementById('main'), { childList: true, subtree: true });

const main = document.getElementById('main');
let nav = 0, controller = null, cleanups = [];
let router;

function runCleanups() { for (const fn of cleanups.splice(0)) { try { fn(); } catch { /* limpeza best-effort */ } } }

function logout(message) {
  resetSession(); closeActiveDialog(); clearNotice();
  if (message) notice(message, { error: true });
  router.go('login');
}

async function onRoute(found) {
  const id = ++nav;
  controller?.abort(); runCleanups();
  controller = new AbortController();
  closeNav(); closeActiveDialog();
  const { route, params, query } = found;
  const signal = controller.signal;

  if (route?.area === 'app' && !isSignedIn()) { session.returnTo = location.hash; router.go('login'); return; }
  if (route?.area === 'auth' && isSignedIn()) { router.go('app/inicio'); return; }

  const area = route?.area === 'app' ? 'app' : 'public';
  const activeModule = area === 'app' ? findModule(params.modulo || 'inicio', role()) : null;
  if (area === 'app') await Promise.race([refreshUnread(), new Promise(r => setTimeout(r, 600))]);
  document.body.dataset.route = found.path;
  renderChrome({ area, activeModule, path: found.path }, { onLogout: () => { logout(); announce('Você saiu da conta.'); } });
  document.body.classList.remove('has-bookbar');
  document.documentElement.dataset.area = area;

  mount(main, h('div', { class: 'container' }, loading()));
  let title = route?.title || 'Página não encontrada';
  const ctx = {
    host: main, params, query, signal, router, path: found.path, route,
    cleanup: (fn) => cleanups.push(fn),
    setTitle: (t) => { title = t; },
    isCurrent: () => id === nav && !signal.aborted
  };
  try {
    if (!route) { (await import('./views/notFound.js')).default(ctx); }
    else { const view = await route.load(); await view(ctx); }
  } catch (error) {
    if (id !== nav || error?.name === 'AbortError') return;
    mount(main, h('div', { class: 'container' }, errorState(error.message || 'Não foi possível abrir esta tela.', () => router.refresh())));
  }
  if (id !== nav) return;
  document.title = `${title} — TutorClass`;
  const anchor = found.path === 'sobre' ? 'sobre' : null;
  if (anchor) { document.getElementById(anchor)?.scrollIntoView(); document.getElementById(anchor)?.querySelector('h2')?.focus?.({ preventScroll: true }); }
  else { scrollTo(0, 0); focusHeading(main); }
  announce(`${title}. Página carregada.`);
}

setExpiredHandler((message) => logout(message || 'Sua sessão expirou. Entre novamente.'));
onSessionReset(() => { document.body.classList.remove('in-app'); });
try { configure(); } catch (error) { mount(main, h('div', { class: 'container' }, errorState(error.message))); throw error; }
setupSkipLinks();
router = createRouter(ROUTES, onRoute);
globalThis.TutorClassApp = Object.freeze({ router, session: { get signedIn() { return isSignedIn(); } } });
router.start();
if (location.protocol === 'file:') notice('Abra o site por http://localhost:5242. Arquivos abertos diretamente não têm origem HTTP aceita pela API.', { error: true });
