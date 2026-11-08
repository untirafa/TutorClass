// Avisos (popups) no topo da tela: cartão colorido com ícone, título, botão X e barra de tempo.
// Somem em 4 segundos; o tempo pausa enquanto o ponteiro ou o foco do teclado estiver sobre o aviso (WCAG 2.2.1).
import { h, icon } from '../core/dom.js';

const KINDS = {
  success: { icon: 'check', title: 'Tudo certo', role: 'status' },
  error: { icon: 'alert', title: 'Algo deu errado', role: 'alert' },
  warning: { icon: 'lock', title: 'Atenção', role: 'alert' },
  info: { icon: 'info', title: 'Aviso', role: 'status' },
  upgrade: { icon: 'spark', title: 'Que tal ir além?', role: 'status' }
};
export const TOAST_MS = 4000;
const MAX = 3;

export function notice(message, { error = false, kind, title, duration = TOAST_MS, action } = {}) {
  const root = document.getElementById('toast-root');
  if (!root || !message) return;
  const type = kind || (error ? 'error' : 'success');
  const spec = KINDS[type] || KINDS.info;
  while (root.children.length >= MAX) root.firstElementChild.remove();

  const close = h('button', { type: 'button', class: 'toast-close', 'aria-label': 'Fechar aviso' }, icon('close', 18));
  const bar = h('span', { class: 'toast-bar', 'aria-hidden': 'true', style: `--toast-ms:${duration}ms` });
  const node = h('div', { class: `toast is-${type}`, role: spec.role },
    h('span', { class: 'toast-icon', 'aria-hidden': 'true' }, icon(spec.icon, 22)),
    h('div', { class: 'toast-body' }, h('strong', title || spec.title), h('p', message),
      action ? h('a', { class: 'toast-action', href: action.href, onclick: () => dismiss() }, action.label) : null),
    close, bar);

  let remaining = duration, started = Date.now(), timer;
  const dismiss = () => {
    clearTimeout(timer);
    if (!node.isConnected) return;
    node.classList.add('is-leaving');
    const done = () => node.remove();
    node.addEventListener('animationend', done, { once: true });
    setTimeout(done, 400);
  };
  const start = () => { started = Date.now(); node.classList.remove('is-paused'); timer = setTimeout(dismiss, remaining); };
  const pause = () => { clearTimeout(timer); remaining = Math.max(1200, remaining - (Date.now() - started)); node.classList.add('is-paused'); };
  node.addEventListener('pointerenter', pause); node.addEventListener('pointerleave', start);
  node.addEventListener('focusin', pause); node.addEventListener('focusout', start);
  close.addEventListener('click', dismiss);
  root.append(node); start();
  return node;
}
export const clearNotice = () => { document.getElementById('toast-root')?.replaceChildren(); };
