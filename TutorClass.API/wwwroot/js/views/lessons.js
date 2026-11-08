// Aulas: lista, detalhes e ações (confirmar, concluir, reagendar, cancelar, avaliar, pagar, acompanhar).
import { h, mount } from '../core/dom.js';
import { get, post, put, errorText, isAbort } from '../core/api.js';
import { isStudent, isTeacher, isGuardian } from '../core/session.js';
import { money, when, utcInputToIso, isoToUtcInput, brasiliaHint, estimateTotal, durationLabel } from '../core/format.js';
import { pageHeader, dl, emptyState, pagedList, statusBadge, btn, linkButton, card } from '../ui/components.js';
import { field, optional, createForm } from '../ui/form.js';
import { openDialog } from '../ui/dialog.js';
import { formDialog, confirmDialog } from '../ui/dialogs.js';
import { notice } from '../ui/toast.js';
import { lessonCard } from './shared.js';
import { showFollowup, editFollowup } from './followups.js';

export const PAYMENT_FIELD = field('formaPagamento', 'Forma de pagamento simulado', 'text', { options: [{ value: 'Pix', label: 'Pix' }, { value: 'Cartao', label: 'Cartão' }] });
export const timeFields = (price) => [
  field('inicio', 'Início da aula (UTC)', 'datetime-local', { help: 'Em UTC: 15:00 UTC = 12:00 em Brasília.', liveHint: brasiliaHint }),
  field('duracaoMinutos', 'Duração em minutos', 'number', { min: 30, max: 240, step: 1, value: 60, liveHint: (v) => { const t = estimateTotal(price, Number(v)); return t == null ? '' : `Total estimado: ${money(t)}`; } })
];
const subjectName = (l) => (typeof l.materia === 'string' ? l.materia : l.materia?.nome);

export default async function lessons(ctx) {
  const data = await get('/api/aulas/minhas', { signal: ctx.signal });
  const reload = () => ctx.router.refresh();
  const host = h('div', { class: 'stack' });
  const statuses = [['', 'Todas'], ['Solicitada', 'Solicitadas'], ['Confirmada', 'Confirmadas'], ['Concluida', 'Concluídas'], ['Cancelada', 'Canceladas']];
  const out = h('div', { 'aria-live': 'polite' });
  const render = (status) => mount(out, pagedList(
    data.filter(l => !status || l.status === status).sort((a, b) => new Date(b.inicio) - new Date(a.inicio)),
    (l) => lessonCard(l, { onOpen: () => openLesson(l.id, { reload }), extra: isStudent() && l.status === 'Concluida' ? btn('Avaliar professor', () => rateLesson(l.id, { onDone: reload }), { kind: 'primary' }) : null }), { size: 9, empty: 'Nenhuma aula nesta seleção.', label: 'aulas' }));
  const filter = createForm({ fields: [field('status', 'Mostrar aulas', 'text', { required: false, options: statuses.map(([value, label]) => ({ value, label })) })], submit: 'Filtrar', onSubmit: (v) => render(v.status || '') });
  host.append(h('p', { class: 'muted' }, 'Todos os horários são exibidos no fuso de Brasília.'), data.length ? filter.el : null, out);
  ctx.host.append(pageHeader('Minhas aulas', null, isStudent() ? linkButton('Encontrar um professor', '#app/professores') : null), host);
  if (!data.length) mount(out, emptyState(isStudent() ? 'Você ainda não possui aulas.' : 'Você ainda não recebeu solicitações de aula.', isStudent() ? linkButton('Encontrar um professor', '#app/professores') : linkButton('Completar meu perfil profissional', '#app/profissional', 'secondary')));
  else render('');
}

/* Detalhe de aula. Para responsáveis, usa os dados resumidos da lista e os endpoints /api/responsavel. */
export async function openLesson(idOrSummary, { reload, guardianOf } = {}) {
  const guardian = !!guardianOf;
  const l = guardian ? idOrSummary : await get(`/api/aulas/${idOrSummary}`);
  const id = l.id;
  const subject = subjectName(l);
  const price = Number(l.precoAcordado) / (Number(l.duracaoMinutos) / 60);
  const dlg = openDialog(subject, { size: 'md' });
  const refresh = () => { dlg.close(); reload?.(); };
  dlg.body.append(dl({
    Professor: l.professor?.nome ?? l.professor, Aluno: l.aluno?.nome ?? l.aluno, 'Início (Brasília)': when(l.inicio), Duração: `${l.duracaoMinutos} min`,
    'Valor acordado': money(l.precoAcordado), Situação: statusBadge(l.status), 'Motivo do cancelamento': l.motivoCancelamento }));
  const actions = h('div', { class: 'actions' }); dlg.body.append(actions);
  const add = (...b) => actions.append(...b.filter(Boolean));
  const when_ = (fn) => (e) => fn(e);
  const open = ['Solicitada', 'Confirmada'].includes(l.status);
  const base = guardian ? `/api/responsavel/alunos/${encodeURIComponent(guardianOf)}/aulas/${id}` : `/api/aulas/${id}`;

  const transition = (verb, label) => btn(label, () => confirmDialog(label, `Aula de ${subject} em ${when(l.inicio)}.`, async () => { await post(`/api/aulas/${id}/${verb}`); refresh(); }));
  if (isTeacher() && l.status === 'Solicitada') add(transition('confirmar', 'Confirmar aula'));
  if (isTeacher() && l.status === 'Confirmada') add(transition('concluir', 'Concluir aula'));
  if (open) {
    add(btn('Reagendar', () => formDialog('Reagendar aula', timeFields(price), { inicio: isoToUtcInput(l.inicio), duracaoMinutos: l.duracaoMinutos },
      async (v) => { await put(`${base}/reagendar`, { inicio: utcInputToIso(v.inicio), duracaoMinutos: v.duracaoMinutos }); refresh(); },
      { description: 'Use um intervalo disponível do professor, em UTC. O pagamento já registrado é mantido.', success: 'Aula reagendada.' })));
    add(btn('Cancelar aula', () => formDialog('Cancelar aula', [optional('motivo', 'Motivo')], {},
      async (v) => { await post(`${base}/cancelar`, v); refresh(); },
      { description: 'Confirme o cancelamento desta aula.', save: 'Confirmar cancelamento', danger: true, success: 'Aula cancelada.' }), { kind: 'danger' }));
  }
  if (!guardian) add(btn('Histórico de reagendamentos', async () => {
    const rows = await get(`/api/aulas/${id}/reagendamentos`);
    const d = openDialog('Histórico de reagendamentos');
    d.body.append(rows.length ? h('div', { class: 'stack' }, rows.map(r => card({ title: when(r.createdAtUtc) }, h('p', `${when(r.inicioAnterior)} → ${when(r.inicioNovo)}`)))) : emptyState('Esta aula ainda não foi reagendada.'));
  }));
  if (l.status === 'Concluida' && !guardian) {
    add(btn('Ver acompanhamento', () => showFollowup(id)));
    if (isTeacher()) add(btn('Registrar acompanhamento', () => editFollowup(id)));
    if (isStudent()) {
      add(btn('Avaliar professor', () => rateLesson(id)));
      add(btn('Ver gravação', async () => { const r = await get(`/api/aulas/${id}/gravacao`); (await import('./recordings.js')).showRecording(r); }));
    }
  }
  if (guardian && l.status === 'Concluida') add(btn('Situação da gravação', async () => {
    const r = await get(`${base}/gravacao/resumo`);
    const d = openDialog('Gravação da aula');
    d.body.append(dl({ 'Aula concluída': r.aulaConcluida ? 'Sim' : 'Não', 'Gravação existe': r.existeGravacao ? 'Sim' : 'Não', Disponível: r.gravacaoDisponivel ? 'Sim' : 'Não', Título: r.titulo, 'Disponível desde': when(r.dataDisponibilizacaoUtc) }),
      h('p', { class: 'small muted' }, 'Por privacidade, responsáveis veem apenas a situação da gravação, sem acessar o vídeo.'));
  }));
  if ((isStudent() || guardian) && l.status !== 'Cancelada') add(btn('Pagamento simulado', () => payLesson(l, { guardianOf, reload })));
  if (isTeacher() && l.status !== 'Cancelada') add(btn('Compartilhar material', async () => { const m = await import('./materials.js'); dlg.close(); m.materialForm(null, l, null, reload); }));
  return dlg;
}

// Avaliação do professor (uma por aula concluída; o servidor recusa a segunda).
export function rateLesson(lessonId, { onDone } = {}) {
  return formDialog('Como foi sua aula?', [
    field('nota', 'Nota', 'text', { options: [{ value: '5', label: '★★★★★ 5 – Excelente' }, { value: '4', label: '★★★★ 4 – Muito boa' }, { value: '3', label: '★★★ 3 – Boa' }, { value: '2', label: '★★ 2 – Regular' }, { value: '1', label: '★ 1 – Ruim' }] }),
    optional('comentario', 'Comentário (opcional)')], {},
  async (v) => { await post(`/api/aulas/${lessonId}/avaliacao`, { nota: Number(v.nota), comentario: v.comentario }); onDone?.(); },
  { description: 'Sua avaliação aparece no perfil público do professor e só pode ser enviada uma vez por aula.', save: 'Publicar avaliação', success: 'Avaliação publicada. Obrigado!' });
}

export function payLesson(l, { guardianOf, reload } = {}) {
  const url = guardianOf ? `/api/responsavel/alunos/${encodeURIComponent(guardianOf)}/aulas/${l.id}/pagar` : `/api/financeiro/aulas/${l.id}/pagar`;
  return formDialog('Pagamento de aula', [PAYMENT_FIELD], {}, async (v) => {
    const r = await post(url, v);
    notice(`Pagamento simulado registrado: ${money(r.valorBruto)}.`); reload?.();
  }, { description: `Aula de ${subjectName(l)}: ${money(l.precoAcordado)}. Não há transação bancária nem coleta de dados de cartão.`, save: 'Confirmar pagamento simulado' });
}
