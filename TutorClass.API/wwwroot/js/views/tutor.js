// TutorIA: resposta progressiva (SSE). O texto do modelo é renderizado sem HTML (lib/markdown.js).
import { h } from '../core/dom.js';
import { get, tutorStream, isAbort } from '../core/api.js';
import { session } from '../core/session.js';
import { announce } from '../core/a11y.js';
import { render as renderMarkdown } from '../lib/markdown.js';
import { pageHeader, btn, linkButton, emptyState } from '../ui/components.js';
import { createForm, field } from '../ui/form.js';
import { notice } from '../ui/toast.js';

export const MODERATION_REASONS = ['MENSAGEM_OFENSIVA', 'DADOS_PESSOAIS', 'AMEACA_OU_ASSEDIO', 'TENTATIVA_ACESSO_INDEVIDO', 'ERRO_MODERACAO'];

export default async function tutor(ctx) {
  const status = await get('/api/tutoria/status', { signal: ctx.signal });
  const statusText = h('p', { class: 'muted', role: 'status' });
  const log = h('div', { class: 'chat-log', role: 'log', 'aria-live': 'polite', 'aria-relevant': 'additions text', 'aria-label': 'Conversa com a TutorIA', tabindex: '0' });
  let allowed = false;
  const nodes = new WeakMap();

  const update = (s) => {
    allowed = !!s.possuiAcesso && !s.limiteAtingido;
    // A mensagem de plano do servidor não aparece na tela; quem não tem acesso recebe o convite amigável no topo.
    statusText.textContent = s.possuiAcesso ? `${s.limiteAtingido ? (s.mensagem || '') : ''} ${s.restantesHoje == null ? 'Sem limite diário do plano.' : `${s.restantesHoje} mensagem(ns) restante(s) hoje.`}`.trim() : '';
    form.submitButton.toggleAttribute('data-blocked', !allowed);
    form.controls.mensagem.disabled = !allowed;
    plansLink.hidden = allowed;
  };
  const messages = () => {
    log.replaceChildren(...(session.tutorHistory.length ? session.tutorHistory.map(m => {
      const bubble = h('div', { class: `msg${m.role === 'Você' ? ' own' : ''}` }, h('strong', m.role));
      const body = h('div', { class: 'msg-body' });
      if (m.role === 'Você') body.textContent = m.text; else renderMarkdown(body, m.text);
      bubble.append(body);
      if (m.pending) bubble.append(h('small', { class: 'msg-note' }, m.text ? 'Recebendo resposta…' : 'A TutorIA está preparando a resposta…'));
      if (m.interrupted) bubble.append(h('small', { class: 'msg-note' }, 'Resposta incompleta. Atualize o status antes de tentar novamente.'));
      nodes.set(m, bubble); return bubble;
    }) : [h('p', { class: 'muted' }, 'Sua conversa de estudos começa aqui.')]));
  };

  const refresh = btn('Atualizar status', async () => { update(await get('/api/tutoria/status')); refresh.hidden = true; }, { iconName: 'refresh' }); refresh.hidden = true;
  const plansLink = linkButton('Conhecer os planos', '#app/planos', 'secondary'); plansLink.hidden = true;

  const form = createForm({
    fields: [field('mensagem', 'Qual é sua dúvida?', 'textarea', { maxLength: 4000, rows: 3 })], submit: 'Enviar pergunta',
    onSubmit: async (v) => {
      if (!allowed) throw new Error(statusText.textContent || 'A TutorIA não está disponível no seu plano agora.');
      const mensagem = v.mensagem;
      const controller = new AbortController(), epoch = session.epoch;
      session.tutorStream = controller;
      const answer = { role: 'TutorIA', text: '', pending: true };
      session.tutorHistory.push({ role: 'Você', text: mensagem }, answer); messages();
      const input = form.controls.mensagem; input.disabled = true; refresh.hidden = true;
      log.setAttribute('aria-busy', 'true'); // leitores de tela esperam a resposta inteira em vez de ler cada trecho
      announce('A TutorIA está respondendo.');
      try {
        const response = await tutorStream({ mensagem }, {
          signal: controller.signal,
          onDelta: (piece) => {
            if (epoch !== session.epoch) throw new DOMException('Sessão alterada.', 'AbortError');
            answer.text += piece;
            const bubble = nodes.get(answer);
            if (log.isConnected && bubble) { renderMarkdown(bubble.querySelector('.msg-body'), answer.text); log.scrollTop = log.scrollHeight; }
          }
        });
        if (epoch !== session.epoch) throw new DOMException('Sessão alterada.', 'AbortError');
        update(response);
        if (MODERATION_REASONS.includes(response.motivo)) {
          session.tutorHistory.splice(session.tutorHistory.indexOf(answer), 1);
          statusText.textContent = `TutorClass: ${response.mensagem}`; notice(statusText.textContent, { error: true }); return;
        }
        if (response.motivo || !response.resposta) throw new Error(response.mensagem || 'A TutorIA não conseguiu responder agora.');
        answer.text = response.resposta; form.reset();
        announce('Resposta da TutorIA recebida.');
      } catch (error) {
        if (answer.text.trim()) answer.interrupted = true; else session.tutorHistory.splice(session.tutorHistory.indexOf(answer), 1);
        statusText.textContent = `TutorClass: ${error.message}`;
        allowed = false; refresh.hidden = false;
        if (isAbort(error)) return;
        throw error;
      } finally {
        answer.pending = false; log.removeAttribute('aria-busy');
        if (session.tutorStream === controller) session.tutorStream = null;
        if (epoch === session.epoch && log.isConnected) { messages(); log.scrollTop = log.scrollHeight; form.controls.mensagem.disabled = !allowed; if (allowed) form.controls.mensagem.focus(); }
      }
    }
  });
  ctx.cleanup(() => session.tutorStream?.abort());
  ctx.host.append(pageHeader('TutorIA', 'Tire dúvidas de estudo com a assistente. Ela não substitui o seu professor e não deve receber dados pessoais.'),
    statusText, log, form.el, h('div', { class: 'actions' }, refresh, plansLink));
  update(status); messages();
}
