// Catálogo dos módulos da área interna: quem vê, rótulo, ícone e como carregar (sob demanda).
const L = (file, fn = 'default') => () => import(`./views/${file}.js`).then(m => m[fn]);

export const MODULES = [
  { id: 'inicio', short: 'Início', label: 'Visão geral', icon: 'home', roles: ['Aluno', 'Professor', 'Responsavel', 'Administrador'], load: L('dashboard') },
  { id: 'professores', short: 'Professores', label: 'Encontrar professores', icon: 'search', roles: ['Aluno', 'Responsavel'], load: L('teachers', 'searchModule') },
  { id: 'professor', label: 'Perfil do professor', icon: 'id', roles: ['Aluno', 'Responsavel'], hidden: true, parent: 'professores', load: L('teachers', 'profileModule') },
  { id: 'vinculados', short: 'Alunos', label: 'Alunos vinculados', icon: 'users', roles: ['Responsavel'], load: L('guardian', 'listModule') },
  { id: 'dependente', label: 'Acompanhar aluno', icon: 'users', roles: ['Responsavel'], hidden: true, parent: 'vinculados', load: L('guardian', 'studentModule') },
  { id: 'alunos', short: 'Alunos', label: 'Meus alunos', icon: 'users', roles: ['Professor'], load: L('teacherStudents') },
  { id: 'aulas', short: 'Aulas', label: 'Minhas aulas', icon: 'calendar', roles: ['Aluno', 'Professor'], load: L('lessons') },
  { id: 'materiais', label: 'Materiais e atividades', icon: 'book', roles: ['Aluno', 'Professor'], load: L('materials') },
  { id: 'mentoria', label: 'Mentoria e chat', icon: 'chat', roles: ['Aluno', 'Professor'], load: L('mentoring') },
  { id: 'acompanhamentos', label: 'Acompanhamentos', icon: 'chart', roles: ['Aluno', 'Professor'], load: L('followups') },
  { id: 'tutoria', short: 'TutorIA', label: 'TutorIA', icon: 'spark', roles: ['Aluno'], load: L('tutor') },
  { id: 'planos', label: 'Planos', icon: 'layers', roles: ['Aluno'], load: L('plans') },
  { id: 'gravacoes', label: 'Gravações', icon: 'play', roles: ['Aluno'], load: L('recordings') },
  { id: 'financeiro', label: 'Financeiro', icon: 'wallet', roles: ['Aluno', 'Professor', 'Responsavel'], load: L('finance') },
  { id: 'profissional', label: 'Perfil profissional', icon: 'id', roles: ['Professor'], load: L('teacherProfile') },
  { id: 'disponibilidade', short: 'Horários', label: 'Disponibilidade', icon: 'clock', roles: ['Professor'], load: L('availability') },
  { id: 'analise', short: 'Análise', label: 'Análise de professores', icon: 'shield', roles: ['Administrador'], load: L('adminReview') },
  { id: 'indicadores', short: 'Indicadores', label: 'Indicadores', icon: 'chart', roles: ['Administrador'], load: L('adminAnalytics') },
  { id: 'faq-admin', short: 'FAQ', label: 'Gerenciar perguntas', icon: 'help', roles: ['Administrador'], load: L('adminFaq') },
  { id: 'notificacoes', short: 'Avisos', label: 'Notificações', icon: 'bell', roles: ['Aluno', 'Professor', 'Responsavel', 'Administrador'], load: L('notifications') },
  { id: 'conta', label: 'Minha conta', icon: 'user', roles: ['Aluno', 'Professor', 'Responsavel', 'Administrador'], load: L('account') },
  { id: 'ajuda', short: 'Dúvidas', label: 'Perguntas frequentes', icon: 'help', roles: ['Aluno', 'Professor', 'Responsavel', 'Administrador'], load: L('help', 'appModule') }
];

// Atalhos da barra inferior no celular (Manual PIM IV: menu inferior); o restante fica em "Menu".
export const SHORTCUTS = {
  Aluno: ['inicio', 'professores', 'aulas', 'tutoria'],
  Professor: ['inicio', 'aulas', 'alunos', 'disponibilidade'],
  'Responsavel': ['inicio', 'vinculados', 'financeiro', 'ajuda'],
  Administrador: ['inicio', 'analise', 'indicadores', 'faq-admin']
};

export const modulesFor = (role) => MODULES.filter(m => m.roles.includes(role));
export const findModule = (id, role) => MODULES.find(m => m.id === id && m.roles.includes(role));
