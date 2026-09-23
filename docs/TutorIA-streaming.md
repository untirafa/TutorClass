# TutorIA: resposta progressiva e apresentação

## Verificação local de ofensas

Antes de obter contexto e chamar o provedor, `TutorIaModeracao` reconhece um conjunto restrito de ofensas diretas em frases inteiras. Retorna `MENSAGEM_OFENSIVA` com a quota intacta e mantém o acesso para reformular. Não registra a mensagem na memória da IA e não cria denúncias, sanções ou uma fila de revisão humana. Por isso o aviso não promete que a conversa será analisada.

É uma primeira camada conservadora, não um classificador completo: ofensas indiretas, disfarçadas ou inseridas em textos maiores podem passar. Palavras isoladas dentro de perguntas acadêmicas, citações, relatos de bullying e pedidos de ajuda não são motivo automático de bloqueio. As proteções do provedor continuam aplicáveis às mensagens que chegam até ele.

O frontend mantém negrito e itálico no tamanho do texto, inclusive itálico com negritos internos. A entidade de espaço `&#x20;`/`&#32;` é convertida em espaço, sem interpretar HTML da resposta.

O Web e o Mobile usam `POST /api/tutoria/chat/stream`, com o mesmo JWT e o mesmo JSON `{ "mensagem": "..." }` do chat tradicional. O endpoint `POST /api/tutoria/chat` continua disponível para clientes anteriores.

A API usa `streamGenerateContent?alt=sse` do Gemini, encaminha os trechos imediatamente e solicita que proxies não armazenem a resposta em buffer. Não há animação que esconda uma resposta previamente pronta. O streaming reduz o tempo até começar a ler; o tempo total e a espera pelo primeiro trecho continuam dependendo do provedor.

Eventos SSE:

- `delta`: `{ "text": "trecho" }`.
- `complete`: o contrato existente `TutorIaChatResponse`, incluindo resposta completa, plano e quota.
- `error`: mensagem controlada quando uma falha ocorre depois do início da transmissão.

Autenticação, restrições de plano, limite diário e exclusão mútua por aluno permanecem no backend. Uso e memória são registrados somente após a geração terminar com sucesso. Não se repetem tentativas depois que algum texto já foi entregue. Se a confirmação final se perder depois da persistência, pode haver consumo sem confirmação no cliente: por isso ambos os clientes exigem atualização de status antes de nova tentativa e nunca reenviam automaticamente.

O Web apresenta títulos, negrito, listas e blocos de código sem executar HTML da resposta. Fórmulas comuns (frações, raízes, potências e símbolos) são convertidas para notação linear legível. O Mobile utiliza texto nativo legível, preservando acentos e código. Não se trata de um renderizador completo de LaTeX; comandos não reconhecidos são preservados para não apagar informação matemática. O prompt também orienta o modelo a preferir fórmulas em notação linear.

Os testes usam respostas sintéticas e streams interrompidos/controlados, sem chamadas ao Gemini ou a bancos de produção. Cobrem entrega anterior ao término, UTF-8, autenticação, plano, validação, consumo único, cancelamento, falha parcial e apresentação.

Para disponibilizar a alteração, publique a API e os arquivos de `wwwroot` juntos (incluindo `tutor-text.js`). O Mobile requer recompilação e instalação do novo aplicativo. Não há migrations, novos secrets ou alterações necessárias nos pacotes. Esta implementação não realiza deploy automaticamente.

## Recuperação de indisponibilidade

`Gemini:FallbackModel` agora tem o padrão `gemini-3.1-flash-lite`. Antes de entregar texto, uma falha HTTP 429/500/502/503/504, de rede ou timeout troca imediatamente para esse modelo, usando a mesma chave e contexto. Não há troca após texto parcial, nem em falhas de autenticação ou bloqueios de segurança. O fluxo continua limitado a três tentativas no total; não há requisições especulativas simultâneas. As novas tentativas podem ter custo no provedor. A contingência simulada existente continua desligada por padrão.

No streaming, `Gemini:FirstTextTimeoutSeconds` limita a espera pelo primeiro texto a 8 segundos por tentativa (ou ao timeout geral, se menor). Após o primeiro texto, há até `Gemini:TimeoutSeconds` para concluir. Isso limita a espera, mas não é uma promessa de resposta em 8 segundos. O endpoint tradicional mantém seu timeout geral.

Para priorizar baixa latência em uma futura publicação, avaliar `Gemini__Model=gemini-3.1-flash-lite` e usar o modelo atual como `Gemini__FallbackModel`. Essa inversão não foi aplicada à configuração de produção. O Flash-Lite é voltado a tarefas mais simples: validar também a qualidade de respostas de matemática e programação. Referência oficial: https://ai.google.dev/gemini-api/docs/models/gemini-3.1-flash-lite

Os testes locais verificam a troca de modelo e o cancelamento da tentativa lenta com HTTP sintético. Disponibilidade, quota, qualidade e latência reais da conta ainda precisam ser verificadas em um teste autorizado. Modelos do mesmo provedor podem falhar juntos; a troca não elimina uma indisponibilidade geral.
