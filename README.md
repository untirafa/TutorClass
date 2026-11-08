# TutorClass

TutorClass e uma Web API em ASP.NET Core para uma plataforma marketplace que conecta alunos e professores particulares.

A API implementa o nucleo do dominio: usuarios, perfis principais, materias, candidatura de professores, qualificacoes, disponibilidade, aulas/agendamentos, acompanhamento do aluno, avaliacoes, reputacao, persistencia PostgreSQL/Neon e endpoints REST.

## Site público e API na mesma origem

Depois do deploy, **https://tutorclass.onrender.com/** abre o frontend público.
A mesma aplicação ASP.NET Core atende `/api/...`, `/swagger` e `/health`.

Os arquivos públicos estão em `TutorClass.API/wwwroot` e entram automaticamente no `dotnet publish` e na imagem Docker. `config.js` usa `window.location.origin`: em produção chama a API do próprio Render; localmente chama a API local. O front-end é feito de módulos ES nativos (sem build, sem CDN) com rotas por fragmento (`/#home`, `/#professores`, `/#login`, `/#app/inicio`), sem fallback que transforme erros da API em HTML, e segue a **WCAG 2.2 AA** (veja [acessibilidade](docs/Acessibilidade.md)).

Não é necessário configurar CORS para o site público na mesma origem. CORS adicional é habilitado somente em Development. Remova do painel do Render as antigas variáveis `Cors__AllowedOrigins__*` usadas para testes locais; mesmo presentes, são ignoradas em Production.

Veja [estrutura, publicação e validação](docs/Frontend-publicacao.md) e [acessibilidade](docs/Acessibilidade.md). Para testar sem a API: `npm install`, `npm run mock` (contas `aluno@`, `professor@`, `responsavel@`, `admin@teste.dev`, senha `Senha@2026`), `npm test` e `npm run audit:a11y`.
Cadastro, login, JWT Bearer, confirmação em `/api/auth/me` e mensagens reais da API permanecem preservados.

## Executar site e API em desenvolvimento

Na pasta raiz `TutorClass`, execute e mantenha o terminal aberto:

```powershell
dotnet run --project TutorClass.API --launch-profile http
```

- Site: http://localhost:5242
- Swagger: http://localhost:5242/swagger
- Estado da API e do banco: http://localhost:5242/health

Em **Development**, quando `ConnectionStrings:TutorClassDb` estiver vazia, a API cria automaticamente um banco SQLite persistente em `TutorClass.API/.local/tutorclass.db`, com as tabelas e materias iniciais. Nao precisa instalar PostgreSQL nem executar migrations para esse modo. As contas continuam existindo depois de reiniciar. O banco local e separado do Neon; dados nao sao transferidos automaticamente entre eles. Nao apague `.local` se quiser manter suas contas.

Se `Jwt:Secret` nao estiver configurado em Development, uma chave aleatoria e criada e reutilizada em `.local/jwt-secret.key`. A pasta `.local` e ignorada pelo Git. Uma connection string ou chave JWT configurada explicitamente continua tendo prioridade. Em producao, PostgreSQL e segredo JWT continuam obrigatorios; nao existe fallback automatico. O schema SQLite usa `EnsureCreated`: alteracoes futuras nas entidades exigem uma estrategia de atualizacao do banco local; nao aplique as migrations PostgreSQL nele ([documentacao EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers)).

O servidor entrega os arquivos de `TutorClass.API/wwwroot` em Development e Production. Reinicie a API apos atualizar o codigo. O site local chama a API local; suas contas podem estar no banco SQLite de desenvolvimento, separado do Neon.

Para verificar:

1. Abra `/health`: espere `status: Healthy` e `database.status: Healthy`. No modo local, `database.provider: SQLite`, `postgresql: NotConfigured` e `mongodb: NotConfigured` sao esperados. O chat com MongoDB continua exigindo configuracao propria.
2. No site, crie uma conta de aluno ou professor, com senha de pelo menos 8 caracteres. Para professor, informe preco por hora maior que zero. Saia e entre novamente com os mesmos dados.
3. No Swagger, abra `POST /api/auth/registrar/aluno`, clique em **Try it out** e envie um email ainda nao utilizado:

```json
{
  "nome": "Aluno de teste",
  "email": "aluno.teste@example.com",
  "senha": "SenhaDeTeste@2026",
  "dataNascimento": null
}
```

4. O cadastro retorna **201**. Execute `POST /api/auth/login` com o mesmo email e senha: deve retornar **200** e `accessToken`.
5. Em **Authorize**, cole somente o valor de `accessToken`, sem aspas nem `Bearer`. Execute `GET /api/auth/me`: deve retornar **200** com seu usuario. Cadastro/login sao publicos e nao exigem token.
6. Email repetido ou senha incorreta retornam **400** com mensagem explicativa. `/me` sem token retorna **401**. Professor recem-cadastrado ja pode entrar, mas so aparece no catalogo publico apos aprovacao.

Validacao automatizada: `dotnet test TutorClass.Tests`. Para usar Neon/PostgreSQL, siga a configuracao e as migrations abaixo; essas etapas sao opcionais para editar o HTML/CSS localmente.

## Arquitetura

```text
TutorClass.slnx
TutorClass.API
TutorClass.Application
TutorClass.Domain
TutorClass.Infrastructure
TutorClass.Tests
```

- `Domain`: entidades, enums e regras centrais. Exemplo: uma candidatura so muda de estado por metodos como `Aprovar`, `Reprovar` e `SolicitarComplementacao`.
- `Application`: DTOs e services de casos de uso. Controllers nao expõem entidades EF.
- `Infrastructure`: EF Core, Npgsql, `DbContext`, Fluent API, seed de materias e migrations.
- `API`: controllers REST, OpenAPI, middleware global de excecoes e composicao via DI.
- `Tests`: testes unitarios das regras principais do dominio.

## Modelagem Inicial

### Heranca de usuarios

`Usuario` e a classe base abstrata para:

- `Aluno`
- `Responsavel`
- `Professor`
- `Administrador`

No EF Core foi escolhida a estrategia TPH, com discriminador `tipo_usuario`, porque os perfis compartilham dados basicos de conta e a primeira fase ainda nao justifica tabelas separadas para cada subtipo. Essa decisao simplifica login/autorizacao futura sem remover a especializacao de dominio.

No PostgreSQL, todos esses perfis ficam na tabela `usuarios`. Nao existem tabelas separadas `professores`, `alunos`, `responsaveis` ou `administradores`; consultas manuais devem filtrar por `tipo_usuario`.

### Convencao PostgreSQL

O modelo C# usa nomes idiomaticos como `Id`, `Nome`, `Email` e `CreatedAtUtc`, mas o modelo relacional no PostgreSQL usa `snake_case` para tabelas, colunas, indices e constraints da aplicacao.

Exemplos de consulta:

```sql
select id, nome, email, tipo_usuario
from usuarios
order by nome;
```

```sql
select id, nome, email
from usuarios
where tipo_usuario = 'Professor'
order by nome;
```

A view `vw_professores` existe apenas para facilitar consultas administrativas/manuais de leitura. Ela nao substitui a entidade `Professor` nem e usada como fonte principal pela aplicacao.

### Relacionamentos e cardinalidades

- Professor x Materia: N:N via `ProfessorMateria`.
- Responsavel x Aluno: N:N via `ResponsavelAluno`, permitindo varios responsaveis por aluno e varios alunos por responsavel.
- Professor x Qualificacao: 1:N.
- Professor x Disponibilidade: 1:N.
- Professor x Candidatura: 1:N historico, com regra de dominio para impedir mais de uma candidatura ativa.
- Aula relaciona Aluno, Professor e Materia, preservando o preco acordado no momento da solicitacao.
- Gravacao de aula: 1 aula pode possuir no maximo 1 gravacao simulada, armazenando apenas metadados e URL externa.
- Acompanhamento: 1 aula concluida pode possuir no maximo 1 registro de acompanhamento do professor.
- Avaliacao: 1 aula concluida pode gerar no maximo 1 avaliacao do aluno para aquele professor.

## Candidatura, Qualificacao, Professor e Avaliacao

`CandidaturaProfessor` possui estados:

- `Pendente`
- `EmAnalise`
- `AguardandoDocumentacao`
- `Aprovada`
- `Reprovada`

As transicoes sao encapsuladas. O codigo evita alteracao arbitraria como `candidatura.Status = ...`.

`QualificacaoProfessor` representa evidencias profissionais verificaveis, sem exigir diploma universitario obrigatorio. Tipos iniciais:

- `Graduacao`
- `PosGraduacao`
- `Curso`
- `Certificacao`
- `Experiencia`
- `Outro`

Status de verificacao:

- `Pendente`
- `Verificada`
- `Rejeitada`

`Professor` guarda biografia, preco por hora, materias, qualificacoes, disponibilidade, status de aprovacao e reputacao profissional.

`AvaliacaoProfessor` esta vinculada obrigatoriamente a uma `Aula` concluida. Apenas o aluno daquela aula pode avaliar, uma unica vez, com nota de 1 a 5 e comentario opcional.

## Aula e Agendamento

`Aula` representa uma aula particular contratada e agendada pelo aluno com um professor. O TutorClass nao possui aulas publicas ao vivo, transmissao promocional, visitantes assistindo aulas ou aulas abertas usadas como propaganda de professores.

Fluxo oficial:

```text
Aluno contrata professor
-> agenda Aula
-> Aula acontece ao vivo
-> Aula e concluida
-> dependendo do plano, a gravacao pode ser acessada posteriormente
```

Todos os planos podem contratar e participar da aula ao vivo. A diferenca e posterior: Gratuito e Foco nao acessam gravacoes; Mentoria pode rever as gravacoes das proprias aulas elegiveis.

`Aula` guarda:

- aluno;
- professor;
- materia;
- inicio UTC;
- duracao em minutos;
- preco acordado;
- status;
- timestamps de confirmacao, cancelamento e conclusao.

O preco acordado e calculado pelo backend com base no preco por hora atual do professor e na duracao solicitada. Esse valor fica gravado na aula para preservar o historico caso o professor altere seu preco no futuro.

Estados de aula:

- `Solicitada`
- `Confirmada`
- `Concluida`
- `Cancelada`

Exemplos concretos de POO usados:

- encapsulamento: `Status` possui setter privado;
- invariantes: duracao deve estar entre 30 e 240 minutos, e preco acordado deve ser maior que zero;
- comportamento no dominio: transicoes acontecem por `Confirmar`, `Cancelar` e `Concluir`;
- composicao: uma aula compoe referencias a aluno, professor e materia sem criar heranca artificial.

Regras principais:

- apenas professores aprovados e ativos podem receber aula;
- o aluno autenticado solicita aula para si;
- o professor precisa ensinar a materia;
- o horario precisa estar no futuro e dentro da disponibilidade cadastrada;
- aluno e professor nao podem ter aulas ativas sobrepostas;
- professor so confirma/conclui aula propria;
- aluno ou professor da aula podem cancelar;
- aula cancelada nao pode ser concluida;
- somente aula confirmada pode virar concluida.

## Gravacoes das Aulas

As gravacoes do TutorClass sao simuladas nesta versao academica e continuam sendo um beneficio oficial do plano Mentoria.

`GravacaoAula` representa a gravacao de uma aula particular ao vivo que aconteceu anteriormente. Ela nao representa video independente, material complementar, biblioteca publica, conteudo promocional ou publicacao escolhida pelo professor.

Conceitualmente, em producao:

```text
Aula termina
-> sistema de gravacao processa a midia
-> storage externo recebe o arquivo
-> TutorClass recebe uma URL protegida
-> gravacao fica disponivel conforme o plano do aluno
```

Nesta versao academica, a captura da videochamada nao e realizada de verdade. O backend utiliza uma midia demonstrativa vinculada a uma `Aula` real para simular a gravacao resultante. A midia e simulada; o vinculo com a aula, a persistencia dos metadados e a autorizacao sao reais.

O backend armazena apenas metadados e a URL de um video externo:

- aula vinculada;
- titulo;
- descricao opcional;
- URL do video;
- miniatura opcional;
- duracao em segundos;
- data de disponibilizacao;
- status `Disponivel`.

O PostgreSQL nao armazena arquivo de video nem conteudo binario. A URL de demonstracao pode apontar para um video publico de teste; em producao, o ideal seria usar storage privado/CDN com URLs temporarias assinadas, expiracao e controle de acesso no provedor de midia.

A autorizacao de acesso e real:

- o usuario precisa estar autenticado como aluno;
- a aula precisa pertencer ao aluno autenticado;
- o aluno precisa possuir Mentoria ativa no momento da consulta;
- a aula precisa estar concluida;
- a gravacao precisa estar disponivel.

Alunos dos planos Gratuito e Foco podem contratar e participar da aula ao vivo, mas recebem bloqueio ao tentar rever a gravacao depois. Um aluno Mentoria tambem nao consegue acessar gravacao de aula de outro aluno.

Administrador pode cadastrar, atualizar ou desativar manualmente a referencia da gravacao como operacao tecnica de simulacao ou correcao. Essa operacao nao significa que o professor escolhe publicar uma gravacao: o beneficio pertence ao plano Mentoria e a gravacao representa o resultado simulado do processamento externo da aula concluida.

O upload, streaming proprio, WebRTC, transcodificacao, CDN e DRM ficaram fora do escopo desta versao.

Endpoints de gravacao:

```text
POST /api/aulas/{aulaId}/gravacao
PUT  /api/aulas/{aulaId}/gravacao
POST /api/aulas/{aulaId}/gravacao/desativar
GET  /api/aulas/{aulaId}/gravacao
GET  /api/gravacoes/minhas
```

Exemplo de cadastro de referencia:

```json
{
  "titulo": "Biologia - Sistema Respiratorio",
  "urlVideo": "https://www.w3schools.com/html/mov_bbb.mp4",
  "duracaoSegundos": 3120,
  "descricao": "Video externo de demonstracao.",
  "miniaturaUrl": null
}
```

Fluxo manual rapido pelo Swagger:

1. Execute `POST /api/dev/test-data`.
2. Na resposta, localize `gravacoesSimuladas`.
3. Login como administrador: `admin.dev@tutorclass.local` / `TutorClass@123`.
4. Use `POST /api/aulas/{aulaId}/gravacao` em uma aula concluida que ainda nao possua gravacao, ou `PUT /api/aulas/{aulaId}/gravacao` para simular/corrigir uma referencia tecnica existente.
5. Login como `ana.clara.dev@tutorclass.local` e chame `GET /api/aulas/{aulaIdDaAna}/gravacao`: esperado bloqueio por plano Gratuito.
6. Login como `bruno.lima.dev@tutorclass.local` e chame `GET /api/aulas/{aulaIdDoBruno}/gravacao`: esperado bloqueio por plano Foco.
7. Login como `camila.rocha.dev@tutorclass.local` e chame `GET /api/aulas/{aulaIdDaCamila}/gravacao`: esperado retorno com URL e metadados.
8. Ainda como Camila, tente acessar o `aulaId` de Ana ou Bruno: esperado bloqueio por nao ser a aluna daquela aula.
9. Use `GET /api/gravacoes/minhas` como Camila para listar apenas as gravacoes disponiveis dela.

## Machine Learning e Analise de Dados

O TutorClass possui um bloco academico simples de Machine Learning e Analise de Dados para gerar indicadores agregados do negocio ficticio.

Os indicadores sao calculados a partir dos dados estruturados do PostgreSQL/Neon, como usuarios, professores, aulas, avaliacoes, assinaturas, mentorias e pagamentos simulados. O modulo nao usa MongoDB, nao analisa mensagens do chat da Mentoria e nao utiliza IA generativa.

Indicadores disponiveis:

- total de alunos ativos;
- total de professores aprovados;
- total de aulas;
- aulas concluidas;
- aulas canceladas;
- media geral de avaliacoes;
- quantidade de mentorias ativas;
- assinaturas ativas por plano, incluindo alunos gratuitos sem assinatura paga;
- materia mais procurada;
- professor mais contratado;
- receita simulada de comissao da plataforma;
- repasses simulados aos professores;
- adicionais simulados de Mentoria.

O ranking de materias mostra quantidade de aulas, percentual sobre o total e uma tendencia simples (`Baixa`, `Media` ou `Alta`) baseada na comparacao entre os ultimos 30 dias e os 30 dias anteriores.

A previsao de demanda por materia usa um classificador supervisionado simples por centroides. O modelo e treinado em memoria com um dataset academico pequeno de exemplos rotulados como `Baixa`, `Media` e `Alta`, e classifica as materias atuais usando features agregadas do banco:

- aulas nos ultimos 30 dias;
- aulas nos 30 dias anteriores;
- crescimento percentual;
- quantidade de alunos distintos;
- taxa de conclusao.

Essa implementacao e demonstrativa e explicavel. Ela existe para evidenciar uso de uma tecnica real e simples de Machine Learning no backend, nao para substituir uma solucao avancada de ciencia de dados, AutoML ou pipeline em cloud.

Endpoints administrativos:

```text
GET /api/admin/analytics/resumo
GET /api/admin/analytics/materias
GET /api/admin/analytics/demanda-materias
```

Todos os endpoints de Analytics exigem perfil `Administrador` e retornam apenas dados agregados, sem e-mails, senhas, documentos, mensagens privadas ou conteudo de chat.

## FAQ e ChatBot de Ajuda

O FAQ/ChatBot de Ajuda e um assistente simples baseado em perguntas e respostas cadastradas no PostgreSQL. Ele existe para explicar o funcionamento do TutorClass e nao utiliza IA, embeddings, NLP avancado, MongoDB, historico de conversa ou TutorIA.

A TutorIA continua sendo um recurso separado para duvidas educacionais e estudo. Regras atuais:

- Gratuito: sem acesso a TutorIA;
- Foco: 7 mensagens por dia;
- Mentoria: sem limite diario imposto pelo TutorClass.

O FAQ esta disponivel para todos os planos e cada mensagem do ChatBot e tratada de forma independente. A correspondencia usa uma pontuacao simples e previsivel:

- pergunta exata;
- palavras-chave;
- trechos da pergunta;
- categoria;
- fallback quando nao ha correspondencia suficiente.

`PerguntaFrequente` guarda:

- pergunta;
- resposta;
- categoria;
- palavras-chave;
- ativo/inativo;
- ordem de exibicao;
- texto e rota de acao opcionais;
- timestamps de criacao e atualizacao.

Categorias iniciais:

- `Conta`
- `Aulas`
- `Professores`
- `Pagamentos`
- `Planos`
- `Mentoria`
- `TutorIA`
- `Gravacoes`

Endpoints publicos:

```text
GET  /api/faq
GET  /api/faq?categoria=Mentoria
GET  /api/faq?termo=cancelar
POST /api/faq/chat
```

Exemplo de ChatBot:

```json
{
  "mensagem": "Como cancelo uma aula?"
}
```

Endpoints administrativos, restritos ao perfil `Administrador`:

```text
POST /api/admin/faq
PUT  /api/admin/faq/{id}
POST /api/admin/faq/{id}/desativar
POST /api/admin/faq/{id}/ativar
```

Exemplo de criacao administrativa:

```json
{
  "pergunta": "Como cancelar uma aula?",
  "resposta": "Voce pode cancelar uma aula pela area de suas aulas, respeitando as regras de cancelamento existentes.",
  "categoria": "Aulas",
  "palavrasChave": "cancelar,cancelamento,cancelo,desistir,aula",
  "ordemExibicao": 1,
  "textoAcao": "Ver minhas aulas",
  "rotaAcao": "/minhas-aulas"
}
```

## Notificacoes Internas

As notificacoes do TutorClass sao internas e persistidas no PostgreSQL. Elas foram pensadas para o sino/central de notificacoes do frontend e sao geradas pelo backend a partir de eventos relevantes de negocio.

Nesta etapa nao existem push notifications, e-mails, SMS, WhatsApp, notificacoes do sistema operacional, filas externas ou SignalR para notificacoes gerais. Notificacoes do chat da Mentoria tambem nao fazem parte deste modulo; o chat continua com seu controle proprio de tempo real e nao gera uma notificacao persistente por mensagem.

`Notificacao` guarda:

- usuario destinatario;
- tipo;
- titulo;
- mensagem;
- status de leitura;
- data de leitura opcional;
- rota de acao opcional para o frontend;
- referencia logica opcional para o recurso de origem;
- chave de evento para evitar duplicidade;
- prioridade simples (`Normal` ou `Importante`).

Tipos atuais:

- `AulaSolicitada`
- `AulaConfirmada`
- `AulaCancelada`
- `AulaReagendada`
- `GravacaoDisponivel`
- `AcompanhamentoMentoriaConcluido`
- `MentoriaIniciada`
- `MentoriaEncerrada`
- `MentorAlterado`
- `PagamentoConfirmado`
- `AssinaturaConfirmada`
- `NovoMaterial`

Eventos automaticos implementados:

- aluno solicita aula: professor recebe aviso;
- professor confirma aula: aluno recebe aviso;
- aluno/professor/responsavel autorizado reagenda aula: a outra parte relevante recebe aviso;
- aluno ou professor cancela aula: a outra parte relevante recebe aviso;
- gravacao de aula fica disponivel: aluno Mentoria dono da aula recebe aviso;
- acompanhamento de Mentoria e concluido: aluno recebe aviso;
- aluno seleciona mentor: professor e aluno recebem aviso;
- aluno troca mentor: professor antigo, novo professor e aluno recebem avisos;
- pagamento de aula confirmado: pagador recebe aviso; se o pagador for responsavel, o vinculo Responsavel x Aluno ja foi validado pelo fluxo financeiro;
- cobranca de assinatura confirmada: pagador recebe aviso.
- professor disponibiliza material pedagogico: aluno destinatario recebe aviso.

Notificacao nao e log. O backend cria apenas alertas relevantes para a experiencia do usuario.

Endpoints:

```text
GET  /api/notificacoes
GET  /api/notificacoes?somenteNaoLidas=true&page=1&pageSize=20
GET  /api/notificacoes/resumo
POST /api/notificacoes/{id}/marcar-lida
POST /api/notificacoes/marcar-todas-lidas
```

O usuario autenticado sempre consulta e altera apenas as proprias notificacoes. Nao existe endpoint publico para o frontend criar notificacoes arbitrarias.

## Responsavel e Alunos Vinculados

O Responsavel e um usuario separado no TutorClass. Ele nao e segundo dono da conta do aluno, nao faz login como aluno e nao recebe token do aluno. Suas permissoes derivam exclusivamente do vinculo `ResponsavelAluno`.

Regra central:

```text
Responsavel autenticado
-> solicita acao para Aluno X
-> backend valida ResponsavelAluno
-> se nao houver vinculo valido: 403 Forbidden
```

O service `ResponsavelAlunoService` centraliza a validacao de vinculo e evita que cada controller implemente uma regra diferente.

Responsavel vinculado pode:

- listar seus alunos vinculados;
- consultar aulas/agendamentos do aluno;
- solicitar, reagendar e cancelar aula em nome do aluno;
- pagar aula e assinatura do aluno;
- consultar plano e historico financeiro do aluno;
- ver mentores ativos e historico resumido de Mentoria;
- consultar historico do chat da Mentoria em modo somente leitura;
- consultar suas proprias notificacoes.

Responsavel nao pode:

- acessar aluno nao vinculado;
- alterar login, senha ou dados sensiveis da conta do aluno;
- usar TutorIA em nome do aluno;
- escolher, trocar ou encerrar mentor;
- enviar mensagem no chat da Mentoria;
- entrar nos grupos SignalR do chat;
- acessar gravacoes ou receber `UrlVideo`;
- ver conteudo pedagogico detalhado do acompanhamento;
- acessar notificacoes privadas do aluno.

Ao solicitar aula para um aluno vinculado, a `Aula` continua registrando o aluno beneficiario em `AlunoId` e registra o executor em `SolicitadaPorUsuarioId`. Em cancelamentos e reagendamentos, os campos de auditoria tambem preservam quem executou a acao.

Endpoints:

```text
GET  /api/responsavel/alunos
GET  /api/responsavel/alunos/{alunoId}/aulas
POST /api/responsavel/alunos/{alunoId}/aulas
PUT  /api/responsavel/alunos/{alunoId}/aulas/{aulaId}/reagendar
POST /api/responsavel/alunos/{alunoId}/aulas/{aulaId}/cancelar
GET  /api/responsavel/alunos/{alunoId}/plano
POST /api/responsavel/alunos/{alunoId}/assinatura
GET  /api/responsavel/alunos/{alunoId}/financeiro/historico
POST /api/responsavel/alunos/{alunoId}/aulas/{aulaId}/pagar
GET  /api/responsavel/alunos/{alunoId}/mentoria
GET  /api/responsavel/alunos/{alunoId}/mentoria/acompanhamentos
GET  /api/responsavel/alunos/{alunoId}/mentorias/{vinculoMentoriaId}/chat/mensagens
GET  /api/responsavel/alunos/{alunoId}/aulas/{aulaId}/gravacao/resumo
```

O historico do chat e a unica excecao de privacidade desta etapa: por seguranca e acompanhamento parental, o Responsavel vinculado pode consultar mensagens ja persistidas, sempre por HTTP e somente leitura. Ele nao envia mensagens e nao participa do SignalR.

## Avaliacao x Reputacao

Avaliacoes por estrelas representam a opiniao dos alunos apos aulas concluidas:

- nota de 1 a 5 por aula concluida;
- apenas uma avaliacao por aula;
- media de estrelas calculada pelo backend;
- quantidade de avaliacoes calculada pelo backend;
- `AulasConcluidas` e incrementado quando o professor conclui uma aula.

Reputacao profissional representa confiabilidade e trajetoria dentro da TutorClass:

- `Novo`
- `Verificado`
- `Experiente`
- `Destaque`
- `Referencia`

Regra atual de progressao:

- `Novo`: ainda sem avaliacoes;
- `Verificado`: a partir de 1 avaliacao;
- `Experiente`: pelo menos 5 aulas concluidas, 3 avaliacoes e media >= 4,0;
- `Destaque`: pelo menos 15 aulas concluidas, 10 avaliacoes e media >= 4,5;
- `Referencia`: pelo menos 30 aulas concluidas, 20 avaliacoes e media >= 4,7.

Planos pagos e publicidade nao alteram estrelas, qualificacoes verificadas ou reputacao profissional. Um destaque pago futuro deve aparecer explicitamente como `Patrocinado`.

## Materias Iniciais

O seed da migration inclui somente:

- Portugues
- Literatura
- Ingles
- Espanhol
- Artes
- Historia
- Geografia
- Filosofia
- Sociologia
- Fisica
- Quimica
- Biologia

## Endpoints Iniciais

- `GET /api/materias`
- `GET /api/professores`
- `GET /api/professores/{professorId}`
- `POST /api/professores`
- `POST /api/professores/{professorId}/materias`
- `POST /api/professores/{professorId}/qualificacoes`
- `POST /api/professores/{professorId}/disponibilidades`
- `GET /api/professores/{professorId}/disponibilidades`
- `GET /api/professores/{professorId}/horarios-livres?data=AAAA-MM-DD`
- `GET /api/professores/me/disponibilidades`
- `POST /api/professores/me/disponibilidades`
- `PUT /api/professores/me/disponibilidades/{disponibilidadeId}`
- `DELETE /api/professores/me/disponibilidades/{disponibilidadeId}`
- `POST /api/professores/{professorId}/candidaturas`
- `GET /api/candidaturas-professor/{candidaturaId}`
- `POST /api/admin/candidaturas-professor/{candidaturaId}/aprovar`
- `POST /api/admin/candidaturas-professor/{candidaturaId}/reprovar`
- `POST /api/admin/candidaturas-professor/{candidaturaId}/solicitar-complementacao`
- `POST /api/aulas`
- `GET /api/aulas/{id}`
- `GET /api/aulas/minhas`
- `POST /api/aulas/{id}/confirmar`
- `POST /api/aulas/{id}/cancelar`
- `POST /api/aulas/{id}/concluir`
- `PUT /api/aulas/{aulaId}/acompanhamento`
- `GET /api/aulas/{aulaId}/acompanhamento`
- `POST /api/aulas/{aulaId}/avaliacao`
- `GET /api/professores/{professorId}/avaliacoes`

## CORS

O site e a API compartilham a origem. Em Production não há necessidade de liberar origens externas.
A política só usa `Cors:AllowedOrigins` em Development, com origens locais explícitas em `appsettings.Development.json` (localhost/127.0.0.1 nas portas 5242, 3000 e 5173).
Para um frontend separado durante desenvolvimento, ajuste essa lista à origem real, sem wildcard.

## Configuracao Neon/PostgreSQL

Nao ha connection string real no codigo-fonte.

Em desenvolvimento, use User Secrets ou variavel de ambiente:

```powershell
dotnet user-secrets set "ConnectionStrings:TutorClassDb" "<SUA_CONNECTION_STRING_DO_NEON>" --project TutorClass.API
```

Alternativa por ambiente:

```powershell
$env:ConnectionStrings__TutorClassDb="<SUA_CONNECTION_STRING_DO_NEON>"
```

Aplicar migrations:

```powershell
dotnet dotnet-ef database update --project TutorClass.Infrastructure --startup-project TutorClass.API
```

## Execucao

```powershell
dotnet restore TutorClass.slnx
dotnet build TutorClass.slnx
dotnet test TutorClass.slnx
dotnet run --project TutorClass.API
```

Ao iniciar, procure no terminal uma mensagem parecida com:

```text
Now listening on: http://localhost:5242
Hosting environment: Development
```

No ambiente Development:

```text
Health:  http://localhost:5242/health
Swagger: http://localhost:5242/swagger
OpenAPI: http://localhost:5242/swagger/v1/swagger.json
```

No modo local SQLite, a API prepara o banco durante a inicializacao. Com PostgreSQL configurado, os endpoints dependem da connection string e das migrations aplicadas manualmente.

Para configurar um segredo JWT proprio (obrigatorio fora de Development):

```powershell
dotnet user-secrets set "Jwt:Secret" "<SUA_CHAVE_JWT_FORTE>" --project TutorClass.API
```

Opcionalmente, ajuste:

```powershell
dotnet user-secrets set "Jwt:Issuer" "TutorClass" --project TutorClass.API
dotnet user-secrets set "Jwt:Audience" "TutorClass.Client" --project TutorClass.API
dotnet user-secrets set "Jwt:ExpirationMinutes" "120" --project TutorClass.API
```

## User Secrets e Migration

Configure a connection string do Neon sem colocar credenciais no codigo:

```powershell
dotnet user-secrets set "ConnectionStrings:TutorClassDb" "<SUA_CONNECTION_STRING_DO_NEON>" --project TutorClass.API
```

A connection string do Neon deve usar PostgreSQL com SSL. Depois, aplique a migration:

```powershell
dotnet dotnet-ef database update --project TutorClass.Infrastructure --startup-project TutorClass.API
```

Para confirmar no Neon, abra o painel do projeto e verifique se existem tabelas como `usuarios`, `materias`, `professores_materias`, `candidaturas_professor`, `qualificacoes_professor`, `disponibilidades_professor`, `aulas`, `acompanhamentos_aula`, `avaliacoes_professor` e `__EFMigrationsHistory`. A tabela `materias` deve conter as 12 materias iniciais.

## Fluxo Manual: Candidatura de Professor

Endpoint temporario apenas para `Development`:

```http
POST /api/dev/test-data
```

Ele cria ou reutiliza:

- `admin.dev@tutorclass.local`
- 5 alunos ficticios
- 2 responsaveis ficticios
- 5 professores ficticios oficiais:
  - `helena.biologia.dev@tutorclass.local`
  - `rafael.fisica.dev@tutorclass.local`
  - `patricia.portugues.dev@tutorclass.local`
  - `sofia.ingles.dev@tutorclass.local`
  - `marcos.historia.dev@tutorclass.local`

O retorno informa os IDs e e-mails criados ou reutilizados. Para acoes administrativas, faca login como `admin.dev@tutorclass.local` e use o JWT no Swagger.

Roteiro no Swagger:

1. Execute `POST /api/dev/test-data` e copie os IDs.
2. Confirme que o professor ainda nao aparece em `GET /api/professores`.
3. Adicione qualificacao:

```json
{
  "tipo": "Certificacao",
  "titulo": "Certificacao de idioma",
  "instituicao": "Instituicao Teste",
  "descricao": "Evidencia temporaria sem upload real de arquivo.",
  "dataConclusao": "2025-05-10",
  "documentoComprovacao": "https://example.com/certificado.pdf"
}
```

4. Envie candidatura:

```json
{
  "mensagem": "Tenho experiencia com aulas particulares e quero atuar como professor na TutorClass."
}
```

5. Consulte a candidatura em `GET /api/candidaturas-professor/{candidaturaId}`.
6. Tente aprovar antes de verificar a qualificacao. A API deve retornar erro de dominio informando que o professor precisa possuir qualificacao verificada.
7. Inicie analise:

```json
{
  "motivo": null
}
```

8. Liste as qualificacoes do professor em `GET /api/professores/{professorId}/qualificacoes` e copie o `id` da qualificacao.
9. Verifique a qualificacao:

Use em:

```http
POST /api/admin/qualificacoes/{qualificacaoId}/verificar
```

10. Aprove a candidatura:

```json
{
  "motivo": null
}
```

11. Chame `GET /api/professores` e confirme que o professor aprovado aparece.

Alternativas de decisao administrativa, sempre apos iniciar analise:

```json
{
  "motivo": "Documento ilegivel. Envie uma nova comprovacao."
}
```

Use esse corpo em `solicitar-complementacao` ou `reprovar`, ajustando o motivo.

Para rejeitar uma qualificacao pendente:

```http
POST /api/admin/qualificacoes/{qualificacaoId}/rejeitar
```

Nao envie `administradorId` no corpo. O administrador responsavel vem do JWT autenticado.

## Autenticacao e JWT

Cadastro publico:

```http
POST /api/auth/registrar/aluno
POST /api/auth/registrar/responsavel
```

Login:

```http
POST /api/auth/login
```

```json
{
  "email": "admin.dev@tutorclass.local",
  "senha": "TutorClass@123"
}
```

Usuario atual:

```http
GET /api/auth/me
```

No Swagger:

1. Execute `POST /api/auth/login`.
2. Copie `accessToken`.
3. Clique em `Authorize`.
4. Cole apenas o token. O Swagger envia como `Bearer TOKEN`.
5. Chame endpoints protegidos.

Credenciais de desenvolvimento geradas por `POST /api/dev/test-data`:

```text
admin.dev@tutorclass.local / TutorClass@123
ana.clara.dev@tutorclass.local / TutorClass@123
bruno.lima.dev@tutorclass.local / TutorClass@123
camila.rocha.dev@tutorclass.local / TutorClass@123
diego.costa.dev@tutorclass.local / TutorClass@123
lara.nunes.dev@tutorclass.local / TutorClass@123
mariana.martins.dev@tutorclass.local / TutorClass@123
ricardo.nunes.dev@tutorclass.local / TutorClass@123
helena.biologia.dev@tutorclass.local / TutorClass@123
rafael.fisica.dev@tutorclass.local / TutorClass@123
patricia.portugues.dev@tutorclass.local / TutorClass@123
sofia.ingles.dev@tutorclass.local / TutorClass@123
marcos.historia.dev@tutorclass.local / TutorClass@123
```

O endpoint dev so responde em `Development`. Em `Production`, ele retorna `404`.

Professores aprovados e visiveis em `GET /api/professores`:

```text
Helena Duarte - Biologia - R$ 85/h
Rafael Moreira - Fisica - R$ 95/h
Patricia Azevedo - Portugues/Literatura - R$ 78/h
```

Professores nao visiveis na busca publica:

```text
Sofia Almeida - Ingles - candidatura EmAnalise, qualificacao pendente
Marcos Vieira - Historia/Sociologia - candidatura Pendente
```

Endpoints administrativos agora exigem JWT de `Administrador`:

```text
POST /api/admin/candidaturas-professor/{id}/iniciar-analise
POST /api/admin/candidaturas-professor/{id}/aprovar
POST /api/admin/candidaturas-professor/{id}/reprovar
POST /api/admin/candidaturas-professor/{id}/solicitar-complementacao
POST /api/admin/qualificacoes/{id}/verificar
POST /api/admin/qualificacoes/{id}/rejeitar
```

Endpoints do professor exigem JWT de `Professor` e so permitem atuar no proprio `professorId`:

```text
POST /api/professores/{professorId}/materias
POST /api/professores/{professorId}/qualificacoes
POST /api/professores/{professorId}/disponibilidades
POST /api/professores/{professorId}/candidaturas
```

## Fluxo Manual: Aula e Agendamento

Use primeiro:

```http
POST /api/dev/test-data
```

Depois, no Swagger:

1. Faca login como aluno:

```json
{
  "email": "ana.clara.dev@tutorclass.local",
  "senha": "TutorClass@123"
}
```

2. Clique em `Authorize` e cole o `accessToken`.
3. Execute `GET /api/professores` e copie o `id` de `Helena Duarte`.
4. Execute `GET /api/materias` e copie o `id` de `Biologia`.
5. Consulte a disponibilidade:

```http
GET /api/professores/{professorId}/disponibilidades
```

6. Consulte horarios livres para uma segunda-feira futura:

```http
GET /api/professores/{professorId}/horarios-livres?data=2026-09-07
```

7. Solicite uma aula:

```json
{
  "professorId": "COLE_O_ID_DA_HELENA",
  "materiaId": "COLE_O_ID_DE_BIOLOGIA",
  "inicio": "2026-09-07T14:00:00Z",
  "duracaoMinutos": 60
}
```

8. Guarde o `id` da aula retornada.
9. Faca login como professora:

```json
{
  "email": "helena.biologia.dev@tutorclass.local",
  "senha": "TutorClass@123"
}
```

10. Clique em `Authorize` novamente com o token da professora.
11. Execute:

```http
GET /api/aulas/minhas
POST /api/aulas/{id}/confirmar
POST /api/aulas/{id}/concluir
```

Para cancelar uma aula propria:

```json
{
  "motivo": "Nao poderei comparecer neste horario."
}
```

## Planos, IA e Mentoria

Planos disponiveis:

- `Gratuito`: sem assinatura paga; permite aulas e materiais, mas nao libera IA educacional nem Mentoria.
- `Foco`: R$29,99/mes, com promocao de R$19,99 apenas na primeira assinatura elegivel do usuario; libera IA educacional com limite de 7 usos por dia.
- `Mentoria`: R$79,99/mes, com promocao de R$59,99 apenas na primeira assinatura elegivel do usuario; libera IA sem limite diario do plano e ate 2 mentores ativos.

Endpoints principais:

```text
GET  /api/planos/me
POST /api/planos/me/assinatura
POST /api/planos/me/cancelar
GET  /api/planos/me/ia
POST /api/planos/me/ia/uso

PUT  /api/professores/me/mentoria

POST /api/mentorias/me
PUT  /api/mentorias/me/slots/{slot}/trocar
GET  /api/mentorias/me
GET  /api/mentorias/acompanhamentos/pendentes
GET  /api/mentorias/acompanhamentos/historico
PUT  /api/mentorias/acompanhamentos/{acompanhamentoId}
POST /api/mentorias/acompanhamentos/{acompanhamentoId}/concluir
```

## TutorIA

A TutorIA e o assistente educacional do TutorClass. Utiliza a mesma abstracao `ITutorIaProvider`, com selecao entre `Simulado` e `Gemini`. O controller e as decisoes de autorizacao, plano e quota continuam exclusivamente no backend.

Endpoints:

```text
GET  /api/tutoria/status
POST /api/tutoria/chat
```

Regras:

- `Gratuito`: nao possui acesso; a TutorIA nao e chamada e `UsoIaAluno` nao e incrementado.
- `Foco`: ate 7 mensagens por dia; a 7a e permitida e a 8a tentativa e bloqueada sem chamar a TutorIA.
- `Mentoria`: possui TutorIA sem limite diario imposto pelo plano; o uso ainda e registrado para historico/telemetria.

O contador diario reaproveita `usos_ia_aluno` com chave unica por aluno/data. A data de "hoje" e calculada no fuso brasileiro de Sao Paulo. A mensagem so consome limite depois que o provider responde com sucesso. Falhas externas nao consomem quota.

Exemplo de chat:

```json
{
  "mensagem": "Explique regra de tres com um exemplo simples."
}
```

Regras implementadas:

- a promocao do primeiro mes e registrada no historico de assinaturas e nao volta apos cancelar e assinar novamente;
- professor mentor continua sendo `Professor`; a Mentoria e uma modalidade habilitada por `OfereceMentoria` e `LimiteMentorados`;
- aluno precisa ter plano Mentoria ativo para selecionar mentor;
- cada aluno pode ter ate 2 mentores ativos;
- um professor nao ocupa duas vagas do mesmo aluno;
- troca por vaga respeita intervalo de 30 dias e preserva historico;
- ao concluir aula com mentor ativo de aluno Mentoria, a API cria acompanhamento de Mentoria `Pendente` e retorna `requerAcompanhamentoMentoria` com o ID;
- acompanhamento concluido gera adicional simulado de R$20, limitado a 2 adicionais por mes para o mesmo par professor/aluno;
- o terceiro acompanhamento do mes continua salvo, mas nao gera novo adicional.

Gravacoes reais e IA real ficam preparados como regra de plano/autorizacao, sem streaming, gateway de pagamento ou provedor externo nesta etapa.

## Chat de Mentoria

O chat de Mentoria funciona somente entre o aluno e o professor do vinculo `MentoriaAlunoProfessor`. O PostgreSQL continua sendo a fonte da verdade para plano, usuarios, professores, assinatura e vinculo ativo. O MongoDB armazena conversas e mensagens. O SignalR entrega mensagens em tempo real depois da persistencia.

Configuracao MongoDB:

```text
MongoDb:ConnectionString
MongoDb:DatabaseName
```

Use User Secrets ou variaveis de ambiente. Nao coloque credenciais reais em arquivos versionados. Se MongoDB nao estiver configurado, a API continua iniciando e `/health` retorna `mongodb = NotConfigured`; endpoints do chat retornam mensagem clara de indisponibilidade.

Collections:

- `chat_mentoria`: uma conversa por `vinculo_mentoria_id`, com `aluno_id`, `professor_id` e `data_criacao_utc`.
- `mensagens_mentoria`: mensagens de texto, remetente, conteudo, data de envio, lida e data de leitura.

Indices MongoDB:

- `ux_chat_mentoria_vinculo_mentoria_id` unico por vinculo.
- `ix_mensagens_mentoria_vinculo_data_envio`.
- `ix_mensagens_mentoria_chat_data_envio`.

Endpoints:

```text
GET  /api/mentorias/{vinculoMentoriaId}/chat
GET  /api/mentorias/{vinculoMentoriaId}/chat/mensagens?limit=30&before=...
POST /api/mentorias/{vinculoMentoriaId}/chat/mensagens
POST /api/mentorias/{vinculoMentoriaId}/chat/mensagens/lidas
```

Hub SignalR:

```text
/hubs/mentoria-chat
```

Metodos do Hub:

- `EntrarNoChat(vinculoMentoriaId)`: valida usuario autenticado e participante antes de entrar no grupo.
- `EnviarMensagem(vinculoMentoriaId, conteudo)`: valida vinculo ativo, salva no MongoDB e publica `MensagemRecebida`.
- `MarcarComoLidas(vinculoMentoriaId)`: marca mensagens do outro participante como lidas.

Quando a Mentoria e encerrada, o historico continua disponivel, mas o chat fica somente leitura e novas mensagens sao bloqueadas pelo backend.

## Financeiro Simulado

O financeiro do TutorClass separa tres fluxos:

- assinatura de plano: Aluno ou Responsavel paga, TutorClass recebe, beneficios pertencem ao aluno beneficiario;
- pagamento de aula: Aluno ou Responsavel paga a aula, TutorClass intermedeia, calcula comissao e valor liquido previsto ao professor;
- adicional de Mentoria: TutorClass registra R$20 simulados ao professor mentor apos acompanhamento elegivel concluido.

As mensalidades pagas sao registradas em `cobrancas_assinatura`, uma cobranca por assinatura e competencia. Planos gratuitos nao geram cobranca. Registros financeiros aceitam apenas forma de pagamento simulada `Pix` ou `Cartao`, sem armazenar dados sensiveis.

Comissao de aula:

```text
Valor bruto: R$100,00
Comissao TutorClass: 12% = R$12,00
Valor liquido previsto ao professor: R$88,00
```

O plano do aluno nao altera o preco da aula. Plano Mentoria e aulas contratadas continuam registros financeiros independentes.

Endpoints financeiros:

```text
GET  /api/financeiro/aulas/comissao?valorBruto=100
POST /api/financeiro/aulas/{aulaId}/pagar
POST /api/financeiro/assinaturas/{assinaturaId}/cobrancas
GET  /api/financeiro/me/historico
GET  /api/financeiro/alunos/{alunoId}/historico
GET  /api/financeiro/professores/me/resumo?ano=2026&mes=9
```

Responsaveis so podem pagar ou consultar o historico financeiro de alunos vinculados por `ResponsavelAluno`. O adicional de Mentoria nao aparece no historico financeiro do aluno/responsavel porque nao e uma cobranca dele.

Reagendamento de aula:

```text
PUT /api/aulas/{aulaId}/reagendar
GET /api/aulas/{aulaId}/reagendamentos
```

Reagendar mantem a mesma aula e o pagamento ja registrado, valida disponibilidade e conflitos de agenda, rejeita horario passado e grava o horario anterior em `aulas_reagendamentos`.

Fluxo manual rapido pelo Swagger:

1. Execute `POST /api/dev/test-data`.
2. Login como `camila.rocha.dev@tutorclass.local` com `TutorClass@123`.
3. Consulte `GET /api/planos/me` e `GET /api/mentorias/me`.
4. Login como `helena.biologia.dev@tutorclass.local`.
5. Consulte `GET /api/mentorias/acompanhamentos/pendentes`.
6. Use `PUT /api/mentorias/acompanhamentos/{id}` para preencher resumo/metas/observacoes.
7. Use `POST /api/mentorias/acompanhamentos/{id}/concluir` para gerar o adicional simulado, se ainda houver cota mensal.
8. Login como `ricardo.nunes.dev@tutorclass.local` para consultar `GET /api/financeiro/alunos/{alunoId}/historico`.
9. Login como `helena.biologia.dev@tutorclass.local` para consultar `GET /api/financeiro/professores/me/resumo`.
10. Use `PUT /api/aulas/{aulaId}/reagendar` e depois `GET /api/aulas/{aulaId}/reagendamentos` para demonstrar o historico.
11. Configure MongoDB, reinicie a API e use `GET /api/mentorias/me` para obter o `vinculoMentoriaId`.
12. Use `GET /api/mentorias/{vinculoMentoriaId}/chat`, `POST /api/mentorias/{vinculoMentoriaId}/chat/mensagens` e conecte no Hub `/hubs/mentoria-chat` com JWT para testar tempo real.

## Primeira Implementacao Minima

Esta etapa entrega:

- solution em camadas;
- dominio inicial com encapsulamento;
- modelagem EF Core para PostgreSQL;
- primeira migration;
- seed das materias permitidas;
- endpoints REST iniciais;
- middleware global de excecoes;
- testes unitarios das regras principais.

Proximas etapas recomendadas:

- autenticação/autorização real;
- cadastro completo de aluno, responsavel e administrador;
- agendamento de aulas;
- avaliacao vinculada a aula concluida;
- progresso do aluno;
- calculo evolutivo de reputacao;
- pagamentos reais;
- chat de Mentoria em tempo real;
- gravacoes reais de aulas com storage privado e URLs assinadas;
- NoSQL para chat, eventos ou logs quando houver justificativa real.

## Deploy do Backend

O backend pode ser publicado como Web Service Docker no Render mantendo PostgreSQL/Neon e MongoDB Atlas externos.

Arquivos de deploy:

```text
Dockerfile
.dockerignore
render.yaml
```

O container executa `TutorClass.API` em `Production` e escuta em `0.0.0.0:${PORT}`. Em desenvolvimento local, o fluxo atual continua o mesmo:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
dotnet run --project TutorClass.API --no-launch-profile --urls http://127.0.0.1:5242
```

Variaveis de ambiente para cadastrar no Render:

```text
ASPNETCORE_ENVIRONMENT=Production
Swagger__Enabled=true
ConnectionStrings__TutorClassDb=<connection string do Neon>
Jwt__Secret=<chave forte de producao>
Jwt__Issuer=TutorClass
Jwt__Audience=TutorClass.Client
Jwt__ExpirationMinutes=120
MongoDb__ConnectionString=<connection string do MongoDB Atlas>
MongoDb__DatabaseName=<nome do database MongoDB>
```

Nao coloque connection strings, senhas ou secrets em arquivos versionados. Use variaveis de ambiente no Render e User Secrets em desenvolvimento.

Configuracao sugerida no Render:

- tipo: Web Service;
- runtime: Docker;
- Dockerfile: `./Dockerfile`;
- Health Check Path: `/health`;
- branch: branch principal do repositorio;
- auto deploy: conforme preferencia do projeto.

Swagger para demonstracao:

```text
/swagger/index.html
/swagger/v1/swagger.json
```

No ambiente publicado deste projeto academico, o Swagger fica habilitado quando `Swagger__Enabled=true`. Nao ha exposicao de secrets no Swagger.

Frontend e API sao publicados na mesma origem. Nao configure CORS local no Render; a politica de origens adicionais e restrita a Development.

MongoDB Atlas:

- o Atlas precisa permitir conexao do ambiente Render em Network Access;
- para demonstracao, pode ser necessario liberar temporariamente `0.0.0.0/0` no Atlas, ou usar uma estrategia de egress/IP fixo quando disponivel;
- mantenha o usuario do MongoDB com permissao apenas no database necessario.

Validacao publica apos deploy:

```text
GET /health
GET /swagger/index.html
GET /swagger/v1/swagger.json
GET /api/materias
GET /api/professores
POST /api/auth/login
GET /api/auth/me
GET /api/mentorias/{vinculoMentoriaId}/chat/mensagens
```

O endpoint `POST /api/dev/test-data` permanece exclusivo de `Development`; em `Production`, retorna `404`.


### Provider Gemini e contexto dinamico

Configure `TutorIA:Provider=Simulado` para respostas locais de desenvolvimento/testes (padrao quando ausente). Para Gemini, configure via User Secrets ou ambiente:

- `TutorIA__Provider=Gemini`
- `Gemini__ApiKey`: chave privada, nunca versionar.
- `Gemini__Model`: identificador do modelo habilitado na sua conta, sem prefixo `models/`.

Essas mesmas tres variaveis devem ser cadastradas no painel Environment do Render. Se Gemini estiver selecionado, configuracao ausente/invalida impede a inicializacao; o fallback para Simulado fica desligado por padrao e so pode ser habilitado explicitamente para apresentacao. Os testes HTTP fixam explicitamente Simulado.

O `ContextoIaService` consulta PostgreSQL por aluno autenticado no JWT. Nome vem de `Usuarios/Aluno`; plano vem da assinatura ativa mais recente (sem assinatura = Gratuito); quota vem de `UsoIaAluno` na data de Sao Paulo. Materias sao distintas, ordenadas por nome, exclusivamente de aulas nao canceladas do aluno (maximo 5). Proximas aulas sao confirmadas e futuras, ordenadas por inicio UTC e ID (maximo 5). Mentores vem de vinculos ativos com professores ativos, ordenados por slot e ID (maximo 2). O vinculo nao define materia, portanto o campo do mentor fica nulo. As listas sao limitadas no banco antes de materializar. O contexto cadastral nao inclui assuntos estudados inferidos. O historico curto da propria TutorIA e enviado separadamente, conforme descrito abaixo.

Somente `mensagem` e recebida no request, obrigatoria e com ate 4.000 caracteres. Campos extras nao alimentam contexto ou autorizacao. Professor e Responsavel nao acessam TutorIA. Gemini recebe apenas nome, plano, quota, nomes de materias, horarios/professores das aulas autorizadas e nomes de mentores; nao recebe IDs internos, senha, JWT, documentos, dados financeiros detalhados, mensagens privadas, observacoes ou chat de Mentoria. Gemini nunca decide autorizacao ou quota.

A instrucao central fica em `TutorIaPrompts`. A quota enviada corresponde ao instante anterior a resposta; sucesso incrementa o contador e a resposta HTTP contem a quota atualizada. Datas enviadas sao UTC e a instrucao pede apresentacao no fuso de Sao Paulo. Listas limitadas nao representam todo o historico.

Timeout padrao: 30 segundos (`Gemini:TimeoutSeconds`, 1–120). Saida padrao: 2.048 tokens (`Gemini:MaxOutputTokens`, 1–8192). Cada tentativa respeita o timeout e CancellationToken. Apenas HTTP 503/429 tem ate tres tentativas, com esperas de 1s e 2s; nao ha retry para outros erros ou cancelamento. O tempo total pode chegar a cerca de 93s com timeout de 30s por tentativa. Falhas finais sem fallback (HTTP 401/403, 429, 5xx, timeout, rede, JSON inesperado, ausencia de candidates ou texto vazio) retornam o contrato existente com `motivo=ERRO_TUTORIA` e mensagem: “A TutorIA está temporariamente indisponível. Tente novamente em alguns instantes.” O endpoint preserva HTTP 200 para esse contrato de erro de negocio.

Logs Gemini contem apenas provider, duracao, status HTTP, sucesso e categoria. Logs automaticos do HttpClient desse provider estao desabilitados; chave fica no header, nunca na URL. Prompt, contexto, resposta e excecoes brutas nao sao registrados.

A exclusao mutua por aluno existente permanece durante contexto, chamada e persistencia. Ela protege chamadas concorrentes dentro de uma instancia; nao e um lock distribuido para multiplas replicas. `UsoIaAluno` e sua chave unica permanecem. Nenhuma migration foi criada.

Validacao automatizada: `dotnet test TutorClass.Tests`. Para validacao real, iniciar a API em Development com User Secrets configurados e `TutorIA__Provider=Gemini`, autenticar um aluno de desenvolvimento com acesso e enviar ao endpoint existente as perguntas: “Responda apenas: TutorIA funcionando.”, “Qual é o meu plano?”, “Quantas mensagens ainda tenho hoje?”, “Quando é minha próxima aula?”, “Quem são meus mentores?” e “Explique orientação a objetos de forma simples.” Cada sucesso real registra uso. Nunca salvar JWT ou respostas pessoais em logs compartilhados.

Contrato REST Gemini consultado: [documentacao oficial generateContent](https://ai.google.dev/api/generate-content).

Contingencia opcional para apresentacao: `TutorIA:FallbackApresentacao` (Render: `TutorIA__FallbackApresentacao`), padrao `false`. Se `true`, depois de tres falhas HTTP 503/429 o provider usa `TutorIaSimuladaProvider` existente e registra log explicito `FallbackApresentacao=True`, `FromProvider=Gemini`, `ToProvider=Simulado`. A resposta do Simulado se identifica como simulada. Nao aciona fallback por cancelamento, validacao, 400/401/403, timeout ou outros erros. Configuracao Gemini continua obrigatoria.

Retries nao registram uso individualmente: sucesso final Gemini ou Simulado registra exatamente um `UsoIaAluno`; tres falhas sem fallback retornam a mensagem controlada e nao consomem quota. Nenhuma regra de plano ou contexto e alterada.

### Conversa curta da TutorIA

A memoria e exclusiva da TutorIA, por AlunoId autenticado, em RAM na instancia da API. Guarda apenas trocas bem-sucedidas apos persistir UsoIaAluno, ate 10 mensagens (5 pares user/assistant), limitadas tambem a 12.000 caracteres no total. Remove pares inteiros mais antigos, sem truncar mensagens. Expira 30 minutos apos a ultima troca armazenada; no maximo 500 alunos ficam no cache, com descarte dos mais antigos. Reinicio/deploy ou outra replica nao compartilha essa memoria. Nao existe nova tabela ou migration; nao foi reutilizado MongoDB/chat de Mentoria.

O request publico continua apenas mensagem. O backend recupera historico pelo aluno autorizado. No Gemini, assistant vira model e user permanece user; contexto atual e nova pergunta vem ao final. Contexto atual prevalece sobre informacoes antigas de plano/quota/aulas. Historico nao altera permissoes. O prompt permite saudacao na primeira interacao e pede respostas diretas nas continuacoes, sem repeticao do nome.

Nao sao armazenados JWT, credenciais ou entidades do banco. Uma filtragem conservadora descarta o par inteiro se detectar senha, token, chaves, documentos, contatos ou outros padroes sensiveis no texto de entrada ou saida. Trata-se de deteccao por padroes, nao garantia de identificar toda informacao sensivel em texto livre. Historico/prompt/resposta nao sao registrados nos logs.

Saida padrao aumentada para 2.048 tokens; configuracao explicita Gemini:MaxOutputTokens continua prevalecendo. Timeout mantido em 30s por tentativa, com retries 503/429 existentes. O prompt pede normalmente ate 300 palavras e raciocinio completo. MAX_TOKENS agora gera categoria tecnica limite_saida e erro controlado, sem retornar texto cortado, gravar memoria ou consumir quota. Timeout tem categoria propria e tambem nao devolve resposta parcial. Nao houve mudanca no Gemini__Model nem nas configuracoes do Render.
