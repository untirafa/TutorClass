// Tabela de rotas. area: public | auth (somente visitantes) | app (exige sessão).
const V = (file, fn = 'default') => () => import(`./views/${file}.js`).then(m => m[fn]);

export const ROUTES = [
  { path: 'home', title: 'Início', area: 'public', load: V('home') },
  { path: 'sobre', title: 'Início', area: 'public', load: V('home') },
  { path: 'professores', title: 'Buscar professores', area: 'public', load: V('teachers', 'publicSearch') },
  { path: 'professores/:id', title: 'Perfil do professor', area: 'public', load: V('teachers', 'publicProfile') },
  { path: 'materias', title: 'Matérias', area: 'public', load: V('subjects') },
  { path: 'planos', title: 'Planos', area: 'public', load: V('plans', 'publicPlans') },
  { path: 'ajuda', title: 'Central de ajuda', area: 'public', load: V('help', 'publicHelp') },
  { path: 'privacidade', title: 'Privacidade e termos', area: 'public', load: V('privacy') },
  { path: 'termos-de-uso', title: 'Termos de uso', area: 'public', load: V('legal', 'terms') },
  { path: 'politica-de-privacidade', title: 'Política de privacidade', area: 'public', load: V('legal', 'policy') },
  { path: 'regras-de-convivencia', title: 'Regras de convivência', area: 'public', load: V('legal', 'rules') },
  { path: 'acessibilidade', title: 'Acessibilidade', area: 'public', load: V('accessibility') },
  { path: 'login', title: 'Entrar', area: 'auth', load: V('auth') },
  { path: 'cadastro-aluno', title: 'Criar conta de aluno', area: 'auth', load: V('auth') },
  { path: 'cadastro-professor', title: 'Cadastro de professor', area: 'auth', load: V('auth') },
  { path: 'cadastro-responsavel', title: 'Cadastro de responsável', area: 'auth', load: V('auth') },
  { path: 'app', title: 'Área interna', area: 'app', load: V('appHost') },
  { path: 'app/:modulo', title: 'Área interna', area: 'app', load: V('appHost') },
  { path: 'app/:modulo/:id', title: 'Área interna', area: 'app', load: V('appHost') }
];
