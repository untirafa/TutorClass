import { h, mount } from '../core/dom.js';
import { publicGet } from '../core/api.js';
import { pageHeader, externalLink, errorState, section } from '../ui/components.js';

export default async function privacy(ctx) {
  let data = null, failed = false;
  try { data = await publicGet('/api/privacidade/documentos', { signal: ctx.signal }); } catch { failed = true; }
  mount(ctx.host, h('div', { class: 'container stack prose' },
    pageHeader('Privacidade e termos', 'Como o TutorClass trata seus dados e quais documentos valem para o uso da plataforma.'),
    failed ? errorState('Não foi possível carregar os documentos agora. Tente novamente em instantes.', ctx.router.refresh) : [
      data.orientacoes ? h('p', null, data.orientacoes) : null,
      data.aceiteObrigatorio ? h('p', { class: 'badge tone-warn' }, 'O aceite dos documentos é obrigatório para usar a plataforma.') : null,
      section('Documentos', (data.documentos || []).length
        ? h('ul', null, data.documentos.map(d => h('li', null, externalLink(`${d.tipo} (versão ${d.versao})`, d.url))))
        : h('p', { class: 'muted' }, 'Nenhum documento publicado.')),
      section('Contato de privacidade', h('p', null, data.contatoPrivacidade || 'Contato não informado.'))],
    section('Nossos compromissos', h('ul', null,
      h('li', 'Seu token de acesso fica apenas na memória desta aba: ao recarregar a página ou sair, você precisa entrar novamente.'),
      h('li', 'Não usamos cookies de rastreamento nem serviços externos de fontes, imagens ou estatísticas.'),
      h('li', 'As preferências de acessibilidade (tamanho do texto, tema, contraste e movimento) são guardadas somente neste navegador.'),
      h('li', 'Pagamentos e gravações são simulados nesta versão acadêmica: nenhum dado de cartão é solicitado.')))));
}
