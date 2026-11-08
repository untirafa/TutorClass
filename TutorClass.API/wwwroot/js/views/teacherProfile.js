// Perfil profissional do professor: dados públicos, matérias, qualificações, mentoria e candidatura.
import { h } from '../core/dom.js';
import { get, put, post, subjects } from '../core/api.js';
import { session } from '../core/session.js';
import { money, when, statusLabel } from '../core/format.js';
import { pageHeader, dl, btn, badge, statusBadge, card, emptyState, linkButton, section } from '../ui/components.js';
import { field, optional } from '../ui/form.js';
import { formDialog } from '../ui/dialogs.js';
import { qualificationCard, subjectOptions } from './shared.js';
import { mentoringSettings } from './mentoring.js';

const QUALIFICATION_FIELDS = [
  field('tipo', 'Tipo', 'text', { options: [{ value: 'Graduacao', label: 'Graduação' }, { value: 'PosGraduacao', label: 'Pós-graduação' }, 'Curso', { value: 'Certificacao', label: 'Certificação' }, { value: 'Experiencia', label: 'Experiência' }, 'Outro'] }),
  field('titulo', 'Título', 'text', { maxLength: 200 }), optional('instituicao', 'Instituição', 'text'), optional('descricao', 'Descrição'),
  optional('dataConclusao', 'Data de conclusão', 'date'), optional('documentoComprovacao', 'Referência do documento comprobatório', 'text', { help: 'Informe o nome ou link do documento; a administração fará a verificação.' })
];

export default async function teacherProfile(ctx) {
  const uid = encodeURIComponent(session.user.id);
  const [p, candidaturas] = await Promise.all([get('/api/professores/me/perfil', { signal: ctx.signal }), get('/api/professores/me/candidaturas', { signal: ctx.signal }).catch(() => [])]);
  const reload = () => ctx.router.refresh();
  const last = candidaturas[0];
  const checklist = [
    ['Dados do perfil (apresentação e preço)', !!p.biografia && p.precoHora > 0], ['Ao menos uma matéria vinculada', (p.materias || []).length > 0],
    ['Ao menos uma qualificação enviada', (p.qualificacoes || []).length > 0], ['Horários de disponibilidade cadastrados', (p.disponibilidades || []).length > 0],
    ['Candidatura enviada para análise', candidaturas.length > 0]];

  ctx.host.append(pageHeader('Perfil profissional', 'Seu cadastro inicia como candidato. A aprovação e a verificação das qualificações são feitas pela administração.',
    linkButton('Ver meu perfil público', `#professores/${uid}`, 'secondary')),
    section('Situação do cadastro',
      h('p', null, 'Situação atual: ', statusBadge(p.status)),
      h('ul', { class: 'stack-sm', style: 'list-style:none;padding:0' }, checklist.map(([label, done]) => h('li', null, done ? '✔ ' : '○ ', h('span', { class: 'sr-only' }, done ? 'Concluído: ' : 'Pendente: '), label)))),
    section('Dados públicos',
      dl({ Nome: p.nome, 'E-mail': p.email, 'Preço por hora': money(p.precoHora), Apresentação: p.biografia, Mentoria: p.ofereceMentoria ? `Oferece (até ${p.limiteMentorados})` : 'Não oferece' }),
      h('div', { class: 'actions' },
        btn('Editar apresentação e preço', () => formDialog('Dados públicos', [field('biografia', 'Apresentação', 'textarea', { maxLength: 2000 }), field('precoHora', 'Preço por hora (R$)', 'number', { min: 0.01, step: 0.01, inputMode: 'decimal' })], p,
          async (v) => { await put('/api/professores/me/perfil', { biografia: v.biografia, precoHora: v.precoHora }); reload(); }, { success: 'Perfil atualizado.' })),
        btn('Configurar mentoria', () => mentoringSettings(reload)))),
    section('Matérias que você ensina',
      (p.materias || []).length ? h('ul', null, p.materias.map(m => h('li', m.nome))) : emptyState('Vincule ao menos uma matéria para aparecer nas buscas.'),
      btn('Vincular matéria', async () => formDialog('Matéria que você ensina', [field('materiaId', 'Matéria', 'text', { options: subjectOptions((await subjects()).filter(m => !(p.materias || []).some(x => x.id === m.id))) })], {},
        async (v) => { await post(`/api/professores/${uid}/materias`, { materiaId: v.materiaId }); reload(); }, { success: 'Matéria vinculada.' }))),
    section('Qualificações',
      (p.qualificacoes || []).length ? h('div', { class: 'grid cards' }, p.qualificacoes.map(qualificationCard)) : emptyState('Adicione suas qualificações para análise.'),
      btn('Adicionar qualificação', () => formDialog('Nova qualificação', QUALIFICATION_FIELDS, {}, async (v) => { await post(`/api/professores/${uid}/qualificacoes`, v); reload(); }, { success: 'Qualificação enviada para verificação.' }))),
    section('Candidatura',
      candidaturas.length ? h('div', { class: 'stack' }, candidaturas.map(c => card({ title: `Candidatura · ${statusLabel(c.status)}` }, statusBadge(c.status),
        dl({ Apresentação: c.mensagem, 'Retorno da análise': c.motivo, 'Data da análise': when(c.dataAnaliseUtc) })))) : emptyState('Você ainda não enviou candidatura.'),
      last && last.status !== 'Reprovada' && last.status !== 'AguardandoDocumentacao' ? h('p', { class: 'muted small' }, 'Sua candidatura está em andamento. Você será avisado por notificação.') : null,
      btn(candidaturas.length ? 'Enviar nova candidatura' : 'Enviar candidatura', () => formDialog('Candidatura a professor', [field('mensagem', 'Apresentação para análise', 'textarea', { maxLength: 2000 })], {},
        async (v) => { await post(`/api/professores/${uid}/candidaturas`, v); reload(); }, { success: 'Candidatura enviada. Acompanhe a situação nesta página.' }), { kind: 'primary' })));
}
