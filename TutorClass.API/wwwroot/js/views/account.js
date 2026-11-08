import { h } from '../core/dom.js';
import { get } from '../core/api.js';
import { roleLabel } from '../core/session.js';
import { pageHeader, dl, linkButton } from '../ui/components.js';
import { accessibilityPanel } from '../shell.js';

export default async function account(ctx) {
  const user = await get('/api/auth/me');
  ctx.host.append(pageHeader('Minha conta'),
    h('div', { class: 'panel stack' }, dl({ Nome: user.nome, 'E-mail': user.email, Perfil: roleLabel(user.perfil) }),
      h('p', { class: 'muted' }, 'Sua sessão fica apenas nesta aba. Ao recarregar a página ou sair, entre novamente.')),
    h('div', { class: 'actions' }, h('button', { type: 'button', class: 'btn secondary', onclick: accessibilityPanel }, 'Preferências de acessibilidade'),
      linkButton('Privacidade e termos', '#privacidade', 'ghost'), linkButton('Declaração de acessibilidade', '#acessibilidade', 'ghost')));
}
