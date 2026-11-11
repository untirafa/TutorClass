// Moderação (administração): fila de denúncias, decisão com sanção e reativação de conta suspensa.
import { h } from '../core/dom.js';
import { get, post, q } from '../core/api.js';
import { when } from '../core/format.js';
import { pageHeader, card, badge, btn, dl, pagedList, stat, statusBadge } from '../ui/components.js';
import { field, createForm } from '../ui/form.js';
import { formDialog, confirmDialog } from '../ui/dialogs.js';
import { REASON_LABEL, TARGET_LABEL } from '../ui/report.js';

const SANCTION = { Nenhuma: 'Nenhuma', Advertencia: 'Advertência', Suspensao: 'Suspensão da conta' };
const FILTERS = [{ value: '', label: 'Todas' }, { value: 'Aberta', label: 'Abertas' }, { value: 'EmAnalise', label: 'Em análise' }, { value: 'Procedente', label: 'Procedentes' }, { value: 'Improcedente', label: 'Improcedentes' }];
let filterValue = '';

export default async function adminReports(ctx) {
  const [summary, rows] = await Promise.all([
    get('/api/admin/denuncias/resumo', { signal: ctx.signal }),
    get(`/api/admin/denuncias${q({ status: filterValue, pageSize: 100 })}`, { signal: ctx.signal })
  ]);
  const reload = () => ctx.router.refresh();

  const decide = (d) => formDialog('Decidir denúncia', [
    field('decisao', 'Decisão', 'text', { options: [{ value: 'Procedente', label: 'Procedente (houve violação)' }, { value: 'Improcedente', label: 'Improcedente (sem violação)' }] }),
    field('sancao', 'Sanção (só vale se procedente)', 'text', { options: Object.entries(SANCTION).map(([value, label]) => ({ value, label })),
      help: 'A suspensão bloqueia o acesso da conta imediatamente, até você reativá-la.' }),
    field('observacao', 'Justificativa da decisão', 'textarea', { minLength: 3, maxLength: 500, rows: 3, help: 'Fica registrada internamente. Quem denunciou não vê esse texto.' })
  ], {}, async (v) => {
    const procedente = v.decisao === 'Procedente';
    await post(`/api/admin/denuncias/${d.id}/decidir`, { procedente, sancao: procedente ? v.sancao : 'Nenhuma', observacao: v.observacao });
    reload();
  }, { description: `Denúncia contra ${d.alvoUsuario} (${d.alvoPerfil}).`, save: 'Registrar decisão', success: 'Decisão registrada.' });

  const filter = createForm({
    fields: [field('status', 'Mostrar', 'text', { options: FILTERS })], values: { status: filterValue }, submit: 'Filtrar',
    onSubmit: async (v) => { filterValue = v.status || ''; reload(); }
  });

  ctx.host.append(pageHeader('Moderação de denúncias', 'Analise denúncias de perfis, avaliações e mensagens, decida e aplique sanções.'),
    h('div', { class: 'grid stats' }, stat('Abertas', summary.abertas), stat('Em análise', summary.emAnalise), stat('Procedentes', summary.procedentes), stat('Improcedentes', summary.improcedentes)),
    filter.el,
    pagedList(rows, (d) => card({ title: `${TARGET_LABEL[d.tipo] || d.tipo} · ${REASON_LABEL[d.motivo] || d.motivo}`, subtitle: d.resumo },
      h('p', null, statusBadge(d.status), ' ', d.sancao !== 'Nenhuma' ? badge(`Sanção: ${SANCTION[d.sancao]}`, 'bad') : null, ' ',
        !d.alvoAtivo ? badge('Conta suspensa', 'bad') : null),
      dl({
        Denunciante: d.denunciante, Denunciado: `${d.alvoUsuario} (${d.alvoPerfil})`, 'Denúncias contra esta pessoa': d.denunciasContraAlvo,
        Detalhes: d.descricao, Enviada: when(d.criadaEmUtc), Decidida: d.analisadaEmUtc ? when(d.analisadaEmUtc) : null,
        Justificativa: d.observacao, 'Conta reativada': d.contaReativadaEmUtc ? when(d.contaReativadaEmUtc) : null
      }),
      h('div', { class: 'actions' },
        d.status === 'Aberta' ? btn('Iniciar análise', async () => { await post(`/api/admin/denuncias/${d.id}/iniciar-analise`); reload(); }) : null,
        d.status === 'Aberta' || d.status === 'EmAnalise' ? btn('Decidir', () => decide(d), { kind: 'primary' }) : null,
        d.status === 'Procedente' && d.sancao === 'Suspensao' && !d.contaReativadaEmUtc
          ? btn('Reativar conta', () => confirmDialog('Reativar conta?', `${d.alvoUsuario} voltará a acessar o TutorClass imediatamente.`,
            async () => { await post(`/api/admin/denuncias/${d.id}/reativar-conta`); reload(); }, { confirm: 'Reativar conta', success: 'Conta reativada.' })) : null)),
    { empty: 'Nenhuma denúncia encontrada.', label: 'denúncias' }));
}
