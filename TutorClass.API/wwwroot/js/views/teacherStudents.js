import { h } from '../core/dom.js';
import { get } from '../core/api.js';
import { pageHeader, card, btn, linkButton, emptyState, pagedList } from '../ui/components.js';

export default async function teacherStudents(ctx) {
  const [recipients, mentorias] = await Promise.all([get('/api/materiais/destinatarios', { signal: ctx.signal }), get('/api/mentorias/me', { signal: ctx.signal })]);
  const pupils = new Map();
  for (const r of recipients) { if (!pupils.has(r.alunoId)) pupils.set(r.alunoId, { alunoId: r.alunoId, alunoNome: r.alunoNome, materias: [] }); pupils.get(r.alunoId).materias.push(r.materiaNome); }
  for (const m of mentorias.filter(m => m.ativo)) { if (!pupils.has(m.alunoId)) pupils.set(m.alunoId, { alunoId: m.alunoId, alunoNome: m.alunoNome, materias: [] }); pupils.get(m.alunoId).mentoria = m; }
  ctx.host.append(pageHeader('Meus alunos', 'Seus alunos aparecem quando contratam uma aula ou selecionam você como mentor.'),
    pagedList([...pupils.values()], (p) => card({ title: p.alunoNome, subtitle: [...new Set(p.materias)].join(' · ') || 'Mentoria' },
      h('div', { class: 'actions' },
        p.materias.length ? btn('Enviar atividade', async () => (await import('./materials.js')).materialForm(null, null, p, ctx.reload)) : null,
        p.mentoria ? btn('Abrir conversa', async () => (await import('./mentoring.js')).openChat(p.mentoria)) : null),
      p.mentoria ? null : h('p', { class: 'small muted' }, 'Chat disponível quando o aluno escolher você como mentor no plano Mentoria.')),
      { empty: 'Nenhum aluno vinculado ainda. Complete seu perfil, aguarde a aprovação e cadastre horários para receber aulas.', label: 'alunos' }),
    linkButton('Ver perfil e situação da aprovação', '#app/profissional', 'ghost'));
}
