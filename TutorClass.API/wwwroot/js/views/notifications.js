import { h, mount } from '../core/dom.js';
import { get, post, q, errorText, isAbort } from '../core/api.js';
import { role } from '../core/session.js';
import { when } from '../core/format.js';
import { announce } from '../core/a11y.js';
import { setUnread } from '../shell.js';
import { pageHeader, card, badge, emptyState, loading, btn, link } from '../ui/components.js';
import { createForm, field } from '../ui/form.js';
import { notice } from '../ui/toast.js';

const MAP = [['aula', 'aulas'], ['material', 'materiais'], ['mentoria', 'mentoria'], ['plano', 'planos'], ['assinatura', 'planos'], ['financeiro', 'financeiro'], ['pagamento', 'financeiro'], ['candidatura', 'profissional'], ['qualific', 'profissional'], ['gravac', 'gravacoes']];
export function notificationRoute(route) {
  if (!route) return null;
  const target = MAP.find(([m]) => String(route).toLowerCase().includes(m));
  if (!target) return null;
  if (role() === 'Responsavel') return '#app/vinculados';
  return `#app/${target[1]}`;
}

export default async function notifications(ctx) {
  const summary = await get('/api/notificacoes/resumo');
  setUnread(summary.naoLidas);
  const title = h('h2', { 'aria-live': 'polite' }, `${summary.naoLidas} notificação(ões) não lida(s)`);
  const list = h('div', { class: 'stack' }), more = btn('Carregar mais', () => load(true));
  let page = 1, onlyUnread = 'false';

  const filter = createForm({ fields: [field('filtro', 'Mostrar', 'text', { options: [{ value: 'false', label: 'Todas' }, { value: 'true', label: 'Somente não lidas' }] })], submit: 'Filtrar',
    onSubmit: async (v) => { onlyUnread = v.filtro; await load(false); } });
  const markAll = btn('Marcar todas como lidas', async () => { await post('/api/notificacoes/marcar-todas-lidas'); notice('Notificações marcadas como lidas.'); ctx.router.refresh(); });

  async function load(next) {
    if (!next) { page = 1; mount(list, loading()); }
    const rows = await get(`/api/notificacoes${q({ somenteNaoLidas: onlyUnread, page, pageSize: 20 })}`, { signal: ctx.signal });
    if (!next) list.replaceChildren();
    if (!rows.length && page === 1) list.append(emptyState('Nenhuma notificação encontrada.'));
    for (const n of rows) {
      const target = notificationRoute(n.rotaAcao);
      const actions = h('div', { class: 'actions' },
        !n.lida ? btn('Marcar como lida', async () => { await post(`/api/notificacoes/${n.id}/marcar-lida`); ctx.router.refresh(); }, { kind: 'ghost' }) : null,
        target ? link('Abrir área relacionada', target) : null);
      list.append(card({ title: n.titulo }, badge(n.lida ? 'Lida' : 'Não lida', n.lida ? 'neutral' : 'warn'), n.prioridade === 'Importante' ? badge('Importante', 'bad') : null,
        h('p', n.mensagem), h('p', { class: 'small muted' }, when(n.dataCriacaoUtc)), actions));
    }
    more.hidden = rows.length < 20; page++;
    if (next) announce(`${rows.length} notificação(ões) adicionada(s).`);
  }
  ctx.host.append(pageHeader('Notificações'), title, h('div', { class: 'actions' }, markAll), filter.el, list, more);
  await load(false);
}
