// Central de ajuda: busca nas perguntas frequentes (API) e assistente de ajuda.
import { h, mount } from '../core/dom.js';
import { publicGet, post, q, errorText, isAbort } from '../core/api.js';
import { announce } from '../core/a11y.js';
import { pageHeader, loading, emptyState, errorState, link } from '../ui/components.js';
import { createForm, field } from '../ui/form.js';
import { faqItem } from './shared.js';
import { appInstallGuide } from '../ui/appDownload.js';

export const FAQ_CATEGORIES = ['Conta', 'Aulas', 'Professores', 'Pagamentos', 'Planos', 'Mentoria', 'TutorIA', 'Gravacoes'];
const catLabel = (c) => (c === 'Gravacoes' ? 'Gravações' : c);

async function build(ctx) {
  const results = h('div', { class: 'stack-sm', 'aria-live': 'polite' });
  const search = async (v) => {
    mount(results, loading('Buscando respostas…'));
    try {
      const items = await publicGet('/api/faq' + q({ termo: v.termo, categoria: v.categoria }), { signal: ctx.signal });
      mount(results, items.length ? h('div', { class: 'stack-sm' }, items.map(faqItem)) : emptyState('Nenhuma pergunta encontrada. Tente outras palavras ou use o assistente abaixo.'));
      announce(`${items.length} resposta(s) encontrada(s).`);
    } catch (e) { if (!isAbort(e)) mount(results, errorState(errorText(e), () => search(v))); }
  };
  const form = createForm({
    fields: [field('termo', 'Pesquisar dúvidas', 'search', { required: false, autocomplete: 'off' }),
      field('categoria', 'Categoria', 'text', { required: false, options: [{ value: '', label: 'Todas' }, ...FAQ_CATEGORIES.map(c => ({ value: c, label: catLabel(c) }))] })],
    submit: 'Pesquisar', onSubmit: search
  });

  const answer = h('div', { class: 'panel stack', role: 'status', 'aria-live': 'polite', hidden: true });
  const chat = createForm({
    fields: [field('mensagem', 'Pergunte sobre a plataforma', 'textarea', { maxLength: 500, rows: 3 })], submit: 'Consultar assistente de ajuda',
    onSubmit: async (v) => {
      const r = await post('/api/faq/chat', { mensagem: v.mensagem }, { auth: false });
      answer.hidden = false;
      mount(answer, h('p', null, r.encontrado ? [h('strong', r.pergunta || 'Resposta'), h('br'), r.resposta] : (r.mensagem || 'Não encontrei uma resposta para esta pergunta.')),
        r.encontrado && r.textoAcao && /^#?[\w\-/]+$/.test(r.rotaAcao || '') ? link(r.textoAcao, r.rotaAcao.startsWith('#') ? r.rotaAcao : `#${r.rotaAcao.replace(/^\//, '')}`) : null);
      announce('O assistente respondeu.');
    }
  });
  const wrap = h('div', { class: 'stack' },
    h('section', { class: 'stack', 'aria-labelledby': 'faq-s' }, h('h2', { id: 'faq-s' }, 'Perguntas frequentes'), form.el, results),
    h('section', { class: 'stack', 'aria-labelledby': 'faq-a' }, h('h2', { id: 'faq-a' }, 'Assistente de ajuda'),
      h('p', { class: 'muted' }, 'Descreva sua dúvida sobre contas, aulas, planos ou pagamentos. O assistente consulta as perguntas frequentes cadastradas.'), chat.el, answer),
    appInstallGuide());
  mount(ctx.host, wrap);
  if (ctx.query?.termo) { form.controls.termo.value = ctx.query.termo; }
  await search({ termo: ctx.query?.termo });
}

export async function publicHelp(ctx) {
  const box = h('div', { class: 'container stack' }, pageHeader('Central de ajuda', 'Encontre respostas rápidas sobre o TutorClass.')); const body = h('div'); box.append(body);
  mount(ctx.host, box); await build({ ...ctx, host: body });
}
export async function appModule(ctx) {
  ctx.host.append(pageHeader('Perguntas frequentes', 'Pesquise dúvidas ou fale com o assistente de ajuda.')); const body = h('div'); ctx.host.append(body);
  await build({ ...ctx, host: body });
}
