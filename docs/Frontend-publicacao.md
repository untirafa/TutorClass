# Publicação do front-end pelo ASP.NET Core

## Resultado

O front-end foi reestruturado por completo. Continua sendo servido pela própria API (`TutorClass.API/wwwroot`) na **mesma origem**: navegador → API ASP.NET Core → PostgreSQL/MongoDB. Não há CDN, fonte externa, imagem de terceiros ou etapa de build. Nenhum commit, push ou deploy foi executado.

Funcionalidades preservadas: cadastro e login (aluno, professor, responsável), busca e perfil público de professores, agendamento, aulas e pagamento simulado, materiais, TutorIA (SSE), mentoria com chat, acompanhamentos, gravações, financeiro, planos e assinatura, notificações, conta/privacidade, painel do responsável, perfil/disponibilidade do professor e módulos administrativos (candidaturas, qualificações, analytics, FAQ).

## Estrutura

```
TutorClass.API/wwwroot/
  index.html          casca: skip links, landmarks, regiões ao vivo, <main id="main">
  config.js           apiBaseUrl = window.location.origin
  assets/css/         tokens, base, layout, components (temas, alto contraste, escala de texto)
  assets/img/         logo-tutorclass.webp, hero.svg
  js/main.js          inicialização, rotas e foco
  js/routes.js        tabela de rotas   |  js/modules.js  módulos por perfil
  js/shell.js         cabeçalho (marca + casinha à esquerda; Acessibilidade, Avisos e Sair à direita), barra lateral, painel de acessibilidade
  js/core/plan.js     convite amigável de upgrade para quem está no plano Gratuito
  js/core/            http, session, api, router, a11y, dom, format
  js/lib/markdown.js  renderizador seguro (somente texto → elementos DOM)
  js/ui/              toast, dialog, form, components
  js/views/           uma tela por arquivo
```

Rotas por fragmento, sem barra inicial: `#home`, `#professores`, `#professores/:id`, `#professores?materia=<id>`, `#login`, `#cadastro-aluno|professor|responsavel`, `#app/:modulo/:id?`. O fragmento não vai ao servidor, portanto `/api/inexistente`, `/swagger/inexistente`, `/login` etc. continuam retornando 404 real.

## Arquivos antigos a remover

Substituídos pela nova estrutura (apague-os no seu repositório):

- `TutorClass.API/wwwroot/app.js`, `api.js`, `tutor-text.js`, `style.css`, `logo-tutorclass.jpeg`
- `tests/frontend-auth.test.mjs`, `frontend-chat-moderacao.test.mjs`, `frontend-http.test.mjs`, `frontend-stream.test.mjs`, `frontend-tutor-text.test.mjs`, `frontend-tutoria-errors.test.mjs` (a cobertura equivalente está em `tests/http`, `stream`, `markdown`, `router-format`)

## Executar

```powershell
dotnet run --project TutorClass.API --launch-profile http   # API + site em http://localhost:5242
npm install                                                  # só ferramentas de teste
npm run mock                                                 # API simulada em memória (porta 5242)
npm run serve                                                # só arquivos estáticos
```

Contas da API simulada: `aluno@`, `professor@`, `responsavel@`, `admin@`, `menor@teste.dev` (aluno de 12 anos, sem acesso ao financeiro) — senha `Senha@2026`. Na API real, use `POST /api/dev/test-data` (Development) ou cadastre-se: as contas do mock não existem no banco.

## Validação

```powershell
npm test                  # node --test tests/*.test.mjs  (29 testes)
npm run mock              # em outro terminal:
npm run audit:a11y        # axe-core (WCAG 2.2 A/AA) em 134 telas/estados
node scripts/e2e.mjs      # fluxos de aluno, responsável, professor e administrador
dotnet test TutorClass.Tests --configuration Release
```

Resultado nesta entrega: 29 testes Node aprovados; auditoria axe sem violações nas 134 combinações; e2e sem falhas. **Os testes .NET e a integração com a API real/Neon não foram executados** (sem SDK .NET no ambiente de desenvolvimento do front-end): `FrontendHostingTests.cs` foi atualizado para os novos caminhos e deve ser rodado por você.

## Render

Nenhuma variável nova. Depois de commit/push aprovados, o redeploy publica o novo `wwwroot`.
