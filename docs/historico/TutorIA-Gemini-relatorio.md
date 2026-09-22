# Relatorio: TutorIA com Gemini

## Implementacao

A unica porta de entrada continua `TutorIaController`: `GET /api/tutoria/status` e `POST /api/tutoria/chat`. Foram reaproveitados `ITutorIaProvider`, `TutorIaService`, `TutorIaSimuladaProvider`, `TutorIaPrompts`, `IApplicationDbContext`, assinaturas e `UsoIaAluno`, incluindo a chave unica aluno/data e o semaforo por aluno.

Fluxo: JWT/policy Aluno → aluno ativo → lock por aluno → plano/quota → mensagem obrigatoria de ate 4000 caracteres → ContextoIaService → provider selecionado na DI → resposta valida → UsoIaAluno + SaveChanges → resposta com contador atualizado. Gratuito bloqueado; Foco permite 1–7 e bloqueia 8; Mentoria sem limite comercial diario. Professor/Responsavel nao podem usar esse endpoint.

## Contexto e fontes

| Campo | Fonte e restricao |
|---|---|
| AlunoId / Nome | Aluno ativo identificado pelo JWT; ID nao enviado ao Gemini |
| Plano | Assinatura ativa mais recente; ausencia = Gratuito |
| Usadas / restantes | UsoIaAluno do aluno na data de Sao Paulo; limite Foco central existente |
| Materias | Materias de aulas nao canceladas do proprio aluno; distintas, ordem alfabetica, Take(5) |
| Proximas aulas | Aulas confirmadas futuras do proprio aluno, joins de materia/professor, inicio UTC e ID, Take(5) |
| Mentores | Vinculos ativos do proprio aluno com professor ativo, ordem slot/ID, Take(2) |

Mentor.Materia fica nulo: o vinculo nao possui uma materia definida. Nao ha UltimosAssuntosEstudados, historico de conversa, memoria, RAG ou nova persistencia. As consultas filtram/projetam/limitam antes de materializar, seguindo a abstracao IQueryable existente; sao sincronas, com verificacao de cancelamento antes/depois. O CancellationToken e propagado para chamada HTTP e persistencia.

## Provider, erros e seguranca

`TutorIA:Provider` seleciona Simulado (padrao) ou Gemini. Gemini exige chave/modelo validos ao inicializar. Nao ha fallback nem retry automatico. HttpClient vem da DI, timeout padrao 30s, resposta com maxOutputTokens=1024. Limites configuraveis com validacao: timeout 1–120s, tokens 1–8192. Buffer HTTP limitado a 1 MiB.

O corpo usa systemInstruction centralizada, contexto curto estruturado em JSON e pergunta, conforme a API oficial generateContent. A allowlist enviada exclui IDs, credenciais, documentos, dados financeiros, mensagens privadas, observacoes e chat de Mentoria. Campos extras no JSON do frontend sao ignorados e nao alimentam contexto, plano ou quota.

401/403, 429, 5xx, outros erros HTTP, rede, timeout, JSON invalido/inesperado, candidates ausentes, geracao interrompida e resposta vazia geram erro controlado. Nenhuma falha do provider registra uso. Cancelamento do cliente e propagado. O contrato HTTP existente e preservado: HTTP 200 com motivo ERRO_TUTORIA e mensagem de indisponibilidade; sem erro bruto Google.

Logs Gemini: provider, duracao, status HTTP, sucesso e categoria. Nao registram prompt/contexto/resposta, chave ou excecao bruta. Logging automatico desse HttpClient desabilitado. A chave e enviada em header, nunca na URL. Gemini nao decide autorizacao, plano ou quota.

A protecao de concorrencia existente permanece por instancia/processo. Nao garante exclusao mutua entre multiplas replicas; nao foi introduzida infraestrutura distribuida. Nenhuma migration criada.

## Arquivos

Criados:
- TutorClass.Application/Abstractions/IContextoIaService.cs
- TutorClass.Application/DTOs/ContextoIaAluno.cs (inclui AulaContextoIa e MentorContextoIa)
- TutorClass.Application/Services/ContextoIaService.cs
- TutorClass.Infrastructure/AI/GeminiTutorIaProvider.cs (provider, options, registro DI e excecao controlada)
- TutorClass.Tests/GeminiTutorIaTests.cs
- docs/historico/TutorIA-Gemini-relatorio.md

Alterados:
- TutorClass.Application/DTOs/TutorIaDtos.cs: contexto no request interno do provider; request HTTP permanece apenas mensagem.
- TutorClass.Application/Services/TutorIaService.cs: contexto apos autorizacao, validacao 4000, resposta vazia e cancelamento.
- TutorClass.Application/Services/TutorIaPrompts.cs: instrucao central e limites de informacao.
- TutorClass.Application/DependencyInjection.cs: registro do contexto.
- TutorClass.Infrastructure/TutorClass.Infrastructure.csproj: framework reference para HttpClient factory.
- TutorClass.API/Program.cs: selecao do provider.
- TutorClass.Tests/AuthFlowTests.cs: factory HTTP fixa Simulado.
- TutorClass.Tests/ResponsavelPermissoesTests.cs: verifica tambem POST chat bloqueado.
- README.md: configuracao, contexto, seguranca, limites, erros e Render.

## Testes e chamada real

Suite completa final: 224 testes aprovados, zero falhas, incluindo contexto forjado no JSON e bloqueio do POST para Responsavel. Comando: dotnet test TutorClass.Tests --no-restore --verbosity quiet. git diff --check sem erros. A API temporaria usada no teste real foi encerrada.

Teste real via API local, autenticacao JWT da conta de desenvolvimento Camila e PostgreSQL/Neon, usando ApiKey/Model ja existentes nos User Secrets e TutorIA__Provider=Gemini somente no processo temporario:

- “Responda apenas: TutorIA funcionando.”: HTTP Google 200, resposta com 20 caracteres, uso 0 → 1.
- “Qual e o meu plano?”: HTTP Google 503 em duas tentativas; erro controlado, uso permaneceu 1.
- “Quantas mensagens ainda tenho hoje?”: HTTP Google 503; uso permaneceu 1.
- “Quando e minha proxima aula?”: HTTP Google 503; uso permaneceu 1.
- “Quem sao meus mentores?”: HTTP Google 503; uso permaneceu 1.
- “Explique orientacao a objetos de forma simples.”: HTTP Google 200; resposta educacional contendo objetos/classes; uso 1 → 2.

Resultado: geracao real confirmada em duas perguntas e falha externa sem consumo confirmada em cinco chamadas. Contexto PostgreSQL foi montado no fluxo real. Nao foi possivel confirmar a correção das respostas pessoais porque o provider retornou 503. Consulta auxiliar GET /api/aulas/minhas retornou HTTP 500; nao houve alteracao desse endpoint nesta entrega. Nenhuma resposta pessoal ou segredo foi gravado em arquivo de teste.

## Render e pendencias

Configurar `TutorIA__Provider=Gemini`, `Gemini__ApiKey` e `Gemini__Model` no Render. Nenhum valor real foi versionado. Os User Secrets ja possuem chave/modelo; a selecao persistente do provider nao foi alterada.

Pendencias: repetir as quatro perguntas pessoais quando Gemini estiver disponivel; investigar separadamente o HTTP 500 de /api/aulas/minhas; configurar o provider no ambiente de deploy. Multiplas replicas exigiriam revisao da protecao de concorrencia existente. Nao foi feito deploy.
