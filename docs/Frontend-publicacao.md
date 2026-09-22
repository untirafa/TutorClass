# Publicação do frontend pelo ASP.NET Core

## Resultado preparado

Depois de commit, push e redeploy aprovados, **https://tutorclass.onrender.com/** deverá abrir o site público.
A mesma aplicação atende /api/..., /swagger e /health. Nenhum commit, push ou deploy foi executado nesta tarefa.

O fluxo permanece: navegador → API ASP.NET Core → PostgreSQL Neon.
Não há conexão do frontend com Neon nem novos segredos em arquivos públicos.

## Diff resumido e arquivos

| Arquivo | Alteração |
|---|---|
| TutorClass.API/wwwroot/index.html | Movido da raiz com os scripts da correção anterior preservados |
| TutorClass.API/wwwroot/style.css | Movido da raiz sem mudança de conteúdo |
| TutorClass.API/wwwroot/app.js | Movido da raiz sem mudança de conteúdo: cadastro, login, /me e sessão preservados |
| TutorClass.API/wwwroot/api.js | Movido da raiz sem mudança de conteúdo: JSON, Bearer, timeout e erros preservados |
| TutorClass.API/wwwroot/config.js | Movido; base da API agora é window.location.origin |
| TutorClass.API/Program.cs | UseDefaultFiles + UseStaticFiles em todos os ambientes; removida entrega manual exclusiva de Development; CORS adicional só em Development |
| TutorClass.API/appsettings.json | Retiradas as origens locais da configuração geral da primeira correção; conteúdo voltou ao equivalente do arquivo versionado |
| TutorClass.API/appsettings.Development.json | Mantidas as origens locais explícitas da correção anterior |
| render.yaml | Retirada configuração de CORS da produção |
| .dockerignore | Excluídas pastas .local do contexto Docker |
| TutorClass.Tests/AuthFlowTests.cs | Factory de testes aceita Production/Development com banco isolado, substituição completa do provedor e DataProtection efêmero |
| TutorClass.Tests/FrontendHostingTests.cs | Novos testes de arquivos públicos, endpoints, 404, autenticação e CORS em Production |
| TutorClass.Tests/FrontendCorsTests.cs | Testes de CORS local passam a rodar em Development |
| tests/frontend-auth.test.mjs | Caminho wwwroot e expectativa de mesma origem; testes anteriores preservados |
| tests/frontend-http.test.mjs | Caminho wwwroot; acrescentados testes de origem Render, domínio alternativo e localhost |
| scripts/check-auth.mjs | Import atualizado; mesma origem não exige preflight de CORS |
| scripts/serve-frontend.mjs | Arquivos lidos de wwwroot; ferramenta opcional de prévia estática, não substitui a API |
| README.md | Instruções atuais de publicação e desenvolvimento |
| docs/historico/Frontend-API-validacao.md | Relatório anterior preservado e identificado como histórico |
| docs/Frontend-publicacao.md | Este relatório |

Os hashes SHA-256 de app.js, api.js, index.html e style.css coincidiram antes/depois da mudança de pasta. As correções locais ainda não publicadas foram preservadas. Nenhuma mudança no domínio, banco, migrations ou contratos de autenticação.

O git diff sem staging mostra os arquivos antigos removidos e wwwroot como novo. São movimentações, não descarte do frontend. Não foi feito git add apenas para alterar a apresentação do diff.

## Como funciona

- wwwroot é a única pasta pública. O SDK Web já inclui seus arquivos no dotnet publish.
- UseDefaultFiles resolve / para index.html; UseStaticFiles entrega CSS/JS e futuros recursos públicos colocados em wwwroot.
- As rotas do frontend usam fragmentos: /#login, /#cadastro-aluno, /#app/inicio. O navegador não envia o fragmento ao servidor; não há necessidade de fallback.
- /api/inexistente, /swagger/inexistente, /health/inexistente e arquivos inexistentes retornam 404, não HTML.
- /appsettings.json e /.git/config não ficam acessíveis.
- config.js usa a origem da página, tanto no Render quanto em localhost. Preferências antigas de localStorage continuam ignoradas.
- Os endpoints reais continuam POST /api/auth/registrar/aluno, POST /api/auth/login e GET /api/auth/me. JWT é mantido em RAM e enviado como Authorization: Bearer. As mensagens da API continuam sendo exibidas.
- Não existe origem CORS extra em Production, mesmo se uma variável antiga permanecer no painel.

## Executar localmente

Na raiz do projeto:

```powershell
dotnet run --project TutorClass.API --launch-profile http
```

Abra http://localhost:5242. API, frontend, Swagger e health são servidos juntos.
Reinicie qualquer instância antiga em execução depois de atualizar os arquivos: os arquivos do frontend agora ficam em wwwroot.

Se não houver conexão PostgreSQL em Development, o modo SQLite local continua funcionando como antes. Contas locais não são automaticamente contas do Neon.

O script node scripts/serve-frontend.mjs continua disponível para prévia dos arquivos. Ele não implementa /api; para cadastro/login, prefira executar ASP.NET Core. Uma API local separada pode ser escolhida temporariamente no diálogo de conexão e precisa permitir a origem da prévia em Development.

## Validação

```powershell
node --test tests/frontend-*.test.mjs
dotnet test TutorClass.Tests --configuration Release --no-build --no-restore --verbosity quiet
dotnet publish TutorClass.API/TutorClass.API.csproj --configuration Release --no-restore --output artifacts/public-hosting /p:UseAppHost=false
```

- **24 testes JavaScript aprovados**, incluindo os testes anteriores de payloads, JWT, /me, erros, sessão parcial e os novos testes de origem dinâmica.
- **308 testes .NET aprovados na suíte completa**, sem falhas nem testes ignorados. Antes disso, os 48 testes selecionados de autenticação, Swagger, CORS Development e hospedagem Production também passaram.
- dotnet publish Release concluído; wwwroot e os cinco arquivos públicos estão no pacote.
- Pacote publicado executado com ambiente Production, a partir do diretório de publicação: /, CSS, os três scripts, /swagger e /swagger/v1/swagger.json retornaram 200; /health retornou JSON 200; /api/auth/me sem token retornou 401; rotas e arquivos privados/inexistentes retornaram 404.
- No teste do pacote, o banco estava propositalmente indisponível e /health informou Unhealthy/Degraded. Isso valida o endpoint, não a conectividade com Neon. A autenticação foi validada no host Production com banco isolado em memória.
- Docker não está instalado neste ambiente: **build/execução da imagem Docker não foram executados**. O Dockerfile existente já publica a API e copia /app/publish inteiro para a imagem final, incluindo wwwroot; não precisou de alteração.
- Não foi feita nova validação contra o Render, pois esta versão ainda não foi publicada. A validação da interface em navegador também não foi executada nesta etapa.

## Render e aprovação

Nenhuma variável nova é necessária para o frontend. Preserve as variáveis existentes do backend, principalmente ASPNETCORE_ENVIRONMENT=Production, ConnectionStrings__TutorClassDb e Jwt__Secret.

As antigas Cors__AllowedOrigins__* para localhost podem ser removidas do painel: são desnecessárias e ignoradas em Production. Não adicione https://tutorclass.onrender.com como solução de CORS: o navegador já usa a mesma origem.

Após aprovação, será necessário incluir os arquivos movidos/novos no commit, fazer push para a branch usada pelo Render e realizar redeploy (automático ou manual). Até isso ocorrer, o serviço publicado continua na versão anterior.

