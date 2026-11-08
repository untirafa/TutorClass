import { h } from '../core/dom.js';
import { get } from '../core/api.js';
import { money, number } from '../core/format.js';
import { pageHeader, stat, dataTable, section, grid } from '../ui/components.js';

const pct = (n) => `${Number(n ?? 0).toFixed(1).replace('.', ',')}%`;
export default async function adminAnalytics(ctx) {
  const [r, ranking, demand] = await Promise.all([get('/api/admin/analytics/resumo', { signal: ctx.signal }), get('/api/admin/analytics/materias', { signal: ctx.signal }), get('/api/admin/analytics/demanda-materias', { signal: ctx.signal })]);
  ctx.host.append(pageHeader('Indicadores', 'Visão geral da plataforma. Valores financeiros são simulados.'),
    section('Resumo', grid([stat('Alunos', number(r.totalAlunos)), stat('Professores aprovados', number(r.totalProfessoresAprovados)), stat('Aulas', number(r.totalAulas)), stat('Aulas concluídas', number(r.aulasConcluidas)),
      stat('Aulas canceladas', number(r.aulasCanceladas)), stat('Mentorias ativas', number(r.mentoriasAtivas)), stat('Média das avaliações', Number(r.mediaAvaliacoes ?? 0).toFixed(1).replace('.', ','))], 'stats'),
      h('p', null, `Matéria mais procurada: ${r.materiaMaisProcurada || '—'} · Professor mais contratado: ${r.professorMaisContratado || '—'}`)),
    section('Financeiro simulado', grid([stat('Comissão TutorClass', money(r.receitaComissaoSimulada)), stat('Repasses a professores', money(r.repassesProfessoresSimulados)), stat('Adicionais de mentoria', money(r.adicionaisMentoriaSimulados))], 'stats')),
    section('Assinaturas por plano', dataTable({ caption: 'Assinaturas ativas por plano', rows: r.assinaturasPorPlano || [], columns: [{ label: 'Plano', key: 'plano' }, { label: 'Quantidade', render: x => number(x.quantidade) }], empty: 'Sem assinaturas.' })),
    section('Ranking de matérias', dataTable({ caption: 'Aulas por matéria', rows: ranking, empty: 'Sem dados de aulas.', columns: [{ label: 'Matéria', key: 'nome' }, { label: 'Aulas', render: x => number(x.quantidadeAulas) },
      { label: 'Participação', render: x => h('span', null, pct(x.percentual), ' ', h('span', { class: 'meter', 'aria-hidden': 'true' }, h('span', { style: `width:${Math.min(100, x.percentual)}%` }))) }, { label: 'Tendência', key: 'tendencia' }] })),
    section('Demanda por matéria (30 dias)', dataTable({ caption: 'Demanda recente por matéria', rows: demand, empty: 'Sem dados de demanda.', columns: [{ label: 'Matéria', key: 'nome' }, { label: 'Últimos 30 dias', render: x => number(x.aulasUltimos30Dias) },
      { label: '30 dias anteriores', render: x => number(x.aulas30DiasAnteriores) }, { label: 'Crescimento', render: x => pct(x.crescimentoPercentual) }, { label: 'Alunos distintos', render: x => number(x.quantidadeAlunosDistintos) }, { label: 'Conclusão', render: x => pct(x.taxaConclusao) }, { label: 'Previsão', key: 'demandaPrevista' }] })));
}
