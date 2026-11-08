// Janelas modais sobre <dialog>: foco preso pelo navegador, Esc fecha, fundo inerte e retorno do foco.
import { h, icon, uid } from '../core/dom.js';

let active = null;
let returnFocus = null;

export function openDialog(title, { size = 'md', description } = {}) {
  if (active?.open) { returnFocus ||= null; active.close(); }
  else returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null;

  const titleId = uid('dlg-title');
  const closers = [];
  const body = h('div', { class: 'dialog-body stack' });
  const titleNode = h('h2', { id: titleId, tabindex: '-1' }, title);
  const closeButton = h('button', { type: 'button', class: 'icon-btn', 'aria-label': 'Fechar janela' }, icon('close', 22));
  const dialog = h('dialog', { class: `dialog dialog-${size}`, 'aria-labelledby': titleId },
    h('div', { class: 'dialog-top' }, titleNode, closeButton),
    description ? h('p', { class: 'muted dialog-desc' }, description) : null,
    body);
  const api = {
    dialog, body, titleNode,
    get open() { return dialog.open; },
    onClose(fn) { closers.push(fn); },
    close() { if (dialog.open) dialog.close(); },
    setTitle(text) { titleNode.textContent = text; }
  };
  closeButton.addEventListener('click', () => api.close());
  dialog.addEventListener('click', (event) => { if (event.target === dialog) api.close(); }); // clique no fundo
  dialog.addEventListener('close', () => {
    for (const fn of closers) { try { fn(); } catch { /* limpeza não deve travar o fechamento */ } }
    dialog.remove();
    if (active === api) {
      active = null;
      const target = returnFocus; returnFocus = null;
      if (target?.isConnected) target.focus();
    }
  });
  document.getElementById('dialog-root').append(dialog);
  dialog.showModal();
  titleNode.focus({ preventScroll: true });
  active = api;
  return api;
}
export const closeActiveDialog = () => active?.close();
export const dialogOpen = () => !!active?.open;
