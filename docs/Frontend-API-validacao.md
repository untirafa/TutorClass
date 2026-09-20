> Histórico da primeira etapa. A configuração e as instruções de CORS abaixo foram substituídas pela publicação na mesma origem. Consulte [Frontend-publicacao.md](Frontend-publicacao.md) para executar e publicar a versão atual. Os resultados registrados aqui são anteriores à reorganização em wwwroot.

# Integração do frontend com a API

## Diagnóstico confirmado em 19/09/2026 (Brasília)

- O site usava `localStorage["tutorclass.api"]` ou `http://localhost:5242`. Em outro computador esse endereço aponta para aquele computador.
- A API local pode usar SQLite quando não há conexão PostgreSQL configurada em Development. Um cadastro nesse banco não aparece no Neon. Não foi possível confirmar qual banco estava configurado no outro computador.
- A API publicada confirmada pelo usuário é **https://tutorclass.onrender.com**. Seu Swagger e health responderam; o health informou PostgreSQL saudável.
- O Render respondeu aos preflights das origens locais com HTTP 204, mas **sem Access-Control-Allow-Origin**. Isso impede chamadas do navegador, mesmo quando Swagger e testes HTTP diretos funcionam.
- O formulário ainda recusava um login válido se o perfil escolhido não coincidisse com o perfil retornado pela API. Agora o perfil vem de `GET /api/auth/me`.

## Contratos conferidos no Swagger publicado

| Operação | Endpoint | JSON enviado |
|---|---|---|
| Aluno | POST /api/auth/registrar/aluno | nome, email, senha: string; dataNascimento: string YYYY-MM-DD ou null |
| Professor | POST /api/auth/registrar/professor | nome, email, senha, biografia: string; precoHora: número |
| Login | POST /api/auth/login | email, senha: string |
| Conta autenticada | GET /api/auth/me | Sem corpo; Authorization: Bearer TOKEN |

Não existe `POST /api/auth/registrar` sem o perfil nesse Swagger. Os endpoints e payloads existentes foram preservados. Cadastro retorna 201 com o usuário; login retorna 200 com accessToken, expiresAtUtc e usuario.

## Configuração e execução

A URL está centralizada em `config.js`, carregado antes de `api.js` e `app.js`. O navegador não lê um arquivo .env automaticamente. Para outro ambiente, publique config.js com a URL correspondente, sem segredos e sem /api ou /swagger.

A antiga preferência de URL em localStorage é ignorada. “Configurar conexão” permite testar outra API apenas até recarregar a página; a troca só acontece depois de validar /health e encerra a sessão. JWT e senha não são gravados em armazenamento persistente.

Para servir **somente o frontend**, na raiz do repositório:

```powershell
node scripts/serve-frontend.mjs
```

Abra http://localhost:5242. Esse servidor entrega apenas os arquivos públicos do site, sem expor arquivos do repositório. Não inicia outra API nem outro banco.

Se a porta já estiver ocupada pela API local, encerre essa instância antes de usar o servidor estático, ou use a própria API local para servir o site:

```powershell
dotnet run --project TutorClass.API --launch-profile http
```

**Reinicie/recompile a API local após atualizar os arquivos**: a instância antiga não serve os novos /config.js e /api.js. Mesmo servido pela API local, o site passa a chamar a API do Render definida em config.js. Para testar deliberadamente a API local, use “Configurar conexão” e informe sua URL local.

Não abra index.html como file://. Não é necessário configurar Neon no computador que apenas executa o frontend.

## Aplicar CORS no Render (pendente na instância publicada)

No serviço da API, em Environment, configure os valores exatos abaixo e aplique/reinicie o serviço:

```text
Cors__AllowedOrigins__0=http://localhost:5242
Cors__AllowedOrigins__1=http://127.0.0.1:5242
```

Ao publicar o frontend em um domínio, acrescente somente sua origem real (protocolo e domínio, sem caminho ou barra final) como `Cors__AllowedOrigins__2`. Nenhuma URL publicada de frontend foi fornecida nesta tarefa.

O código já lê essas variáveis. As mesmas duas origens constam em appsettings.json e render.yaml para novos deploys. Variáveis antigas no painel podem substituir os valores do arquivo; confirme o painel mesmo após um deploy. Não apague uma origem publicada legítima já utilizada: mantenha-a em outro índice.

Em Development, as origens de desenvolvimento anteriormente aceitas (portas 3000 e 5173) continuam permitidas. Em produção, não foi usado AllowAnyOrigin, origem null ou wildcard. A configuração de CORS não altera autenticação/autorização.

Depois de aplicar, OPTIONS /api/auth/login com Origin http://localhost:5242, Access-Control-Request-Method POST e Access-Control-Request-Headers content-type,authorization deve retornar:
- Access-Control-Allow-Origin: http://localhost:5242
- Access-Control-Allow-Headers contendo content-type e authorization.

Um 204 sem esses cabeçalhos **não** indica sucesso de CORS.

## Testes e resultados

```powershell
node --test tests/frontend-*.test.mjs
dotnet test TutorClass.Tests --configuration Release --no-restore --filter "FullyQualifiedName~AuthFlowTests|FullyQualifiedName~SwaggerTests|FullyQualifiedName~FrontendCorsTests"
```

- **21 testes JavaScript aprovados**: transporte e handlers reais do formulário, com DOM/rede simulados. Cadastro de aluno/professor, payloads, configuração do Render apesar do localStorage antigo, login de aluno/admin, /me com Bearer, descarte de sessão parcial, 401, erros de domínio/validação, JSON inválido, timeout e falhas de rede.
- **Servidor estático validado**: index.html, config.js, api.js e app.js retornaram 200; caminhos do Git e da configuração do backend retornaram 404. Prova feita na porta temporária 5243, sem interromper a API local na 5242.
- **28 testes .NET aprovados**: autenticação e autorização (incluindo administrador em banco de teste), Swagger e CORS permitido/recusado. Banco isolado de testes; não são logins do administrador real no Neon.
- A primeira compilação Debug encontrou DLLs bloqueadas pela instância local em execução; a execução Release passou sem interromper essa instância.
- **API real do Render**: cadastro de um aluno, login, /me autenticado, segundo login da mesma conta, cadastro duplicado (400), senha incorreta (400) e /me sem token (401) passaram.
- O novo aluno retornou o ID `dd908206-595d-44cb-b334-d2ff7092cec7` e email `integracao.4cd50b26-0c37-4685-8d87-9dba0c8254de@example.test`. A conta de teste permaneceu no serviço; a senha foi gerada aleatoriamente e ficou apenas em RAM.
- A persistência foi verificada através da API (cadastro seguido de logins e /me), cujo health informa PostgreSQL. O banco Neon não foi acessado diretamente.
- **CORS publicado ainda bloqueado** na última consulta. Os testes HTTP Node não estão sujeitos ao CORS do navegador.
- **Pendente**: login de conta antiga do usuário e administrador real, pois não foram fornecidas credenciais.
- **Pendente**: validação visual e fluxo completo em navegador após aplicar CORS. A ferramenta de navegador não conseguiu anexar uma aba (timeout); não foi alegado sucesso visual.

Para repetir a prova real (cria uma nova conta de teste a cada execução):

```powershell
$env:API_BASE_URL='https://tutorclass.onrender.com'
node scripts/check-auth.mjs
```

O script aceita opcionalmente TUTORCLASS_EXISTING_EMAIL / TUTORCLASS_EXISTING_PASSWORD e TUTORCLASS_ADMIN_EMAIL / TUTORCLASS_ADMIN_PASSWORD fornecidos por ambiente seguro. Não coloque credenciais em arquivos versionados, histórico de comandos ou mensagens. Sem elas, esses casos são explicitamente PENDING. Não imprime senhas nem JWT.

Após atualizar o CORS, valide pelo site: cadastrar aluno; fazer login; abrir Minha conta; sair; entrar com conta antiga e administrador; informar senha incorreta e confirmar a mensagem real. Na aba Network do navegador, confira destino Render, POST JSON, cadastro 201, login 200 e GET /api/auth/me 200 com Bearer.

## Arquivos envolvidos

- config.js, api.js, app.js, index.html: configuração, transporte, autenticação e carregamento dos scripts.
- TutorClass.API/Program.cs: inclusão dos dois novos arquivos na lista pública servida em Development.
- TutorClass.API/appsettings.json, TutorClass.API/appsettings.Development.json, render.yaml: origens CORS explícitas.
- scripts/serve-frontend.mjs, scripts/check-auth.mjs: execução do frontend e validação real.
- tests/frontend-http.test.mjs, tests/frontend-auth.test.mjs, TutorClass.Tests/FrontendCorsTests.cs: regressões.
- README.md e este relatório: execução, publicação e limites da validação.

Não houve migration, alteração dos contratos de autenticação nem publicação automática no GitHub/Render.

