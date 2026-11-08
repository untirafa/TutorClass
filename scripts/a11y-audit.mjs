// Auditoria automatizada de acessibilidade (WCAG 2.2 A/AA) com axe-core + Playwright.
// Pré-requisito: npm install  (instala playwright e @axe-core/playwright) e npx playwright install chromium.
// Uso: node scripts/mock-api.mjs  (em outro terminal)  →  node scripts/a11y-audit.mjs
// Opcional: BASE_URL=http://localhost:5242 ; TUTORCLASS_PASSWORD para outra senha das contas de teste.
import { chromium } from 'playwright';
import AxeBuilder from '@axe-core/playwright';

const BASE = process.env.BASE_URL || 'http://localhost:5242';
const PASS = process.env.TUTORCLASS_PASSWORD || 'Senha@2026';
const TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'];
const PUBLIC = ['home', 'professores', 'materias', 'planos', 'ajuda', 'privacidade', 'acessibilidade', 'login', 'cadastro-aluno', 'cadastro-professor', 'cadastro-responsavel'];
const ROLES = {
  'aluno@teste.dev': ['inicio', 'professores', 'aulas', 'materiais', 'mentoria', 'acompanhamentos', 'tutoria', 'planos', 'gravacoes', 'financeiro', 'notificacoes', 'conta', 'ajuda'],
  'professor@teste.dev': ['inicio', 'alunos', 'aulas', 'materiais', 'mentoria', 'acompanhamentos', 'financeiro', 'profissional', 'disponibilidade', 'notificacoes'],
  'responsavel@teste.dev': ['inicio', 'vinculados', 'professores', 'financeiro', 'ajuda', 'notificacoes'],
  'admin@teste.dev': ['inicio', 'analise', 'indicadores', 'faq-admin']
};
const browser = await chromium.launch();
const failures = []; let checked = 0;

async function audit(page, label) {
  await page.waitForTimeout(400);
  const res = await new AxeBuilder({ page }).withTags(TAGS).analyze();
  checked++;
  for (const v of res.violations) failures.push({ label, id: v.id, impact: v.impact, help: v.help, nodes: v.nodes.slice(0, 3).map(n => n.target.join(' ')) });
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);
  if (overflow) failures.push({ label, id: 'reflow', impact: 'serious', help: 'Rolagem horizontal da página (1.4.10)', nodes: [] });
}
async function context(opts) { const c = await browser.newContext({ locale: 'pt-BR', ...opts }); return c; }
async function login(page, email) {
  await page.goto(`${BASE}/#login`); await page.fill('input[name=email]', email); await page.fill('input[name=senha]', PASS);
  await page.click('button[type=submit]'); await page.waitForURL(/#app\//);
}
const variants = [
  ['desktop', { viewport: { width: 1280, height: 900 } }],
  ['320px', { viewport: { width: 320, height: 640 } }],
  ['escuro', { viewport: { width: 1280, height: 900 }, colorScheme: 'dark' }],
  ['alto-contraste', { viewport: { width: 1280, height: 900 }, prefs: { contrast: 'high' } }]
];
for (const [name, opts] of variants) {
  const { prefs, ...ctxOpts } = opts;
  const ctx = await context(ctxOpts);
  if (prefs) await ctx.addInitScript((p) => localStorage.setItem('tutorclass.prefs', JSON.stringify(p)), prefs);
  const page = await ctx.newPage();
  for (const r of PUBLIC) { await page.goto(`${BASE}/#${r}`); await page.waitForSelector('main h1, main h2'); await audit(page, `${name} · #${r}`); }
  if (name !== 'escuro' && name !== 'alto-contraste') {
    // painel de acessibilidade e janela de solicitação de aula
    await page.goto(`${BASE}/#home`); await page.click('text=Acessibilidade >> visible=true'); await page.waitForSelector('dialog[open]'); await audit(page, `${name} · painel de acessibilidade`);
  }
  for (const [email, mods] of Object.entries(ROLES)) {
    if (name !== 'desktop' && name !== '320px' && email !== 'aluno@teste.dev') continue;
    const p2 = await (await context({ ...ctxOpts })).newPage(); if (prefs) await p2.addInitScript((p) => localStorage.setItem('tutorclass.prefs', JSON.stringify(p)), prefs);
    await login(p2, email);
    for (const m of mods) { await p2.goto(`${BASE}/#app/${m}`); await p2.waitForSelector('main h1'); await audit(p2, `${name} · ${email.split('@')[0]} · #app/${m}`); }
    await p2.context().close();
  }
  await ctx.close();
}
await browser.close();
const byId = {};
for (const f of failures) (byId[f.id] ||= []).push(f);
console.log(`Páginas/estados verificados: ${checked}`);
if (!failures.length) console.log('Nenhuma violação encontrada pelo axe-core (WCAG 2.2 A/AA + boas práticas).');
for (const [id, list] of Object.entries(byId)) { console.log(`\n✖ ${id} (${list[0].impact}) — ${list[0].help} — ${list.length} ocorrência(s)`); for (const f of list.slice(0, 4)) console.log(`   ${f.label}: ${f.nodes.join(' | ')}`); }
process.exitCode = failures.length ? 1 : 0;
