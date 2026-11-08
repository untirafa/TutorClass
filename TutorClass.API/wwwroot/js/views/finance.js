// Financeiro simulado: aluno (histórico, pagar aula, mensalidade) e professor (resumo mensal e comissão).
import { h, mount } from '../core/dom.js';
import { get, post, q } from '../core/api.js';
import { isStudent, isGuardian } from '../core/session.js';
import { money, when, paymentLabel, statusLabel } from '../core/format.js';
import { pageHeader, dl, btn, statusBadge, dataTable, emptyState, loading } from '../ui/components.js';
import { field, createForm } from '../ui/form.js';
import { formDialog } from '../ui/dialogs.js';
import { openDialog } from '../ui/dialog.js';
import { notice } from '../ui/toast.js';
import { PAYMENT_FIELD, payLesson } from './lessons.js';

const NOTE = 'Financeiro simulado: nenhum valor é transferido e nenhum dado de cartão é solicitado.';

export default async function finance(ctx) {
  ctx.host.append(pageHeader('Financeiro'), h('p', { class: 'state state-empty' }, NOTE));
  if (isStudent()) await student(ctx); else if (isGuardian()) await guardian(ctx); else await teacher(ctx);
}

async function student(ctx) {
  const rows = await get('/api/financeiro/me/historico', { signal: ctx.signal });
  const reload = () => ctx.router.refresh();
  const payAny = btn('Pagar uma aula', async () => {
    const lessons = (await get('/api/aulas/minhas')).filter(a => a.status !== 'Cancelada');
    if (!lessons.length) throw new Error('Nenhuma aula disponível para pagamento.');
    formDialog('Escolher aula', [field('aulaId', 'Aula', 'text', { options: lessons.map(l => ({ value: l.id, label: `${l.materia} · ${when(l.inicio)} · ${money(l.precoAcordado)}` })) })], {},
      (v) => { payLesson(lessons.find(l => l.id === v.aulaId), { reload }); }, { save: 'Continuar', keepOpen: true });
  }, { kind: 'primary' });
  const subs = rows.filter(r => r.tipo === 'Assinatura');
  const monthly = btn('Registrar mensalidade', () => {
    if (!subs.length) throw new Error('Você ainda não possui uma assinatura paga.');
    const now = new Date();
    formDialog('Mensalidade simulada', [
      field('assinaturaId', 'Assinatura', 'text', { options: subs.map(s => ({ value: s.id, label: `${s.descricao} · ${when(s.dataUtc)} · ${statusLabel(s.status)}` })) }),
      field('anoCompetencia', 'Ano', 'number', { min: 2020, max: 2100, step: 1, value: now.getFullYear() }),
      field('mesCompetencia', 'Mês', 'number', { min: 1, max: 12, step: 1, value: now.getMonth() + 1 }), PAYMENT_FIELD], {},
    async (v) => { const { assinaturaId, ...body } = v; const r = await post(`/api/financeiro/assinaturas/${assinaturaId}/cobrancas`, body); notice(`Mensalidade simulada registrada: ${money(r.valor)}.`); reload(); },
    { description: 'Registre a competência da assinatura. O sistema valida duplicidades e calcula o valor.', save: 'Confirmar mensalidade simulada', success: null });
  });
  ctx.host.append(h('div', { class: 'actions' }, payAny, monthly),
    h('section', { class: 'stack', 'aria-labelledby': 'fin-h' }, h('h2', { id: 'fin-h' }, 'Histórico'),
      dataTable({ caption: 'Lançamentos financeiros', empty: 'Seu histórico financeiro está vazio.', rows,
        columns: [{ label: 'Data', render: r => when(r.dataUtc) }, { label: 'Descrição', key: 'descricao' }, { label: 'Tipo', key: 'tipo' }, { label: 'Situação', render: r => statusBadge(r.status) }, { label: 'Valor', render: r => money(r.valor) }] })));
}

// Responsável: histórico e pagamento de aulas de cada aluno vinculado (simulado).
async function guardian(ctx) {
  const students = await get('/api/responsavel/alunos', { signal: ctx.signal });
  if (!students.length) { ctx.host.append(emptyState('Nenhum aluno está vinculado à sua conta ainda. A vinculação é feita pela administração.')); return; }
  const reload = () => ctx.router.refresh();
  for (const st of students) {
    const base = `/api/responsavel/alunos/${encodeURIComponent(st.id)}`;
    const [rows, lessons] = await Promise.all([get(`${base}/financeiro/historico`, { signal: ctx.signal }), get(`${base}/aulas`, { signal: ctx.signal }).catch(() => [])]);
    const payable = lessons.filter(a => a.status !== 'Cancelada');
    const pay = btn('Pagar uma aula', () => {
      if (!payable.length) throw new Error('Nenhuma aula disponível para pagamento.');
      formDialog(`Escolher aula de ${st.nome}`, [field('aulaId', 'Aula', 'text', { options: payable.map(l => ({ value: l.id, label: `${l.materia} · ${when(l.inicio)} · ${money(l.precoAcordado)}` })) })], {},
        (v) => { payLesson(payable.find(l => l.id === v.aulaId), { guardianOf: st.id, reload }); }, { save: 'Continuar', keepOpen: true });
    }, { kind: 'primary' });
    ctx.host.append(h('section', { class: 'stack', 'aria-labelledby': `fin-${st.id}` }, h('h2', { id: `fin-${st.id}` }, st.nome), h('div', { class: 'actions' }, pay),
      dataTable({ caption: `Lançamentos financeiros de ${st.nome}`, empty: 'Sem lançamentos financeiros.', rows,
        columns: [{ label: 'Data', render: r => when(r.dataUtc) }, { label: 'Descrição', key: 'descricao' }, { label: 'Tipo', key: 'tipo' }, { label: 'Situação', render: r => statusBadge(r.status) }, { label: 'Valor', render: r => money(r.valor) }] })));
  }
}

async function teacher(ctx) {
  const now = new Date();
  const area = h('div', { class: 'stack', 'aria-live': 'polite' });
  const load = async (v) => {
    mount(area, loading());
    const r = await get(`/api/financeiro/professores/me/resumo${q({ ano: v.ano, mes: v.mes })}`, { signal: ctx.signal });
    mount(area, dl({ 'Aulas (bruto)': money(r.valorBrutoAulas), 'Comissão TutorClass': money(r.comissoesTutorClass), 'Aulas (líquido)': money(r.valorLiquidoAulas), 'Adicionais de mentoria': money(r.adicionaisMentoria), 'Total previsto': money(r.totalPrevisto) }),
      dataTable({ caption: `Lançamentos de ${String(r.mes).padStart(2, '0')}/${r.ano}`, empty: 'Nenhum lançamento neste período.', rows: r.itens,
        columns: [{ label: 'Aluno', key: 'alunoNome' }, { label: 'Tipo', key: 'tipo' }, { label: 'Data', render: i => when(i.dataUtc) }, { label: 'Bruto', render: i => money(i.valorBruto) }, { label: 'Comissão', render: i => money(i.valorComissao) },
          { label: 'Líquido', render: i => money(i.valorLiquidoProfessor) }, { label: 'Adicional', render: i => money(i.valorAdicionalMentoria) }, { label: 'Situação', render: i => statusBadge(i.status) }] }));
  };
  const form = createForm({ fields: [field('ano', 'Ano', 'number', { min: 2020, max: 2100, value: now.getFullYear() }), field('mes', 'Mês', 'number', { min: 1, max: 12, value: now.getMonth() + 1 })], submit: 'Consultar período', onSubmit: load, className: 'inline-form' });
  const calc = btn('Calcular comissão de uma aula', () => {
    const { dlg, form: f } = formDialog('Demonstrativo de comissão', [field('valorBruto', 'Valor bruto (R$)', 'number', { min: 0.01, step: 0.01, inputMode: 'decimal' })], {}, async (v) => {
      const r = await get(`/api/financeiro/aulas/comissao${q({ valorBruto: v.valorBruto })}`);
      out.replaceChildren(dl({ Bruto: money(r.valorBruto), 'Comissão': money(r.valorComissao), 'Líquido previsto': money(r.valorLiquidoProfessor) })); announce2(out);
    }, { save: 'Calcular', keepOpen: true });
    const out = h('div', { 'aria-live': 'polite' }); dlg.body.append(out);
    const announce2 = (n) => n.setAttribute('aria-live', 'polite');
  });
  ctx.host.append(form.el, area, h('div', { class: 'actions' }, calc));
  await load({ ano: now.getFullYear(), mes: now.getMonth() + 1 });
}
