import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';
import * as http from '../TutorClass.API/wwwroot/js/core/http.js';

// Executa o módulo real trocando apenas o fetch global.
globalThis.location = { protocol: 'http:', origin: 'http://localhost:5242' };
function client(fetch) {
  return {
    normalizeBaseUrl: http.normalizeBaseUrl,
    request: (...a) => { globalThis.fetch = fetch; return http.request(...a); },
  };
}
const base = 'https://api.example.test';
test('cadastro serializa somente o payload e usa JSON, sem Bearer', async () => {
  const body = {nome:'Aluno Teste', email:'aluno@example.test', senha:'synthetic-test-value', dataNascimento:null};
  const api = client(async (url, options) => {
    assert.equal(url, base + '/api/auth/registrar/aluno');
    assert.equal(options.method, 'POST');
    assert.equal(options.headers['Content-Type'], 'application/json');
    assert.equal(options.headers.Authorization, undefined);
    assert.deepEqual(JSON.parse(options.body), body);
    return Response.json({id:'student-id', perfil:'Aluno'}, {status:201});
  });
  assert.equal((await api.request(base + '/', '/api/auth/registrar/aluno', {method:'POST', body})).perfil, 'Aluno');
});
test('me envia Authorization Bearer e interpreta o JSON', async () => {
  const api = client(async (url, options) => {
    assert.equal(url, base + '/api/auth/me');
    assert.equal(options.headers.Authorization, 'Bearer synthetic-token');
    assert.equal(options.body, undefined);
    return Response.json({id:'student-id'});
  });
  assert.equal((await api.request(base, '/api/auth/me', {token:'synthetic-token'})).id, 'student-id');
});
for (const [label, body, expected] of [
  ['erro de dominio', {error:'Email ja cadastrado.'}, 'Email ja cadastrado.'],
  ['validacao', {title:'Invalid', errors:{Email:['E-mail inválido.'], Senha:['Senha curta.']}}, 'E-mail inválido. Senha curta.'],
  ['ProblemDetails', {title:'Falha', detail:'Conta bloqueada.'}, 'Conta bloqueada.'],
  ['message', {message:'Tente novamente.'}, 'Tente novamente.']
]) test('exibe ' + label + ' com status HTTP', async () => {
  await assert.rejects(client(async () => Response.json(body, {status:400})).request(base, '/api/auth/login'),
    error => error.status === 400 && error.message === expected);
});
test('erro HTML do proxy conserva HTTP e não renderiza markup', async () => {
  await assert.rejects(client(async () => new Response('<html>proxy error</html>', {status:502, headers:{'content-type':'text/html'}})).request(base, '/api/auth/login'),
    error => error.status === 502 && error.message.includes('502') && !error.message.includes('<html>'));
});
test('401 sem corpo continua identificável para encerrar sessão', async () => {
  await assert.rejects(client(async () => new Response(null, {status:401})).request(base, '/api/auth/me'),
    error => error.status === 401 && /sessão expirou/.test(error.message));
});
test('204 não exige JSON', async () => {
  assert.equal(await client(async () => new Response(null, {status:204})).request(base, '/api/test'), null);
});
test('200 HTML informa erro de configuração', async () => {
  await assert.rejects(client(async () => new Response('<html>site</html>')).request(base, '/api/auth/login'), /não é JSON/);
});
test('falha de rede informa API e origem para diagnóstico CORS', async () => {
  await assert.rejects(client(async () => {throw new TypeError('Failed to fetch');}).request(base, '/api/auth/login'),
    error => error.message.includes(base) && error.message.includes('http://localhost:5242'));
});
test('timeout não repete uma mutação', async () => {
  let calls = 0;
  const api = client((_url, {signal}) => new Promise((_resolve, reject) => {
    calls++; signal.addEventListener('abort', () => reject(new DOMException('Aborted', 'AbortError')));
  }));
  await assert.rejects(api.request(base, '/api/auth/registrar/aluno', {method:'POST', body:{}, timeout:5}), /Consulte o resultado/);
  assert.equal(calls, 1);
});
test('rejeita endereços incorretos e mixed content antes do fetch', () => {
  const api = client(() => {throw new Error('Não deveria fazer requisição');});
  for (const value of ['', 'COLE_AQUI_A_URL_PUBLICADA_DA_API', 'https://user:pass@api.test', base + '/swagger', base + '/api', base + '?token=x'])
    assert.throws(() => api.normalizeBaseUrl(value));
  assert.throws(() => api.normalizeBaseUrl('http://localhost:5242', 'https:'), /HTTPS/);
});


const configSource = await readFile(new URL('../TutorClass.API/wwwroot/config.js', import.meta.url), 'utf8');
for (const origin of ['https://tutorclass.onrender.com', 'https://custom.example.test', 'http://localhost:5242']) {
  test('configuração acompanha a origem ' + origin, () => {
    const context=vm.createContext({window:{location:{origin}}});
    vm.runInContext(configSource,context);
    assert.equal(context.window.TUTORCLASS_CONFIG.apiBaseUrl,origin);
  });
}

