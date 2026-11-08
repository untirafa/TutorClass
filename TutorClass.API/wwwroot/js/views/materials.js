// Materiais e atividades enviados pelo professor ao aluno (link compartilhável + instruções).
import { h } from '../core/dom.js';
import { get, post, put, del } from '../core/api.js';
import { isTeacher, isStudent } from '../core/session.js';
import { when, typeLabel } from '../core/format.js';
import { pageHeader, card, badge, dl, externalLink, btn, emptyState, pagedList } from '../ui/components.js';
import { field, optional, createForm } from '../ui/form.js';
import { openDialog } from '../ui/dialog.js';
import { formDialog, confirmDialog } from '../ui/dialogs.js';
import { notice } from '../ui/toast.js';

const TYPES = [{ value: 'Exercicio', label: 'Atividade / exercício' }, 'PDF', 'Link', { value: 'Video', label: 'Vídeo' }, { value: 'Documento', label: 'Documento' }, 'Outro'];
const MATERIAL_FIELDS = [
  field('titulo', 'Título', 'text', { maxLength: 160 }),
  optional('descricao', 'Instruções ou descrição', 'textarea', { maxLength: 1000 }),
  field('tipo', 'Tipo de material', 'text', { options: TYPES }),
  optional('nomeArquivo', 'Nome do arquivo', 'text', { maxLength: 255 }),
  field('urlArquivo', 'Link ou referência do material', 'url', { maxLength: 2000, help: 'Cole um link compartilhável de PDF, vídeo ou documento. O aluno recebe o link e as instruções; não há envio de arquivo por esta tela.' })
];

function materialCard(m, reload) {
  const kind = typeLabel(m.tipo);
  return card({ title: m.titulo, subtitle: m.descricao }, badge(kind, 'neutral'),
    h('p', { class: 'small muted' }, `${m.materiaNome} · ${isStudent() ? m.professorNome : m.alunoNome}`), externalLink('Abrir material', m.urlArquivo),
    h('div', { class: 'actions' },
      btn('Detalhes', async () => {
        const data = await get(`/api/materiais/${m.id}`); const d = openDialog(data.titulo);
        d.body.append(dl({ Instruções: data.descricao, Aluno: data.alunoNome, Professor: data.professorNome, Matéria: data.materiaNome, Tipo: kind, Arquivo: data.nomeArquivo, 'Criado em': when(data.createdAtUtc) }), externalLink('Abrir material', data.urlArquivo));
      }),
      isTeacher() ? [btn('Editar', () => materialForm(m, null, null, reload)),
        btn('Excluir', () => confirmDialog('Excluir material', `Remover o material “${m.titulo}”?`, async () => { await del(`/api/materiais/${m.id}`); reload(); }, { confirm: 'Excluir', danger: true, success: 'Material excluído.' }), { kind: 'danger' })] : null));
}

export default async function materials(ctx) {
  const items = await get('/api/materiais/me', { signal: ctx.signal });
  const reload = () => ctx.router.refresh();
  const out = h('div', { 'aria-live': 'polite' });
  const render = (tipo) => out.replaceChildren(pagedList(items.filter(m => !tipo || m.tipo === tipo), (m) => materialCard(m, reload), { empty: 'Nenhum material nesta seleção.', label: 'materiais' }));
  const filter = createForm({ fields: [field('tipo', 'Mostrar', 'text', { required: false, options: [{ value: '', label: 'Todos os materiais' }, { value: 'Exercicio', label: 'Só atividades / exercícios' }] })], submit: 'Filtrar', onSubmit: (v) => render(v.tipo || '') });
  ctx.host.append(pageHeader('Materiais e atividades', null, isTeacher() ? btn('Enviar material ou atividade', () => materialForm(null, null, null, reload), { kind: 'primary' }) : null),
    h('p', { class: 'muted' }, isTeacher() ? 'Escolha um aluno com aula ou mentoria ativa. O material fica salvo e o aluno recebe uma notificação.' : 'Aqui ficam os materiais e exercícios enviados pelos seus professores.'),
    items.length ? filter.el : null, out);
  render('');
}

export async function materialForm(existing, lesson, recipient, reload) {
  if (existing) {
    formDialog('Editar material', MATERIAL_FIELDS, existing, async (v) => { await put(`/api/materiais/${existing.id}`, v); reload?.(); }, { success: 'Material atualizado.' }); return;
  }
  const recipients = (await get('/api/materiais/destinatarios')).filter(r => (!recipient || r.alunoId === recipient.alunoId) && (!lesson || (r.alunoId === lesson.aluno.id && r.materiaId === lesson.materia.id)));
  if (!recipients.length) {
    const d = openDialog('Enviar material ou atividade');
    d.body.append(emptyState('Você ainda não possui aluno e matéria elegíveis. Uma aula não cancelada ou uma mentoria ativa com essa matéria permite o envio.'),
      btn('Ver meus alunos', () => { d.close(); location.hash = 'app/alunos'; })); return;
  }
  const fields = [
    field('destinatario', 'Aluno e matéria', 'text', { options: recipients.map((r, i) => ({ value: String(i), label: `${r.alunoNome} · ${r.materiaNome}` })) }),
    field('aulaId', 'Vincular a uma aula', 'text', { required: false, options: [{ value: '', label: 'Material geral para este aluno' }] }),
    ...MATERIAL_FIELDS];
  const { form } = formDialog('Enviar material ou atividade', fields, { tipo: 'Exercicio' }, async (v) => {
    const target = recipients[Number(v.destinatario)]; const { destinatario, ...body } = v;
    await post('/api/materiais', { ...body, alunoId: target.alunoId, materiaId: target.materiaId, aulaId: v.aulaId || null });
    notice(`Material salvo e enviado para ${target.alunoNome}.`); reload?.();
  }, { description: 'O conteúdo será visível apenas ao aluno escolhido, ao professor e aos responsáveis autorizados.', save: 'Enviar ao aluno', success: null });
  const sel = form.controls.aulaId;
  const update = () => {
    const target = recipients[Number(form.controls.destinatario.value)];
    sel.replaceChildren(new Option('Material geral para este aluno', ''), ...target.aulas.map(a => new Option(`${when(a.inicio)} · ${a.status}`, a.id)));
    if (lesson) sel.value = lesson.id;
  };
  form.controls.destinatario.addEventListener('change', update); update();
}
