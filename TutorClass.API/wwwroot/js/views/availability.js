import { h } from '../core/dom.js';
import { get, post, put, del } from '../core/api.js';
import { DAYS, dayLabel, brasiliaAvailability, brasiliaToUtcSlots, timeToMin } from '../core/format.js';
import { pageHeader, card, btn, pagedList } from '../ui/components.js';
import { field } from '../ui/form.js';
import { formDialog, confirmDialog } from '../ui/dialogs.js';

// Os horários são digitados e exibidos em Brasília; a API guarda em UTC. O intervalo que cruza a meia-noite UTC
// (21h de Brasília) é dividido em dois registros e voltam juntos na tela.
const FIELDS = [
  field('diaSemana', 'Dia da semana', 'text', { options: DAYS.map(([value, label]) => ({ value, label })) }),
  field('inicio', 'Início (horário de Brasília)', 'time'), field('fim', 'Fim (horário de Brasília)', 'time')
];
const payload = (row) => ({ diaSemana: row.diaSemana, inicio: row.inicio, fim: row.fim });

// Troca os registros antigos pelos novos; se algo falhar no meio, devolve os antigos para não perder a disponibilidade.
async function saveGroup(group, slots) {
  const old = group ? group.rows : [];
  const created = [];
  try {
    for (const row of old) await del(`/api/professores/me/disponibilidades/${row.id}`);
    for (const slot of slots) created.push(await post('/api/professores/me/disponibilidades', slot));
  } catch (error) {
    for (const row of created) { try { await del(`/api/professores/me/disponibilidades/${row.id}`); } catch { /* melhor esforço */ } }
    for (const row of old) { try { await post('/api/professores/me/disponibilidades', payload(row)); } catch { /* melhor esforço */ } }
    throw error;
  }
}

function edit(group, reload) {
  formDialog(group ? 'Editar disponibilidade' : 'Nova disponibilidade', FIELDS, group ? { diaSemana: group.dia, inicio: group.inicio, fim: group.fim } : {}, async (v) => {
    if (timeToMin(v.inicio) >= timeToMin(v.fim)) throw new Error('O fim deve ser posterior ao início, no mesmo dia.');
    await saveGroup(group, brasiliaToUtcSlots(v.diaSemana, v.inicio, v.fim)); reload();
  }, { description: 'Informe o dia e o horário de Brasília, o mesmo que você vê no relógio. Os alunos só poderão pedir aulas dentro destes intervalos.', success: 'Disponibilidade salva.' });
}

export default async function availability(ctx) {
  const rows = await get('/api/professores/me/disponibilidades', { signal: ctx.signal });
  const groups = brasiliaAvailability(rows);
  const reload = () => ctx.router.refresh();
  ctx.host.append(pageHeader('Disponibilidade', 'Os alunos só conseguem solicitar aulas dentro destes intervalos. Todos os horários são de Brasília.', btn('Adicionar disponibilidade', () => edit(null, reload), { kind: 'primary' })),
    pagedList(groups, (g) => card({ title: dayLabel(g.dia), subtitle: `${g.inicio} às ${g.fim} (Brasília)` },
      h('div', { class: 'actions' }, btn('Editar', () => edit(g, reload)),
        btn('Remover', () => confirmDialog('Remover disponibilidade', 'Este intervalo deixará de receber novas solicitações. As regras de aulas existentes serão verificadas.', async () => { for (const row of g.rows) await del(`/api/professores/me/disponibilidades/${row.id}`); reload(); }, { confirm: 'Remover', danger: true, success: 'Disponibilidade removida.' }), { kind: 'danger' }))),
      { empty: 'Você ainda não cadastrou horários disponíveis.', label: 'horários' }));
}
