import {randomBytes, randomUUID} from 'node:crypto';
import * as TutorClassHttp from '../TutorClass.API/wwwroot/js/core/http.js';

const base = TutorClassHttp.normalizeBaseUrl(process.env.API_BASE_URL);
const result = [];
const record = (test, status, detail) => {result.push({test,status,...(detail?{detail}:{})});};
const ensure = (condition, message) => {if (!condition) throw new Error(message);};
async function login(email, senha, expectedRole) {
  const login = await TutorClassHttp.request(base, '/api/auth/login', {method:'POST', body:{email,senha},timeout:60000});
  ensure(typeof login?.accessToken === 'string' && login.accessToken.length > 0, 'Login sem token.');
  const me = await TutorClassHttp.request(base, '/api/auth/me', {token:login.accessToken,timeout:60000});
  ensure(me.email === email && (!expectedRole || me.perfil === expectedRole), 'Identidade retornada não corresponde à conta testada.');
  return me;
}
try {
  const health = await TutorClassHttp.request(base,'/health',{timeout:60000});
  ensure(health.service === 'TutorClass.API' && health.database?.status === 'Healthy','API ou banco indisponível.');
  record('health','PASS',health.database.provider);
  const swagger = await TutorClassHttp.request(base,'/swagger/v1/swagger.json',{timeout:60000});
  for (const [path,method] of [['/api/auth/registrar/aluno','post'],['/api/auth/login','post'],['/api/auth/me','get']])
    ensure(!!swagger.paths?.[path]?.[method], 'Endpoint ausente no Swagger: ' + path);
  record('contratos Swagger','PASS');

  // Cria UMA conta identificável de teste. Credenciais e tokens permanecem só em RAM.
  const email = 'integracao.' + randomUUID() + '@example.test';
  const senha = randomBytes(24).toString('base64url');
  const body = {nome:'Teste integração frontend',email,senha,dataNascimento:null};
  const registered = await TutorClassHttp.request(base,'/api/auth/registrar/aluno',{method:'POST',body,timeout:60000});
  ensure(registered?.id && registered.email === email,'Resposta de cadastro inesperada.');
  record('cadastro real na API','PASS',{id:registered.id,email});
  const me = await login(email,senha,'Aluno');
  ensure(me.id === registered.id,'Cadastro e login retornaram usuários diferentes.');
  record('login novo aluno e GET me','PASS');
  await login(email,senha,'Aluno');
  record('novo login da mesma conta já cadastrada','PASS');
  for (const [name,path,payload,expected] of [
    ['cadastro duplicado','/api/auth/registrar/aluno',body,'Email ja cadastrado.'],
    ['senha incorreta','/api/auth/login',{email,senha:randomBytes(24).toString('hex')},'Email ou senha invalidos.']
  ]) {
    let failed = false;
    try {await TutorClassHttp.request(base,path,{method:'POST',body:payload,timeout:60000});}
    catch(error) {ensure(error.status === 400 && error.message === expected,'Erro HTTP não corresponde ao contrato.');failed=true;}
    ensure(failed,'API deveria rejeitar ' + name);
    record(name,'PASS');
  }
  try {await TutorClassHttp.request(base,'/api/auth/me',{timeout:60000});throw new Error('me aceitou acesso anônimo.');}
  catch(error) {ensure(error.status === 401,'me não retornou 401 sem token.');record('me sem token','PASS');}

  for (const [prefix,role] of [['EXISTING',null],['ADMIN','Administrador']]) {
    const email=process.env['TUTORCLASS_'+prefix+'_EMAIL'], password=process.env['TUTORCLASS_'+prefix+'_PASSWORD'];
    if (!email || !password) {record('conta '+prefix,'PENDING','Credenciais não fornecidas.');continue;}
    await login(email,password,role);
    record('conta '+prefix+' login e me','PASS');
  }
  const origin=process.env.FRONTEND_ORIGIN || new URL(base).origin;
  if (origin === new URL(base).origin) {
    record('CORS', 'NOT_REQUIRED', 'Frontend e API na mesma origem.');
  } else {
  const preflight=await fetch(base+'/api/auth/login',{method:'OPTIONS',headers:{Origin:origin,'Access-Control-Request-Method':'POST','Access-Control-Request-Headers':'content-type,authorization'},signal:AbortSignal.timeout(60000)});
  record('CORS publicado',preflight.headers.get('access-control-allow-origin')===origin?'PASS':'BLOCKED',
    {origin,http:preflight.status,allowOrigin:preflight.headers.get('access-control-allow-origin')});
  }
} catch(error) {
  record('execução','FAIL',error.message);
  process.exitCode=1;
}
console.log(JSON.stringify({api:base,at:new Date().toISOString(),results:result},null,2));

