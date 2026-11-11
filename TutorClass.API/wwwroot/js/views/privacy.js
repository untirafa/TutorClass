import { h, mount } from '../core/dom.js';
import { publicGet } from '../core/api.js';
import { pageHeader, externalLink, errorState, section, link } from '../ui/components.js';
import { LEGAL_VERSION } from './legal.js';

export default async function privacy(ctx) {
  let data = null, failed = false;
  try { data = await publicGet('/api/privacidade/documentos', { signal: ctx.signal }); } catch { failed = true; }
  mount(ctx.host, h('div', { class: 'container stack prose' },
    pageHeader('Privacidade e termos', 'Como o TutorClass trata seus dados e quais documentos valem para o uso da plataforma.'),
    data?.orientacoes ? h('p', null, data.orientacoes) : null,
    data?.aceiteObrigatorio ? h('p', { class: 'badge tone-warn' }, 'O aceite dos documentos é obrigatório para usar a plataforma.') : null,
    section('Documentos', h('ul', null,
      h('li', null, link(`Política de privacidade (versão ${LEGAL_VERSION})`, '#politica-de-privacidade')),
      h('li', null, link(`Termos de uso (versão ${LEGAL_VERSION})`, '#termos-de-uso')),
      h('li', null, link('Regras de convivência e denúncias', '#regras-de-convivencia')),
      (data?.documentos || []).map(d => h('li', null, externalLink(`${d.tipo} (versão ${d.versao})`, d.url))))),
    failed ? errorState('Não foi possível carregar o contato de privacidade agora. Tente novamente em instantes.', ctx.router.refresh)
      : section('Contato de privacidade', h('p', null, data.contatoPrivacidade || 'O e-mail de contato da equipe será divulgado nesta página.')),
    section('Nossos compromissos', h('ul', null,
      h('li', 'Seu token de acesso fica apenas na memória desta aba: ao recarregar a página ou sair, você precisa entrar novamente.'),
      h('li', 'Não usamos cookies de rastreamento nem serviços externos de fontes, imagens ou estatísticas.'),
      h('li', 'As preferências de acessibilidade (tamanho do texto, tema, contraste e movimento) são guardadas somente neste navegador.'),
      h('li', 'Pagamentos e gravações são simulados nesta versão acadêmica: nenhum dado de cartão é solicitado.')))));
}
