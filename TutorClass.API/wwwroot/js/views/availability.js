import { h } from '../core/dom.js';
import { get, post, put, del } from '../core/api.js';
import { DAYS, dayLabel, hhmm, timeWithSeconds, brasiliaHint } from '../core/format.js';
import { pageHeader, card, btn, pagedList } from '../ui/components.js';
import { field } from '../ui/form.js';
import { formDialog, confirmDialog } from '../ui/dialogs.js';
import { weeklySlotText } from './shared.js';

const FIELDS = [
  field('diaSemana', 'Dia da semana (em UTC)', 'text', { options: DAYS.map(([value, label]) => ({ value, label })) }),
  field('inicio', 'Início (UTC)', 'time', { liveHint: brasiliaHint }), field('fim', 'Fim (UTC)', 'time', { liveHint: brasiliaHint })
];

function edit(d, reload) {
  formDialog(d ? 'Editar disponibilidade' : 'Nova disponibilidade', FIELDS, d ? { ...d, inicio: hhmm(d.inicio), fim: hhmm(d.fim) } : {}, async (v) => {
    if (v.inicio >= v.fim) throw new Error('O fim deve ser posterior ao início. Evite intervalos que atravessem a meia-noite em UTC.');
    const body = { diaSemana: v.diaSemana, inicio: timeWithSeconds(v.inicio), fim: timeWithSeconds(v.fim) };
    await (d ? put(`/api/professores/me/disponibilidades/${d.id}`, body) : post('/api/professores/me/disponibilidades', body)); reload();
  }, { description: 'Cadastre intervalos em UTC: 15:00 UTC corresponde a 12:00 em Brasília. O dia da semana também é em UTC.', success: 'Disponibilidade salva.' });
}

export default async function availability(ctx) {
  const rows = await get('/api/professores/me/disponibilidades', { signal: ctx.signal });
  const reload = () => ctx.router.refresh();
  ctx.host.append(pageHeader('Disponibilidade', 'Os alunos só conseguem solicitar aulas dentro destes intervalos.', btn('Adicionar disponibilidade', () => edit(null, reload), { kind: 'primary' })),
    pagedList(rows, (d) => card({ title: dayLabel(d.diaSemana), subtitle: `${hhmm(d.inicio)} às ${hhmm(d.fim)} UTC` }, h('p', { class: 'small muted' }, weeklySlotText(d)),
      h('div', { class: 'actions' }, btn('Editar', () => edit(d, reload)),
        btn('Remover', () => confirmDialog('Remover disponibilidade', 'Este intervalo deixará de receber novas solicitações. As regras de aulas existentes serão verificadas.', async () => { await del(`/api/professores/me/disponibilidades/${d.id}`); reload(); }, { confirm: 'Remover', danger: true, success: 'Disponibilidade removida.' }), { kind: 'danger' }))),
      { empty: 'Você ainda não cadastrou horários disponíveis.', label: 'horários' }));
}
