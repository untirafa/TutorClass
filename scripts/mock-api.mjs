// API simulada para pré-visualizar o front-end SEM .NET/banco: serve wwwroot e responde /api/* com dados em memória.
// Uso: node scripts/mock-api.mjs   (PORT=5242 por padrão).  Não é a API real: serve só para ver e testar telas.
// Contas de teste (senha Senha@2026): aluno@teste.dev · professor@teste.dev · responsavel@teste.dev · admin@teste.dev · menor@teste.dev (aluno menor de idade)
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join, normalize } from 'node:path';
import { fileURLToPath } from 'node:url';
import { randomUUID } from 'node:crypto';

const ROOT = fileURLToPath(new URL('../TutorClass.API/wwwroot/', import.meta.url));
const MIME = { '.html': 'text/html', '.css': 'text/css', '.js': 'text/javascript', '.svg': 'image/svg+xml', '.webp': 'image/webp', '.json': 'application/json' };
const id = () => randomUUID();
const now = Date.now(), day = 86400000;
const iso = (ms) => new Date(ms).toISOString();

const subjects = ['Matemática', 'Português', 'Redação', 'Física', 'Química', 'Biologia', 'História', 'Geografia'].map(nome => ({ id: id(), nome }));
const S = (n) => subjects.find(s => s.nome === n);
const users = [
  { id: id(), nome: 'Mariana Souza', email: 'aluno@teste.dev', perfil: 'Aluno', dataNascimento: '2000-03-15' },
  { id: id(), nome: 'Beatriz Lima', email: 'professor@teste.dev', perfil: 'Professor' },
  { id: id(), nome: 'Carlos Souza', email: 'responsavel@teste.dev', perfil: 'Responsavel' },
  { id: id(), nome: 'Admin TutorClass', email: 'admin@teste.dev', perfil: 'Administrador' },
  { id: id(), nome: 'Pedro Souza', email: 'menor@teste.dev', perfil: 'Aluno', dataNascimento: '2014-05-10' }
];
const [aluno, prof, resp, admin] = users;
const teachers = [
  { id: prof.id, nome: prof.nome, biografia: 'Professora de Letras com 6 anos de experiência em redação para vestibular e ENEM.', precoHora: 80, nivelReputacao: 'Experiente', mediaAvaliacoes: 4.8, quantidadeAvaliacoes: 2, aulasConcluidas: 14, ofereceMentoria: true, limiteMentorados: 2, textoAvaliacoes: '4,8 de 5 (2 avaliações)', materias: ['Português', 'Redação'] },
  { id: id(), nome: 'Rafael Menezes', biografia: 'Engenheiro, ensina Matemática e Física com foco em resolução de problemas.', precoHora: 110, nivelReputacao: 'Destaque', mediaAvaliacoes: 4.9, quantidadeAvaliacoes: 1, aulasConcluidas: 30, ofereceMentoria: false, limiteMentorados: 0, textoAvaliacoes: '4,9 de 5 (1 avaliação)', materias: ['Matemática', 'Física'] },
  { id: id(), nome: 'Ana Paula Ribeiro', biografia: 'Bióloga e química, prepara para vestibulares de medicina.', precoHora: 95, nivelReputacao: 'Verificado', mediaAvaliacoes: null, quantidadeAvaliacoes: 0, aulasConcluidas: 3, ofereceMentoria: true, limiteMentorados: 3, textoAvaliacoes: 'Ainda sem avaliações', materias: ['Biologia', 'Química'] },
  { id: id(), nome: 'Lucas Prado', biografia: 'Historiador. Aulas dinâmicas de História e Geografia.', precoHora: 60, nivelReputacao: 'Novo', mediaAvaliacoes: null, quantidadeAvaliacoes: 0, aulasConcluidas: 0, ofereceMentoria: false, limiteMentorados: 0, textoAvaliacoes: 'Ainda sem avaliações', materias: ['História', 'Geografia'] }
];
const quals = [{ id: id(), professorId: prof.id, tipo: 'Graduacao', titulo: 'Licenciatura em Letras', instituicao: 'USP', descricao: null, dataConclusao: '2020-12-15', documentoComprovacao: 'diploma.pdf', statusVerificacao: 'Verificada', dataVerificacaoUtc: iso(now - 30 * day) }];
const avail = [{ id: id(), diaSemana: 'Monday', inicio: '15:00:00', fim: '19:00:00' }, { id: id(), diaSemana: 'Wednesday', inicio: '15:00:00', fim: '19:00:00' }];
const reviews = [
  { id: id(), aulaId: id(), alunoId: aluno.id, aluno: 'Mariana Souza', professorId: prof.id, nota: 5, comentario: 'Explica redação de um jeito muito claro.', createdAtUtc: iso(now - 6 * day) },
  { id: id(), aulaId: id(), alunoId: id(), aluno: 'João Pedro', professorId: prof.id, nota: 4, comentario: null, createdAtUtc: iso(now - 20 * day) },
  { id: id(), aulaId: id(), alunoId: id(), aluno: 'Clara M.', professorId: teachers[1].id, nota: 5, comentario: 'Física finalmente fez sentido.', createdAtUtc: iso(now - 3 * day) }
];
const lessons = [
  { id: id(), professor: { id: prof.id, nome: prof.nome }, aluno: { id: aluno.id, nome: aluno.nome }, solicitadaPorUsuarioId: aluno.id, materia: { id: S('Redação').id, nome: 'Redação' }, inicio: iso(now + 2 * day), duracaoMinutos: 60, precoAcordado: 80, status: 'Confirmada' },
  { id: id(), professor: { id: prof.id, nome: prof.nome }, aluno: { id: aluno.id, nome: aluno.nome }, solicitadaPorUsuarioId: aluno.id, materia: { id: S('Português').id, nome: 'Português' }, inicio: iso(now - 5 * day), duracaoMinutos: 90, precoAcordado: 120, status: 'Concluida' },
  { id: id(), professor: { id: prof.id, nome: prof.nome }, aluno: { id: aluno.id, nome: aluno.nome }, solicitadaPorUsuarioId: aluno.id, materia: { id: S('Português').id, nome: 'Português' }, inicio: iso(now + 5 * day), duracaoMinutos: 60, precoAcordado: 80, status: 'Solicitada' }
];
const summary = (l) => ({ id: l.id, professor: l.professor.nome, aluno: l.aluno.nome, solicitadaPorUsuarioId: l.solicitadaPorUsuarioId, materia: l.materia.nome, inicio: l.inicio, duracaoMinutos: l.duracaoMinutos, precoAcordado: l.precoAcordado, status: l.status });
const mentoria = [{ id: id(), alunoId: aluno.id, alunoNome: aluno.nome, professorId: prof.id, professorNome: prof.nome, slot: 1, ativo: true, dataInicioUtc: iso(now - 12 * day), dataFimUtc: null, dataUltimaTrocaUtc: null, motivoEncerramento: null }];
const messages = [{ id: id(), vinculoMentoriaId: mentoria[0].id, remetenteUsuarioId: prof.id, remetenteNome: prof.nome, conteudo: 'Oi, Mariana! Vamos revisar a estrutura da dissertação na próxima aula.', dataEnvioUtc: iso(now - day), lida: true }];
const materiais = [{ id: id(), professorId: prof.id, professorNome: prof.nome, alunoId: aluno.id, alunoNome: aluno.nome, materiaId: S('Redação').id, materiaNome: 'Redação', aulaId: null, titulo: 'Proposta de redação — ENEM 2025', descricao: 'Escreva 30 linhas e envie na próxima aula.', tipo: 'Exercicio', nomeArquivo: 'proposta.pdf', urlArquivo: 'https://example.com/proposta.pdf', createdAtUtc: iso(now - 2 * day) }];
const faq = [
  { id: id(), pergunta: 'Como solicito uma aula?', resposta: 'Abra o perfil do professor, escolha matéria, data e horário e clique em “Solicitar aula”.', categoria: 'Aulas', palavrasChave: 'aula agendar', ativo: true, ordemExibicao: 1, textoAcao: 'Buscar professores', rotaAcao: 'professores' },
  { id: id(), pergunta: 'O pagamento é real?', resposta: 'Nesta versão acadêmica o pagamento é simulado e nenhum dado de cartão é solicitado.', categoria: 'Pagamentos', palavrasChave: 'pagamento cartão pix', ativo: true, ordemExibicao: 2 },
  { id: id(), pergunta: 'O que o plano Mentoria inclui?', resposta: 'TutorIA sem limite diário do plano, até 2 mentores, chat e gravações elegíveis.', categoria: 'Planos', palavrasChave: 'plano mentoria', ativo: true, ordemExibicao: 3 }
];
const notifs = [{ id: id(), tipo: 'AulaConfirmada', titulo: 'Aula confirmada', mensagem: 'Sua aula de Redação foi confirmada pela professora Beatriz.', lida: false, dataCriacaoUtc: iso(now - 3600000), rotaAcao: '/aulas', prioridade: 'Importante' }];
const dependents = [{ id: aluno.id, nome: aluno.nome, email: aluno.email, dataNascimento: '2009-04-12', parentesco: 'Filha' }];
const candidaturas = [{ id: id(), professorId: teachers[3].id, professorNome: 'Lucas Prado', professorEmail: 'lucas@teste.dev', status: 'Pendente', mensagem: 'Sou historiador com 4 anos de experiência.', motivo: null, administradorAnaliseId: null, dataAnaliseUtc: null, createdAtUtc: iso(now - day) }];
let plan = { planoAtual: 'Gratuito', assinaturaAtiva: false, possuiIaEducacional: false, limiteMensagensIaDia: null, possuiMentoria: false, possuiGravacoesElegiveis: false, jaUsouPromocaoPrimeiraAssinatura: false };
// MOCK_PLAN=Mentoria inicia o aluno já assinante (útil para auditar telas dos planos pagos).
if (process.env.MOCK_PLAN === 'Mentoria') plan = { ...plan, planoAtual: 'Mentoria', assinaturaAtiva: true, possuiIaEducacional: true, possuiMentoria: true, possuiGravacoesElegiveis: true };
let iaUsed = 0;
const sessions = new Map();
const reports = []; // denúncias da sessão de demonstração

const send = (res, status, data, type = 'application/json') => { res.writeHead(status, { 'Content-Type': type + '; charset=utf-8', 'Cache-Control': 'no-store' }); res.end(data == null ? '' : typeof data === 'string' ? data : JSON.stringify(data)); };
const bad = (res, error, status = 400) => send(res, status, { error });
const body = (req) => new Promise(r => { let s = ''; req.on('data', c => s += c); req.on('end', () => { try { r(s ? JSON.parse(s) : {}); } catch { r({}); } }); });

async function api(req, res, url) {
  const p = url.pathname, m = req.method, q = url.searchParams, b = ['POST', 'PUT'].includes(m) ? await body(req) : {};
  const token = (req.headers.authorization || '').replace('Bearer ', ''); const me = sessions.get(token);
  const need = (...roles) => { if (!me) { send(res, 401, null); return false; } if (roles.length && !roles.includes(me.perfil)) { send(res, 403, { error: 'Seu perfil não tem permissão para esta ação.' }); return false; } return true; };
  const seg = p.split('/').filter(Boolean); // ['api', ...]
  if (p === '/health') return send(res, 200, { status: 'Healthy', service: 'TutorClass.API', database: { status: 'Healthy', provider: 'Mock' } });
  // --- auth
  if (p === '/api/auth/login' && m === 'POST') {
    const u = users.find(x => x.email === b.email); if (!u || b.senha !== 'Senha@2026') return bad(res, 'Email ou senha invalidos.');
    const t = id(); sessions.set(t, u); return send(res, 200, { accessToken: t, expiresAtUtc: iso(now + day), usuario: u });
  }
  if (p === '/api/auth/me') { if (!need()) return; return send(res, 200, me); }
  if (p.startsWith('/api/auth/registrar/') && m === 'POST') {
    if (users.some(u => u.email === b.email)) return bad(res, 'Email ja cadastrado.');
    if (!b.senha || b.senha.length < 8 || !/\p{L}/u.test(b.senha) || !/\d/.test(b.senha) || !/[^\p{L}\d\s]/u.test(b.senha)) return send(res, 400, { title: 'Validação', errors: { Senha: ['A senha deve ter ao menos 8 caracteres.'] } });
    const perfil = { aluno: 'Aluno', professor: 'Professor', responsavel: 'Responsavel' }[seg[3]];
    const u = { id: id(), nome: b.nome, email: b.email, perfil, ...(perfil === 'Aluno' ? { dataNascimento: b.dataNascimento ?? null } : {}) }; users.push(u); return send(res, 201, u);
  }
  // --- público
  if (p === '/api/materias') return send(res, 200, subjects);
  if (p === '/api/professores' && m === 'GET') { const mid = q.get('materiaId'); const nome = mid && S0(mid); return send(res, 200, teachers.filter(t => !nome || t.materias.includes(nome))); }
  let r;
  if ((r = p.match(/^\/api\/professores\/([^/]+)$/)) && m === 'GET' && r[1] !== 'me') { const t = teachers.find(x => x.id === r[1]); if (!t) return bad(res, 'Professor não encontrado.', 404); return send(res, 200, { ...t, qualificacoes: quals.filter(x => x.professorId === t.id), disponibilidades: t.id === prof.id ? avail : [] }); }
  if ((r = p.match(/^\/api\/professores\/([^/]+)\/avaliacoes$/))) return send(res, 200, reviews.filter(x => x.professorId === r[1]));
  if ((r = p.match(/^\/api\/professores\/([^/]+)\/horarios-livres$/))) { const d = q.get('data'); if (!d) return bad(res, 'Informe a data.'); return send(res, 200, [{ data: d, inicio: '15:00:00', fim: '16:00:00' }, { data: d, inicio: '17:00:00', fim: '18:00:00' }]); }
  if (p === '/api/faq' && m === 'GET') { const t = (q.get('termo') || '').toLowerCase(), c = q.get('categoria'); return send(res, 200, faq.filter(f => f.ativo && (!c || f.categoria === c) && (!t || (f.pergunta + f.resposta + f.palavrasChave).toLowerCase().includes(t)))); }
  if (p === '/api/faq/chat') { const t = (b.mensagem || '').toLowerCase(); const f = faq.find(x => t.split(/\s+/).some(w => w.length > 3 && (x.pergunta + x.palavrasChave).toLowerCase().includes(w))); return send(res, 200, f ? { encontrado: true, pergunta: f.pergunta, resposta: f.resposta, textoAcao: f.textoAcao, rotaAcao: f.rotaAcao } : { encontrado: false, mensagem: 'Não encontrei uma resposta. Tente descrever de outra forma.' }); }
  if (p === '/api/privacidade/documentos') return send(res, 200, { documentos: [{ tipo: 'Termos de uso', versao: '1.0', url: 'https://example.com/termos' }, { tipo: 'Política de privacidade', versao: '1.0', url: 'https://example.com/privacidade' }], contatoPrivacidade: 'privacidade@tutorclass.example', orientacoes: 'Tratamos dados conforme a LGPD.', aceiteObrigatorio: true });
  // --- autenticado
  if (!p.startsWith('/api/')) return send(res, 404, null);
  if (!need()) return;
  if (p === '/api/notificacoes/resumo') return send(res, 200, { naoLidas: notifs.filter(n => !n.lida).length });
  if (p === '/api/notificacoes' && m === 'GET') return send(res, 200, q.get('somenteNaoLidas') === 'true' ? notifs.filter(n => !n.lida) : notifs);
  if (p === '/api/notificacoes/marcar-todas-lidas') { notifs.forEach(n => n.lida = true); return send(res, 200, { quantidadeAlterada: 1 }); }
  if ((r = p.match(/^\/api\/notificacoes\/([^/]+)\/marcar-lida$/))) { notifs.find(n => n.id === r[1]).lida = true; return send(res, 200, {}); }
  if (p === '/api/aulas/minhas') return send(res, 200, lessons.map(summary));
  if (p === '/api/aulas' && m === 'POST') { const t = teachers.find(x => x.id === b.professorId); const l = { id: id(), professor: { id: t.id, nome: t.nome }, aluno: { id: me.id, nome: me.nome }, solicitadaPorUsuarioId: me.id, materia: subjects.find(s => s.id === b.materiaId), inicio: b.inicio, duracaoMinutos: b.duracaoMinutos, precoAcordado: t.precoHora * b.duracaoMinutos / 60, status: 'Solicitada' }; lessons.push(l); return send(res, 201, l); }
  if ((r = p.match(/^\/api\/aulas\/([^/]+)$/)) && m === 'GET') return send(res, 200, lessons.find(l => l.id === r[1]));
  if ((r = p.match(/^\/api\/aulas\/([^/]+)\/(confirmar|concluir|cancelar)$/))) { const l = lessons.find(x => x.id === r[1]); l.status = { confirmar: 'Confirmada', concluir: 'Concluida', cancelar: 'Cancelada' }[r[2]]; return send(res, 200, l); }
  if ((r = p.match(/^\/api\/aulas\/([^/]+)\/reagendar$/))) { const l = lessons.find(x => x.id === r[1]); l.inicio = b.inicio; return send(res, 200, l); }
  if (p.match(/^\/api\/aulas\/[^/]+\/reagendamentos$/)) return send(res, 200, []);
  if (p.match(/^\/api\/aulas\/[^/]+\/acompanhamento$/)) { if (m === 'PUT') return send(res, 200, b); return send(res, 200, { resumo: 'Boa evolução na estrutura do texto.', pontosFortes: 'Coesão', pontosMelhorar: 'Repertório', proximosPassos: 'Treinar propostas' }); }
  if (p.match(/^\/api\/aulas\/[^/]+\/avaliacao$/)) return send(res, 201, { ...b, id: id() });
  if (p.match(/^\/api\/aulas\/[^/]+\/gravacao$/)) return send(res, 200, { titulo: 'Gravação: Português', descricao: null, urlVideo: 'https://example.com/video', duracaoSegundos: 5400, disponivel: true, materia: 'Português', professor: prof.nome, aulaInicio: lessons[1].inicio });
  if (p === '/api/gravacoes/minhas') return send(res, 200, [{ titulo: 'Gravação: Português', descricao: 'Aula de revisão', urlVideo: 'https://example.com/video', duracaoSegundos: 5400, disponivel: true, materia: 'Português', professor: prof.nome, aulaInicio: lessons[1].inicio }]);
  if (p === '/api/planos/me') return send(res, 200, { alunoId: me.id, ...plan });
  if (p === '/api/planos/me/ia') return send(res, 200, { planoAtual: plan.planoAtual, permitido: plan.possuiIaEducacional, quantidadeUsadaHoje: iaUsed, limiteDiario: plan.limiteMensagensIaDia });
  if (p === '/api/planos/me/assinatura') { plan = { ...plan, planoAtual: b.plano, assinaturaAtiva: true, possuiIaEducacional: true, limiteMensagensIaDia: b.plano === 'Foco' ? 7 : null, possuiMentoria: b.plano === 'Mentoria', possuiGravacoesElegiveis: b.plano === 'Mentoria', jaUsouPromocaoPrimeiraAssinatura: true }; return send(res, 200, { plano: b.plano, valorCobradoPrimeiroMes: b.plano === 'Foco' ? 19.99 : 59.99, valorMensal: b.plano === 'Foco' ? 29.99 : 79.99 }); }
  if (p === '/api/planos/me/cancelar') { plan = { ...plan, planoAtual: 'Gratuito', assinaturaAtiva: false, possuiIaEducacional: false, limiteMensagensIaDia: null, possuiMentoria: false, possuiGravacoesElegiveis: false }; return send(res, 200, {}); }
  if (p === '/api/tutoria/status') return send(res, 200, { nome: me.nome, possuiAcesso: plan.possuiIaEducacional, plano: plan.planoAtual, limiteDiario: plan.limiteMensagensIaDia, usadasHoje: iaUsed, restantesHoje: plan.limiteMensagensIaDia == null ? null : plan.limiteMensagensIaDia - iaUsed, limiteAtingido: false, mensagem: plan.possuiIaEducacional ? 'TutorIA disponível.' : 'A TutorIA está disponível a partir do plano Foco.' });
  if (p === '/api/tutoria/chat/stream') {
    iaUsed++; const text = '### Dica de estudo\nPara resolver **equações do 2º grau**, use \\(\\frac{-b \\pm \\sqrt{\\Delta}}{2a}\\).\n- Identifique a, b e c\n- Calcule o discriminante';
    res.writeHead(200, { 'Content-Type': 'text/event-stream', 'Cache-Control': 'no-store' });
    const parts = text.match(/.{1,24}/gs); let i = 0;
    const t = setInterval(() => { if (i < parts.length) res.write(`event: delta\ndata: ${JSON.stringify({ text: parts[i++] })}\n\n`); else { clearInterval(t); res.write(`event: complete\ndata: ${JSON.stringify({ possuiAcesso: true, resposta: text, usadasHoje: iaUsed, restantesHoje: plan.limiteMensagensIaDia == null ? null : plan.limiteMensagensIaDia - iaUsed, limiteAtingido: false })}\n\n`); res.end(); } }, 60);
    return;
  }
  if (p === '/api/mentorias/me') { if (m === 'POST') return send(res, 201, mentoria[0]); return send(res, 200, mentoria); }
  if ((r = p.match(/^\/api\/mentorias\/([^/]+)\/chat$/))) return send(res, 200, { id: id(), vinculoMentoriaId: r[1], somenteLeitura: false });
  if ((r = p.match(/^\/api\/mentorias\/([^/]+)\/chat\/mensagens$/))) {
    if (m === 'POST') { if (/idiota|burro/i.test(b.conteudo)) return send(res, 400, { codigo: 'MENSAGEM_BLOQUEADA', error: 'Mensagem não enviada. Reformule seu texto com respeito para continuar a conversa.' }); const msg = { id: id(), vinculoMentoriaId: r[1], remetenteUsuarioId: me.id, remetenteNome: me.nome, conteudo: b.conteudo, dataEnvioUtc: iso(Date.now()), lida: false }; messages.push(msg); return send(res, 201, msg); }
    return send(res, 200, { mensagens: messages, proximoBeforeUtc: null });
  }
  if (p.endsWith('/chat/mensagens/lidas')) return send(res, 200, { quantidadeMarcada: 0 });
  if (p.startsWith('/api/mentorias/acompanhamentos')) return send(res, 200, []);
  if (p === '/api/materiais/me') return send(res, 200, materiais);
  if (p === '/api/materiais/destinatarios') return send(res, 200, [{ alunoId: aluno.id, alunoNome: aluno.nome, materiaId: S('Redação').id, materiaNome: 'Redação', mentoriaAtiva: true, aulas: [{ id: lessons[0].id, inicio: lessons[0].inicio, status: 'Confirmada' }] }]);
  if (p === '/api/materiais' && m === 'POST') { const x = { ...b, id: id(), professorNome: me.nome, alunoNome: aluno.nome, materiaNome: 'Redação', createdAtUtc: iso(Date.now()) }; materiais.push(x); return send(res, 201, x); }
  if ((r = p.match(/^\/api\/materiais\/([^/]+)$/))) { const x = materiais.find(k => k.id === r[1]); if (m === 'DELETE') { materiais.splice(materiais.indexOf(x), 1); return send(res, 204, null); } if (m === 'PUT') Object.assign(x, b); return send(res, 200, x); }
  if (p === '/api/financeiro/me/historico') return send(res, 200, [{ tipo: 'Aula', id: id(), valor: 120, descricao: 'Português – 90 min', dataUtc: iso(now - 5 * day), status: 'Pago' }]);
  if (p.match(/^\/api\/financeiro\/aulas\/[^/]+\/pagar$/)) return send(res, 200, { valorBruto: 80 });
  if (p === '/api/financeiro/professores/me/resumo') return send(res, 200, { professorId: me.id, ano: +q.get('ano'), mes: +q.get('mes'), valorBrutoAulas: 200, comissoesTutorClass: 30, valorLiquidoAulas: 170, adicionaisMentoria: 20, totalPrevisto: 190, itens: [{ tipo: 'Aula', id: id(), alunoNome: aluno.nome, valorBruto: 120, valorComissao: 18, valorLiquidoProfessor: 102, valorAdicionalMentoria: 0, dataUtc: iso(now - 5 * day), status: 'Pago' }] });
  if (p === '/api/financeiro/aulas/comissao') { const v = +q.get('valorBruto'); return send(res, 200, { valorBruto: v, percentualComissao: 15, valorComissao: v * .15, valorLiquidoProfessor: v * .85 }); }
  // professor
  if (p === '/api/professores/me/perfil') { if (m === 'PUT') { Object.assign(teachers[0], { biografia: b.biografia, precoHora: b.precoHora }); } return send(res, 200, { id: prof.id, nome: prof.nome, email: prof.email, status: 'Candidato', biografia: teachers[0].biografia, precoHora: teachers[0].precoHora, ofereceMentoria: true, limiteMentorados: 2, materias: [S('Português'), S('Redação')].map(x => ({ id: x.id, nome: x.nome })), qualificacoes: quals, disponibilidades: avail }); }
  if (p === '/api/professores/me/candidaturas') return send(res, 200, []);
  if (p === '/api/professores/me/disponibilidades') { if (m === 'POST') { const d = { id: id(), ...b }; avail.push(d); return send(res, 201, d); } return send(res, 200, avail); }
  if ((r = p.match(/^\/api\/professores\/me\/disponibilidades\/([^/]+)$/))) { const i = avail.findIndex(a => a.id === r[1]); if (m === 'DELETE') avail.splice(i, 1); else Object.assign(avail[i], b); return send(res, m === 'DELETE' ? 204 : 200, m === 'DELETE' ? null : avail[i]); }
  if (p === '/api/professores/me/mentoria') return send(res, 200, teachers[0]);
  if (p.match(/^\/api\/professores\/[^/]+\/(materias|candidaturas|qualificacoes)$/) && m === 'POST') return send(res, 201, id());
  if (p.match(/^\/api\/professores\/[^/]+\/qualificacoes$/)) return send(res, 200, quals);
  // responsável
  if (p === '/api/responsavel/alunos') return send(res, 200, dependents);
  if ((r = p.match(/^\/api\/responsavel\/alunos\/([^/]+)\/(aulas|plano|financeiro\/historico|mentoria|mentoria\/acompanhamentos)$/))) {
    if (r[2] === 'aulas') return m === 'POST' ? send(res, 201, lessons[2]) : send(res, 200, lessons.map(summary));
    if (r[2] === 'plano') return send(res, 200, { alunoId: aluno.id, ...plan });
    if (r[2].startsWith('financeiro')) return send(res, 200, [{ tipo: 'Aula', id: id(), valor: 120, descricao: 'Português – 90 min', dataUtc: iso(now - 5 * day), status: 'Pago' }]);
    if (r[2] === 'mentoria') return send(res, 200, mentoria.map(x => ({ ...x })));
    return send(res, 200, []);
  }
  if (p.match(/^\/api\/responsavel\/alunos\/[^/]+\/mentorias\/[^/]+\/chat\/mensagens$/)) return send(res, 200, { mensagens: messages, proximoBeforeUtc: null });
  if (p.match(/^\/api\/responsavel\/alunos\/[^/]+\/assinatura$/)) return send(res, 200, { plano: b.plano, valorCobradoPrimeiroMes: 19.99 });
  if (p.match(/^\/api\/responsavel\/alunos\/[^/]+\/aulas\/[^/]+\/(pagar|cancelar|reagendar|gravacao\/resumo)$/)) return send(res, 200, { valorBruto: 80, aulaConcluida: true, existeGravacao: true, gravacaoDisponivel: true, titulo: 'Gravação' });
  // admin
  if (p === '/api/admin/analytics/resumo') return send(res, 200, { totalAlunos: 128, totalProfessoresAprovados: 24, totalAulas: 540, aulasConcluidas: 470, aulasCanceladas: 30, mediaAvaliacoes: 4.7, mentoriasAtivas: 18, assinaturasPorPlano: [{ plano: 'Foco', quantidade: 40 }, { plano: 'Mentoria', quantidade: 18 }], materiaMaisProcurada: 'Matemática', professorMaisContratado: 'Rafael Menezes', receitaComissaoSimulada: 8120.5, repassesProfessoresSimulados: 45900, adicionaisMentoriaSimulados: 1200 });
  if (p === '/api/admin/analytics/materias') return send(res, 200, [{ materiaId: id(), nome: 'Matemática', quantidadeAulas: 180, percentual: 33.3, tendencia: 'Alta' }, { materiaId: id(), nome: 'Redação', quantidadeAulas: 120, percentual: 22.2, tendencia: 'Estável' }]);
  if (p === '/api/admin/analytics/demanda-materias') return send(res, 200, [{ materiaId: id(), nome: 'Matemática', aulasUltimos30Dias: 80, aulas30DiasAnteriores: 60, crescimentoPercentual: 33.3, quantidadeAlunosDistintos: 40, taxaConclusao: 91.2, demandaPrevista: 'Alta' }]);
  if (p === '/api/admin/candidaturas-professor') return send(res, 200, candidaturas);
  if ((r = p.match(/^\/api\/admin\/candidaturas-professor\/([^/]+)\/([^/]+)$/))) { const c = candidaturas.find(x => x.id === r[1]); c.status = { aprovar: 'Aprovada', reprovar: 'Reprovada', 'iniciar-analise': 'EmAnalise', 'solicitar-complementacao': 'AguardandoDocumentacao' }[r[2]]; c.motivo = b.motivo; return send(res, 200, null); }
  if (p.startsWith('/api/admin/qualificacoes/')) return send(res, 200, quals[0]);
  if (p === '/api/admin/faq' && m === 'POST') { const f = { ...b, id: id(), ativo: true }; faq.push(f); return send(res, 201, f); }
  if (m === 'DELETE' && (r = p.match(/^\/api\/admin\/faq\/([^/]+)$/))) { const i = faq.findIndex(x => x.id === r[1]); if (i < 0) return bad(res, 'FAQ nao encontrado.'); faq.splice(i, 1); res.writeHead(204); return res.end(); }
  if ((r = p.match(/^\/api\/admin\/faq\/([^/]+)(?:\/(ativar|desativar))?$/))) { const f = faq.find(x => x.id === r[1]); if (r[2]) f.ativo = r[2] === 'ativar'; else Object.assign(f, b); return send(res, 200, f); }
  // --- denúncias e moderação
  if (p === '/api/denuncias' && m === 'POST') {
    if (!need()) return;
    if (!['Professor', 'Avaliacao', 'Mensagem'].includes(b.tipo) || !b.alvoId) return bad(res, 'Conteudo denunciado e obrigatorio.');
    if (b.motivo === 'Outro' && (b.descricao || '').trim().length < 10) return bad(res, 'Para o motivo "Outro", descreva o ocorrido com pelo menos 10 caracteres.');
    if (reports.some(d => d.denuncianteId === me.id && d.tipo === b.tipo && d.alvoId === b.alvoId && ['Aberta', 'EmAnalise'].includes(d.status))) return bad(res, 'Voce ja denunciou este conteudo e a denuncia esta em analise.');
    const alvo = b.tipo === 'Professor' ? teachers.find(t => t.id === b.alvoId) : b.tipo === 'Avaliacao' ? reviews.find(x => x.id === b.alvoId) : messages.find(x => x.id === b.alvoId);
    if (!alvo) return bad(res, 'Conteudo denunciado nao encontrado.');
    const resumo = b.tipo === 'Professor' ? `Perfil do professor ${alvo.nome}` : b.tipo === 'Avaliacao' ? `Avaliacao ${alvo.nota}/5: ${alvo.comentario || '(sem comentario)'}` : `Mensagem na Mentoria: ${alvo.conteudo}`;
    const d = { id: id(), tipo: b.tipo, alvoId: b.alvoId, motivo: b.motivo, descricao: b.descricao || null, resumo, status: 'Aberta', sancao: 'Nenhuma', observacao: null, denuncianteId: me.id, denunciante: me.nome, alvoUsuarioId: alvo.remetenteUsuarioId || alvo.alunoId || alvo.id, alvoUsuario: alvo.remetenteNome || alvo.aluno || alvo.nome || 'Usuario', alvoPerfil: b.tipo === 'Professor' ? 'Professor' : b.tipo === 'Avaliacao' ? 'Aluno' : 'Professor', alvoAtivo: true, criadaEmUtc: iso(Date.now()), analisadaEmUtc: null, contaReativadaEmUtc: null };
    reports.push(d); return send(res, 201, d);
  }
  if (p === '/api/denuncias/minhas') { if (!need()) return; return send(res, 200, reports.filter(d => d.denuncianteId === me.id)); }
  if (p === '/api/admin/denuncias/resumo') { if (!need('Administrador')) return; const c = (st) => reports.filter(d => d.status === st).length; return send(res, 200, { abertas: c('Aberta'), emAnalise: c('EmAnalise'), procedentes: c('Procedente'), improcedentes: c('Improcedente') }); }
  if (p === '/api/admin/denuncias' && m === 'GET') { if (!need('Administrador')) return; const st = q.get('status'); return send(res, 200, reports.filter(d => !st || d.status === st).map(d => ({ ...d, denunciasContraAlvo: reports.filter(x => x.alvoUsuarioId === d.alvoUsuarioId).length }))); }
  if ((r = p.match(/^\/api\/admin\/denuncias\/([^/]+)\/(iniciar-analise|decidir|reativar-conta)$/)) && m === 'POST') {
    if (!need('Administrador')) return;
    const d = reports.find(x => x.id === r[1]); if (!d) return bad(res, 'Denuncia nao encontrada.');
    if (r[2] === 'iniciar-analise') { if (['Procedente', 'Improcedente'].includes(d.status)) return bad(res, 'Esta denuncia ja foi decidida.'); d.status = 'EmAnalise'; }
    else if (r[2] === 'decidir') {
      if (['Procedente', 'Improcedente'].includes(d.status)) return bad(res, 'Esta denuncia ja foi decidida.');
      if (!b.observacao || b.observacao.trim().length < 3) return bad(res, 'Informe a justificativa da decisao (pelo menos 3 caracteres).');
      if (!b.procedente && b.sancao !== 'Nenhuma') return bad(res, 'Denuncia improcedente nao pode ter sancao.');
      d.status = b.procedente ? 'Procedente' : 'Improcedente'; d.sancao = b.procedente ? b.sancao : 'Nenhuma'; d.observacao = b.observacao; d.analisadaEmUtc = iso(Date.now());
      if (d.sancao === 'Suspensao') reports.filter(x => x.alvoUsuarioId === d.alvoUsuarioId).forEach(x => x.alvoAtivo = false);
    } else { if (d.sancao !== 'Suspensao' || d.contaReativadaEmUtc) return bad(res, 'Somente denuncias com suspensao aplicada permitem reativar a conta.'); d.contaReativadaEmUtc = iso(Date.now()); reports.filter(x => x.alvoUsuarioId === d.alvoUsuarioId).forEach(x => x.alvoAtivo = true); }
    return send(res, 200, { ...d, denunciasContraAlvo: reports.filter(x => x.alvoUsuarioId === d.alvoUsuarioId).length });
  }
  return bad(res, 'Registro ou endpoint não encontrado.', 404);
}
function S0(mid) { return subjects.find(s => s.id === mid)?.nome; }

const port = Number(process.env.PORT || 5242);
createServer(async (req, res) => {
  const url = new URL(req.url, 'http://localhost');
  try {
    if (url.pathname.startsWith('/api/') || url.pathname === '/health') return await api(req, res, url);
    let path = normalize(decodeURIComponent(url.pathname)).replace(/^([/\\])+/, '');
    if (path === '' || path.endsWith('/')) path += 'index.html';
    if (path.startsWith('..')) return send(res, 404, null);
    const data = await readFile(join(ROOT, path));
    res.writeHead(200, { 'Content-Type': (MIME[extname(path)] || 'application/octet-stream') + (extname(path) === '.webp' ? '' : '; charset=utf-8'), 'Cache-Control': 'no-store' });
    res.end(data);
  } catch (e) { send(res, e.code === 'ENOENT' ? 404 : 500, null); }
}).listen(port, 'localhost', () => console.log(`Prévia com API simulada: http://localhost:${port}\nContas (senha Senha@2026): aluno@teste.dev · professor@teste.dev · responsavel@teste.dev · admin@teste.dev · menor@teste.dev (aluno de 12 anos)`));
