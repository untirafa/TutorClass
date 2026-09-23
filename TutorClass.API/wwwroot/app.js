'use strict';
/* TutorClass — front-end sem dependências.
   HTML: estrutura pública e recipientes. CSS: aparência. JS: rotas, componentes e REST.
   O servidor continua sendo a autoridade para permissões, valores, quotas e regras.
   Nenhum JWT ou senha é persistido. Recarregar a página encerra a sessão do front-end. */
const $ = id => document.getElementById(id);
const store = {
  get(key) { try { return localStorage.getItem(key); } catch { return null; } },
  set(key, value) { try { localStorage.setItem(key, value); } catch { /* Navegação privada */ } }
};
// Overrides antigos em localStorage não podem redirecionar o site para outro banco.
// Uma troca manual é válida somente até recarregar a página.
const state = { base: window.TUTORCLASS_CONFIG?.apiBaseUrl || '', user:null, token:null, epoch:0, subjects:[], teachers:[], publicLoaded:false, chatTimer:null, tutorHistory:[] };
const student = () => state.user?.perfil === 'Aluno';
const teacher = () => state.user?.perfil === 'Professor';
const administrator = () => state.user?.perfil === 'Administrador';
const menuAllowed = menu => !menu[2] || menu[2].split('|').includes(state.user?.perfil);
const uid = () => encodeURIComponent(state.user.id);
const money = n => n == null ? 'Não informado' : new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'}).format(n);
const when = value => value ? new Intl.DateTimeFormat('pt-BR',{dateStyle:'short',timeStyle:'short',timeZone:'America/Sao_Paulo'}).format(new Date(value)) : '—';
const utc = value => { const d = new Date(value + 'Z'); if (!Number.isFinite(d.getTime()) || d <= new Date()) throw new Error('Escolha um horário futuro em UTC.'); return d.toISOString(); };
const plans = [
  {name:'Gratuito',price:0,promo:'Para começar a explorar.',features:['Contratação de aulas particulares','Materiais do seu professor','Sem mensalidade']},
  {name:'Foco',price:29.99,promo:'R$ 19,99 no primeiro mês elegível.',features:['Tudo do plano Gratuito','TutorIA: até 7 mensagens por dia','Apoio para suas dúvidas']},
  {name:'Mentoria',price:79.99,promo:'R$ 59,99 no primeiro mês elegível.',features:['TutorIA sem limite diário do plano','Até 2 mentores ativos e chat','Gravações das próprias aulas elegíveis']}
];
function el(tag, text, cls) { const e=document.createElement(tag); if(text!=null)e.textContent=text; if(cls)e.className=cls; return e; }
function append(parent,...children){ parent.append(...children.filter(Boolean)); return parent; }
function text(text,cls='muted'){return el('p',text,cls);}
function link(label,href,cls='text-link'){const a=el('a',label,cls);a.href=href;return a;}
function safeLink(label,url){try{const u=new URL(url);if(['http:','https:'].includes(u.protocol)){const a=link(label,u.href);a.target='_blank';a.rel='noopener noreferrer';return a;}}catch{}return text(url || 'Referência indisponível');}
function empty(target,message,error=false){target.replaceChildren(text(message,`empty${error?' error':''}`));}
function notice(message,error=false){$('toast').textContent=message;$('toast').className=`toast${error?' error':''}`;$('toast').hidden=!message;}
function card(title,description){const c=el('article',null,'card');if(title)c.append(el('h3',title));if(description)c.append(text(description));return c;}
function list(target,items,render,message='Nenhum registro encontrado.'){if(!items.length)return empty(target,message);target.replaceChildren(...items.map(render));}
function details(data){const dl=el('dl');Object.entries(data).forEach(([key,val])=>append(dl,el('dt',key),el('dd',val == null || val === '' ? '—' : String(val))));return dl;}
function errorText(error){return error.name==='AbortError' ? '' : error.message;}
async function run(button,fn,errorTarget){if(button.disabled)return;button.disabled=true;button.setAttribute('aria-busy','true');if(errorTarget)errorTarget.hidden=true;try{await fn();}catch(e){if(errorText(e)){if(errorTarget){errorTarget.textContent=e.message;errorTarget.hidden=false;errorTarget.className='inline-message error';}else notice(e.message,true);}}finally{button.disabled=false;button.removeAttribute('aria-busy');}}
function button(label,fn,cls='secondary'){const b=el('button',label,cls);b.type='button';b.addEventListener('click',()=>run(b,fn));return b;}
function submit(form,fn){form.addEventListener('submit',e=>{e.preventDefault();run(e.submitter || form.querySelector('[type=submit]'),()=>fn(Object.fromEntries(new FormData(form))));});}
function stopChat(){clearInterval(state.chatTimer);state.chatTimer=null;}
function resetSession(){stopChat();state.tutorStream?.abort();state.epoch++;state.user=null;state.token=null;state.tutorHistory=[];$('workspace-content').replaceChildren();$('sidebar-name').textContent='';$('sidebar-role').textContent='';$('modal').close();$('auth-form').reset();}
// Autenticação explícita por requisição: nenhum JWT é enviado aos endpoints públicos.
async function api(path,{method='GET',body,auth=true}={}){
  if(auth&&!state.token)throw new Error('Entre na sua conta para continuar.');
  const epoch=state.epoch;
  try{
    const data=await TutorClassHttp.request(state.base,path,{method,body,token:auth?state.token:undefined,timeout:path==='/api/tutoria/chat'?125000:60000});
    if(epoch!==state.epoch)throw new DOMException('Sessão alterada.','AbortError');
    return data;
  }catch(e){if(epoch!==state.epoch)throw new DOMException('Sessão alterada.','AbortError');if(e.status===401&&auth){resetSession();location.hash='login';notice(e.message,true);throw new DOMException('Sessão expirada.','AbortError');}throw e;}
}
const get=path=>api(path);
const publicGet=path=>api(path,{auth:false});
const mutate=(path,body,method='POST')=>api(path,{method,body});
function modal(title){stopChat();$('modal-title').textContent=title;const content=el('div',null,'stack');$('modal-content').replaceChildren(content);if(!$('modal').open)$('modal').showModal();return content;}
$('modal-close').addEventListener('click',()=>$('modal').close());
$('modal').addEventListener('close',()=>{stopChat();$('modal-content').replaceChildren();});
// Formulários declarativos mantêm contratos da API separados do estilo.
const field=(name,label,type='text',extra={})=>({name,label,type,...extra});
function formFields(fields,values={}){
  const form=el('form',null,'stack');
  fields.forEach(f=>{
    const label=el('label',f.label,f.type==='checkbox'?'checkbox':'');
    const input=el(f.type==='textarea'?'textarea':f.options?'select':'input');
    input.name=f.name;input.id=`field-${f.name}`;
    if(f.options)f.options.forEach(o=>input.append(new Option(o.label??o,o.value??o)));
    else if(f.type!=='textarea')input.type=f.type;
    input.required=f.required!==false&&f.type!=='checkbox';
    for(const key of ['min','max','step','maxLength','minLength','placeholder','pattern'])if(f[key]!==undefined)input[key]=f[key];
    if(f.type==='checkbox')input.checked=!!values[f.name];else input.value=values[f.name]??f.value??(f.options?.[0]?.value??f.options?.[0]??'');
    label.append(input);if(f.help)label.append(el('small',f.help));form.append(label);
  });return form;
}
function formValues(form,fields){const values={};fields.forEach(f=>{const control=form.elements.namedItem(f.name);values[f.name]=f.type==='checkbox'?control.checked:f.type==='number'?(control.value===''?null:Number(control.value)):control.value.trim()||null;});return values;}
function formDialog(title,fields,values,onSave,{description='',save='Salvar',close=true}={}){
  const content=modal(title);if(description)content.append(text(description));
  const form=formFields(fields,values),error=el('p',null,'inline-message error'),saveButton=el('button',save);saveButton.type='submit';error.hidden=true;
  form.append(error,saveButton);content.append(form);
  form.addEventListener('submit',e=>{e.preventDefault();run(saveButton,async()=>{await onSave(formValues(form,fields),content);if(close)$('modal').close();},error);});
  return {content,form};
}
function confirmAction(title,description,fn){formDialog(title,[],{},async()=>{await fn();notice('Alteração concluída.');await renderWorkspace();},{description,save:'Confirmar'});}
const optional=(name,label,type='textarea')=>field(name,label,type,{required:false});
const summaryFields=[field('resumo','Resumo','textarea'),optional('pontosFortes','Pontos fortes'),optional('pontosMelhorar','Pontos a melhorar'),optional('proximosPassos','Próximos passos')];
const mentoringFields=[...summaryFields,optional('metaAtual','Meta atual'),optional('observacoes','Observações')];
const paymentField=field('formaPagamento','Forma de pagamento simulado','text',{options:['Pix','Cartao']});
const lessonFields=[field('inicio','Início da aula (UTC)','datetime-local',{help:'Ex.: 15:00 UTC = 12:00 em Brasília.'}),field('duracaoMinutos','Duração em minutos','number',{min:30,max:240,step:1,value:60})];
async function subjects(){if(!state.subjects.length)state.subjects=await publicGet('/api/materias');return state.subjects;}
function subjectField(items){return field('materiaId','Matéria','text',{options:items.map(m=>({value:m.id,label:m.nome}))});}
function planCards(target,internal=false){target.replaceChildren(...plans.map(p=>{
  const c=el('article',null,`price-card${p.name==='Foco'?' featured':''}`);append(c,el('h3',p.name));const price=el('div',money(p.price),'price');price.append(el('small',' / mês'));c.append(price,text(p.promo,'promo'));const ul=el('ul');p.features.forEach(f=>ul.append(el('li',f)));c.append(ul);
  if(internal&&p.name!=='Gratuito')c.append(button(`Escolher ${p.name}`,()=>formDialog(`Assinar ${p.name}`,[],{},async()=>{const result=await mutate('/api/planos/me/assinatura',{plano:p.name});notice(`Plano ${result.plano} ativado. Primeiro mês: ${money(result.valorCobradoPrimeiroMes)}; mensalidade: ${money(result.valorMensal)}.`);await renderWorkspace();},{description:`Mensalidade de ${money(p.price)}. ${p.promo} Aulas são cobradas separadamente. Esta versão registra uma assinatura simulada.`,save:'Confirmar assinatura simulada'}),'button'));
  else c.append(link(internal?'Encontrar professores':'Começar agora →',internal?'#app/professores':'#cadastro-aluno','button secondary'));return c;
}));}
function teacherCard(p){const c=card(p.nome,p.materias.join(' · '));c.prepend(el('div',p.nome.split(' ').slice(0,2).map(s=>s[0]).join(''),'avatar'));append(c,el('span',p.nivelReputacao,'badge'),text(p.biografia||'Conheça o perfil deste professor.'),text(p.textoAvaliacoes),el('strong',`${money(p.precoHora)} / hora`));c.append(button('Conhecer profissional →',()=>teacherDetails(p)));return c;}
function reviewCard(r){const c=card(null,r.comentario||'Avaliação sem comentário.');c.prepend(el('span','★'.repeat(r.nota)+'☆'.repeat(5-r.nota),'stars'));append(c,el('strong',r.aluno,'review-author'),el('small',when(r.createdAtUtc)));return c;}
async function loadPublicTeachers(){const area=$('public-teachers');empty(area,'Buscando professores…');try{const id=$('public-subject').value;const teachers=await publicGet('/api/professores'+(id?`?materiaId=${encodeURIComponent(id)}`:''));if(!id)state.teachers=teachers;list(area,teachers,teacherCard,'Ainda não há professores aprovados para esta matéria.');const results=await Promise.allSettled(teachers.filter(p=>p.quantidadeAvaliacoes>0).slice(0,6).map(p=>publicGet(`/api/professores/${p.id}/avaliacoes`)));const reviews=results.flatMap(r=>r.status==='fulfilled'?r.value:[]).sort((a,b)=>new Date(b.createdAtUtc)-new Date(a.createdAtUtc));list($('public-reviews'),reviews.slice(0,6),reviewCard,results.some(r=>r.status==='rejected')?'Não foi possível carregar as avaliações agora.':'As avaliações aparecerão aqui depois das primeiras aulas avaliadas.');}catch(e){empty(area,'Não foi possível carregar os profissionais. Tente buscar novamente ou confira a conexão no rodapé.',true);empty($('public-reviews'),'As avaliações estarão disponíveis quando a conexão com a plataforma for restabelecida.');}}
async function loadPublic(){state.publicLoaded=true;planCards($('landing-plans'));const results=await Promise.allSettled([
  subjects().then(items=>$('public-subject').replaceChildren(new Option('Todas as matérias',''),...items.map(m=>new Option(m.nome,m.id)))),
  loadPublicTeachers(),
  publicGet('/api/faq').then(items=>list($('public-faq'),items,faqItem,'Nenhuma pergunta cadastrada ainda.'))
]);if(results[0].status==='rejected')$('public-subject').replaceChildren(new Option('Todas as matérias',''));if(results[2].status==='rejected')empty($('public-faq'),'Não foi possível carregar as perguntas. Tente novamente pela conexão no rodapé.');}
function faqItem(item){const d=el('details');append(d,el('summary',item.pergunta),text(item.resposta,'prewrap'));return d;}
submit($('public-filter'),loadPublicTeachers);
async function teacherDetails(p){const content=modal(p.nome);empty(content,'Carregando perfil…');const [profile,reviews]=await Promise.all([publicGet(`/api/professores/${p.id}`),publicGet(`/api/professores/${p.id}/avaliacoes`)]);content.replaceChildren();append(content,text(profile.biografia),details({'Matérias':profile.materias.join(', '),'Hora/aula':money(profile.precoHora),'Reputação':profile.nivelReputacao,'Avaliações':profile.textoAvaliacoes,'Aulas concluídas':profile.aulasConcluidas,'Oferece mentoria':profile.ofereceMentoria?'Sim':'Não'}));
  content.append(el('h3','Qualificações'));const qualifications=el('div',null,'stack');list(qualifications,profile.qualificacoes,q=>card(q.titulo,`${q.instituicao||q.tipo} · ${q.statusVerificacao}`),'Nenhuma qualificação pública disponível.');content.append(qualifications);
  const dateField=field('data','Consultar horários livres (data UTC)','date');const f=formFields([dateField]);const query=el('button','Consultar horários');query.type='submit';const slots=el('div',null,'stack');f.append(query);content.append(f,slots);
  submit(f,async values=>{const rows=await publicGet(`/api/professores/${p.id}/horarios-livres?data=${values.data}`);list(slots,rows,s=>text(`${s.data}: ${s.inicio.slice(0,5)} às ${s.fim.slice(0,5)} UTC`),'Nenhum intervalo livre nesta data.');});
  if(student())content.append(button('Solicitar uma aula',()=>bookLesson(profile),'button'));else if(!state.user)content.append(button('Entrar para solicitar aula',()=>{$('modal').close();location.hash='login';},'button'));
  content.append(el('h3','Avaliações'));const reviewList=el('div',null,'stack');list(reviewList,reviews,reviewCard,'Nenhuma avaliação publicada.');content.append(reviewList);
}
async function bookLesson(p){const items=(await subjects()).filter(m=>p.materias.includes(m.nome));if(!items.length)throw new Error('Este professor ainda não possui matérias disponíveis.');formDialog(`Aula com ${p.nome}`,[subjectField(items),...lessonFields],{},async v=>{await mutate('/api/aulas',{professorId:p.id,materiaId:v.materiaId,inicio:utc(v.inicio),duracaoMinutos:v.duracaoMinutos});notice('Solicitação enviada. Aguarde a confirmação do professor.');location.hash='app/aulas';},{description:`Valor por hora: ${money(p.precoHora)}. Consulte os intervalos disponíveis no perfil. O valor final é calculado pela duração da aula.`,save:'Solicitar aula'});}
function authMode(){return location.hash.startsWith('#cadastro')?'register':'login';}
function renderAuth(){
  const registering=authMode()==='register',adminOption=$('auth-role').querySelector('[value="Administrador"]');
  adminOption.hidden=registering;adminOption.disabled=registering;
  if(registering&&$('auth-role').value==='Administrador')$('auth-role').value='Aluno';
  const professor=$('auth-role').value==='Professor';
  $('auth-title').textContent=registering?'Crie sua conta':'Entre na sua conta';
  $('auth-description').textContent=registering?'Seu próximo passo começa aqui.':'Entre com seu e-mail e senha. Seu perfil será identificado pela API.';
  $('login-tab').classList.toggle('active',!registering);$('register-tab').classList.toggle('active',registering);
  $('register-fields').hidden=!registering;$('student-fields').hidden=!registering||professor;$('teacher-fields').hidden=!registering||!professor;
  const f=$('auth-form');f.elements.nome.required=registering;f.elements.biografia.required=registering&&professor;f.elements.precoHora.required=registering&&professor;
  f.elements.senha.minLength=registering?8:1;f.elements.senha.autocomplete=registering?'new-password':'current-password';
  $('auth-submit').textContent=registering?'Criar minha conta →':'Entrar na minha conta →';$('auth-feedback').hidden=true;
}
$('auth-role').addEventListener('change',renderAuth);
$('auth-form').addEventListener('submit',e=>{e.preventDefault();run($('auth-submit'),async()=>{
  const v=Object.fromEntries(new FormData($('auth-form'))),registering=authMode()==='register';
  if(registering){const body={nome:v.nome.trim(),email:v.email.trim(),senha:v.senha};if(v.perfil==='Professor'){body.biografia=v.biografia.trim();body.precoHora=Number(v.precoHora);}else body.dataNascimento=v.dataNascimento||null;await api(`/api/auth/registrar/${v.perfil.toLowerCase()}`,{method:'POST',auth:false,body});$('auth-form').elements.senha.value='';location.hash='login';notice('Conta criada. Entre com seu e-mail e senha.');return;}
  const login=await api('/api/auth/login',{method:'POST',auth:false,body:{email:v.email.trim(),senha:v.senha}});
  if(typeof login?.accessToken!=='string'||!login.accessToken.trim())throw new Error('A API não retornou um token de acesso válido.');
  resetSession();state.token=login.accessToken;
  try{const user=await get('/api/auth/me');if(!user?.id||!user.nome||!user.perfil)throw new Error('A API não retornou os dados da conta autenticada.');state.user=user;$('auth-role').value=user.perfil;}
  catch(error){resetSession();throw error;}
  notice('Bem-vindo à sua área.');location.hash='app/inicio';
},$('auth-feedback'));});
const menus=[['inicio','Visão geral'],['professores','Encontrar professores','Aluno'],['alunos','Meus alunos','Professor'],['aulas','Minhas aulas','Aluno|Professor'],['materiais','Materiais e atividades','Aluno|Professor'],['mentoria','Mentoria e chat','Aluno|Professor'],['acompanhamentos','Acompanhamentos','Aluno|Professor'],['tutoria','TutorIA','Aluno'],['planos','Planos','Aluno'],['gravacoes','Gravações','Aluno'],['financeiro','Financeiro','Aluno|Professor'],['notificacoes','Notificações'],['profissional','Perfil profissional','Professor'],['disponibilidade','Disponibilidade','Professor'],['analise','Análise de professores','Administrador'],['conta','Minha conta'],['ajuda','Ajuda']];
function route(){stopChat();state.tutorStream?.abort();const hash=location.hash.slice(1)||'home';const auth=['login','cadastro-aluno','cadastro-professor'].includes(hash),app=hash.startsWith('app/');if(app&&!state.user){location.hash='login';return;}
  $('landing').hidden=auth||app;$('auth-view').hidden=!auth;$('workspace').hidden=!app;$('footer').hidden=app;document.querySelector('.public-nav').hidden=app;$('account-link').textContent=state.user?'Minha área ↗':'Entrar ↗';$('account-link').href=state.user?'#app/inicio':'#login';
  if(auth){if(hash==='cadastro-professor')$('auth-role').value='Professor';if(hash==='cadastro-aluno')$('auth-role').value='Aluno';renderAuth();window.scrollTo(0,0);}
  else if(app){$('sidebar-name').textContent=state.user.nome;$('sidebar-role').textContent=state.user.perfil;$('workspace-nav').replaceChildren(...menus.filter(menuAllowed).map(m=>{const a=link(m[1],`#app/${m[0]}`);a.className=hash===`app/${m[0]}`?'active':'';return a;}));renderWorkspace();}
  else{if(!state.publicLoaded)loadPublic();requestAnimationFrame(()=>$(hash)?.scrollIntoView({block:'start'}));}
}
async function renderWorkspace(){if(!state.user)return;const key=location.hash.split('/')[1]||'inicio';const menu=menus.find(m=>m[0]===key&&menuAllowed(m));if(!menu){location.hash='app/inicio';return;}$('workspace-title').textContent=menu[1];$('workspace-eyebrow').textContent=`ÁREA DO ${state.user.perfil.toUpperCase()}`;const host=el('div',null,'stack');$('workspace-content').replaceChildren(host);empty(host,'Carregando…');try{await views[key](host);}catch(e){if(errorText(e))empty(host,e.message,true);}}
$('refresh-view').addEventListener('click',()=>run($('refresh-view'),renderWorkspace));
$('logout').addEventListener('click',()=>{resetSession();notice('Você saiu da conta.');location.hash='home';});
window.addEventListener('hashchange',route);
const views={};
views.inicio=async host=>{const lessons=await get('/api/aulas/minhas');host.replaceChildren();const greeting=el('div',null,'panel');append(greeting,el('h2',`Bom ter você aqui, ${state.user.nome.split(' ')[0]}.`),text(student()?'Organize seus estudos e escolha seu próximo passo.':'Acompanhe seus alunos e organize sua rotina de ensino.'));host.append(greeting);const stats=el('div',null,'cards');[['Aulas solicitadas','Solicitada'],['Aulas confirmadas','Confirmada'],['Aulas concluídas','Concluida']].forEach(([label,status])=>{const c=card(label);c.append(el('span',lessons.filter(l=>l.status===status).length,'stat-number'));stats.append(c);});host.append(stats);const next=lessons.filter(l=>l.status==='Confirmada'&&new Date(l.inicio)>new Date()).sort((a,b)=>new Date(a.inicio)-new Date(b.inicio));host.append(el('h3','Próximas aulas'));const grid=el('div',null,'cards');list(grid,next.slice(0,3),lessonCard,'Nenhuma aula confirmada pela frente.');host.append(grid);if(teacher())host.append(link('Preparar meu perfil profissional →','#app/profissional'));};
views.professores=async host=>{const items=await subjects();host.replaceChildren();const form=formFields([field('materiaId','Filtrar por matéria','text',{required:false,options:[{value:'',label:'Todas as matérias'},...items.map(m=>({value:m.id,label:m.nome}))]})]);form.className='toolbar';const b=el('button','Buscar');b.type='submit';form.append(b);const area=el('div',null,'cards');host.append(form,area);const load=async()=>{empty(area,'Buscando…');const data=await publicGet('/api/professores'+(form.elements.materiaId.value?`?materiaId=${form.elements.materiaId.value}`:''));list(area,data,teacherCard,'Nenhum professor encontrado.');};submit(form,load);await load();};
function lessonCard(l){const c=card(l.materia,`${l.professor} · ${l.aluno}`);append(c,el('span',l.status,'badge'),text(`${when(l.inicio)} (Brasília) · ${l.duracaoMinutos} min`),el('strong',money(l.precoAcordado)));c.append(button('Ver aula e ações',()=>lessonDetails(l.id)));return c;}
views.aulas=async host=>{const data=await get('/api/aulas/minhas');host.replaceChildren();const grid=el('div',null,'cards');list(grid,data,lessonCard,'Você ainda não possui aulas.');if(student())host.append(link('Encontrar um professor →','#app/professores','button'));host.append(text('Todos os horários da lista são exibidos no fuso de Brasília.'),grid);};
async function lessonDetails(id){const l=await get(`/api/aulas/${id}`);const content=modal(l.materia.nome);append(content,details({'Professor':l.professor.nome,'Aluno':l.aluno.nome,'Início (Brasília)':when(l.inicio),'Duração':`${l.duracaoMinutos} min`,'Valor acordado':money(l.precoAcordado),'Situação':l.status,'Motivo do cancelamento':l.motivoCancelamento}));const actions=el('div',null,'actions');content.append(actions);
  const transition=(verb,label)=>actions.append(button(label,()=>confirmAction(label,`Aula de ${l.materia.nome} em ${when(l.inicio)}.`,()=>mutate(`/api/aulas/${id}/${verb}`))));
  if(teacher()&&l.status==='Solicitada')transition('confirmar','Confirmar aula');if(teacher()&&l.status==='Confirmada')transition('concluir','Concluir aula');
  if(['Solicitada','Confirmada'].includes(l.status)){
    actions.append(button('Reagendar',()=>formDialog('Reagendar aula',lessonFields,{inicio:l.inicio.replace(/Z$/,'').slice(0,16),duracaoMinutos:l.duracaoMinutos},async v=>{await mutate(`/api/aulas/${id}/reagendar`,{inicio:utc(v.inicio),duracaoMinutos:v.duracaoMinutos},'PUT');notice('Aula reagendada.');await renderWorkspace();},{description:'Use um intervalo disponível do professor, em UTC. O pagamento já registrado é mantido.'})));
    actions.append(button('Cancelar aula',()=>formDialog('Cancelar aula',[optional('motivo','Motivo')],{},async v=>{await mutate(`/api/aulas/${id}/cancelar`,v);notice('Aula cancelada.');await renderWorkspace();},{description:'Confirme o cancelamento desta aula.',save:'Confirmar cancelamento'}),'danger'));
  }
  actions.append(button('Histórico de reagendamentos',async()=>{const rows=await get(`/api/aulas/${id}/reagendamentos`);const target=modal('Histórico de reagendamentos');list(target,rows,r=>card(when(r.createdAtUtc),`${when(r.inicioAnterior)} → ${when(r.inicioNovo)}`),'Esta aula ainda não foi reagendada.');}));
  if(l.status==='Concluida'){
    actions.append(button('Ver acompanhamento',()=>showFollowup(id)));
    if(teacher())actions.append(button('Registrar acompanhamento',()=>editFollowup(id)));
    if(student()){
      actions.append(button('Avaliar professor',()=>formDialog('Como foi sua aula?',[field('nota','Nota de 1 a 5','number',{min:1,max:5,step:1,value:5}),optional('comentario','Comentário')],{},async v=>{await mutate(`/api/aulas/${id}/avaliacao`,v);notice('Avaliação publicada.');},{save:'Publicar avaliação'})));
      actions.append(button('Ver gravação',async()=>showRecording(await get(`/api/aulas/${id}/gravacao`))));
    }
  }
  if(student()&&l.status!=='Cancelada')actions.append(button('Pagamento simulado',()=>payLesson(l)));
  if(teacher()&&l.status!=='Cancelada')actions.append(button('Compartilhar material',()=>materialForm(null,l)));
}
async function showFollowup(id){const a=await get(`/api/aulas/${id}/acompanhamento`);const c=modal('Acompanhamento da aula');if(!a){empty(c,'Ainda não há acompanhamento.');return;}c.append(details({'Resumo':a.resumo,'Pontos fortes':a.pontosFortes,'Pontos a melhorar':a.pontosMelhorar,'Próximos passos':a.proximosPassos}));}
async function editFollowup(id){let a={};try{a=await get(`/api/aulas/${id}/acompanhamento`)||{};}catch(e){if(e.status!==404)throw e;}formDialog('Acompanhamento da aula',summaryFields,a,async v=>{await mutate(`/api/aulas/${id}/acompanhamento`,v,'PUT');notice('Acompanhamento salvo.');});}
function payLesson(l){formDialog('Pagamento de aula',[paymentField],{},async v=>{const result=await mutate(`/api/financeiro/aulas/${l.id}/pagar`,v);notice(`Pagamento simulado registrado: ${money(result.valorBruto)}.`);await renderWorkspace();},{description:`Aula de ${typeof l.materia==='string'?l.materia:l.materia.nome}: ${money(l.precoAcordado)}. Não há transação bancária nem coleta de dados de cartão.`,save:'Confirmar pagamento simulado'});}
const materialFields=[field('titulo','Título','text',{maxLength:160}),field('descricao','Instruções ou descrição','textarea',{required:false,maxLength:1000}),field('tipo','Tipo de material','text',{options:[{value:'Exercicio',label:'Atividade / exercício'},'PDF','Link','Video','Documento','Outro']}),field('nomeArquivo','Nome do arquivo','text',{required:false,maxLength:255}),field('urlArquivo','Link ou referência do material','text',{maxLength:2000,help:'Cole um link compartilhável de PDF, vídeo ou documento. O aluno receberá o link e as instruções; não há envio de arquivo diretamente nesta tela.'})];
function materialCard(m){
  const c=card(m.titulo,m.descricao),kind=m.tipo==='Exercicio'?'Atividade / exercício':m.tipo;
  append(c,el('span',kind,'badge'),text(`${m.materiaNome} · ${student()?m.professorNome:m.alunoNome}`),safeLink('Abrir material ↗',m.urlArquivo));
  c.append(button('Detalhes',async()=>{const data=await get(`/api/materiais/${m.id}`);const content=modal(data.titulo);content.append(details({'Instruções':data.descricao,'Aluno':data.alunoNome,'Professor':data.professorNome,'Matéria':data.materiaNome,'Tipo':kind,'Arquivo':data.nomeArquivo,'Criado em':when(data.createdAtUtc)}),safeLink('Abrir material ↗',data.urlArquivo));}));
  if(teacher()){const actions=el('div',null,'actions');actions.append(button('Editar',()=>materialForm(m)),button('Excluir',()=>confirmAction('Excluir material',`Remover o material “${m.titulo}”?`,()=>mutate(`/api/materiais/${m.id}`,undefined,'DELETE')),'danger'));c.append(actions);}
  return c;
}
views.materiais=async host=>{
  const items=await get('/api/materiais/me');host.replaceChildren();
  if(teacher())host.append(button('Enviar material ou atividade',()=>materialForm(),'button'));
  host.append(text(teacher()?'Escolha um aluno com aula ou mentoria ativa. O material fica salvo e o aluno recebe uma notificação.':'Aqui ficam os materiais e exercícios enviados pelos seus professores.'));
  const filter=formFields([field('tipo','Mostrar','text',{options:[{value:'',label:'Todos os materiais'},{value:'Exercicio',label:'Só atividades / exercícios'}]})]),grid=el('div',null,'cards');
  const render=()=>list(grid,items.filter(m=>!filter.elements.tipo.value||m.tipo===filter.elements.tipo.value),materialCard,'Nenhum material nesta seleção.');
  filter.elements.tipo.addEventListener('change',render);host.append(filter,grid);render();
};
async function materialForm(existing,lesson,recipient){
  if(existing){formDialog('Editar material',materialFields,existing,async v=>{await mutate(`/api/materiais/${existing.id}`,v,'PUT');notice('Material atualizado no banco.');await renderWorkspace();});return;}
  const recipients=(await get('/api/materiais/destinatarios')).filter(r=>(!recipient||r.alunoId===recipient.alunoId)&&(!lesson||(r.alunoId===lesson.aluno.id&&r.materiaId===lesson.materia.id)));
  if(!recipients.length){const content=modal('Enviar material ou atividade');content.append(text('Você ainda não possui aluno e matéria elegíveis. Uma aula não cancelada ou uma mentoria ativa com essa matéria permite o envio.'),button('Ver meus alunos',()=>{$('modal').close();location.hash='app/alunos';}));return;}
  const targets=recipients.map((r,index)=>({value:String(index),label:`${r.alunoNome} · ${r.materiaNome}`}));
  const fields=[field('destinatario','Aluno e matéria','text',{options:targets}),field('aulaId','Vincular a uma aula (opcional)','text',{required:false,options:[{value:'',label:'Material geral para este aluno'}]}),...materialFields];
  const {form}=formDialog('Enviar material ou atividade',fields,{tipo:'Exercicio'},async v=>{
    const target=recipients[Number(v.destinatario)],{destinatario,...body}=v;
    await mutate('/api/materiais',{...body,alunoId:target.alunoId,materiaId:target.materiaId,aulaId:v.aulaId||null});
    notice(`Atividade/material salvo e enviado para ${target.alunoNome}.`);await renderWorkspace();
  },{description:'O conteúdo será visível apenas ao aluno escolhido, ao professor e aos responsáveis autorizados.',save:'Enviar ao aluno'});
  const updateLessons=()=>{const target=recipients[Number(form.elements.destinatario.value)];form.elements.aulaId.replaceChildren(new Option('Material geral para este aluno',''),...target.aulas.map(a=>new Option(`${when(a.inicio)} · ${a.status}`,a.id)));if(lesson)form.elements.aulaId.value=lesson.id;};
  form.elements.destinatario.addEventListener('change',updateLessons);updateLessons();
}
views.alunos=async host=>{
  const [recipients,mentorias]=await Promise.all([get('/api/materiais/destinatarios'),get('/api/mentorias/me')]);
  const pupils=new Map();for(const r of recipients){if(!pupils.has(r.alunoId))pupils.set(r.alunoId,{alunoId:r.alunoId,alunoNome:r.alunoNome,materias:[]});pupils.get(r.alunoId).materias.push(r.materiaNome);}
  for(const m of mentorias.filter(m=>m.ativo)){if(!pupils.has(m.alunoId))pupils.set(m.alunoId,{alunoId:m.alunoId,alunoNome:m.alunoNome,materias:[]});pupils.get(m.alunoId).mentoria=m;}
  host.replaceChildren(text('Seus alunos aparecem quando contratam uma aula ou selecionam você como mentor.'));
  const grid=el('div',null,'cards');list(grid,[...pupils.values()],p=>{const c=card(p.alunoNome,p.materias.join(' · '));const actions=el('div',null,'actions');if(p.materias.length)actions.append(button('Enviar atividade',()=>materialForm(null,null,p)));if(p.mentoria)actions.append(button('Abrir conversa',()=>openChat(p.mentoria)));c.append(actions);if(!p.mentoria)c.append(text('Chat disponível quando o aluno escolher você como mentor no plano Mentoria.'));return c;},'Nenhum aluno vinculado ainda. Complete seu perfil, aguarde a aprovação e cadastre horários para receber aulas.');host.append(grid,link('Ver perfil e situação da aprovação →','#app/profissional'));
};
views.mentoria=async host=>{const rows=await get('/api/mentorias/me');host.replaceChildren();const toolbar=el('div',null,'actions');if(student())toolbar.append(button('Selecionar mentor',()=>selectMentor(),'button'));else toolbar.append(button('Configurar oferta de mentoria',()=>mentoringSettings()));host.append(toolbar,text('Conversas são privadas entre os participantes. Vínculos encerrados mantêm o histórico disponível para leitura.'));const grid=el('div',null,'cards');list(grid,rows,m=>{const c=card(student()?m.professorNome:m.alunoNome,`Vaga ${m.slot} · Desde ${when(m.dataInicioUtc)}`);c.append(el('span',m.ativo?'Ativa':'Encerrada','badge'));const actions=el('div',null,'actions');actions.append(button('Abrir conversa',()=>openChat(m)));if(student()&&m.ativo)actions.append(button('Trocar mentor',()=>selectMentor(m.slot)));c.append(actions);return c;},'Nenhum vínculo de mentoria encontrado.');host.append(grid);};
async function selectMentor(slot){const teachers=(await publicGet('/api/professores')).filter(p=>p.ofereceMentoria);if(!teachers.length)throw new Error('Nenhum professor está oferecendo mentoria no momento.');const fields=[field('professorId','Professor mentor','text',{options:teachers.map(p=>({value:p.id,label:`${p.nome} · ${p.materias.join(', ')}`}))})];if(!slot)fields.push(field('slot','Vaga','number',{min:1,max:2,step:1,value:1}));formDialog(slot?'Trocar mentor':'Selecionar mentor',fields,{},async v=>{if(slot)await mutate(`/api/mentorias/me/slots/${slot}/trocar`,{novoProfessorId:v.professorId},'PUT');else await mutate('/api/mentorias/me',v);notice('Mentoria atualizada.');await renderWorkspace();},{description:slot?'A troca respeita o intervalo mínimo de 30 dias por vaga. O histórico anterior é preservado.':'É necessário ter plano Mentoria ativo. Você pode ter até dois mentores, sujeitos à disponibilidade.'});}
function mentoringSettings(){formDialog('Oferta de mentoria',[field('ofereceMentoria','Quero oferecer mentoria','checkbox'),field('limiteMentorados','Limite de mentorados','number',{min:0,max:50,step:1,value:2})],{},async v=>{await mutate('/api/professores/me/mentoria',v,'PUT');notice('Configuração de mentoria salva.');await renderWorkspace();},{description:'Informe a oferta e o limite que deseja aplicar ao seu perfil.'});}
async function openChat(mentoring){const prefix=`/api/mentorias/${mentoring.id}/chat`;const info=await get(prefix),content=modal(`Conversa com ${student()?mentoring.professorNome:mentoring.alunoNome}`);const status=text('Atualização automática enquanto esta conversa estiver aberta.');const log=el('div',null,'chat-log');log.setAttribute('role','log');log.setAttribute('aria-live','polite');const older=button('Carregar mensagens anteriores',async()=>{if(before)await load(true);});const refresh=button('Atualizar conversa',()=>load());content.append(status,el('div',null,'actions'),older,refresh,log);let before=null,busy=false;const seen=new Map();
  function render(){const rows=[...seen.values()].sort((a,b)=>new Date(a.dataEnvioUtc)-new Date(b.dataEnvioUtc)||a.id.localeCompare(b.id));list(log,rows,m=>{const c=el('div',null,`chat-message${m.remetenteUsuarioId===state.user?.id?' own':''}`);append(c,el('strong',m.remetenteNome),el('span',m.conteudo),el('small',`${when(m.dataEnvioUtc)}${m.lida?' · Lida':''}`));return c;},'Inicie a conversa com seu mentorado ou mentor.');older.hidden=!before;}
  async function load(previous=false){if(busy)return;busy=true;try{const page=await get(`${prefix}/mensagens?limit=30${previous&&before?'&before='+encodeURIComponent(before):''}`);if(!content.isConnected)return;const oldCount=seen.size;page.mensagens.forEach(m=>seen.set(m.id,m));if(previous||oldCount===0)before=page.proximoBeforeUtc;render();status.textContent=info.somenteLeitura?'Vínculo encerrado: conversa disponível somente para leitura.':'Atualização automática a cada 8 segundos.';await mutate(`${prefix}/mensagens/lidas`);}catch(e){if(errorText(e)&&content.isConnected)status.textContent=e.message;}finally{busy=false;}}
  if(!info.somenteLeitura){const fields=[field('conteudo','Mensagem','textarea',{maxLength:4000})],f=formFields(fields);const send=el('button','Enviar mensagem');send.type='submit';f.append(send);submit(f,async()=>{const v=formValues(f,fields);const saved=await mutate(`${prefix}/mensagens`,v);seen.set(saved.id,saved);f.reset();render();log.scrollTop=log.scrollHeight;});content.append(f);}
  await load();if(content.isConnected&&$('modal').open)state.chatTimer=setInterval(()=>{if(document.visibilityState==='visible')load();},8000);
}
views.acompanhamentos=async host=>{const results=await Promise.allSettled([get('/api/aulas/minhas'),get('/api/mentorias/acompanhamentos/pendentes'),get('/api/mentorias/acompanhamentos/historico')]);host.replaceChildren();host.append(el('h3','Acompanhamento de aulas'));const lessons=el('div',null,'cards');if(results[0].status==='fulfilled')list(lessons,results[0].value.filter(a=>a.status==='Concluida'),l=>{const c=lessonCard(l);c.append(button('Consultar acompanhamento',()=>showFollowup(l.id)));if(teacher())c.append(button('Preencher acompanhamento',()=>editFollowup(l.id)));return c;},'Nenhuma aula concluída para acompanhar.');else empty(lessons,results[0].reason.message,true);host.append(lessons,el('h3','Acompanhamento de mentoria'));const grid=el('div',null,'cards');const data=results.slice(1).flatMap(r=>r.status==='fulfilled'?r.value:[]);const unique=[...new Map(data.map(a=>[a.id,a])).values()];list(grid,unique,a=>{const c=card(`${a.materiaNome} · ${student()?a.professorNome:a.alunoNome}`,a.resumo||'Acompanhamento aguardando preenchimento.');append(c,el('span',a.status,'badge'),text(when(a.dataAulaUtc)));c.append(button('Consultar',()=>{const content=modal('Acompanhamento de mentoria');content.append(details({'Resumo':a.resumo,'Pontos fortes':a.pontosFortes,'Pontos a melhorar':a.pontosMelhorar,'Próximos passos':a.proximosPassos,'Meta atual':a.metaAtual,'Observações':a.observacoes,'Situação':a.status}));}));if(teacher()&&a.status==='Pendente'){c.append(button('Editar',()=>editMentoringFollowup(a,false)),button('Preencher e concluir',()=>editMentoringFollowup(a,true)));}return c;},'Nenhum acompanhamento de mentoria encontrado.');host.append(grid);results.slice(1).filter(r=>r.status==='rejected'&&errorText(r.reason)).forEach(r=>host.append(text(r.reason.message,'inline-message error')));};
function editMentoringFollowup(a,complete){formDialog(complete?'Concluir acompanhamento de mentoria':'Editar acompanhamento de mentoria',mentoringFields,a,async v=>{const result=await mutate(`/api/mentorias/acompanhamentos/${a.id}${complete?'/concluir':''}`,v,complete?'POST':'PUT');notice(complete?result.mensagem:'Acompanhamento salvo.');await renderWorkspace();},{description:complete?'Após concluir, o adicional simulado será calculado pelo sistema conforme a elegibilidade.':'Registre o desenvolvimento do aluno.',save:complete?'Concluir acompanhamento':'Salvar'});}
views.planos=async host=>{const [plan,usage]=await Promise.all([get('/api/planos/me'),get('/api/planos/me/ia')]);host.replaceChildren();const current=el('div',null,'panel');append(current,el('h3',`Plano atual: ${plan.planoAtual}`),details({'Assinatura ativa':plan.assinaturaAtiva?'Sim':'Não','IA educacional':plan.possuiIaEducacional?'Incluída':'Não incluída','Uso de IA hoje':usage.quantidadeUsadaHoje,'Limite diário':usage.limiteDiario??'Sem limite do plano','Mentoria':plan.possuiMentoria?'Incluída':'Não incluída','Gravações elegíveis':plan.possuiGravacoesElegiveis?'Incluídas':'Não incluídas'}));if(plan.assinaturaAtiva)current.append(button('Cancelar assinatura',()=>confirmAction('Cancelar assinatura','Os benefícios serão encerrados conforme as regras do plano. As aulas contratadas continuam independentes.',()=>mutate('/api/planos/me/cancelar')),'danger'));host.append(current,text('Aulas são cobradas separadamente. As promoções só se aplicam à primeira assinatura elegível e não voltam após cancelamento.'));const grid=el('div',null,'cards');planCards(grid,true);host.append(grid);};
views.tutoria=async host=>{const status=await get('/api/tutoria/status');host.replaceChildren();const statusText=text('','inline-message');const log=el('div',null,'chat-log');log.setAttribute('role','log');log.setAttribute('aria-live','polite');const fields=[field('mensagem','Qual é sua dúvida?','textarea',{maxLength:4000})],form=formFields(fields),send=el('button','Enviar pergunta');send.type='submit';form.append(send);host.append(statusText,log,form);let allowed=false;
  function update(s){allowed=s.possuiAcesso&&!s.limiteAtingido;send.disabled=!allowed;statusText.textContent=`${s.mensagem||''} ${s.possuiAcesso?(s.restantesHoje==null?'Sem limite diário do plano.':`${s.restantesHoje} mensagem(ns) restante(s) hoje.`):''}`;}
  state.tutorStream?.abort();
  const messageNodes=new WeakMap();
  function messages(){list(log,state.tutorHistory,m=>{
    const bubble=el('div',null,`chat-message tutor-message${m.role==='Você'?' own':''}`);
    bubble.append(el('strong',m.role,'tutor-author'));
    const body=el('div',null,'tutor-body');
    if(m.role==='Você')body.textContent=m.text;
    else TutorClassText.render(body,m.text);
    bubble.append(body);
    if(m.pending)bubble.append(el('small',m.text?'Recebendo resposta…':'A TutorIA está preparando a resposta…','tutor-progress'));
    if(m.interrupted)bubble.append(el('small','Resposta incompleta. Atualize o status antes de tentar novamente.','tutor-interrupted'));
    messageNodes.set(m,bubble);
    return bubble;
  },'Sua conversa de estudos começa aqui.');}
  update(status);messages();if(!allowed)host.append(link('Conhecer os planos →','#app/planos'));
  const refresh=button('Atualizar status',async()=>{update(await get('/api/tutoria/status'));refresh.hidden=true;});refresh.hidden=true;host.append(refresh);
  form.addEventListener('submit',async e=>{e.preventDefault();if(!allowed||send.disabled)return;await run(send,async()=>{
    const mensagem=form.elements.mensagem.value.trim();if(!mensagem)throw new Error('Escreva sua dúvida antes de enviar.');
    const controller=new AbortController(),epoch=state.epoch;
    state.tutorStream=controller;
    const answer={role:'TutorIA',text:'',pending:true};
    state.tutorHistory.push({role:'Você',text:mensagem},answer);messages();
    const input=form.elements.mensagem;input.disabled=true;refresh.hidden=true;
    try{
      const response=await TutorClassHttp.stream(state.base,'/api/tutoria/chat/stream',{
        body:{mensagem},token:state.token,signal:controller.signal,onDelta:trecho=>{
          if(epoch!==state.epoch)throw new DOMException('Sessão alterada.','AbortError');
          answer.text+=trecho;if(host.isConnected){
            const bubble=messageNodes.get(answer);
            TutorClassText.render(bubble.querySelector('.tutor-body'),answer.text);
            bubble.querySelector('.tutor-progress').textContent='Recebendo resposta…';
            log.scrollTop=log.scrollHeight;
          }
        }});
      if(epoch!==state.epoch)throw new DOMException('Sessão alterada.','AbortError');
      update(response);
      if(response.motivo||!response.resposta)throw new Error(response.mensagem||'A TutorIA não conseguiu responder agora.');
      answer.text=response.resposta;form.reset();
    }catch(error){
      answer.interrupted=true;allowed=false;refresh.hidden=false;
      if(epoch===state.epoch&&error.status===401){resetSession();location.hash='login';}
      throw error;
    }finally{
      answer.pending=false;input.disabled=false;
      if(state.tutorStream===controller)state.tutorStream=null;
      if(epoch===state.epoch&&host.isConnected){messages();log.scrollTop=log.scrollHeight;}
    }
  });send.disabled=!allowed;});
};
views.gravacoes=async host=>{const data=await get('/api/gravacoes/minhas');host.replaceChildren(text('As gravações desta versão são mídias demonstrativas. O acesso depende do plano Mentoria e da elegibilidade das suas próprias aulas.'));const grid=el('div',null,'cards');list(grid,data,r=>{const c=card(r.titulo,r.descricao);append(c,text(`${r.materia} · ${r.professor}`),text(`Aula: ${when(r.aulaInicio)} · ${Math.round(r.duracaoSegundos/60)} min`));if(r.disponivel)c.append(button('Abrir gravação',()=>showRecording(r)));return c;},'Nenhuma gravação elegível disponível.');host.append(grid);};
function showRecording(r){const content=modal(r.titulo);append(content,text(r.descricao||'Gravação demonstrativa vinculada à sua aula.'),details({'Professor':r.professor,'Matéria':r.materia,'Aula':when(r.aulaInicio),'Duração':`${Math.round(r.duracaoSegundos/60)} minutos`}));if(r.disponivel)content.append(safeLink('Assistir à gravação externa ↗',r.urlVideo));else content.append(text('Gravação indisponível.'));}
views.financeiro=async host=>{host.replaceChildren(text('Financeiro simulado: nenhum valor é transferido e nenhum dado de cartão é solicitado.','inline-message'));if(student()){
  const rows=await get('/api/financeiro/me/historico');const actions=el('div',null,'actions');actions.append(button('Pagar uma aula',async()=>{const lessons=(await get('/api/aulas/minhas')).filter(a=>a.status!=='Cancelada');if(!lessons.length)throw new Error('Nenhuma aula disponível.');formDialog('Escolher aula',[field('aulaId','Aula','text',{options:lessons.map(l=>({value:l.id,label:`${l.materia} · ${when(l.inicio)} · ${money(l.precoAcordado)}`}))})],{},v=>{payLesson(lessons.find(l=>l.id===v.aulaId));},{save:'Continuar',close:false});}));
  const subscriptions=rows.filter(r=>r.tipo==='Assinatura');actions.append(button('Registrar mensalidade',()=>{if(!subscriptions.length)throw new Error('Você ainda não possui uma assinatura paga.');const now=new Date();formDialog('Mensalidade simulada',[field('assinaturaId','Assinatura','text',{options:subscriptions.map(s=>({value:s.id,label:`${s.descricao} · ${when(s.dataUtc)} · ${s.status}`}))}),field('anoCompetencia','Ano','number',{min:2020,max:2100,step:1,value:now.getFullYear()}),field('mesCompetencia','Mês','number',{min:1,max:12,step:1,value:now.getMonth()+1}),paymentField],{},async v=>{const {assinaturaId,...body}=v;const result=await mutate(`/api/financeiro/assinaturas/${assinaturaId}/cobrancas`,body);notice(`Mensalidade simulada registrada: ${money(result.valor)}.`);await renderWorkspace();},{description:'Registre a competência da assinatura. O sistema valida duplicidades e calcula o valor.',save:'Confirmar mensalidade simulada'});}));host.append(actions);const grid=el('div',null,'cards');list(grid,rows,r=>{const c=card(r.descricao,when(r.dataUtc));append(c,el('span',r.status,'badge'),el('strong',money(r.valor)),text(r.tipo));return c;},'Seu histórico financeiro está vazio.');host.append(grid);
}else{
  const now=new Date(),fields=[field('ano','Ano','number',{min:2020,max:2100,value:now.getFullYear()}),field('mes','Mês','number',{min:1,max:12,value:now.getMonth()+1})],form=formFields(fields);form.className='toolbar';const b=el('button','Consultar período');b.type='submit';form.append(b);const area=el('div',null,'stack');host.append(form,area);async function load(){const v=formValues(form,fields);const result=await get(`/api/financeiro/professores/me/resumo?ano=${v.ano}&mes=${v.mes}`);area.replaceChildren(details({'Aulas (bruto)':money(result.valorBrutoAulas),'Comissão TutorClass':money(result.comissoesTutorClass),'Aulas (líquido)':money(result.valorLiquidoAulas),'Adicionais de mentoria':money(result.adicionaisMentoria),'Total previsto':money(result.totalPrevisto)}));const grid=el('div',null,'cards');list(grid,result.itens,r=>{const c=card(r.alunoNome,`${r.tipo} · ${when(r.dataUtc)}`);c.append(details({'Bruto':money(r.valorBruto),'Comissão':money(r.valorComissao),'Líquido':money(r.valorLiquidoProfessor),'Adicional':money(r.valorAdicionalMentoria),'Situação':r.status}));return c;},'Nenhum lançamento neste período.');area.append(grid);}submit(form,load);await load();
  host.append(button('Calcular comissão de uma aula',()=>formDialog('Demonstrativo de comissão',[field('valorBruto','Valor bruto (R$)','number',{min:0.01,step:0.01})],{},async(v,c)=>{const result=await get(`/api/financeiro/aulas/comissao?valorBruto=${v.valorBruto}`);const old=c.querySelector('dl');if(old)old.remove();c.append(details({'Bruto':money(result.valorBruto),'Comissão':money(result.valorComissao),'Líquido previsto':money(result.valorLiquidoProfessor)}));},{save:'Calcular',close:false})));
}};
views.notificacoes=async host=>{host.replaceChildren();const summary=await get('/api/notificacoes/resumo');const label=el('h3',`${summary.naoLidas} notificação(ões) não lida(s)`);const toolbar=el('div',null,'toolbar');const select=el('select');select.setAttribute('aria-label','Filtrar notificações');select.append(new Option('Todas','false'),new Option('Não lidas','true'));toolbar.append(select,button('Marcar todas como lidas',async()=>{await mutate('/api/notificacoes/marcar-todas-lidas');notice('Notificações marcadas como lidas.');await renderWorkspace();}));const listArea=el('div',null,'stack'),more=button('Carregar mais',()=>load(true));host.append(label,toolbar,listArea,more);let page=1;async function load(next=false){if(!next){page=1;listArea.replaceChildren();}const rows=await get(`/api/notificacoes?somenteNaoLidas=${select.value}&page=${page}&pageSize=20`);if(!rows.length&&page===1)empty(listArea,'Nenhuma notificação encontrada.');for(const n of rows){const c=card(n.titulo,n.mensagem);append(c,el('span',n.lida?'Lida':'Não lida','badge'),el('small',when(n.dataCriacaoUtc)));if(!n.lida)c.append(button('Marcar como lida',async()=>{await mutate(`/api/notificacoes/${n.id}/marcar-lida`);await renderWorkspace();}));const target=notificationRoute(n.rotaAcao);if(target)c.append(link('Abrir área relacionada →',target));listArea.append(c);}more.hidden=rows.length<20;page++;}select.addEventListener('change',()=>run(more,()=>load()));await load();};
function notificationRoute(route){if(!route)return null;const mappings=[['aula','aulas'],['material','materiais'],['mentoria','mentoria'],['plano','planos'],['financeiro','financeiro'],['candidatura','profissional'],['qualific','profissional'],['gravac','gravacoes']];const item=mappings.find(([match])=>route.includes(match));return item?`#app/${item[1]}`:null;}
const days=[['Sunday','Domingo'],['Monday','Segunda-feira'],['Tuesday','Terça-feira'],['Wednesday','Quarta-feira'],['Thursday','Quinta-feira'],['Friday','Sexta-feira'],['Saturday','Sábado']];
const availabilityFields=[field('diaSemana','Dia da semana','text',{options:days.map(([value,label])=>({value,label}))}),field('inicio','Início (UTC)','time'),field('fim','Fim (UTC)','time')];
views.disponibilidade=async host=>{const rows=await get('/api/professores/me/disponibilidades');host.replaceChildren(text('Cadastre intervalos em UTC: 15:00 UTC corresponde a 12:00 em Brasília. Evite intervalos que atravessem a meia-noite.'));host.append(button('Adicionar disponibilidade',()=>availabilityForm(),'button'));const grid=el('div',null,'cards');list(grid,rows,d=>{const c=card(days.find(([v])=>v===d.diaSemana)?.[1]||d.diaSemana,`${d.inicio.slice(0,5)} às ${d.fim.slice(0,5)} UTC`);const a=el('div',null,'actions');a.append(button('Editar',()=>availabilityForm(d)),button('Remover',()=>confirmAction('Remover disponibilidade','Este intervalo deixará de receber novas solicitações. As regras de aulas existentes serão verificadas.',()=>mutate(`/api/professores/me/disponibilidades/${d.id}`,undefined,'DELETE')),'danger'));c.append(a);return c;},'Você ainda não cadastrou horários disponíveis.');host.append(grid);};
function availabilityForm(d){formDialog(d?'Editar disponibilidade':'Nova disponibilidade',availabilityFields,d||{},async v=>{if(v.inicio>=v.fim)throw new Error('O fim deve ser posterior ao início.');v.inicio=v.inicio.length===5?v.inicio+':00':v.inicio;v.fim=v.fim.length===5?v.fim+':00':v.fim;await mutate('/api/professores/me/disponibilidades'+(d?`/${d.id}`:''),v,d?'PUT':'POST');notice('Disponibilidade salva.');await renderWorkspace();});}
const qualificationFields=[field('tipo','Tipo','text',{options:['Graduacao','PosGraduacao','Curso','Certificacao','Experiencia','Outro']}),field('titulo','Título'),optional('instituicao','Instituição','text'),optional('descricao','Descrição'),optional('dataConclusao','Data de conclusão','date'),optional('documentoComprovacao','Referência do documento comprobatório','text')];
views.profissional=async host=>{host.replaceChildren();const intro=el('div',null,'panel');append(intro,el('h3','Prepare seu perfil para ensinar'),text('Seu cadastro inicia como candidato. Vincule as matérias, adicione suas qualificações e envie sua candidatura. A aprovação e a verificação das qualificações são realizadas pela administração.'));host.append(intro);const toolbar=el('div',null,'actions');toolbar.append(button('Vincular matéria',async()=>{formDialog('Matéria que você ensina',[subjectField(await subjects())],{},async v=>{await mutate(`/api/professores/${uid()}/materias`,v);notice('Matéria vinculada.');await renderWorkspace();});}),button('Adicionar qualificação',()=>formDialog('Nova qualificação',qualificationFields,{},async v=>{await mutate(`/api/professores/${uid()}/qualificacoes`,v);notice('Qualificação enviada para verificação.');await renderWorkspace();})),button('Configurar mentoria',mentoringSettings));host.append(toolbar);
  const qualifications=await get(`/api/professores/${uid()}/qualificacoes`);host.append(el('h3','Minhas qualificações'));const grid=el('div',null,'cards');list(grid,qualifications,q=>{const c=card(q.titulo,q.descricao);append(c,el('span',q.statusVerificacao,'badge'),text(`${q.tipo} · ${q.instituicao||'Instituição não informada'}`));return c;},'Adicione suas qualificações para análise.');host.append(grid);
  const section=el('div',null,'panel stack');section.append(el('h3','Minha candidatura'));const key=`tutorclass.candidatura:${state.base}:${state.user.id}`,saved=store.get(key);const result=el('div',null,'stack');const show=async id=>{const c=await get(`/api/candidaturas-professor/${encodeURIComponent(id)}`);result.replaceChildren(details({'Protocolo':c.id,'Situação':c.status,'Apresentação':c.mensagem,'Retorno da análise':c.motivo,'Data da análise':when(c.dataAnaliseUtc)}));store.set(key,c.id);};
  section.append(button('Enviar candidatura',()=>formDialog('Candidatura a professor',[field('mensagem','Apresentação para análise','textarea')],{},async v=>{const id=await mutate(`/api/professores/${uid()}/candidaturas`,v);store.set(key,id);notice(`Candidatura enviada. Protocolo: ${id}`);await show(id);})),button('Consultar pelo protocolo',()=>formDialog('Consultar candidatura',[field('id','Protocolo da candidatura','text',{value:saved||'',help:'Informe o protocolo retornado no envio. A API atual não lista candidaturas por usuário.'})],{},async v=>{await show(v.id);})),result);host.append(section);if(saved){try{await show(saved);}catch(e){if(errorText(e))result.append(text(e.message,'inline-message error'));}}
  const profile=el('div',null,'panel stack');profile.append(el('h3','Perfil público'),text('Seu perfil público fica disponível após aprovação. A API atual não possui uma rota de edição de biografia e preço após o cadastro.'));profile.append(button('Consultar meu perfil público',async()=>{const p=await publicGet(`/api/professores/${uid()}`);await teacherDetails(p);}));host.append(profile);
};
views.conta=async host=>{const user=await get('/api/auth/me');host.replaceChildren();const panel=el('div',null,'panel');panel.append(details({'Nome':user.nome,'E-mail':user.email,'Perfil':user.perfil}),text('Sua sessão é mantida somente nesta página. Ao recarregar ou sair, entre novamente.'));host.append(panel,button('Configurar conexão com a plataforma',connectionDialog));};
views.ajuda=async host=>{host.replaceChildren();const fields=[optional('termo','Pesquisar dúvidas','text'),optional('categoria','Categoria (opcional)','text')],form=formFields(fields);form.className='toolbar';const b=el('button','Pesquisar');b.type='submit';form.append(b);const results=el('div',null,'stack');host.append(form,results);const search=async()=>{const v=formValues(form,fields),q=new URLSearchParams();if(v.termo)q.set('termo',v.termo);if(v.categoria)q.set('categoria',v.categoria);const items=await publicGet('/api/faq?'+q);list(results,items,faqItem,'Nenhuma pergunta encontrada.');};submit(form,search);await search();const chat=formFields([field('mensagem','Pergunte sobre a plataforma','textarea')]);const send=el('button','Consultar assistente de ajuda');send.type='submit';const answer=el('p',null,'inline-message');answer.hidden=true;chat.append(send,answer);submit(chat,async v=>{const response=await api('/api/faq/chat',{method:'POST',auth:false,body:v});answer.hidden=false;answer.textContent=response.resposta||response.mensagem||'Não encontrei uma resposta para esta pergunta.';});host.append(el('h3','Assistente de ajuda'),chat);};
function connectionDialog(){const content=modal('Conexão com a plataforma');append(content,text(`API configurada para este site: ${window.TUTORCLASS_CONFIG?.apiBaseUrl || 'não configurada'}`),text('A alteração abaixo vale até recarregar a página e encerra a sessão. Contas de servidores diferentes podem pertencer a bancos diferentes.'));
  const fields=[field('base','Endereço da API','url')],form=formFields(fields,{base:state.base});const save=el('button','Usar e testar conexão');save.type='submit';form.append(save);submit(form,async v=>{const base=TutorClassHttp.normalizeBaseUrl(v.base);const health=await TutorClassHttp.request(base,'/health',{timeout:60000});if(health?.service!=='TutorClass.API')throw new Error('O endereço não respondeu como a API TutorClass.');resetSession();state.base=base;state.subjects=[];state.teachers=[];state.publicLoaded=false;location.hash='home';route();notice(health.status==='Healthy'?'Conexão estabelecida.':`API acessível com situação ${health.status}.`,health.status!=='Healthy');});content.append(form);
}
$('connection-button').addEventListener('click',connectionDialog);
planCards($('landing-plans'));
route();
if(location.protocol==='file:')notice('Abra o site por http://localhost:5242. Arquivos abertos diretamente não possuem uma origem HTTP permitida pela API.',true);
