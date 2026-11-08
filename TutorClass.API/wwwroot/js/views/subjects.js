import { h, mount } from '../core/dom.js';
import { subjects } from '../core/api.js';
import { pageHeader, card, emptyState, linkButton } from '../ui/components.js';

export default async function subjectsView(ctx) {
  const items = await subjects();
  mount(ctx.host, h('div', { class: 'container stack' },
    pageHeader('Matérias', 'Escolha uma matéria para ver os professores que a ensinam.'),
    items.length ? h('div', { class: 'grid cards' }, items.map(m => card({ title: m.nome }, linkButton('Ver professores', `#professores?materia=${encodeURIComponent(m.id)}`, 'secondary'))))
      : emptyState('Nenhuma matéria cadastrada ainda.')));
}
