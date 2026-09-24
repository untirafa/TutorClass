import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';

const source = await readFile(new URL('../TutorClass.API/wwwroot/app.js', import.meta.url), 'utf8');
const transport = await readFile(new URL('../TutorClass.API/wwwroot/api.js', import.meta.url), 'utf8');
// Execute the actual existing view with a small DOM adapter, not a copy of its decisions.
const view = source.slice(source.indexOf('views.tutoria='), source.indexOf('views.gravacoes='));
const status = {possuiAcesso:true, limiteAtingido:false, restantesHoje:7};
function element(tag, text = '', className = '') {
  return {tag, textContent:text, className, children:[], events:{}, isConnected:true,
    append(...items){this.children.push(...items);}, replaceChildren(...items){this.children=items;},
    setAttribute(){}, addEventListener(name,fn){this.events[name]=fn;},
    querySelector(selector){return this.children.find(n=>n.className===selector.slice(1));},
    reset(){this.elements.mensagem.value='';}};
}
async function setup(frames) {
  const state={base:'https://api.example.test',token:'fake',epoch:1,tutorHistory:[]};
  const host=element('host'), notices=[]; let calls=0;
  const context=vm.createContext({views:{},state,AbortController,DOMException,URL,TextDecoder,
    setTimeout,clearTimeout,location:{protocol:'https:'},
    fetch:async()=>{calls++; return new Response(frames,{headers:{'content-type':'text/event-stream'}});},
    get:async()=>status,el:element,text:(s,c)=>element('p',s,c),
    field:()=>null,formFields:()=>Object.assign(element('form'),{elements:{mensagem:{value:'oi'}}}),
    button:(label,fn)=>{const b=element('button',label); b.events.click=fn; return b;},
    link:()=>element('a'),notice:message=>notices.push(message),
    list:(node,items,render)=>node.replaceChildren(...items.map(render)),
    TutorClassText:{render:(node,text)=>{node.textContent=text;}},
    run:async(button,fn)=>{button.disabled=true;try{await fn();}catch(e){notices.push(e.message);}finally{button.disabled=false;}}
  });
  vm.runInContext(transport,context);vm.runInContext(view,context);
  await context.views.tutoria(host);
  const form=host.children.find(n=>n.tag==='form');
  return {state,host,notices,calls:()=>calls,submit:()=>form.events.submit({preventDefault(){}})};
}
const complete = data => `event: complete\ndata: ${JSON.stringify({...status,...data})}\n\n`;
for(const motivo of ['MENSAGEM_OFENSIVA','DADOS_PESSOAIS','AMEACA_OU_ASSEDIO','TENTATIVA_ACESSO_INDEVIDO','ERRO_MODERACAO']) {
  test(`${motivo}: aviso local sem bolha da IA, duplicação ou bloqueio da reformulação`, async()=>{
    const app=await setup(complete({motivo,mensagem:'Aviso local seguro',resposta:null}));
    await app.submit();
    assert.equal(app.state.tutorHistory.length,1);assert.equal(app.state.tutorHistory[0].role,'Você');
    assert.equal(app.calls(),1);assert.match(app.notices[0],/^TutorClass: Aviso local seguro$/);
    assert.equal(app.host.children.find(n=>n.tag==='form').children[0].disabled,false);
  });
}
test('timeout/503 traduzidos pela API não viram resposta incompleta sem delta',async()=>{
  const app=await setup(complete({motivo:'ERRO_TUTORIA',possuiAcesso:false,resposta:null,
    mensagem:'A TutorIA está temporariamente indisponível. Tente novamente em alguns instantes.'}));
  await app.submit();
  assert.equal(app.state.tutorHistory.length,1);assert.equal(app.calls(),1);
  assert.match(app.notices[0],/temporariamente indisponível/);
  assert.equal(app.host.children.at(-1).hidden,false);
});
for(const ending of [complete({motivo:'ERRO_TUTORIA',resposta:null,mensagem:'Indisponível'}),'']) {
  test(`falha com texto parcial preserva texto e sinaliza interrupção (${ending?'complete':'EOF'})`,async()=>{
    const app=await setup('event: delta\ndata: {"text":"Texto parcial"}\n\n'+ending);
    await app.submit();
    assert.equal(app.state.tutorHistory.length,2);
    assert.equal(app.state.tutorHistory[1].text,'Texto parcial');
    assert.equal(app.state.tutorHistory[1].interrupted,true);assert.equal(app.calls(),1);
  });
}
test('EOF sem texto remove a bolha vazia',async()=>{
  const app=await setup('');await app.submit();
  assert.equal(app.state.tutorHistory.length,1);assert.equal(app.calls(),1);
});
test('sucesso com delta e complete mantém apenas uma resposta',async()=>{
  const app=await setup('event: delta\ndata: {"text":"Olá"}\n\n'+complete({resposta:'Olá!',usadasHoje:1}));
  await app.submit();assert.equal(app.state.tutorHistory.length,2);
  assert.equal(app.state.tutorHistory[1].text,'Olá!');assert.equal(app.state.tutorHistory[1].interrupted,undefined);
  assert.equal(app.notices.length,0);assert.equal(app.calls(),1);
});
