// Peças reutilizadas por várias telas: planos, cartões de professor/aula/avaliação e utilitários.
import { h } from '../core/dom.js';
import { money, when, hhmm, statusLabel, typeLabel } from '../core/format.js';
import { card, badge, stars, avatar, statusBadge, linkButton, link } from '../ui/components.js';
import { session } from '../core/session.js';

export const PLANS = [
  { name: 'Gratuito', price: 0, promo: 'Para começar a explorar.', features: ['Contratação de aulas particulares', 'Materiais do seu professor', 'Sem mensalidade'] },
  { name: 'Foco', price: 29.99, promo: 'R$ 19,99 no primeiro mês elegível.', features: ['Tudo do plano Gratuito', 'TutorIA: até 7 mensagens por dia', 'Apoio para suas dúvidas'] },
  { name: 'Mentoria', price: 79.99, promo: 'R$ 59,99 no primeiro mês elegível.', features: ['TutorIA sem limite diário do plano', 'Até 2 mentores ativos e chat', 'Gravações das próprias aulas elegíveis'] }
];

export const inApp = () => location.hash.startsWith('#app/');
export const teacherHref = (id) => (inApp() ? `#app/professor/${encodeURIComponent(id)}` : `#professores/${encodeURIComponent(id)}`);

export function planCard(plan, { action } = {}) {
  return h('article', { class: `card price-card${plan.name === 'Foco' ? ' featured' : ''}` },
    plan.name === 'Foco' ? h('span', { class: 'badge tone-ok ribbon' }, 'Mais escolhido') : null,
    h('h3', { class: 'card-title' }, `Plano ${plan.name}`),
    h('p', { class: 'price' }, money(plan.price), h('small', ' / mês')),
    h('p', { class: 'promo' }, plan.promo),
    h('ul', null, plan.features.map(f => h('li', f))),
    action ? h('div', { class: 'actions' }, action) : null);
}

export function teacherCard(p) {
  return h('article', { class: 'card teacher-card' },
    h('div', { class: 'card-head' }, avatar(p.nome),
      h('div', null, h('h3', { class: 'card-title' }, h('a', { href: teacherHref(p.id) }, p.nome)), badge(`Reputação: ${p.nivelReputacao}`, 'neutral'))),
    p.mediaAvaliacoes != null && p.quantidadeAvaliacoes > 0
      ? h('p', null, stars(p.mediaAvaliacoes, p.quantidadeAvaliacoes), ' ', h('span', { class: 'small muted' }, `${Number(p.mediaAvaliacoes).toFixed(1).replace('.', ',')} (${p.quantidadeAvaliacoes})`))
      : h('p', { class: 'small muted' }, 'Ainda sem avaliações'),
    h('p', { class: 'small' }, (p.materias || []).join(' · ') || 'Matérias a definir'),
    h('p', { class: 'muted clamp' }, p.biografia || 'Conheça o perfil deste professor.'),
    h('p', null, h('strong', money(p.precoHora)), h('span', { class: 'muted' }, ' / hora'), p.ofereceMentoria ? ' ' : null, p.ofereceMentoria ? badge('Oferece mentoria', 'ok') : null),
    h('div', { class: 'actions' }, linkButton('Ver perfil', teacherHref(p.id), 'secondary')));
}

export function reviewCard(r) {
  return h('article', { class: 'card' }, stars(r.nota),
    h('p', null, r.comentario || 'Avaliação sem comentário.'),
    h('p', { class: 'small muted' }, `${r.aluno || 'Aluno'} · ${when(r.createdAtUtc)}`));
}

export function faqItem(item) {
  return h('details', { class: 'disclosure' }, h('summary', item.pergunta),
    h('div', { class: 'disclosure-body' }, h('p', { style: 'white-space: pre-wrap' }, item.resposta),
      item.rotaAcao && item.textoAcao ? safeAction(item) : null));
}
function safeAction(item) {
  // Rotas de ação vêm do banco: aceitamos apenas fragmentos internos.
  const route = String(item.rotaAcao).replace(/^\/+/, '');
  const hash = route.startsWith('#') ? route : `#${route}`;
  return /^#[\w\-/?=&]*$/.test(hash) ? link(item.textoAcao, hash) : null;
}

export function lessonCard(l, { onOpen, extra } = {}) {
  return card({ title: typeof l.materia === 'string' ? l.materia : l.materia?.nome, subtitle: `${l.professor} · ${l.aluno}` },
    statusBadge(l.status),
    h('p', null, `${when(l.inicio)} (Brasília) · ${l.duracaoMinutos} min`),
    h('p', null, h('strong', money(l.precoAcordado))),
    h('div', { class: 'actions' }, onOpen ? h('button', { type: 'button', class: 'btn secondary', onclick: onOpen }, 'Ver aula e ações') : null, extra));
}

export const subjectOptions = (items, all) => [
  ...(all ? [{ value: '', label: all }] : []), ...items.map(m => ({ value: m.id, label: m.nome }))
];

export function qualificationCard(q) {
  return card({ title: q.titulo, subtitle: `${typeLabel(q.tipo)} · ${q.instituicao || 'Instituição não informada'}` },
    h('p', { class: 'small' }, statusBadge(q.statusVerificacao)), q.descricao ? h('p', q.descricao) : null);
}

// Disponibilidade semanal exibida em UTC e Brasília (a API guarda em UTC).
const SUNDAY = Date.UTC(2026, 10, 8); // domingo de referência
export function weeklySlotText(d) {
  const idx = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'].indexOf(d.diaSemana);
  const at = (t) => new Date(SUNDAY + idx * 86400000 + (Number(t.slice(0, 2)) * 60 + Number(t.slice(3, 5))) * 60000);
  const day = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', timeZone: 'America/Sao_Paulo' }).format(at(d.inicio));
  const time = (t) => new Intl.DateTimeFormat('pt-BR', { timeStyle: 'short', timeZone: 'America/Sao_Paulo' }).format(at(t));
  return `${day.charAt(0).toUpperCase() + day.slice(1)}: ${time(d.inicio)} às ${time(d.fim)} (Brasília) · ${hhmm(d.inicio)}–${hhmm(d.fim)} UTC`;
}
export const isLoggedStudent = () => session.user?.perfil === 'Aluno';
