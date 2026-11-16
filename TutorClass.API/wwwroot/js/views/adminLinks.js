// Administração: análise das solicitações de vínculo entre responsável e aluno.
import { h } from '../core/dom.js';
import { get, post } from '../core/api.js';
import { when, dayOnly } from '../core/format.js';
import { pageHeader, card, dl, btn, statusBadge, pagedList } from '../ui/components.js';
import { field, createForm } from '../ui/form.js';
import { formDialog } from '../ui/dialogs.js';

const STATUSES = ['', 'Pendente', 'Aprovada', 'Recusada'];

function approve(r, reload) {
  if (!r.candidatos.length) return;
  const label = (c) => `${c.nome} · ${c.email}${c.dataNascimento ? ` · nasc. ${dayOnly(c.dataNascimento)}` : ''}`;
  formDialog(`Aprovar vínculo: ${r.responsavelNome}`, [
    field('alunoId', 'Aluno a vincular', 'text', { options: r.candidatos.map(c => ({ value: c.id, label: label(c) })), help: `Nome informado pelo responsável: ${r.nomeAlunoInformado}. Confira o e-mail e a data de nascimento antes de aprovar.` })], {},
  async (v) => { await post(`/api/admin/solicitacoes-vinculo/${r.id}/aprovar`, { alunoId: v.alunoId }); reload(); },
  { description: 'Ao aprovar, o responsável passa a acompanhar aulas, pagamentos e mentoria deste aluno. Os dois recebem um aviso.', save: 'Aprovar vínculo', success: 'Vínculo aprovado.' });
}

function refuse(r, reload) {
  formDialog(`Recusar vínculo: ${r.responsavelNome}`, [field('motivo', 'Motivo (obrigatório)', 'textarea', { maxLength: 500, minLength: 3 })], {},
    async (v) => { await post(`/api/admin/solicitacoes-vinculo/${r.id}/recusar`, { motivo: v.motivo }); reload(); },
    { description: 'O responsável receberá uma notificação com este motivo.', save: 'Recusar', danger: true, success: 'Solicitação recusada.' });
}

export default async function adminLinks(ctx) {
  const rows = await get('/api/admin/solicitacoes-vinculo', { signal: ctx.signal });
  const reload = () => ctx.router.refresh();
  const out = h('div', { 'aria-live': 'polite' });
  const render = (status) => out.replaceChildren(pagedList(rows.filter(r => !status || r.status === status).sort((a, b) => new Date(b.createdAtUtc) - new Date(a.createdAtUtc)),
    (r) => card({ title: r.responsavelNome, subtitle: r.responsavelEmail }, statusBadge(r.status),
      dl({ 'Aluno informado': r.nomeAlunoInformado, Parentesco: r.parentesco, 'Enviada em': when(r.createdAtUtc), 'Aluno vinculado': r.alunoNome, 'Motivo da recusa': r.motivo, 'Analisada em': r.analisadaEmUtc ? when(r.analisadaEmUtc) : null }),
      r.status === 'Pendente' ? h('div', { class: 'stack-sm' },
        r.candidatos.length ? h('p', { class: 'small muted' }, `${r.candidatos.length} aluno(s) com nome compatível.`) : h('p', { class: 'small' }, 'Nenhum aluno ativo com este nome. Confirme com o responsável ou recuse o pedido.'),
        h('div', { class: 'actions' }, r.candidatos.length ? btn('Aprovar', () => approve(r, reload), { kind: 'primary' }) : null, btn('Recusar', () => refuse(r, reload), { kind: 'danger' }))) : null),
    { empty: 'Nenhuma solicitação nesta seleção.', label: 'solicitações' }));
  const filter = createForm({ fields: [field('status', 'Situação', 'text', { required: false, options: STATUSES.map(s => ({ value: s, label: s || 'Todas' })), value: 'Pendente' })], submit: 'Filtrar', onSubmit: (v) => render(v.status || '') });
  const pending = rows.filter(r => r.status === 'Pendente').length;
  ctx.host.append(pageHeader('Vínculos de responsáveis', `${pending} solicitação(ões) aguardando decisão.`), filter.el, out);
  render('Pendente');
}
