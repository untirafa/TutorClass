# Correcao focada da TutorIA

## Bug /api/aulas/minhas

Causa comprovada em PostgreSQL/Neon com Aluno autenticado: `NpgsqlOperationInProgressException: A command is already in progress`. A consulta aberta era `SELECT ... FROM aulas AS a`; durante sua enumeracao, o filtro apos `AsEnumerable()` chamava `ResponsavelAlunoService.PossuiVinculo`, que executava `SELECT EXISTS (... FROM usuarios ... tipo_usuario = 'Responsavel' ...)` na mesma conexao.

Trecho relevante do stack trace anterior:

```text
NpgsqlCommand.ExecuteReader
ResponsavelAlunoService.PossuiVinculo (...:26)
AulaService.<ListarMinhasAsync>b__0 (...:135)
AulaService.ListarMinhasAsync (...:138)
AulasController.ListarMinhas (...:46)
```

Correcao restrita a `AulaService.ListarMinhasAsync`: filtro de autorizacao mantido em IQueryable/SQL, seguido de joins e projecao direta de AulaResumoResponse. Preserva professor, materia, aluno, SolicitadaPorUsuarioId, data, duracao, preco e status. Nao altera permissoes de nenhum perfil. Elimina consultas aninhadas e a carga de tabelas completas nesse endpoint. Nenhuma migration ou ajuste de dados.

Regressao HTTP autenticada verifica 200, isolamento por aluno e campos do resumo. Um test double reproduz a restricao de reader aberto do Npgsql, ausente no InMemory. O teste foi executado contra o metodo original: falhou como esperado; contra a correcao: passou. Validacao real apos correcao: HTTP 200, 18 aulas, todas da aluna de desenvolvimento.

## Gemini 503

Origem comprovada: Google, nao TutorClass. Modelo existente configurado: `gemini-3.8-flash`; consulta de metadados retornou HTTP 200 em 691 ms e `supportedGenerationMethods` incluindo `generateContent`.

Endpoint utilizado: `https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent`. Erros reais: HTTP 503, status sanitizado `UNAVAILABLE`, motivo sanitizado `alta_demanda`; duracoes observadas entre 2.497 e 5.486 ms na primeira rodada. Nao e erro de modelo/endpoint. Nenhuma chave, JWT, connection string, prompt ou contexto completo foi impresso.

Mudanca restrita a diagnostico no provider: logs incluem modelo, endpoint e status/motivo por allowlist. O corpo livre do Google nao e registrado. Mensagem controlada ao frontend permanece, sem consumo em falha. Timeout de 30 segundos mantido, sem troca de modelo, fallback ou retry automatico.

## Contexto dinamico

Mantido ContextoIaService, acesso direto a IApplicationDbContext/PostgreSQL, sem HTTP interno. Plano real observado: Mentoria. Quota vem de UsoIaAluno, sem limite comercial diario nesse plano. Materias continuam restritas a aulas nao canceladas do aluno, ate 5. Proximas aulas confirmadas futuras: 4 no cenario real, ordenadas por data/ID, limite 5. Dois vinculos de mentores ativos observados. Testes verificam nomes de materia/professor, limites e exclusao de outro aluno.

## Perguntas reais

- Plano: resposta Mentoria conferiu com assinatura/status real.
- Quota: confirmou ausencia de limite comercial diario de Mentoria; resposta contabilizada uma vez.
- Proxima aula: materia, professor e data conferiram com a listagem corrigida.
- Orientacao a objetos: resposta educacional real, contendo objetos/classes.
- Senha: resposta indicou que nao possui acesso/informacao; dado nao enviado.
- Mentores: conferencia final confirmou os dois nomes completos dos vinculos ativos. Uma resposta anterior nao coincidiu literalmente; uma nova tentativa recebeu 503; a ultima conferencia isolada passou.

## Quota

Testes preservados: Gratuito bloqueado; Foco 1–7 permitido, 8 bloqueado; Mentoria sem limite comercial; concorrencia existente preservada. Testes HTTP Gemini 503 e timeout retornam contrato controlado, sem 500 bruto e sem consumo. Sucesso incrementa exatamente 1. No teste real, cada falha manteve contador e cada sucesso incrementou 1; o contador passou de 2 a 9: sete respostas bem-sucedidas e seis falhas externas sem consumo.

## Testes

Build da API: sucesso, zero avisos/erros. Suite completa: 230 testes, 230 aprovados, zero falhas. Inclui regressao de aulas, contexto e isolamento, resposta pessoal via provider fake, 503/timeout/sucesso pelo endpoint e sanitizacao de logs.

Arquivos desta correcao: AulaService.cs, GeminiTutorIaProvider.cs, GeminiTutorIaTests.cs, novo AulasMinhasRegressionTests.cs e este relatorio. Modulos de FAQ, Notificacoes, Financeiro, Gravacoes, Responsavel, MongoDB/chat, planos, regras de Mentoria e migrations nao foram alterados nesta etapa.

## Pendencias

Nenhuma pendencia de implementacao ou das seis perguntas reais solicitadas. A disponibilidade do Google continua sujeita a falhas externas intermitentes, tratadas e observadas. Modelo/endpoint mantidos. API temporaria de validacao encerrada. Nenhum deploy realizado.

## Veredito

TutorIA Gemini + contexto dinamico validados

Referencia consultada para metadados: [documentacao oficial Models](https://ai.google.dev/api/models).
