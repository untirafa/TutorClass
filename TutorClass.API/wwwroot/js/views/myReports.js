// "Minhas denúncias": acompanhamento das denúncias feitas pela própria pessoa (GET /api/denuncias/minhas).
import { h } from '../core/dom.js';
import { get } from '../core/api.js';
import { when } from '../core/format.js';
import { pageHeader, card, badge, dl, pagedList, link } from '../ui/components.js';
import { REASON_LABEL, TARGET_LABEL } from '../ui/report.js';

const STATUS = {
  Aberta: ['Recebida', 'warn'], EmAnalise: ['Em análise', 'warn'],
  Procedente: ['Analisada: providências tomadas', 'ok'], Improcedente: ['Analisada: sem violação das regras', 'neutral']
};

export default async function myReports(ctx) {
  const rows = await get('/api/denuncias/minhas', { signal: ctx.signal });
  ctx.host.append(pageHeader('Minhas denúncias', 'Acompanhe o andamento das denúncias que você enviou. A pessoa denunciada não vê quem denunciou.'),
    h('p', { class: 'small muted' }, 'Para denunciar, use o botão “Denunciar” no perfil de um professor, em uma avaliação ou em uma mensagem da Mentoria. ', link('Regras de convivência', '#regras-de-convivencia'), '.'),
    pagedList(rows, (d) => {
      const [label, tone] = STATUS[d.status] || [d.status, 'neutral'];
      return card({ title: TARGET_LABEL[d.tipo] || d.tipo, subtitle: d.resumo }, h('p', null, badge(label, tone)),
        dl({ Motivo: REASON_LABEL[d.motivo] || d.motivo, Detalhes: d.descricao, Enviada: when(d.criadaEmUtc), Analisada: d.analisadaEmUtc ? when(d.analisadaEmUtc) : null }));
    }, { empty: 'Você ainda não enviou denúncias.', label: 'denúncias' }));
}
