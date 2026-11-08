// Entrar e criar conta (Aluno, Professor, Responsável). O perfil de quem entra é definido pela API.
import { h, mount } from '../core/dom.js';
import { api, get, errorText } from '../core/api.js';
import { session, resetSession } from '../core/session.js';
import { announce } from '../core/a11y.js';
import { createForm, field, optional } from '../ui/form.js';
import { notice } from '../ui/toast.js';
import { link } from '../ui/components.js';

const EMAIL = field('email', 'E-mail', 'email', { autocomplete: 'username', maxLength: 254 });
const NAME = field('nome', 'Nome completo', 'text', { autocomplete: 'name', maxLength: 150 });
const NEW_PASSWORD = field('senha', 'Senha', 'password', { autocomplete: 'new-password', minLength: 8, help: 'Use ao menos 8 caracteres.' });

const KINDS = {
  'cadastro-aluno': {
    kind: 'aluno', title: 'Criar conta de aluno', lead: 'Seu próximo passo começa aqui.', submit: 'Criar minha conta',
    fields: [NAME, EMAIL, field('dataNascimento', 'Data de nascimento', 'date', { required: false, help: 'Opcional. Alunos menores de 18 anos não têm acesso à área financeira; um responsável cuida dos pagamentos.', autocomplete: 'bday' }), NEW_PASSWORD],
    body: (v) => ({ nome: v.nome, email: v.email, senha: v.senha, dataNascimento: v.dataNascimento || null })
  },
  'cadastro-professor': {
    kind: 'professor', title: 'Quero ensinar', lead: 'Crie seu perfil. Depois, informe matérias e qualificações e envie sua candidatura para análise.', submit: 'Criar minha conta de professor',
    fields: [NAME, EMAIL, field('biografia', 'Sua apresentação', 'textarea', { maxLength: 2000, help: 'Conte sobre sua experiência e sua forma de ensinar.' }),
      field('precoHora', 'Preço por hora (R$)', 'number', { min: 0.01, step: 0.01, inputMode: 'decimal' }), NEW_PASSWORD],
    body: (v) => ({ nome: v.nome, email: v.email, senha: v.senha, biografia: v.biografia, precoHora: Number(v.precoHora) })
  },
  'cadastro-responsavel': {
    kind: 'responsavel', title: 'Cadastro de responsável', lead: 'Acompanhe aulas, pagamentos e a mentoria dos alunos vinculados à sua conta.', submit: 'Criar minha conta de responsável',
    fields: [NAME, EMAIL, field('telefone', 'Telefone', 'tel', { required: false, autocomplete: 'tel', inputMode: 'tel', help: 'Opcional. Com DDD.' }), NEW_PASSWORD],
    body: (v) => ({ nome: v.nome, email: v.email, senha: v.senha, telefone: v.telefone || null })
  }
};

export async function completeLogin(token) {
  if (typeof token !== 'string' || !token.trim()) throw new Error('A API não retornou um token de acesso válido.');
  resetSession(); session.token = token;
  try {
    const user = await get('/api/auth/me');
    if (!user?.id || !user.nome || !user.perfil) throw new Error('A API não retornou os dados da conta autenticada.');
    session.user = user;
  } catch (error) { resetSession(); throw error; }
}

export default function auth(ctx) {
  const registering = ctx.path.startsWith('cadastro');
  const kind = KINDS[ctx.path];
  const tabs = h('nav', { 'aria-label': 'Entrar ou criar conta' }, h('div', { class: 'auth-tabs' },
    h('a', { href: '#login', 'aria-current': !registering ? 'page' : null }, 'Entrar'),
    h('a', { href: '#cadastro-aluno', 'aria-current': registering ? 'page' : null }, 'Criar conta')));
  let form, extra = null;

  if (!registering) {
    form = createForm({
      fields: [EMAIL, field('senha', 'Senha', 'password', { autocomplete: 'current-password' })], submit: 'Entrar na minha conta',
      onSubmit: async (v) => {
        const login = await api('/api/auth/login', { method: 'POST', auth: false, body: { email: v.email, senha: v.senha } });
        await completeLogin(login?.accessToken);
        const target = session.returnTo; session.returnTo = null;
        notice(`Bem-vindo(a), ${session.user.nome.split(' ')[0]}.`);
        location.hash = target && target.startsWith('#') && !/^#(login|cadastro)/.test(target) ? target : 'app/inicio';
      }
    });
    form.controls.senha.minLength = 1;
    extra = h('p', { class: 'small muted' }, 'Ainda não tem conta? ', link('Crie agora', '#cadastro-aluno'), '. Esqueceu a senha? A recuperação ainda não está disponível nesta versão: fale com a administração.');
  } else {
    form = createForm({
      fields: kind.fields, submit: kind.submit,
      onSubmit: async (v) => {
        await api(`/api/auth/registrar/${kind.kind}`, { method: 'POST', auth: false, body: kind.body(v) });
        form.controls.senha.value = '';
        notice('Conta criada. Entre com seu e-mail e senha.'); location.hash = 'login';
      }
    });
    extra = h('nav', { 'aria-label': 'Tipo de conta' }, h('p', { class: 'small' }, 'Criar conta como: ',
      Object.entries(KINDS).map(([path, k], i) => [i ? ' · ' : '', path === ctx.path ? h('strong', { 'aria-current': 'page' }, k.kind === 'responsavel' ? 'Responsável' : k.kind[0].toUpperCase() + k.kind.slice(1))
        : link(k.kind === 'responsavel' ? 'Responsável' : k.kind[0].toUpperCase() + k.kind.slice(1), `#${path}`)])));
  }

  mount(ctx.host, h('div', { class: 'container' }, h('div', { class: 'auth' },
    h('div', { class: 'stack' }, h('span', { class: 'eyebrow' }, 'Bem-vindo ao TutorClass'),
      h('h1', { tabindex: '-1' }, registering ? kind.title : 'Entre na sua conta'),
      h('p', { class: 'lead' }, registering ? kind.lead : 'Use seu e-mail e senha. Seu perfil (aluno, professor, responsável ou administrador) é identificado automaticamente.'),
      link('← Voltar ao início', '#home')),
    h('div', { class: 'auth-card stack' }, tabs, form.el, extra))));
  announce(registering ? 'Formulário de cadastro.' : 'Formulário de entrada.');
}
