import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';

const app = await readFile(new URL('../TutorClass.API/wwwroot/app.js',import.meta.url),'utf8');
const api = await readFile(new URL('../TutorClass.API/wwwroot/api.js',import.meta.url),'utf8');
const view = app.slice(app.indexOf('async function openChat('),app.indexOf('views.acompanhamentos='));
const run = app.slice(app.indexOf('async function run('),app.indexOf('\n',app.indexOf('async function run(')));
function element(tag,textContent='',className='') {
  return {tag,textContent,className,children:[],events:{},attributes:{},isConnected:true,
    append(...items){this.children.push(...items);},replaceChildren(...items){this.children=items;},
    setAttribute(k,v){this.attributes[k]=v;},removeAttribute(k){delete this.attributes[k];},
    addEventListener(k,fn){this.events[k]=fn;},reset(){this.elements.conteudo.value='';}};
}
test('erro HTTP aparece dentro do modal, não altera histórico e permite reformular',async()=>{
  const content=element('modal-content'),notices=[],sent=[];
  let reject=true,poll;
  const old={id:'old',remetenteUsuarioId:'teacher',remetenteNome:'Professora',conteudo:'Mensagem antiga',dataEnvioUtc:'2026-01-01T00:00:00Z'};
  const context=vm.createContext({URL,AbortController,setTimeout,clearTimeout,TypeError,
    state:{user:{id:'student'}},document:{visibilityState:'visible'},$:()=>({open:true}),
    student:()=>true,modal:()=>content,el:element,text:(s,c)=>element('p',s,c),
    button:(label,fn)=>Object.assign(element('button',label),{onclick:fn}),
    field:()=>null,formFields:()=>Object.assign(element('form'),{elements:{conteudo:{value:'professora vai se foder'}}}),
    formValues:f=>({conteudo:f.elements.conteudo.value}),
    append:(node,...items)=>node.append(...items),when:s=>s,
    list:(node,rows,render)=>node.replaceChildren(...rows.map(render)),
    errorText:e=>e.message,notice:s=>notices.push(s),
    setInterval:fn=>{poll=fn;return 1;},
    fetch:async(url,options)=>{
      if(options.method==='GET')return Response.json(url.includes('/mensagens')?{mensagens:[old],proximoBeforeUtc:null}:{somenteLeitura:false});
      if(url.endsWith('/lidas'))return Response.json({});
      const message=JSON.parse(options.body);sent.push(message);
      if(reject)return Response.json({codigo:'MENSAGEM_BLOQUEADA',categoria:'OFENSA_DIRECIONADA',error:'Mensagem não enviada. Reformule seu texto com respeito e sem ameaças para continuar a conversa.'},{status:400});
      return Response.json({...old,id:'new',remetenteUsuarioId:'student',remetenteNome:'Aluno',conteudo:message.conteudo},{status:201});
    }
  });
  vm.runInContext(api,context);
  vm.runInContext(`const get=p=>TutorClassHttp.request('https://local.example.test',p); const mutate=(p,b)=>TutorClassHttp.request('https://local.example.test',p,{method:'POST',body:b}); ${run}\n${view}`,context);
  await context.openChat({id:'link',professorNome:'Professora'});
  const form=content.children.find(n=>n.tag==='form'),log=content.children.find(n=>n.className==='chat-log');
  const error=form.children.find(n=>n.attributes.role==='alert'),send=form.children.find(n=>n.tag==='button');
  const submit=async()=>{form.events.submit({preventDefault(){}});while(send.disabled)await new Promise(r=>setImmediate(r));};
  await submit();assert.equal(error.hidden,false);assert.match(error.textContent,/Mensagem não enviada/);
  assert.equal(notices.length,0);assert.equal(log.children.length,1);assert.equal(sent.length,1);
  assert.equal(form.elements.conteudo.value,'professora vai se foder');
  await poll();assert.equal(error.hidden,false);
  reject=false;form.elements.conteudo.value='Preciso de ajuda com bullying';await submit();
  assert.equal(error.hidden,true);assert.equal(log.children.length,2);assert.equal(sent.length,2);
  assert.equal(form.elements.conteudo.value,'');assert.equal(notices.length,0);
});
