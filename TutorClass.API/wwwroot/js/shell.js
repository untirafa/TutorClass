// Casca da aplicação: cabeçalho, navegação principal, menu lateral (desktop), barra inferior (celular),
// painel de acessibilidade e contador de notificações.
import { h, icon, mount } from './core/dom.js';
import { session, isSignedIn, role, resetSession, roleLabel, isMinorStudent } from './core/session.js';
import { get, isAbort } from './core/api.js';
import { getPrefs, setPrefs, DEFAULT_PREFS, announce } from './core/a11y.js';
import { openDialog } from './ui/dialog.js';
import { modulesFor, SHORTCUTS } from './modules.js';

const PUBLIC_NAV = [['home', 'Início'], ['professores', 'Buscar professores'], ['materias', 'Matérias'], ['planos', 'Planos'], ['ajuda', 'Ajuda']];
const $ = (id) => document.getElementById(id);
let unread = 0;
// Na área interna o cabeçalho tem só a marca e as ações; a Visão geral (casinha) fica na barra lateral com os demais módulos.
export const QUICK = { Aluno: [], Professor: [], Responsavel: [], Administrador: [] };

export function currentPath() { return (location.hash || '#home').replace(/^#\/?/, '') || 'home'; }

export function accessibilityPanel() {
  const prefs = getPrefs();
  const dlg = openDialog('Preferências de acessibilidade', {
    description: 'Suas escolhas ficam salvas neste navegador e valem para todas as telas.'
  });
  const group = (name, legend, options) => h('fieldset', { class: 'pref-group' },
    h('legend', legend),
    h('div', { class: 'pref-options' }, options.map(([value, label]) => h('label', null,
      h('input', { type: 'radio', name, value, checked: prefs[name] === value, onchange: () => { setPrefs({ [name]: value }); announce(`${legend}: ${label}.`); } }),
      h('span', label)))));
  dlg.body.append(
    group('scale', 'Tamanho do texto', [['100', 'Padrão'], ['115', 'Grande (115%)'], ['130', 'Maior (130%)']]),
    group('theme', 'Tema', [['auto', 'Seguir o sistema'], ['light', 'Claro'], ['dark', 'Escuro']]),
    group('contrast', 'Contraste', [['auto', 'Padrão'], ['high', 'Alto contraste']]),
    group('motion', 'Animações', [['auto', 'Seguir o sistema'], ['reduce', 'Reduzir movimento']]),
    h('button', { type: 'button', class: 'btn secondary', onclick: () => { setPrefs(DEFAULT_PREFS); dlg.close(); announce('Preferências restauradas.'); } }, 'Restaurar padrões'),
    h('p', { class: 'small muted' }, 'Dica: o site também respeita o zoom do navegador (até 400%) e as preferências de contraste e movimento do sistema operacional. ',
      h('a', { href: '#/acessibilidade', onclick: () => dlg.close() }, 'Saiba mais sobre a acessibilidade do TutorClass.')));
}

function headerActions(area, onLogout) {
  const nodes = [];
  nodes.push(h('button', { type: 'button', class: 'btn ghost', onclick: accessibilityPanel }, icon('access', 20), h('span', { class: 'hide-sm' }, 'Acessibilidade')));
  if (isSignedIn()) {
    if (area === 'app') {
      nodes.push(h('a', { class: 'btn ghost', href: '#app/notificacoes', 'aria-label': unread ? `Notificações, ${unread} não lida(s)` : 'Notificações' },
        icon('bell', 20), unread ? h('span', { class: 'count-badge', 'aria-hidden': 'true' }, unread > 99 ? '99+' : String(unread)) : null));
      nodes.push(h('button', { type: 'button', class: 'btn secondary', onclick: onLogout }, icon('logout', 18), 'Sair'));
    } else nodes.push(h('a', { class: 'btn primary', href: '#app/inicio' }, 'Minha área'));
  } else {
    nodes.push(h('a', { class: 'btn secondary', href: '#login' }, 'Entrar'));
    nodes.push(h('a', { class: 'btn primary hide-xs', href: '#cadastro-aluno' }, 'Criar conta'));
  }
  if (area !== 'app') nodes.push(h('button', {
    type: 'button', class: 'btn ghost menu-toggle', id: 'menu-toggle', 'aria-expanded': 'false', 'aria-controls': 'primary-nav',
    onclick: (e) => toggleNav(e.currentTarget)
  }, icon('menu', 22), h('span', { class: 'sr-only' }, 'Menu principal')));
  return nodes;
}

function toggleNav(button, force) {
  const nav = $('primary-nav');
  const open = force ?? button.getAttribute('aria-expanded') !== 'true';
  button.setAttribute('aria-expanded', String(open));
  nav.classList.toggle('is-open', open);
}
export function closeNav() {
  const b = $('menu-toggle'); if (b) toggleNav(b, false);
}

export function renderChrome({ area, activeModule, path }, { onLogout }) {
  const inApp = area === 'app' && isSignedIn();
  document.body.classList.toggle('in-app', inApp);
  const list = $('primary-nav-list');
  const quickIds = inApp ? (QUICK[role()] || []) : [];
  const quick = quickIds.map(id => modulesFor(role()).find(m => m.id === id)).filter(Boolean);
  const items = inApp ? [] : PUBLIC_NAV;
  const nav = $('primary-nav');
  nav.hidden = inApp && !quick.length;
  nav.setAttribute('aria-label', inApp ? 'Início' : 'Principal');
  list.replaceChildren(...(inApp
    ? quick.map(m => h('li', null, h('a', { class: 'home-link', href: `#app/${m.id}`, title: m.label, 'aria-label': m.label, 'aria-current': m.id === activeModule?.id || m.id === activeModule?.parent ? 'page' : null }, icon(m.icon, 22))))
    : items.map(([href, label]) => h('li', null,
      h('a', { href: `#${href}`, 'aria-current': path === href || (href === 'professores' && path.startsWith('professores/')) ? 'page' : null }, label)))));
  mount($('header-actions'), ...headerActions(inApp ? 'app' : 'public', onLogout));

  const sidebar = $('sidebar'), bottom = $('bottom-nav');
  sidebar.hidden = !inApp; bottom.hidden = !inApp;
  if (!inApp) { sidebar.replaceChildren(); bottom.replaceChildren(); return; }

  const user = session.user, mods = modulesFor(role()).filter(m => !m.hidden), inSidebar = mods.filter(m => !quickIds.includes(m.id));
  document.body.dataset.role = role();
  const activeTop = (id) => id === activeModule?.id || id === activeModule?.parent;
  const locked = (m) => m.id === 'financeiro' && isMinorStudent();
  const li = (m, cls) => h('li', null, h('a', { href: `#app/${m.id}`, 'aria-current': activeTop(m.id) ? 'page' : null, class: cls }, icon(locked(m) ? 'lock' : m.icon, 20), m.label, locked(m) ? h('span', { class: 'sr-only' }, ' (bloqueado para menores de 18 anos)') : null));
  mount(sidebar,
    h('div', { class: 'who' }, h('strong', user.nome), h('span', { class: 'muted small' }, `Perfil: ${roleLabel(user.perfil)}`)),
    h('nav', { 'aria-label': 'Menu da área interna' }, h('ul', null, inSidebar.map(m => li(m)))));

  const short = (SHORTCUTS[role()] || []).map(id => mods.find(m => m.id === id)).filter(Boolean);
  mount(bottom, h('ul', null,
    short.map(m => h('li', null, h('a', { href: `#app/${m.id}`, 'aria-current': activeTop(m.id) ? 'page' : null }, icon(m.icon, 22), m.short || m.label))),
    h('li', null, h('button', { type: 'button', 'aria-haspopup': 'dialog', onclick: () => openDrawer(mods, activeTop) }, icon('menu', 22), 'Menu'))));
}

function openDrawer(mods, activeTop) {
  const dlg = openDialog('Menu', { size: 'md' });
  dlg.body.append(h('nav', { class: 'drawer-nav', 'aria-label': 'Todos os módulos' }, h('ul', null,
    mods.map(m => h('li', null, h('a', { href: `#app/${m.id}`, 'aria-current': activeTop(m.id) ? 'page' : null, onclick: () => dlg.close() }, icon(m.icon, 20), m.label))))));
}

export async function refreshUnread() {
  if (!isSignedIn()) { unread = 0; return; }
  try { const s = await get('/api/notificacoes/resumo'); unread = s?.naoLidas ?? 0; }
  catch (e) { if (!isAbort(e)) unread = 0; }
}
export const setUnread = (n) => { unread = n; };

export function setupSkipLinks() {
  // Os atalhos movem também o foco (alguns navegadores só rolam a página).
  document.querySelectorAll('[data-skip]').forEach(a => a.addEventListener('click', (e) => {
    e.preventDefault();
    const target = a.dataset.skip === 'nav'
      ? (document.querySelector('#sidebar:not([hidden]) a, #primary-nav:not([hidden]) a, #bottom-nav:not([hidden]) a'))
      : $('main');
    if (target) { if (!target.matches('a,button')) target.setAttribute('tabindex', '-1'); target.focus(); }
  }));
}

export { resetSession };
