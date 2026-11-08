// Responsável: alunos vinculados e, para cada um, aulas, plano, financeiro e mentoria (somente leitura).
import { h } from '../core/dom.js';
import { get, post } from '../core/api.js';
import { money, when, dayOnly } from '../core/format.js';
import { pageHeader, card, dl, btn, linkButton, link, tabs, emptyState, pagedList, dataTable, statusBadge, badge, loading, errorState } from '../ui/components.js';
import { confirmDialog } from '../ui/dialogs.js';
import { notice } from '../ui/toast.js';
import { lessonCard, planCard, PLANS } from './shared.js';
import { openLesson } from './lessons.js';
import { openChat } from './mentoring.js';

export async function listModule(ctx) {
  const rows = await get('/api/responsavel/alunos', { signal: ctx.signal });
  ctx.host.append(pageHeader('Alunos vinculados', 'Você acompanha aulas, pagamentos e a mentoria apenas dos alunos vinculados à sua conta. A vinculação é feita pela administração.'),
    pagedList(rows, (a) => card({ title: a.nome, subtitle: a.parentesco || 'Aluno vinculado' }, h('p', { class: 'small muted' }, `${a.email || ''}${a.dataNascimento ? ` · Nascimento: ${dayOnly(a.dataNascimento)}` : ''}`),
      h('div', { class: 'actions' }, linkButton('Acompanhar', `#app/dependente/${a.id}`, 'primary'))),
      { empty: 'Nenhum aluno está vinculado à sua conta ainda. Peça a vinculação à administração.', label: 'alunos' }));
}

export async function studentModule(ctx) {
  const id = ctx.params.id;
  if (!id) { ctx.router.go('app/vinculados'); return; }
  const students = await get('/api/responsavel/alunos', { signal: ctx.signal });
  const student = students.find(s => s.id === id);
  if (!student) throw new Error('Aluno não encontrado entre os vinculados à sua conta.');
  ctx.setTitle(student.nome);
  const base = `/api/responsavel/alunos/${encodeURIComponent(id)}`;
  const reload = () => ctx.router.refresh();
  const lazy = (fn) => () => {
    const box = h('div', { class: 'stack' }, loading());
    fn().then(node => box.replaceChildren(node)).catch(e => box.replaceChildren(errorState(e.message || 'Não foi possível carregar.')));
    return box;
  };

  const aulas = lazy(async () => {
    const rows = await get(`${base}/aulas`);
    return h('div', { class: 'stack' }, h('div', { class: 'actions' }, linkButton('Escolher professor e solicitar aula', '#app/professores', 'primary')),
      pagedList(rows.sort((a, b) => new Date(b.inicio) - new Date(a.inicio)), (l) => lessonCard(l, { onOpen: () => openLesson(l, { guardianOf: id, reload }) }), { empty: 'Este aluno ainda não possui aulas.', label: 'aulas' }));
  });
  const plano = lazy(async () => {
    const p = await get(`${base}/plano`);
    return h('div', { class: 'stack' }, dl({ 'Plano atual': p.planoAtual, 'Assinatura ativa': p.assinaturaAtiva ? 'Sim' : 'Não', 'IA educacional': p.possuiIaEducacional ? 'Incluída' : 'Não incluída',
      'Limite diário de IA': p.limiteMensagensIaDia ?? 'Sem limite do plano', Mentoria: p.possuiMentoria ? 'Incluída' : 'Não incluída', 'Gravações elegíveis': p.possuiGravacoesElegiveis ? 'Incluídas' : 'Não incluídas' }),
      h('div', { class: 'grid cards' }, PLANS.filter(x => x.price > 0).map(x => planCard(x, { action: btn(p.planoAtual === x.name && p.assinaturaAtiva ? 'Plano atual' : `Assinar ${x.name}`, () => {
        if (p.planoAtual === x.name && p.assinaturaAtiva) return;
        confirmDialog(`Assinar ${x.name} para ${student.nome}`, `Mensalidade de ${money(x.price)}. ${x.promo} Cobrança simulada; os benefícios pertencem ao aluno.`, async () => {
          const r = await post(`${base}/assinatura`, { plano: x.name }); notice(`Plano ${r.plano} ativado. Primeiro mês: ${money(r.valorCobradoPrimeiroMes)}.`); reload();
        }, { confirm: 'Confirmar assinatura simulada', success: null });
      }, { kind: x.name === 'Foco' ? 'primary' : 'secondary' }) }))));
  });
  const financeiro = lazy(async () => {
    const rows = await get(`${base}/financeiro/historico`);
    return dataTable({ caption: `Financeiro de ${student.nome}`, empty: 'Sem lançamentos financeiros.', rows,
      columns: [{ label: 'Data', render: r => when(r.dataUtc) }, { label: 'Descrição', key: 'descricao' }, { label: 'Tipo', key: 'tipo' }, { label: 'Situação', render: r => statusBadge(r.status) }, { label: 'Valor', render: r => money(r.valor) }] });
  });
  const mentoria = lazy(async () => {
    const [links, followups] = await Promise.all([get(`${base}/mentoria`), get(`${base}/mentoria/acompanhamentos`).catch(() => [])]);
    return h('div', { class: 'stack' }, h('p', { class: 'muted' }, 'Por privacidade, você vê os mentores, o histórico do chat (somente leitura) e a situação dos acompanhamentos, sem o conteúdo pedagógico detalhado.'),
      pagedList(links, (m) => card({ title: m.professorNome, subtitle: `Vaga ${m.slot} · Desde ${when(m.dataInicioUtc)}` }, badge(m.ativo ? 'Ativa' : 'Encerrada', m.ativo ? 'ok' : 'neutral'), m.motivoEncerramento ? h('p', { class: 'small' }, m.motivoEncerramento) : null,
        h('div', { class: 'actions' }, btn('Ver conversa (somente leitura)', () => openChat(m, { readOnlyFor: id })))), { empty: 'Nenhum vínculo de mentoria.', label: 'vínculos' }),
      h('h3', 'Acompanhamentos'),
      dataTable({ caption: 'Acompanhamentos de mentoria', empty: 'Nenhum acompanhamento registrado.', rows: followups,
        columns: [{ label: 'Aula', render: a => when(a.dataAulaUtc) }, { label: 'Matéria', key: 'materiaNome' }, { label: 'Professor', key: 'professorNome' }, { label: 'Situação', render: a => statusBadge(a.status) }] }));
  });

  ctx.host.append(link('← Alunos vinculados', '#app/vinculados'), h('header', { class: 'page-header' }, h('div', null, h('h1', { tabindex: '-1' }, student.nome), h('p', { class: 'lead' }, `${student.parentesco || 'Aluno vinculado'} · ${student.email || ''}`))),
    tabs([{ label: 'Aulas', render: aulas }, { label: 'Plano', render: plano }, { label: 'Financeiro', render: financeiro }, { label: 'Mentoria', render: mentoria }], { label: `Informações de ${student.nome}` }));
}
