// Visão geral por perfil.
import { h, icon } from '../core/dom.js';
import { get } from '../core/api.js';
import { session, role, isTeacher, isStudent } from '../core/session.js';
import { number } from '../core/format.js';
import { pageHeader, stat, grid, section, pagedList, linkButton, statusBadge, emptyState, card } from '../ui/components.js';
import { lessonCard } from './shared.js';
import { openLesson } from './lessons.js';

// Barra de perguntas frequentes presente na tela inicial de todos os perfis.
export function faqBar(ctx) {
  const input = h('input', { type: 'search', id: 'faq-termo', name: 'termo', autocomplete: 'off', maxlength: '120', placeholder: 'Ex.: como pagar uma aula?' });
  return h('section', { class: 'panel stack-sm', 'aria-labelledby': 'faq-bar-h' }, h('h2', { id: 'faq-bar-h' }, 'Perguntas frequentes'),
    h('form', { class: 'faq-bar', role: 'search', 'aria-label': 'Pesquisar nas perguntas frequentes', onsubmit: (e) => { e.preventDefault(); const t = input.value.trim(); ctx.router.go(t ? `app/ajuda?termo=${encodeURIComponent(t)}` : 'app/ajuda'); } },
      h('div', { class: 'field field-grow' }, h('label', { for: 'faq-termo' }, 'Qual é a sua dúvida?'), input),
      h('button', { type: 'submit', class: 'btn primary' }, icon('search', 18), 'Pesquisar')));
}

const first = () => session.user.nome.split(' ')[0];

export default async function dashboard(ctx) {
  const r = role();
  if (r === 'Responsavel') return guardian(ctx);
  if (r === 'Administrador') return admin(ctx);
  return lessonsOverview(ctx);
}

async function lessonsOverview(ctx) {
  const [lessons, profile] = await Promise.all([get('/api/aulas/minhas', { signal: ctx.signal }), isTeacher() ? get('/api/professores/me/perfil', { signal: ctx.signal }).catch(() => null) : null]);
  const count = (s) => lessons.filter(l => l.status === s).length;
  const next = lessons.filter(l => l.status === 'Confirmada' && new Date(l.inicio) > new Date()).sort((a, b) => new Date(a.inicio) - new Date(b.inicio)).slice(0, 3);
  const reload = () => ctx.router.refresh();
  ctx.host.append(pageHeader(`Bom ter você aqui, ${first()}.`, isStudent() ? 'Organize seus estudos e escolha seu próximo passo.' : 'Acompanhe seus alunos e organize sua rotina de ensino.'),
    profile && profile.status !== 'Aprovado' ? h('div', { class: 'panel stack' }, h('h2', 'Complete seu cadastro profissional'), h('p', null, 'Situação: ', statusBadge(profile.status), ' — você só aparece nas buscas depois de aprovado pela administração.'),
      linkButton('Preparar meu perfil profissional', '#app/profissional')) : null,
    faqBar(ctx),
    section('Suas aulas', grid([stat('Solicitadas', count('Solicitada')), stat('Confirmadas', count('Confirmada')), stat('Concluídas', count('Concluida'))], 'stats')),
    section('Próximas aulas', next.length ? h('div', { class: 'grid cards' }, next.map(l => lessonCard(l, { onOpen: () => openLesson(l.id, { reload }) })))
      : emptyState('Nenhuma aula confirmada pela frente.', isStudent() ? linkButton('Encontrar um professor', '#app/professores', 'secondary') : null)),
    isStudent() ? section('Atalhos', h('div', { class: 'actions' }, linkButton('Perguntar à TutorIA', '#app/tutoria', 'secondary'), linkButton('Ver materiais', '#app/materiais', 'secondary'), linkButton('Planos', '#app/planos', 'secondary')))
      : section('Atalhos', h('div', { class: 'actions' }, linkButton('Minha disponibilidade', '#app/disponibilidade', 'secondary'), linkButton('Meus alunos', '#app/alunos', 'secondary'), linkButton('Financeiro', '#app/financeiro', 'secondary'))));
}

async function guardian(ctx) {
  const rows = await get('/api/responsavel/alunos', { signal: ctx.signal });
  ctx.host.append(pageHeader(`Olá, ${first()}.`, 'Acompanhe aulas, pagamentos e a mentoria dos alunos vinculados à sua conta.'),
    faqBar(ctx),
    section('Alunos vinculados', pagedList(rows, (a) => card({ title: a.nome, subtitle: a.parentesco || 'Aluno vinculado' }, h('div', { class: 'actions' }, linkButton('Acompanhar', `#app/dependente/${a.id}`, 'primary'))),
      { empty: 'Nenhum aluno está vinculado à sua conta ainda. A vinculação é feita pela administração.', label: 'alunos' })),
    section('Atalhos', h('div', { class: 'actions' }, linkButton('Encontrar professores', '#app/professores', 'secondary'), linkButton('Financeiro', '#app/financeiro', 'secondary'), linkButton('Notificações', '#app/notificacoes', 'secondary'))));
}

async function admin(ctx) {
  const [r, pending] = await Promise.all([get('/api/admin/analytics/resumo', { signal: ctx.signal }), get('/api/admin/candidaturas-professor', { signal: ctx.signal }).catch(() => [])]);
  const waiting = pending.filter(c => ['Pendente', 'EmAnalise', 'AguardandoDocumentacao'].includes(c.status)).length;
  ctx.host.append(pageHeader(`Olá, ${first()}.`, 'Resumo da plataforma e pendências de análise.'),
    faqBar(ctx),
    section('Hoje na plataforma', grid([stat('Alunos', number(r.totalAlunos)), stat('Professores aprovados', number(r.totalProfessoresAprovados)), stat('Aulas', number(r.totalAulas)), stat('Candidaturas pendentes', waiting)], 'stats')),
    h('div', { class: 'actions' }, linkButton('Analisar candidaturas', '#app/analise', 'primary'), linkButton('Ver indicadores', '#app/indicadores', 'secondary'), linkButton('Perguntas frequentes', '#app/faq-admin', 'secondary')));
}
