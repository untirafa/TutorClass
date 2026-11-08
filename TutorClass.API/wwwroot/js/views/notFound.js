import { h, mount } from '../core/dom.js';
import { pageHeader, linkButton } from '../ui/components.js';
export default function notFound({ host }) {
  mount(host, h('div', { class: 'container stack' },
    pageHeader('Página não encontrada', 'O endereço que você abriu não existe ou mudou de lugar.'),
    h('div', { class: 'actions' }, linkButton('Ir para o início', '#home'), linkButton('Buscar professores', '#professores', 'secondary'))));
}
