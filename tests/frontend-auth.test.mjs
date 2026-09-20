import {test} from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import vm from 'node:vm';
import {setImmediate} from 'node:timers/promises';

const sources = await Promise.all(['config.js','api.js','app.js'].map(file => readFile(new URL('../TutorClass.API/wwwroot/' + file, import.meta.url), 'utf8')));
function setup(fetch, hash = '#login') {
  const nodes = new Map();
  const element = () => ({
    value:'', hidden:false, disabled:false, children:[], events:{}, classList:{toggle(){}},
    addEventListener(event, listener){this.events[event]=listener;},
    setAttribute(){}, removeAttribute(){}, append(...items){this.children.push(...items);},
    replaceChildren(...items){this.children=items;}, querySelector(){return element();},
    close(){}, reset(){}, scrollIntoView(){}
  });
  const get = id => {if(!nodes.has(id))nodes.set(id,element());return nodes.get(id);};
  const fields = {nome:{value:'Aluno Teste'},email:{value:'student@example.test'},senha:{value:'synthetic-password'},
    perfil:{value:'Aluno'},dataNascimento:{value:''},biografia:{value:''},precoHora:{value:''}};
  get('auth-form').elements=fields;
  get('auth-role').value='Aluno';
  const location={hash,protocol:'http:',origin:'http://localhost:5242',port:'5242'};
  const context = vm.createContext({fetch, URL, URLSearchParams, AbortController, DOMException, TypeError,
    setTimeout,clearTimeout,clearInterval,setInterval,Intl,location,
    localStorage:{getItem(){return 'https://outdated.example.test';},setItem(){throw new Error('Não persistir credenciais');}},
    document:{getElementById:get,createElement:element,querySelector:element},
    FormData:class {constructor(){this.rows=Object.entries(fields).map(([key,input])=>[key,input.value]);} [Symbol.iterator](){return this.rows[Symbol.iterator]();}},
    requestAnimationFrame(){},window:{location,addEventListener(){},scrollTo(){}}
  });
  for(const source of sources)vm.runInContext(source,context);
  return {get,fields,location,context, async submit(){
    get('auth-form').events.submit({preventDefault(){}});
    while(get('auth-submit').disabled) await setImmediate();
  }};
}
test('cadastro do formulário usa mesma origem e ignora URL antiga', async () => {
  const calls=[];
  const app=setup(async(url,options)=>{
    calls.push({url,options});
    return Response.json({id:'student-id',perfil:'Aluno'},{status:201});
  },'#cadastro-aluno');
  await app.submit();
  assert.equal(calls.length,1);
  assert.equal(calls[0].url,'http://localhost:5242/api/auth/registrar/aluno');
  assert.deepEqual(JSON.parse(calls[0].options.body),{nome:'Aluno Teste',email:'student@example.test',senha:'synthetic-password',dataNascimento:null});
  assert.equal(app.location.hash,'login');
  assert.equal(app.get('auth-feedback').hidden,true);
});
test('cadastro de professor mantém preço numérico e campos próprios', async () => {
  let payload;
  const app=setup(async(url,options)=>{
    assert.ok(url.endsWith('/api/auth/registrar/professor'));
    payload=JSON.parse(options.body);
    return Response.json({id:'teacher-id'},{status:201});
  },'#cadastro-professor');
  app.fields.perfil.value='Professor';app.fields.biografia.value='Apresentação';app.fields.precoHora.value='80.50';
  await app.submit();
  assert.deepEqual(payload,{nome:'Aluno Teste',email:'student@example.test',senha:'synthetic-password',biografia:'Apresentação',precoHora:80.5});
});
for (const perfil of ['Aluno','Administrador']) test('login de ' + perfil + ' usa perfil da API e confirma me', async () => {
  const calls=[];
  const user={id:'user-id',nome:'Conta Teste',email:'student@example.test',perfil};
  const app=setup(async(url,options)=>{
    calls.push({url,options});
    return url.endsWith('/login') ? Response.json({accessToken:'synthetic-token',usuario:user}) : Response.json(user);
  });
  await app.submit();
  assert.equal(calls.length,2);
  assert.ok(calls[0].url.endsWith('/api/auth/login'));
  assert.equal(calls[0].options.headers.Authorization,undefined);
  assert.deepEqual(JSON.parse(calls[0].options.body),{email:'student@example.test',senha:'synthetic-password'});
  assert.ok(calls[1].url.endsWith('/api/auth/me'));
  assert.equal(calls[1].options.headers.Authorization,'Bearer synthetic-token');
  assert.equal(app.location.hash,'app/inicio');
  assert.equal(app.get('auth-role').value,perfil);
  assert.equal(vm.runInContext('state.user.perfil',app.context),perfil);
});
test('erro real aparece no formulário e não abre área autenticada', async () => {
  const app=setup(async()=>Response.json({error:'Email ou senha invalidos.'},{status:400}));
  await app.submit();
  assert.equal(app.get('auth-feedback').hidden,false);
  assert.equal(app.get('auth-feedback').textContent,'Email ou senha invalidos.');
  assert.equal(app.location.hash,'#login');
  assert.equal(vm.runInContext('state.token',app.context),null);
});
test('falha em me descarta token e não mantém sessão parcial', async () => {
  const app=setup(async url=>url.endsWith('/login')
    ?Response.json({accessToken:'synthetic-token'})
    :Response.json({error:'Usuario autenticado nao encontrado.'},{status:400}));
  await app.submit();
  assert.equal(vm.runInContext('state.token',app.context),null);
  assert.equal(vm.runInContext('state.user',app.context),null);
  assert.equal(app.get('auth-feedback').textContent,'Usuario autenticado nao encontrado.');
});
test('401 em me encerra sessão e mostra erro', async () => {
  const app=setup(async url=>url.endsWith('/login')?Response.json({accessToken:'synthetic-token'}):new Response(null,{status:401}));
  await app.submit();
  assert.equal(vm.runInContext('state.token',app.context),null);
  assert.equal(app.location.hash,'login');
  assert.match(app.get('toast').textContent,/sessão expirou/);
});
test('resposta de login sem token não autentica', async () => {
  const app=setup(async()=>Response.json({usuario:{perfil:'Aluno'}}));
  await app.submit();
  assert.equal(vm.runInContext('state.token',app.context),null);
  assert.match(app.get('auth-feedback').textContent,/token de acesso válido/);
});

