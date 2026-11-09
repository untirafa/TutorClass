import { h, mount, icon } from '../core/dom.js';
import { publicGet, subjects, errorText, isAbort } from '../core/api.js';
import { isSignedIn } from '../core/session.js';
import { teacherCard, reviewCard, faqItem, planCard, PLANS } from './shared.js';
import { loading, emptyState, linkButton, link } from '../ui/components.js';
import { createForm, field } from '../ui/form.js';
import { appBand } from '../ui/appDownload.js';

const settled = (r, fallback) => (r.status === 'fulfilled' ? r.value : fallback);

export default async function home(ctx) {
  const results = await Promise.allSettled([
    subjects(), publicGet('/api/professores', { signal: ctx.signal }), publicGet('/api/faq', { signal: ctx.signal })]);
  if (!ctx.isCurrent()) return;
  const subs = settled(results[0], []), teachers = settled(results[1], null), faq = settled(results[2], null);
  const featured = teachers ? [...teachers].sort((a, b) => (b.mediaAvaliacoes ?? 0) - (a.mediaAvaliacoes ?? 0)).slice(0, 3) : null;

  const search = createForm({
    fields: [field('materia', 'Qual matéria você quer estudar?', 'text', { required: false, options: [{ value: '', label: 'Todas as matérias' }, ...subs.map(m => ({ value: m.id, label: m.nome }))] })],
    submit: 'Buscar professores',
    onSubmit: (v) => { location.hash = `professores${v.materia ? `?materia=${encodeURIComponent(v.materia)}` : ''}`; }
  });
  search.el.classList.add('hero-search');

  const hero = h('section', { class: 'hero', 'aria-labelledby': 'hero-title' }, h('div', { class: 'container hero-inner' },
    h('div', null,
      h('span', { class: 'eyebrow' }, 'Aprenda com quem entende'),
      h('h1', { id: 'hero-title', tabindex: '-1' }, 'Aprender melhor. Ir mais longe.'),
      h('p', { class: 'lead' }, 'Encontre professores, organize suas aulas e acompanhe seu progresso em um só lugar — com preço claro e uma plataforma pensada para todos.'),
      search.el,
      subs.length ? h('ul', { class: 'chips', 'aria-label': 'Matérias disponíveis' }, subs.slice(0, 8).map(m => h('li', null, h('a', { class: 'chip', href: `#professores?materia=${encodeURIComponent(m.id)}` }, m.nome)))) : null)));

  const how = h('section', { class: 'container section', 'aria-labelledby': 'how-title' },
    h('h2', { id: 'how-title', tabindex: '-1' }, 'Como funciona'),
    h('ol', { class: 'steps' },
      h('li', null, h('h3', 'Busque'), h('p', 'Escolha a matéria, compare avaliações e veja o valor por hora de cada professor.')),
      h('li', null, h('h3', 'Agende'), h('p', 'Consulte horários livres e solicite sua aula. O professor confirma e você acompanha tudo na sua área.')),
      h('li', null, h('h3', 'Evolua'), h('p', 'Receba materiais, acompanhamento depois da aula e, nos planos pagos, TutorIA e mentoria.'))));

  const features = h('section', { class: 'container section', 'aria-labelledby': 'feat-title' },
    h('h2', { id: 'feat-title', tabindex: '-1' }, 'Apoio para cada etapa da jornada'),
    h('div', { class: 'grid cards' },
      h('article', { class: 'card' }, icon('calendar', 28), h('h3', { class: 'card-title' }, 'Aulas particulares'), h('p', 'Encontre professores por matéria e escolha um horário disponível para aprender com atenção individual.'), link('Encontre seu professor →', '#professores')),
      h('article', { class: 'card' }, icon('chat', 28), h('h3', { class: 'card-title' }, 'Mentoria que acompanha'), h('p', 'Orientação contínua, metas e um canal de conversa com seus mentores no plano Mentoria.'), link('Conheça os planos →', '#planos')),
      h('article', { class: 'card' }, icon('spark', 28), h('h3', { class: 'card-title' }, 'Mais recursos para estudar'), h('p', 'Materiais do seu professor e a TutorIA para tirar dúvidas, conforme o seu plano.'), link('Comece sua jornada →', '#cadastro-aluno'))));

  const about = h('section', { class: 'container section', id: 'sobre', 'aria-labelledby': 'about-title' },
    h('h2', { id: 'about-title', tabindex: '-1' }, 'Conectamos quem quer aprender a quem ama ensinar'),
    h('div', { class: 'prose stack' },
      h('p', 'O TutorClass aproxima alunos e professores particulares. Reunimos a busca pelo profissional, o agendamento e o acompanhamento da aprendizagem em um só lugar.'),
      h('p', 'Valorizamos a troca, a clareza e o desenvolvimento contínuo. Por isso o site foi construído para ser usado por qualquer pessoa: com teclado, leitor de tela, zoom e em celulares simples.'),
      link('Quero fazer parte como professor →', '#cadastro-professor')));

  const featuredSection = h('section', { class: 'container section stack', 'aria-labelledby': 'feat-teachers' },
    h('h2', { id: 'feat-teachers', tabindex: '-1' }, 'Professores em destaque'),
    featured === null ? h('p', { class: 'muted', role: 'status' }, 'Não foi possível carregar os professores agora. Tente novamente em instantes.')
      : featured.length ? h('div', { class: 'grid cards' }, featured.map(teacherCard)) : emptyState('Ainda não há professores aprovados.'),
    linkButton('Ver todos os professores', '#professores', 'secondary'));

  const reviewsHost = h('div', { class: 'stack' }, loading('Carregando avaliações…'));
  const reviewsSection = h('section', { class: 'container section stack', 'aria-labelledby': 'reviews-title' },
    h('h2', { id: 'reviews-title', tabindex: '-1' }, 'Quem aprende também tem voz'), h('p', { class: 'muted' }, 'Avaliações publicadas por alunos depois de aulas concluídas.'), reviewsHost);

  const plans = h('section', { class: 'container section stack', 'aria-labelledby': 'plans-title' },
    h('h2', { id: 'plans-title', tabindex: '-1' }, 'Um plano para o seu momento'),
    h('p', { class: 'muted' }, 'As aulas são contratadas separadamente, pelo preço de cada professor.'),
    h('div', { class: 'grid cards' }, PLANS.map(p => planCard(p, { action: linkButton(p.price ? `Quero o ${p.name}` : 'Começar agora', '#cadastro-aluno', p.name === 'Foco' ? 'primary' : 'secondary') }))),
    h('p', { class: 'small muted' }, 'Versão acadêmica: pagamentos e gravações são simulados. Acesso aos benefícios sujeito às regras de cada plano.'));

  const cta = h('section', { class: 'container section' }, h('div', { class: 'band' },
    h('div', null, h('h2', 'Compartilhe o que sabe'), h('p', 'Crie seu perfil de professor e conecte-se a novos alunos.')),
    isSignedIn() ? null : linkButton('Quero ensinar', '#cadastro-professor')));

  const faqSection = h('section', { class: 'container section stack', 'aria-labelledby': 'faq-title' },
    h('h2', { id: 'faq-title', tabindex: '-1' }, 'Dúvidas comuns'),
    faq === null ? h('p', { class: 'muted' }, 'Não foi possível carregar as perguntas agora.')
      : faq.length ? h('div', { class: 'stack-sm' }, faq.slice(0, 4).map(faqItem)) : emptyState('Nenhuma pergunta cadastrada ainda.'),
    link('Ver a central de ajuda →', '#ajuda'));

  mount(ctx.host, hero, how, features, about, featuredSection, reviewsSection, plans, appBand(), cta, faqSection);

  // Avaliações recentes: agregadas dos professores mais bem avaliados.
  try {
    const ids = (teachers || []).filter(p => p.quantidadeAvaliacoes > 0).slice(0, 6);
    const lists = await Promise.allSettled(ids.map(p => publicGet(`/api/professores/${p.id}/avaliacoes`, { signal: ctx.signal })));
    if (!ctx.isCurrent()) return;
    const rows = lists.flatMap(r => (r.status === 'fulfilled' ? r.value : [])).sort((a, b) => new Date(b.createdAtUtc) - new Date(a.createdAtUtc)).slice(0, 3);
    mount(reviewsHost, rows.length ? h('div', { class: 'grid cards' }, rows.map(reviewCard)) : emptyState('As avaliações aparecerão aqui depois das primeiras aulas avaliadas.'));
  } catch (e) { if (!isAbort(e)) mount(reviewsHost, emptyState('Não foi possível carregar as avaliações agora.')); }
}
