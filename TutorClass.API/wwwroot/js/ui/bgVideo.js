// Vídeo de fundo do site: decorativo, sem áudio, em repetição. Para atender à WCAG 2.2.2 (pausar, parar, ocultar):
// há um botão fixo para pausar/retomar e o vídeo começa pausado quando o usuário pede "reduzir movimento" no sistema ou no painel de acessibilidade.
import { h, icon } from '../core/dom.js';
import { announce } from '../core/a11y.js';

const KEY = 'tc-bgvideo-paused';
const read = () => { try { return localStorage.getItem(KEY) === '1'; } catch { return false; } };
const write = (v) => { try { localStorage.setItem(KEY, v ? '1' : '0'); } catch { /* sem armazenamento */ } };

export function setupBackgroundVideo() {
  const wrap = document.getElementById('bg-video'), video = wrap?.querySelector('video');
  if (!video) return;
  const root = document.documentElement;
  const reduced = () => root.dataset.motion === 'reduce' || (root.dataset.motion !== 'reduce' && matchMedia('(prefers-reduced-motion: reduce)').matches);
  let userPaused = read();

  const btn = h('button', { type: 'button', class: 'bg-video-toggle', 'aria-pressed': 'false' });
  const paint = () => {
    const paused = video.paused;
    btn.replaceChildren(icon(paused ? 'play' : 'pause', 20), h('span', { class: 'sr-only' }, paused ? 'Reproduzir vídeo de fundo' : 'Pausar vídeo de fundo'));
    btn.title = paused ? 'Reproduzir vídeo de fundo' : 'Pausar vídeo de fundo';
    btn.setAttribute('aria-pressed', String(paused));
    wrap.classList.toggle('is-paused', paused);
  };
  const play = () => video.play().catch(() => {}).finally(paint);
  const hidden = () => document.body.classList.contains('in-app') || ['login'].includes(document.body.dataset.route) || (document.body.dataset.route || '').startsWith('cadastro-');
  const sync = () => { if (userPaused || reduced() || hidden()) { video.pause(); paint(); } else play(); };
  btn.addEventListener('click', () => {
    if (video.paused) { userPaused = false; write(false); play(); announce('Vídeo de fundo reproduzindo.'); }
    else { userPaused = true; write(true); video.pause(); paint(); announce('Vídeo de fundo pausado.'); }
  });
  video.addEventListener('play', paint); video.addEventListener('pause', paint);
  document.body.append(btn);
  new MutationObserver(sync).observe(root, { attributes: true, attributeFilter: ['data-motion'] });
  new MutationObserver(sync).observe(document.body, { attributes: true, attributeFilter: ['class', 'data-route'] });
  matchMedia('(prefers-reduced-motion: reduce)').addEventListener?.('change', sync);
  document.addEventListener('visibilitychange', () => { if (document.hidden) video.pause(); else sync(); });
  sync();
}
