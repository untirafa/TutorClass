// Administração: análise de candidaturas de professores e verificação das qualificações.
import { h } from '../core/dom.js';
import { get, post } from '../core/api.js';
import { when, statusLabel } from '../core/format.js';
import { pageHeader, card, dl, btn, statusBadge, pagedList, emptyState } from '../ui/components.js';
import { field, optional, createForm } from '../ui/form.js';
import { openDialog } from '../ui/dialog.js';
import { formDialog, confirmDialog } from '../ui/dialogs.js';
import { qualificationCard } from './shared.js';

const ACTIONS = {
  'iniciar-analise': { label: 'Iniciar análise', required: false, done: 'Análise iniciada.', from: ['Pendente'] },
  'solicitar-complementacao': { label: 'Solicitar complementação', required: true, done: 'Complementação solicitada.', from: ['Pendente', 'EmAnalise'] },
  aprovar: { label: 'Aprovar', required: false, done: 'Candidatura aprovada.', from: ['Pendente', 'EmAnalise', 'AguardandoDocumentacao'], primary: true },
  reprovar: { label: 'Reprovar', required: true, done: 'Candidatura reprovada.', danger: true, from: ['Pendente', 'EmAnalise', 'AguardandoDocumentacao'] }
};

function decide(c, verb, reload) {
  const a = ACTIONS[verb];
  formDialog(`${a.label}: ${c.professorNome}`, [field('motivo', a.required ? 'Motivo (obrigatório)' : 'Observação', 'textarea', { required: a.required, maxLength: 1000 })], {},
    async (v) => { await post(`/api/admin/candidaturas-professor/${c.id}/${verb}`, { motivo: v.motivo }); reload(); },
    { description: 'O professor receberá uma notificação com esta decisão.', save: a.label, danger: !!a.danger, success: a.done });
}

async function qualifications(c, reload) {
  const rows = await get(`/api/professores/${c.professorId}/qualificacoes`);
  const d = openDialog(`Qualificações de ${c.professorNome}`, { size: 'lg' });
  d.body.append(rows.length ? h('div', { class: 'stack' }, rows.map(q => h('div', { class: 'stack-sm' }, qualificationCard(q),
    q.documentoComprovacao ? h('p', { class: 'small' }, `Documento: ${q.documentoComprovacao}`) : null,
    q.statusVerificacao === 'Pendente' ? h('div', { class: 'actions' },
      btn('Verificar', () => confirmDialog('Verificar qualificação', q.titulo, async () => { await post(`/api/admin/qualificacoes/${q.id}/verificar`); d.close(); reload(); }, { confirm: 'Verificar', success: 'Qualificação verificada.' }), { kind: 'primary' }),
      btn('Rejeitar', () => confirmDialog('Rejeitar qualificação', q.titulo, async () => { await post(`/api/admin/qualificacoes/${q.id}/rejeitar`); d.close(); reload(); }, { confirm: 'Rejeitar', danger: true, success: 'Qualificação rejeitada.' }), { kind: 'danger' })) : null)))
    : emptyState('Este professor ainda não enviou qualificações.'));
}

export default async function adminReview(ctx) {
  const rows = await get('/api/admin/candidaturas-professor', { signal: ctx.signal });
  const reload = () => ctx.router.refresh();
  const out = h('div', { 'aria-live': 'polite' });
  const render = (status) => out.replaceChildren(pagedList(rows.filter(c => !status || c.status === status).sort((a, b) => new Date(b.createdAtUtc) - new Date(a.createdAtUtc)),
    (c) => card({ title: c.professorNome, subtitle: c.professorEmail }, statusBadge(c.status),
      dl({ 'Apresentação': c.mensagem, 'Retorno da análise': c.motivo, 'Enviada em': when(c.createdAtUtc), 'Analisada em': when(c.dataAnaliseUtc) }),
      h('div', { class: 'actions' }, btn('Qualificações', () => qualifications(c, reload)),
        Object.entries(ACTIONS).filter(([, a]) => a.from.includes(c.status)).map(([verb, a]) => btn(a.label, () => decide(c, verb, reload), { kind: a.primary ? 'primary' : a.danger ? 'danger' : 'secondary' })))),
    { empty: 'Nenhuma candidatura nesta seleção.', label: 'candidaturas' }));
  const statuses = ['', 'Pendente', 'EmAnalise', 'AguardandoDocumentacao', 'Aprovada', 'Reprovada'];
  const filter = createForm({ fields: [field('status', 'Situação', 'text', { required: false, options: statuses.map(s => ({ value: s, label: s ? statusLabel(s) : 'Todas' })) })], submit: 'Filtrar', onSubmit: (v) => render(v.status || '') });
  const pending = rows.filter(c => ['Pendente', 'EmAnalise', 'AguardandoDocumentacao'].includes(c.status)).length;
  ctx.host.append(pageHeader('Análise de professores', `${pending} candidatura(s) aguardando decisão.`), filter.el, out);
  render('');
}
