# Segurança, privacidade e integração — TutorClass

Data: 23/09/2026. Escopo: backend, testes e contratos; sem mudanças em Web, Mobile, produção ou documentos de professores. Não representa parecer jurídico nem declaração de conformidade. Diagnóstico realizado antes das alterações, com Git limpo em `fb423b6`. Build inicial aprovado. A suspeita inicial de arquivos truncados foi descartada: a omissão estava na saída da leitura, não nos arquivos. Nenhuma restauração foi feita.

## 1. Diagnóstico da estrutura existente

| Área | Situação encontrada |
|---|---|
| Autenticação | `AuthService`, `AuthController`, `JwtTokenService`. Senhas com `PasswordHasher<Usuario>`, mínimo atual de 8 caracteres; e-mail normalizado e verificação de unicidade; login exige usuário ativo e retorna erro genérico para credenciais inválidas. JWT HS256 com validação de assinatura, emissor, audiência e expiração; segredo mínimo de 32 bytes. |
| Perfis | Políticas Aluno, Professor, Responsável e Administrador; não existe perfil/política Suporte. Não há cadastro público de administrador. Aluno e professor operam recursos próprios; serviços validam proprietários e vínculos. |
| Cadastro e recuperação | Rotas públicas de cadastro para aluno, professor e responsável; professor passa pelas regras de aprovação já existentes. Não foi encontrado fluxo de recuperação de senha. Data de nascimento do aluno é opcional; não foi encontrada validação de idade mínima na entidade/cadastro examinados. Nenhuma regra etária foi criada. |
| Responsáveis | `ResponsavelAlunoService` exige ambos os usuários ativos e vínculo real. Resumos, aulas, operações financeiras e assinatura preservam a identidade do solicitante. Gravações expõem resumo, não URL privada. TutorIA e entrada/envio no SignalR não são permitidos. Havia acesso ao texto privado por rota específica de histórico: retirada aprovada pelo usuário nesta tarefa. |
| TutorIA | `TutorIaService`: valida aluno/plano/mensagem, serializa chamadas por aluno via semáforo estático, verifica uso diário, obtém contexto permitido e chama provider. Consumo/memória somente após sucesso. Foco: 7/dia; Gratuito sem acesso; Mentoria sem limite diário comercial. JSON e SSE compartilham serviço. Gemini dispõe de streaming, fallback de modelo e diagnósticos sanitizados. |
| FAQ/ChatBot | `FaqService`, `FaqController`, `AdminFaqController`, `PerguntaFrequente`: respostas cadastradas com busca local por relevância. Separado do Gemini e da quota TutorIA; mantido sem alterações. Não é módulo de atendimento humano. |
| Mentoria | `MentoriaChatService`, `IMentoriaChatStore`, `MongoMentoriaChatStore`, controller e hub. Collections `chat_mentoria`/`mensagens_mentoria`, filtro por vínculo e paginação. Participantes são aluno/professor; envio exige vínculo ativo, plano Mentoria, aluno ativo, professor aprovado/ativo e oferta de Mentoria. Encerramento bloqueia envio e preserva leitura pelos participantes. Mensagens: até 2.000 caracteres; páginas: até 100. SignalR autoriza entrada e chamadas pelo serviço e transmite por grupo do vínculo. |
| Dados e erros | EF Core/PostgreSQL para domínio; MongoDB para chat; substitutos SQLite/InMemory locais. Middleware retorna 400, 403, 503 e 500 genérico. Logs operacionais não equivalem a trilha de auditoria de suporte. Memória TutorIA é temporária, limitada e filtra certos dados sensíveis. |
| Ausências | Sem rate limiting geral anterior, registro de denúncias, fila de revisão, módulo Suporte, aceite versionado de termos ou catálogo jurídico. Não foi criada duplicação de FAQ/notificações para fingir um módulo de denúncias. |

Contratos reaproveitados: `TutorIaChatRequest/Response`, campo `motivo`, `mensagem`, status/quota; eventos SSE `delta/complete/error`; `MensagemMentoriaResponse`, `MensagensMentoriaPageResponse`, IDs de usuário/vínculo/mensagem; `Responsavel*ResumoResponse`; erros atuais `{error}` e erros de validação do MVC.

## 2. Alterações implementadas

| Arquivos | Motivo |
|---|---|
| `TutorClass.Application/Services/TutorIaModeracao.cs` | Ampliar a validação local existente para quatro categorias; evitar palavra isolada como motivo de bloqueio. |
| `TutorClass.Application/Services/TutorIaService.cs` | Reutilizar a avaliação categorizada antes de contexto/provedor/consumo, preservando o DTO. |
| `TutorClass.Application/Services/MentoriaChatService.cs` | Negar leitura privada ao responsável mesmo vinculado, inclusive chat encerrado; sem consulta ao store. Alteração de permissão explicitamente aprovada. |
| `TutorClass.API/Security/ControleAbuso.cs`, `LimiteRequisicoesAttribute.cs`, `Hubs/LimiteChatHubFilter.cs` | Contadores temporários atômicos e compartilhados entre HTTP e SignalR para envio de chat. |
| `TutorClass.API/Program.cs` | Registrar contador e filtro do hub; sem mudanças de autenticação/configuração de produção. |
| `TutorClass.API/Controllers/AuthController.cs`, `TutorIaController.cs`, `MentoriaChatController.cs` | Aplicar limites proporcionais às operações de envio. |
| `TutorClass.Application/DTOs/PrivacidadeDtos.cs`, `TutorClass.API/Controllers/PrivacidadeController.cs` | Catálogo público dos futuros documentos aprovados; não exige aceite e não muda cadastro. |
| `TutorClass.Tests/TutorIaModeracaoTests.cs`, `ResponsavelPermissoesTests.cs`, `SegurancaContratosTests.cs` | Cobrir segurança, compatibilidade, quota e nova restrição aprovada. |

## 3. Moderação e contrato preservado

`POST /api/tutoria/chat` mantém HTTP 200 e `TutorIaChatResponse` para decisão de moderação. `POST /api/tutoria/chat/stream` retorna somente evento `complete` com o mesmo DTO quando bloqueia; nenhum `delta`. Erros de autenticação/validação continuam separados. `resposta=null`, `possuiAcesso` preservado, `usadasHoje/restantesHoje` inalterados. A interface deve permitir editar a pergunta, sem tratar moderação como queda do provedor e sem reenviar automaticamente.

| `motivo` estável | Critério e mensagem |
|---|---|
| `DADOS_PESSOAIS` | CPF com formato/rótulo e dois verificadores corretos; telefone com rótulo contextual e formato plausível; RG rotulado (não apenas comprimento); e-mail ou declaração explícita de senha/token/chave/passaporte. “Sua mensagem parece conter informações pessoais. Remova esses dados e envie sua pergunta novamente. Você não precisa compartilhar documentos ou números pessoais para estudar com a TutorIA.” |
| `MENSAGEM_OFENSIVA` | Ofensas diretas em expressões delimitadas. “Vamos manter uma comunicação respeitosa. Reformule sua mensagem para que eu possa ajudar com seus estudos. Nenhuma mensagem da sua quota foi consumida.” |
| `AMEACA_OU_ASSEDIO` | Expressões explícitas delimitadas de ameaça, perseguição, chantagem ou assédio. “Esta mensagem não pôde ser enviada porque pode violar as regras de segurança do TutorClass. Reformule sua mensagem com respeito e segurança.” |
| `TENTATIVA_ACESSO_INDEVIDO` | Pedidos explícitos delimitados de ignorar restrições, burlar quota ou invadir/acessar contas de terceiros. “Não posso atender a essa solicitação. Reformule sua mensagem dentro das funcionalidades e das regras de uso da TutorIA.” |

Prioridade: dados pessoais, ameaça, acesso indevido, ofensa. Nenhum dado detectado é interpolado na resposta/log. Não há gravação de bloqueados na memória como sucesso, denúncia automática, sanção ou promessa de análise humana. O filtro funciona no serviço compartilhado, independentemente do cliente. Segurança do provedor continua ativa.

Limite: regras determinísticas em português, não compreensão semântica completa. Conteúdo disfarçado, indireto, outros idiomas e formatos não contemplados podem passar; e-mails exemplificativos também exigem reformulação. Datas, números de exercícios, código, perguntas conceituais de privacidade, citações de ofensa e pedidos de ajuda não são bloqueados apenas por palavra ou número. Dados pessoais plausíveis não são enviados mesmo em citação. Nenhuma classificação de risco é associada permanentemente à pessoa.

## 4. Catálogo jurídico disponível, sem textos inventados

Novo `GET /api/privacidade/documentos`, anônimo, também utilizável na conta autenticada:

```json
{"documentos":[],"contatoPrivacidade":null,"orientacoes":null,"aceiteObrigatorio":false}
```

Quando a responsável fornecer e aprovar o material, o servidor aceita configuração `DocumentosLegais:TermosUso` e `DocumentosLegais:PoliticaPrivacidade`, cada qual com `Publicado`, `Versao`, `Url`. Somente publicação explícita, versão não vazia e URL HTTPS sem credenciais aparecem como `{tipo,versao,url}`. `ContatoPrivacidade` e `Orientacoes` são textos públicos opcionais fornecidos pela responsável. Nada foi preenchido ou alterado no Render. Não buscar URLs automaticamente nem interpretar orientações como HTML no cliente.

Não exigir checkbox nem bloquear cadastro enquanto não houver fluxo de aceite aprovado. Disponibilidade de um documento não ativa obrigatoriedade. Termos não são consentimento genérico. Não há exclusão automática de dados/contas.

**Proposta de aceite, NÃO implementada:** tabela `AceiteTermos` com ID, usuário, versão imutável do documento, instante UTC e identificador/hash da publicação; sem IP/dispositivo por padrão. A interface apresenta a versão publicada; após ato explícito envia versão; servidor valida versão e associa ao usuário no mesmo fluxo transacional aprovado. Privacidade é disponibilizada separadamente, sem um “aceito todo tratamento”. Decidir previamente obrigatoriedade, atualização para contas existentes, evidência e retenção. Nenhuma migration gerada.

## 5. Denúncias e suporte — proposta aguardando aprovação

Não existe repositório reutilizável de denúncias. Persistir em notificações, FAQ, memória efêmera ou logs não atenderia a segurança/privacidade. Portanto **não existe endpoint de envio ativo nesta entrega**; não mostrar botão que prometa registrar um caso ainda.

**Proposta de dados:** uma tabela relacional `Denuncia` com ID, `DenuncianteUsuarioId` obtido do JWT, `UsuarioDenunciadoId?`, `VinculoMentoriaId?`, `MensagemId?` (ID externo Mongo), categoria enum (`OFENSA`, `AMEACA_ASSEDIO`, `PRIVACIDADE`, `OUTRO`), descrição até 1.000 caracteres, status (`RECEBIDA`, `EM_ANALISE`, `ENCERRADA`), criação UTC. Responsável pelo envio da mensagem denunciada é resolvido no servidor, não aceito como identidade confiável do cliente. Não copiar conversa inteira, anexos, IP ou dados de dispositivo. Status não implica culpa ou punição.

**Permissões propostas:** aluno/professor denunciam mensagem somente de conversa da qual participam, ativa ou encerrada. Autorizar vínculo antes de buscar um ID exato no Mongo, confirmando remetente/conversa; não aceitar ID de outra conversa. Responsável pode relatar comportamento sobre aluno vinculado, sem consultar/selecionar mensagens privadas. Denunciante só vê protocolo/status do próprio caso; denunciado e terceiros não veem denúncia. Administrador autorizado analisa ocorrências individualizadas; perfil Suporte exigirá aprovação de política/permissões, sem herdar acesso global aos chats. Acesso eventual à mensagem exata e auditoria dessa consulta precisam ser aprovados. Não criar leitura indiscriminada, broadcast SignalR, exclusão de mensagem ou punição automática.

**Contratos propostos (não implementados):**

```text
POST /api/denuncias
{ categoria, descricao, usuarioDenunciadoId?, vinculoMentoriaId?, mensagemId? }
201 { id, status: "RECEBIDA", criadaEmUtc }
GET /api/denuncias/minhas -> apenas protocolos/status do denunciante
GET /api/admin/denuncias/{id} -> caso autorizado
PATCH /api/admin/denuncias/{id}/status -> transição autorizada, sem punição
```

Limites propostos: 5 envios por 10 minutos/usuário, idempotência para repetição por falha de rede, 400 para validação, 401/403 para acesso, 429 com Retry-After. Decidir categorias finais, tratamento de casos sem alvo, transições, acesso de Suporte, retenção/base legal, auditoria e descarte antes de implementar. Retenção **não definida**, sem TTL ou exclusão automática. Proposta de uma migration relacional após aprovação; não é necessária nova collection de chat. Sem aprovação não há testes de um endpoint inexistente nem promessa de registro.

## 6. Limitação temporária implementada

| Operação | Limite por janela fixa de 60 s | Chave |
|---|---|---|
| Login | 20 | IP do peer da conexão |
| Cadastro (três perfis juntos) | 10 | IP do peer |
| TutorIA JSON + SSE juntos | 12 | ID autenticado |
| Envio Mentoria HTTP + SignalR juntos | 60 | ID autenticado |
| Demais chamadas de métodos do hub | 120 | ID autenticado |

HTTP: 429, `Retry-After` em segundos e `{codigo:"LIMITE_REQUISICOES",error,retryAfterSeconds}`. SignalR: `HubException` com prefixo estável `LIMITE_REQUISICOES`. Não converter em sucesso nem reduzir quota no cliente. Sem repetição automática. Contadores em memória, até 10.000 chaves, expiração de janela, sem conteúdo de mensagem ou bloqueio permanente. Sob saturação de chaves, recusa temporária de novas chaves. Contadores de tentativa incluem mensagens bloqueadas pela moderação, mas são separados da quota comercial. Documentos públicos não dependem desses contadores.

**Limites operacionais:** proteção por processo; reinício zera contadores, múltiplas réplicas não os compartilham. Não substitui proteção de borda contra DDoS. IP de proxy/NAT pode agrupar usuários; não confiamos arbitrariamente em X-Forwarded-For. Validar a configuração de proxies confiáveis e volumes antes de publicar. Nenhuma configuração Render/proxy foi alterada. Uma solução distribuída e mudanças operacionais dependem de aprovação.

## 7. Quota, autorização e riscos remanescentes

Semáforo da TutorIA preservado: bloqueio local por aluno engloba validação, chamada e consumo, com liberação em finally. Mensagens recusadas saem antes do provider e SaveChanges. Teste existente de duas requisições disputando a sétima mensagem continua necessário. A proteção atual não garante atomicidade entre réplicas. Índice/consumo transacional/reserva e liberação exigem projeto específico; não se afirma garantia global.

Endpoint legado `POST /api/planos/me/ia/uso` continua existente e usa UTC, enquanto TutorIA usa o dia de Brasília; o endpoint também não compartilha o semáforo. Recomenda-se aprovar unificação do consumo e da data e eventual descontinuação dessa rota, preservando compatibilidade. Não foi alterado nesta entrega por envolver contrato/regra de consumo anterior.

A rota de histórico do responsável mantém endereço para compatibilidade, mas agora retorna 403 para vínculo válido; validações anteriores de IDs podem continuar retornando 400/403 antes do serviço. Essa mudança foi aprovada e clientes precisam tratar a recusa. Aluno/professor mantêm histórico e SignalR. Permissões de conta inativa com JWT ainda válido e permanência de conexões em grupos após alterações de estado merecem revisão específica de revogação, sem conceder novos acessos aqui. Não houve mudança da proteção de documentos dos professores.

## 8. Requisitos para Web e Android/.NET MAUI — somente especificação

| Requisito | Web | Android/.NET MAUI |
|---|---|---|
| Documentos no cadastro/conta | Consultar catálogo público, mostrar apenas links publicados e versão; indisponibilidade não bloqueia cadastro. Reutilizar na área de privacidade. | Mesmo contrato; abrir link aprovado em navegador, conservar navegação de retorno. |
| Senha/confirmar senha | Botão mostrar/ocultar com nome e estado acessíveis; não submeter formulário nem limpar valor; permitir gerenciadores de senha. | `Entry.IsPassword`, controle com descrição semântica e estado; não anunciar valor da senha. |
| Enter em formulário | Submit nativo quando válido; uma requisição, sem duplo envio. Não interferir com composição de texto. | Ação do teclado/`Completed` para próximo campo ou envio quando apropriado; sem envio acidental em campo multilinha. |
| Chat | Enter envia; Shift+Enter quebra linha; ignorar Enter durante composição IME; instrução visível. | Editor multilinha e botão explícito de envio; teclado físico pode adaptar Enter/Shift+Enter. Não presumir tecla Shift no teclado virtual. |
| Navegação | Tab/Shift+Tab em ordem lógica, sem tabindex positivo; Enter/Espaço conforme controle nativo; foco visível, sem armadilhas. | Ordem semântica e teclado físico; TalkBack, foco visível e retorno de foco após ações. |
| Rótulos e erros | Label associado; obrigatório em texto/atributo; erro junto ao campo com aria-describedby/aria-invalid; resumo focável quando necessário. | Propriedades semânticas, rótulos claros, erro próximo e anúncio acessível; foco no campo corrigível. |
| Contraste | Meta de verificação WCAG 2.2 AA: 4,5:1 texto normal, 3:1 texto grande/controles; não depender só de cor. | Verificar temas claro/escuro, escala de fonte, alvos de toque e contraste. |
| Moderação | Ler códigos novos em `motivo`, preservar pergunta para edição e quota; não chamar novamente nem exibir bloqueio como resposta do Gemini. Anunciar aviso sem repetir todo histórico. | Mesmo contrato, aviso acessível e possibilidade de reformular; não encerrar sessão por moderação. |
| Limites | Respeitar Retry-After, informar espera; não reenviar sozinho ao expirar. | Mesmo comportamento; adaptar mensagens do hub sem depender do parsing de texto livre além do código prefixado. |
| Denúncias | Interface só após aprovação/persistência; seleção de mensagem autorizada, categoria, descrição e confirmação com protocolo real. | Mesmo fluxo, acessível ao toque/TalkBack; responsável não ganha acesso às mensagens privadas. |

Os erros 400 existentes podem ser `{error}` ou validação MVC `{errors:{campo:[...]}}`; preservar ambos. 401 exige autenticação; 403 não deve sugerir impersonar aluno. Nenhuma tela ou protótipo foi criado. Testar fluxo completo por teclado, leitores de tela, tamanho de fonte e orientação na implementação visual futura.

Compatibilidade pendente: o Web atual tem tratamento específico para `MENSAGEM_OFENSIVA`; os códigos novos podem cair no aviso genérico e exigir atualização de status até a pessoa responsável integrar esse contrato. Isso não causa consumo de quota no backend. Não foi alterado o frontend para contornar essa dependência.

Referências técnicas: [WCAG 2.2 — referência rápida](https://www.w3.org/WAI/WCAG22/quickref/) e [Microsoft — acessibilidade em MAUI](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/accessibility?view=net-maui-10.0). São critérios de implementação/verificação, não certificação da interface atual.

## 9. Validação e decisões pendentes

Build inicial: aprovado, sem avisos/erros. Testes com HTTP/provider sintético, EF InMemory e fixtures locais; nenhuma conta real, Gemini, Neon, MongoDB Atlas ou Render acessado. Compilação e resultados ficam fora do repositório. Resultados finais são registrados no relatório de entrega.

Suíte completa executada: 357 aprovados, 1 falha (358 testes). Falha no teste existente `SqliteMentoriaChatStoreTests.Requisicoes_concorrentes_criam_uma_conversa_e_preservam_todas_as_mensagens`, com SQLite `database is locked`, já observado em validações anteriores; o store SQLite não foi modificado. Na execução focada inicial, o teste existente de janela de memória também falhou; ele passou na suíte completa. Não foram ocultadas essas falhas nem alterados componentes fora do escopo para corrigi-las. Relatório TRX externo: `%TEMP%/tcorg-8d4f599f/seguranca-tests/seguranca-final.trx`.

Após acrescentar a regressão para expressão figurada (“matar de rir”), a execução final focada aprovou 92 testes, sem falhas, cobrindo moderação, contratos/rate limiting, responsáveis e TutorIA. O build da API foi concluído nessa execução. `git diff --check` passou. A lista de mudanças em `TutorClass.API/wwwroot`, `TutorClass.Mobile`, `TutorClass.Mobile.Tests` e migrations permanece vazia. Não houve commit, push ou deploy.

Pendências para aprovação: dados/permissões/retenção de denúncias; documentos/contato/orientações jurídicos; registro e fluxo de aceite; política do futuro perfil Suporte; consumo transacional entre réplicas e rota legada de quota. Não há migrations criadas ou executadas. Não há aprovação implícita de novos fluxos parentais, idade, políticas jurídicas ou coleta de dados.
