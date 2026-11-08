// Acompanhamento pedagógico: de aula (professor registra, aluno consulta) e de mentoria.
import { h } from '../core/dom.js';
import { get, post, put, errorText } from '../core/api.js';
import { isTeacher, isStudent } from '../core/session.js';
import { when } from '../core/format.js';
import { pageHeader, dl, card, badge, statusBadge, btn, emptyState, errorState, pagedList } from '../ui/components.js';
import { field, optional } from '../ui/form.js';
import { openDialog } from '../ui/dialog.js';
import { formDialog } from '../ui/dialogs.js';
import { notice } from '../ui/toast.js';
import { lessonCard } from './shared.js';

const SUMMARY = [field('resumo', 'Resumo', 'textarea'), optional('pontosFortes', 'Pontos fortes'), optional('pontosMelhorar', 'Pontos a melhorar'), optional('proximosPassos', 'Próximos passos')];
const MENTORING = [...SUMMARY, optional('metaAtual', 'Meta atual'), optional('observacoes', 'Observações')];

export async function showFollowup(id) {
  const a = await get(`/api/aulas/${id}/acompanhamento`);
  const d = openDialog('Acompanhamento da aula');
  d.body.append(a ? dl({ Resumo: a.resumo, 'Pontos fortes': a.pontosFortes, 'Pontos a melhorar': a.pontosMelhorar, 'Próximos passos': a.proximosPassos }) : emptyState('Ainda não há acompanhamento para esta aula.'));
}
export async function editFollowup(id) {
  let a = {};
  try { a = (await get(`/api/aulas/${id}/acompanhamento`)) || {}; } catch (e) { if (e.status !== 404) throw e; }
  formDialog('Acompanhamento da aula', SUMMARY, a, async (v) => { await put(`/api/aulas/${id}/acompanhamento`, v); }, { success: 'Acompanhamento salvo.' });
}
function editMentoring(a, complete, reload) {
  formDialog(complete ? 'Concluir acompanhamento de mentoria' : 'Editar acompanhamento de mentoria', MENTORING, a, async (v) => {
    const r = await (complete ? post(`/api/mentorias/acompanhamentos/${a.id}/concluir`, v) : put(`/api/mentorias/acompanhamentos/${a.id}`, v));
    notice(complete ? r.mensagem : 'Acompanhamento salvo.'); reload();
  }, { description: complete ? 'Após concluir, o adicional simulado será calculado pelo sistema conforme a elegibilidade.' : 'Registre o desenvolvimento do aluno.', save: complete ? 'Concluir acompanhamento' : 'Salvar', success: null });
}

export default async function followups(ctx) {
  const reload = () => ctx.router.refresh();
  const results = await Promise.allSettled([get('/api/aulas/minhas'), get('/api/mentorias/acompanhamentos/pendentes'), get('/api/mentorias/acompanhamentos/historico')]);
  ctx.host.append(pageHeader('Acompanhamentos', isTeacher() ? 'Registre o desenvolvimento dos seus alunos depois das aulas e da mentoria.' : 'Veja o retorno dos professores sobre a sua evolução.'));

  const lessonsDone = results[0].status === 'fulfilled' ? results[0].value.filter(a => a.status === 'Concluida') : null;
  ctx.host.append(h('section', { class: 'stack', 'aria-labelledby': 'fu-l' }, h('h2', { id: 'fu-l' }, 'Acompanhamento de aulas'),
    lessonsDone === null ? errorState(results[0].reason.message)
      : pagedList(lessonsDone, (l) => lessonCard(l, { extra: [btn('Consultar acompanhamento', () => showFollowup(l.id)), isTeacher() ? btn('Preencher acompanhamento', () => editFollowup(l.id)) : null] }),
        { empty: 'Nenhuma aula concluída para acompanhar.', label: 'aulas' })));

  const data = results.slice(1).flatMap(r => (r.status === 'fulfilled' ? r.value : []));
  const unique = [...new Map(data.map(a => [a.id, a])).values()];
  ctx.host.append(h('section', { class: 'stack', 'aria-labelledby': 'fu-m' }, h('h2', { id: 'fu-m' }, 'Acompanhamento de mentoria'),
    pagedList(unique, (a) => card({ title: `${a.materiaNome} · ${isStudent() ? a.professorNome : a.alunoNome}`, subtitle: a.resumo || 'Acompanhamento aguardando preenchimento.' },
      statusBadge(a.status), h('p', { class: 'small muted' }, when(a.dataAulaUtc)),
      h('div', { class: 'actions' }, btn('Consultar', () => { const d = openDialog('Acompanhamento de mentoria'); d.body.append(dl({ Resumo: a.resumo, 'Pontos fortes': a.pontosFortes, 'Pontos a melhorar': a.pontosMelhorar, 'Próximos passos': a.proximosPassos, 'Meta atual': a.metaAtual, Observações: a.observacoes, Situação: statusBadge(a.status) })); }),
        isTeacher() && a.status === 'Pendente' ? [btn('Editar', () => editMentoring(a, false, reload)), btn('Preencher e concluir', () => editMentoring(a, true, reload), { kind: 'primary' })] : null)),
      { empty: 'Nenhum acompanhamento de mentoria encontrado.', label: 'acompanhamentos' })));
  results.slice(1).filter(r => r.status === 'rejected' && r.reason?.name !== 'AbortError').forEach(r => ctx.host.append(errorState(r.reason.message)));
}
