import { h, mount } from '../core/dom.js';
import { get, post } from '../core/api.js';
import { money } from '../core/format.js';
import { pageHeader, dl, linkButton, btn } from '../ui/components.js';
import { confirmDialog, formDialog } from '../ui/dialogs.js';
import { notice } from '../ui/toast.js';
import { planCard, PLANS } from './shared.js';

export async function publicPlans({ host }) {
  mount(host, h('div', { class: 'container stack' },
    pageHeader('Planos', 'As aulas são contratadas separadamente, pelo preço de cada professor. Os planos liberam recursos extras.'),
    h('div', { class: 'grid cards' }, PLANS.map(p => planCard(p, { action: linkButton(p.price ? `Quero o ${p.name}` : 'Começar agora', '#cadastro-aluno', p.name === 'Foco' ? 'primary' : 'secondary') }))),
    h('p', { class: 'muted' }, 'As promoções valem somente para a primeira assinatura elegível e não voltam após o cancelamento. Nesta versão acadêmica a cobrança é simulada.')));
}

export default async function plans(ctx) {
  const [plan, usage] = await Promise.all([get('/api/planos/me'), get('/api/planos/me/ia')]);
  const reload = () => ctx.router.refresh();
  const current = h('section', { class: 'panel stack', 'aria-labelledby': 'cur-plan' },
    h('h2', { id: 'cur-plan' }, `Plano atual: ${plan.planoAtual}`),
    dl({
      'Assinatura ativa': plan.assinaturaAtiva ? 'Sim' : 'Não', 'IA educacional': plan.possuiIaEducacional ? 'Incluída' : 'Não incluída',
      'Uso de IA hoje': usage.quantidadeUsadaHoje, 'Limite diário': usage.limiteDiario ?? 'Sem limite do plano',
      'Mentoria': plan.possuiMentoria ? 'Incluída' : 'Não incluída', 'Gravações elegíveis': plan.possuiGravacoesElegiveis ? 'Incluídas' : 'Não incluídas'
    }),
    plan.assinaturaAtiva ? btn('Cancelar assinatura', () => confirmDialog('Cancelar assinatura',
      'Os benefícios serão encerrados conforme as regras do plano. As aulas contratadas continuam independentes.',
      async () => { await post('/api/planos/me/cancelar'); reload(); }, { confirm: 'Cancelar assinatura', danger: true, success: 'Assinatura cancelada.' }), { kind: 'danger' }) : null);

  const cards = PLANS.map(p => planCard(p, {
    action: p.name === 'Gratuito' ? linkButton('Encontrar professores', '#app/professores', 'secondary')
      : btn(plan.planoAtual === p.name && plan.assinaturaAtiva ? 'Plano atual' : `Escolher ${p.name}`, () => {
        if (plan.planoAtual === p.name && plan.assinaturaAtiva) return;
        confirmDialog(`Assinar ${p.name}`, `Mensalidade de ${money(p.price)}. ${p.promo} Aulas são cobradas separadamente. Esta versão registra uma assinatura simulada.`,
          async () => {
            const r = await post('/api/planos/me/assinatura', { plano: p.name });
            notice(`Plano ${r.plano} ativado. Primeiro mês: ${money(r.valorCobradoPrimeiroMes)}; mensalidade: ${money(r.valorMensal)}.`); reload();
          }, { confirm: 'Confirmar assinatura simulada', success: null });
      }, { kind: p.name === 'Foco' ? 'primary' : 'secondary' })
  }));
  ctx.host.append(pageHeader('Planos', 'Aulas são cobradas separadamente. As promoções só se aplicam à primeira assinatura elegível e não voltam após cancelamento.'),
    current, h('div', { class: 'grid cards' }, cards));
}
