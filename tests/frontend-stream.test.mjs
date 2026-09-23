import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';
const source = await readFile(new URL('../TutorClass.API/wwwroot/api.js', import.meta.url), 'utf8');
const makeClient = fetch => {
  const context = vm.createContext({fetch, URL, AbortController, DOMException, TypeError, TextDecoder, setTimeout, clearTimeout});
  vm.runInContext(source, context); return context.TutorClassHttp;
};
const encode = new TextEncoder();
test('trecho aparece antes do complete, com UTF-8 dividido e JWT somente na API', async () => {
  let writer, calls = 0;
  const body = new ReadableStream({start(c){writer=c;}});
  const api = makeClient(async (url, options) => {
    calls++; assert.equal(url, 'https://api.example.test/api/tutoria/chat/stream');
    assert.equal(options.headers.Authorization, 'Bearer test-token');
    assert.deepEqual(JSON.parse(options.body), {mensagem:'Olá'});
    return new Response(body, {headers:{'content-type':'text/event-stream'}});
  });
  let first; const firstDelta = new Promise(resolve=>first=resolve);
  const task = api.stream('https://api.example.test','/api/tutoria/chat/stream', {
    token:'test-token',body:{mensagem:'Olá'},onDelta:first});
  const bytes = encode.encode('event: delta\r\ndata: {"text":"ação π"}\r\n\r\n');
  for(const byte of bytes)writer.enqueue(Uint8Array.of(byte));
  assert.equal(await firstDelta, 'ação π');
  let completed = false;task.then(()=>completed=true);
  await new Promise(resolve=>setImmediate(resolve));assert.equal(completed,false);
  writer.enqueue(encode.encode('event: complete\ndata: {"resposta":"ação π","usadasHoje":1}\n\n'));
  assert.equal((await task).usadasHoje,1);assert.equal(calls,1);
});
test('EOF sem complete é falha, preserva trecho e não repete a pergunta', async () => {
  let calls=0, text='';
  const api=makeClient(async()=>{calls++;return new Response('event: delta\ndata: {"text":"Parcial"}\n\n',{headers:{'content-type':'text/event-stream'}});});
  await assert.rejects(api.stream('https://api.example.test','/stream',{body:{},onDelta:t=>text+=t}),/interrompida/);
  assert.equal(text,'Parcial');assert.equal(calls,1);
});
test('401 mantém código HTTP e não tenta reenviar', async () => {
  const api=makeClient(async()=>Response.json({error:'Sessão expirada'},{status:401}));
  await assert.rejects(api.stream('https://api.example.test','/stream',{body:{}}),e=>e.status===401);
});
test('cancelar cancela a requisição e não vira uma resposta de sucesso', async () => {
  const api=makeClient(async(_,options)=>new Promise((resolve,reject)=>options.signal.addEventListener('abort',()=>reject(new DOMException('cancel','AbortError')))));
  const controller=new AbortController();
  const task=api.stream('https://api.example.test','/stream',{body:{},signal:controller.signal});controller.abort();
  await assert.rejects(task,e=>e.name==='AbortError');
});
