// Mentoria: vínculos, escolha/troca de mentor, configuração do professor e chat (atualização a cada 8 s).
import { h } from '../core/dom.js';
import { get, post, put, publicGet, q, errorText, isAbort } from '../core/api.js';
import { session, isStudent, isTeacher } from '../core/session.js';
import { when } from '../core/format.js';
import { announce } from '../core/a11y.js';
import { pageHeader, card, badge, btn, emptyState, pagedList } from '../ui/components.js';
import { field, createForm } from '../ui/form.js';
import { openDialog } from '../ui/dialog.js';
import { formDialog } from '../ui/dialogs.js';
import { reportButton } from '../ui/report.js';

export default async function mentoring(ctx) {
  const rows = await get('/api/mentorias/me', { signal: ctx.signal });
  const reload = () => ctx.router.refresh();
  ctx.host.append(pageHeader('Mentoria e chat', 'Conversas são privadas entre os participantes. Vínculos encerrados mantêm o histórico disponível para leitura.',
    isStudent() ? btn('Selecionar mentor', () => selectMentor(null, reload), { kind: 'primary' }) : btn('Configurar oferta de mentoria', () => mentoringSettings(reload))),
    pagedList(rows, (m) => card({ title: isStudent() ? m.professorNome : m.alunoNome, subtitle: `Vaga ${m.slot} · Desde ${when(m.dataInicioUtc)}` },
      badge(m.ativo ? 'Ativa' : 'Encerrada', m.ativo ? 'ok' : 'neutral'),
      h('div', { class: 'actions' }, btn('Abrir conversa', () => openChat(m)), isStudent() && m.ativo ? btn('Trocar mentor', () => selectMentor(m.slot, reload)) : null)),
      { empty: 'Nenhum vínculo de mentoria encontrado.', label: 'vínculos' }));
}

export async function selectMentor(slot, reload) {
  const teachers = (await publicGet('/api/professores')).filter(p => p.ofereceMentoria);
  if (!teachers.length) throw new Error('Nenhum professor está oferecendo mentoria no momento.');
  const fields = [field('professorId', 'Professor mentor', 'text', { options: teachers.map(p => ({ value: p.id, label: `${p.nome} · ${(p.materias || []).join(', ')}` })) })];
  if (!slot) fields.push(field('slot', 'Vaga', 'number', { min: 1, max: 2, step: 1, value: 1 }));
  formDialog(slot ? 'Trocar mentor' : 'Selecionar mentor', fields, {}, async (v) => {
    if (slot) await put(`/api/mentorias/me/slots/${slot}/trocar`, { novoProfessorId: v.professorId });
    else await post('/api/mentorias/me', { professorId: v.professorId, slot: v.slot });
    reload?.();
  }, { description: slot ? 'A troca respeita o intervalo mínimo de 30 dias por vaga. O histórico anterior é preservado.' : 'É necessário ter plano Mentoria ativo. Você pode ter até dois mentores, sujeitos à disponibilidade.', success: 'Mentoria atualizada.' });
}

export function mentoringSettings(reload) {
  formDialog('Oferta de mentoria', [field('ofereceMentoria', 'Quero oferecer mentoria', 'checkbox'), field('limiteMentorados', 'Limite de mentorados', 'number', { min: 0, max: 50, step: 1, value: 2 })], {},
    async (v) => { await put('/api/professores/me/mentoria', v); reload?.(); }, { description: 'Informe a oferta e o limite que deseja aplicar ao seu perfil.', success: 'Configuração de mentoria salva.' });
}

/**
 * Chat da mentoria. readOnlyFor = alunoId quando o responsável consulta o histórico (somente leitura, sem marcar como lida).
 */
export async function openChat(mentoring, { readOnlyFor } = {}) {
  const guardian = !!readOnlyFor;
  const prefix = `/api/mentorias/${mentoring.id}/chat`;
  const info = guardian ? { somenteLeitura: true } : await get(prefix);
  const who = guardian ? mentoring.professorNome : (isStudent() ? mentoring.professorNome : mentoring.alunoNome);
  const dlg = openDialog(`Conversa com ${who}`, { size: 'lg' });
  const status = h('p', { class: 'muted small', role: 'status' }, 'Carregando mensagens…');
  const log = h('div', { class: 'chat-log', role: 'log', 'aria-live': 'polite', 'aria-relevant': 'additions', 'aria-label': `Mensagens com ${who}`, tabindex: '0' });
  const older = btn('Carregar mensagens anteriores', () => load(true)); older.hidden = true;
  const refreshBtn = btn('Atualizar conversa', () => load(false), { iconName: 'refresh' });
  dlg.body.append(status, h('div', { class: 'actions' }, older, refreshBtn), log);

  let before = null, busy = false, firstRender = true;
  const seen = new Map();
  const render = () => {
    const rows = [...seen.values()].sort((a, b) => new Date(a.dataEnvioUtc) - new Date(b.dataEnvioUtc) || a.id.localeCompare(b.id));
    log.replaceChildren(...(rows.length ? rows.map(m => h('div', { class: `msg${m.remetenteUsuarioId === session.user?.id ? ' own' : ''}` },
      h('strong', m.remetenteNome), h('span', m.conteudo), h('small', `${when(m.dataEnvioUtc)}${m.lida ? ' · Lida' : ''}`),
      !guardian && m.remetenteUsuarioId !== session.user?.id ? reportButton({ tipo: 'Mensagem', alvoId: m.id, vinculoMentoriaId: mentoring.id, alvoNome: `a mensagem de ${m.remetenteNome}` }, 'Denunciar mensagem') : null))
      : [h('p', { class: 'muted' }, 'Inicie a conversa com seu mentorado ou mentor.')]));
    older.hidden = !before;
  };
  async function load(previous = false) {
    if (busy) return; busy = true;
    try {
      const base = guardian ? `/api/responsavel/alunos/${encodeURIComponent(readOnlyFor)}/mentorias/${mentoring.id}/chat/mensagens` : `${prefix}/mensagens`;
      const page = await get(base + q({ limit: 30, before: previous ? before : '' }));
      if (!dlg.open) return;
      const hadNone = seen.size === 0;
      page.mensagens.forEach(m => seen.set(m.id, m));
      if (previous || hadNone) before = page.proximoBeforeUtc;
      render();
      if (firstRender) { log.scrollTop = log.scrollHeight; firstRender = false; }
      status.textContent = info.somenteLeitura ? (guardian ? 'Modo somente leitura para responsáveis.' : 'Vínculo encerrado: conversa disponível somente para leitura.') : 'Atualização automática a cada 8 segundos.';
      if (!guardian) await post(`${prefix}/mensagens/lidas`);
    } catch (e) { if (!isAbort(e) && dlg.open) status.textContent = errorText(e); }
    finally { busy = false; }
  }
  if (!info.somenteLeitura) {
    const form = createForm({
      fields: [field('conteudo', 'Mensagem', 'textarea', { maxLength: 4000, rows: 3 })], submit: 'Enviar mensagem',
      onSubmit: async (v) => {
        const saved = await post(`${prefix}/mensagens`, { conteudo: v.conteudo });
        seen.set(saved.id, saved); form.reset(); render(); log.scrollTop = log.scrollHeight; form.controls.conteudo.focus();
      }
    });
    dlg.body.append(form.el);
  }
  await load();
  const timer = setInterval(() => { if (document.visibilityState === 'visible' && dlg.open) load(); }, 8000);
  dlg.onClose(() => clearInterval(timer));
  return dlg;
}
