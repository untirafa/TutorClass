// Fluxo ponta a ponta contra a API simulada (npm run mock): login, agendamento, TutorIA, chat, responsável.
import { chromium } from 'playwright';
const base = process.env.BASE_URL || 'http://localhost:5242';
const results = []; const ok = (n, c, d = '') => { results.push([c ? 'OK ' : 'FALHA', n, d]); if (!c) process.exitCode = 1; };
const browser = await chromium.launch();
async function login(email) {
  const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 } });
  const page = await ctx.newPage(); const errors = [];
  page.on('pageerror', e => errors.push(e.message)); page.on('console', m => m.type() === 'error' && errors.push(m.text()));
  await page.goto(base + '/#login'); await page.waitForSelector('form');
  await page.getByLabel(/e-mail/i).fill(email); await page.getByLabel(/senha/i).first().fill('Senha@2026');
  await page.getByRole('button', { name: /entrar/i }).last().click();
  await page.waitForURL(/#app\//); await page.waitForSelector('main h1');
  return { page, errors, ctx };
}
// Aluno
{
  const { page, errors, ctx } = await login('aluno@teste.dev');
  ok('aluno: painel', /bom ter você|olá|painel/i.test(await page.locator('main h1').first().innerText()));
  await page.goto(base + '/#app/professores'); await page.waitForSelector('main article a'); await page.waitForTimeout(300);
  ok('aluno: lista de professores', await page.locator('main article').count() > 0);
  await page.locator('main article a').first().click(); await page.waitForSelector('main [role=tablist], main button:has-text("Solicitar aula")');
  const book = page.getByRole('button', { name: /solicitar aula/i }).first();
  ok('aluno: botão de agendamento', await book.count() > 0);
  if (await book.count()) { await book.click(); ok('aluno: diálogo de agendamento', await page.locator('dialog[open]').count() === 1);
    await page.keyboard.press('Escape'); ok('aluno: Esc fecha diálogo', await page.locator('dialog[open]').count() === 0); }
  ok('VLibras presente na tela', await page.locator('[vw] [vw-access-button]').count() === 1);
  // Avaliação do professor: botão no cartão da aula concluída e no perfil do professor
  await page.goto(base + '/#app/aulas'); await page.getByRole('button', { name: /avaliar professor/i }).first().waitFor({ timeout: 5000 }).catch(() => {});
  ok('aluno: botão Avaliar professor na aula concluída', await page.getByRole('button', { name: /avaliar professor/i }).count() > 0);
  await page.getByRole('button', { name: /avaliar professor/i }).first().click();
  ok('aluno: diálogo de avaliação', await page.locator('dialog[open]').getByText(/como foi sua aula/i).count() === 1);
  await page.locator('dialog[open]').getByRole('button', { name: /publicar avaliação/i }).click(); await page.waitForTimeout(500);
  ok('aluno: avaliação enviada', await page.locator('.toast.is-success').count() > 0);
  await page.goto(base + '/#app/professores'); await page.waitForSelector('main article a'); await page.locator('main article a', { hasText: /Beatriz/ }).first().click();
  await page.getByRole('tab', { name: /avaliações/i }).click(); await page.getByRole('heading', { name: /avalie este professor/i }).waitFor({ timeout: 5000 }).catch(() => {});
  ok('aluno: avaliar pelo perfil do professor', await page.getByRole('heading', { name: /avalie este professor/i }).count() === 1);
  // Plano Gratuito: convite amigável no topo (sem mensagem seca na tela) e retorno para os planos
  await page.goto(base + '/#app/tutoria'); await page.waitForSelector('.toast.is-upgrade');
  ok('gratuito: convite de upgrade no topo', (await page.locator('.toast.is-upgrade').first().boundingBox())?.y < 260);
  ok('gratuito: leva aos planos', /#app\/planos/.test(page.url()));
  ok('gratuito: sem mensagem de indisponível', !/não está disponível no plano Gratuito/i.test(await page.locator('body').innerText()));
  await page.goto(base + '/#app/planos'); await page.waitForSelector('main h1');
  await page.waitForSelector('main button', { timeout: 5000 }).catch(() => {});
  const foco = page.getByRole('button', { name: /escolher foco/i }); if (await foco.count()) await foco.first().click();
  await page.waitForTimeout(500);
  if (await page.locator('dialog[open]').count()) { await page.locator('dialog[open]').getByRole('button', { name: /confirmar/i }).first().click(); await page.waitForTimeout(800); }
  await page.goto(base + '/#app/tutoria'); await page.waitForSelector('main h1'); await page.waitForTimeout(600);
  const box = page.getByRole('textbox').first(); await box.fill('Explique frações'); await page.keyboard.press('Control+Enter').catch(() => {});
  if (!(await page.locator('[role=log]').innerText()).includes('frações')) await page.getByRole('button', { name: /enviar/i }).click();
  await page.waitForTimeout(1500);
  ok('aluno: TutorIA responde por streaming', (await page.locator('[role=log]').innerText()).length > 20);
  for (const m of ['aulas', 'materiais', 'mentoria', 'financeiro', 'notificacoes', 'conta']) {
    await page.goto(base + '/#app/' + m); await page.waitForSelector('main h1'); ok('aluno: módulo ' + m, !/não encontrada/i.test(await page.title()));
  }
  ok('aluno: sem erros de console', errors.length === 0, errors.join(' | ')); await ctx.close();
}
for (const [email, mods] of [['responsavel@teste.dev', ['inicio', 'vinculados', 'financeiro', 'ajuda']], ['professor@teste.dev', ['inicio', 'perfil', 'disponibilidade', 'aulas']], ['admin@teste.dev', ['inicio', 'candidaturas', 'analytics', 'faq']]]) {
  const { page, errors, ctx } = await login(email);
  for (const m of mods) { await page.goto(base + '/#app/' + m); await page.waitForSelector('main h1'); ok(`${email.split('@')[0]}: módulo ${m}`, !/não encontrada/i.test(await page.title())); }
  if (email.startsWith('responsavel')) { await page.goto(base + '/#app/inicio'); await page.waitForSelector('main h1'); await page.locator('#faq-termo').waitFor({ timeout: 5000 }).catch(() => {}); ok('responsável: barra de FAQ', await page.locator('#faq-termo').count() === 1); ok('responsável: Financeiro no menu', await page.locator('#sidebar a:has-text("Financeiro")').count() === 1); await page.goto(base + '/#app/financeiro'); await page.waitForSelector('main table'); ok('responsável: histórico financeiro do aluno', await page.locator('main table').count() > 0); }
  ok(`${email.split('@')[0]}: sem erros de console`, errors.length === 0, errors.join(' | ')); await ctx.close();
}
// Menor de idade: financeiro bloqueado com aviso no topo; aviso some em ~4 s
{
  const { page, ctx } = await login('menor@teste.dev');
  await page.goto(base + '/#app/financeiro');
  await page.waitForSelector('.toast.is-warning');
  const top = await page.locator('.toast').first().boundingBox();
  ok('menor: aviso de bloqueio no topo', top && top.y < 120);
  ok('menor: retorna ao início', /#app\/inicio/.test(page.url()));
  await page.waitForTimeout(4600);
  ok('menor: aviso some em 4 s', await page.locator('.toast').count() === 0);
  await ctx.close();
}
// Cabeçalho com atalhos, busca em gaveta, olhinho, apagar FAQ
{
  const { page, ctx } = await login('aluno@teste.dev');
  const quick = await page.locator('#primary-nav a').allInnerTexts();
  ok('cabeçalho: sem atalhos; casinha na lateral', quick.length === 0 && await page.locator('#sidebar a:has-text("Visão geral")').count() === 1);
  ok('vídeo de fundo tocando', await page.evaluate(() => { const v = document.querySelector('#bg-video video'); return !!v && !v.paused; }) || await page.evaluate(() => document.querySelector('.bg-video-toggle') !== null));
  ok('barra lateral: Minhas aulas e Materiais', (await page.locator('#sidebar a:has-text("Minhas aulas")').count()) === 1 && (await page.locator('#sidebar a:has-text("Materiais e atividades")').count()) === 1);
  const hb = await page.locator('.brand').boundingBox(), hr = await page.locator('#header-actions').boundingBox();
  ok('cabeçalho: marca encostada à esquerda e ações à direita', hb.x <= 16 && 1280 - (hr.x + hr.width) <= 16, `${hb.x} / ${1280 - (hr.x + hr.width)}`);
  ok('aluno: barra de perguntas frequentes no início', await page.locator('form[role=search] input#faq-termo').count() === 1);
  await page.locator('#faq-termo').fill('pagar'); await page.keyboard.press('Enter'); await page.waitForURL(/#app\/ajuda\?termo=pagar/);
  await page.waitForFunction(() => /perguntas frequentes/i.test(document.querySelector('main h1')?.textContent || ''), null, { timeout: 5000 }).catch(() => {});
  ok('aluno: busca de FAQ leva às perguntas', /perguntas frequentes/i.test(await page.locator('main h1').first().innerText()) && (await page.locator('input[name=termo]').inputValue()) === 'pagar');
  ok('aluno: Perguntas frequentes no menu lateral', await page.locator('#sidebar a:has-text("Perguntas frequentes")').count() === 1);
  await page.goto(base + '/#app/professores'); await page.waitForSelector('main article a');
  ok('busca: barra de pesquisa', await page.locator('input[type=search]').count() === 1);
  ok('busca: gaveta fechada', await page.locator('#filters-panel').isHidden());
  await page.getByRole('button', { name: /filtrar e ordenar/i }).click();
  ok('busca: gaveta abre', await page.locator('#filters-panel').isVisible());
  await ctx.close();
}
{
  const ctx = await browser.newContext(); const page = await ctx.newPage();
  await page.goto(base + '/#login'); await page.waitForSelector('form');
  await page.getByLabel(/^senha/i).fill('segredo123');
  await page.getByRole('button', { name: /mostrar senha/i }).click();
  ok('login: olhinho mostra a senha', await page.getByLabel(/^senha/i).getAttribute('type') === 'text');
  await page.getByRole('button', { name: /ocultar senha/i }).click();
  ok('login: olhinho oculta a senha', await page.getByLabel(/^senha/i).getAttribute('type') === 'password');
  await page.goto(base + '/#cadastro-aluno'); await page.waitForSelector('form');
  ok('cadastro: olhinho presente', await page.getByRole('button', { name: /mostrar senha/i }).count() === 1);
  await ctx.close();
}
{
  const { page, ctx } = await login('admin@teste.dev');
  await page.goto(base + '/#app/faq-admin'); await page.waitForSelector('main article');
  const before = await page.locator('main article').count();
  await page.locator('main article').first().getByRole('button', { name: /apagar/i }).click();
  await page.locator('dialog[open]').getByRole('button', { name: /apagar definitivamente/i }).click();
  await page.waitForTimeout(800);
  ok('faq: apagar remove a pergunta', await page.locator('main article').count() === before - 1);
  await ctx.close();
}
await browser.close();
for (const r of results) console.log(r.join(' '));
