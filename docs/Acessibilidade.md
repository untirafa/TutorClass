# Acessibilidade do front-end (WCAG 2.2 AA)

O TutorClass adota a WCAG 2.2 nível AA, prevista na documentação do projeto (Projeto Integrado Multidisciplinar) e no manual.

## Recursos implementados

| Critério | Implementação |
|---|---|
| 1.3.1 Informações e relações | Landmarks (`header`, `nav`, `main`, `aside`, `footer`), títulos em ordem, tabelas com `caption` e `scope`, listas semânticas |
| 1.4.3 / 1.4.11 Contraste | Tokens de cor com contraste ≥ 4,5:1 (texto) e 3:1 (componentes), em claro, escuro e alto contraste |
| 1.4.4 / 1.4.10 Redimensionar e refluxo | Escala de texto 100/115/130 %, layout sem rolagem horizontal a 320 px |
| 1.4.12 Espaçamento | Unidades relativas; nenhum conteúdo cortado ao ajustar espaçamento |
| 2.1.1 / 2.1.2 Teclado | Todas as ações por teclado; diálogos nativos `<dialog>` com foco preso e Esc; abas com setas |
| 2.4.1 Contornar blocos | Links "Pular para o conteúdo" e "Pular para a navegação" |
| 2.4.3 / 2.4.7 / 2.4.11 Foco | Foco movido para o título a cada troca de rota; anel de foco com halo; nada fica encoberto por barras fixas |
| 2.2.1 Tempo | Avisos no topo da tela somem em 4 s, com pausa ao passar o mouse ou focar, botão X e anúncio por leitor de tela (`role=status`/`alert`) |
| 2.3.3 Movimento | Preferência "reduzir movimento" (sistema e painel) |
| 2.5.8 Tamanho do alvo | Alvos ≥ 44 px |
| 3.3.1 / 3.3.3 Erros | Mensagens ligadas por `aria-describedby`, resumo de erros com links e foco |
| 4.1.2 / 4.1.3 Nome, função e mensagens | Regiões `aria-live` (polida/assertiva), `aria-busy` na resposta em streaming da TutorIA |
| Preferências | Painel "Acessibilidade": tema claro/escuro/automático, alto contraste, tamanho do texto, reduzir movimento (salvas em `localStorage`; nenhum dado pessoal) |

- **VLibras:** o widget oficial do Governo Federal (tradução para Libras) está em todas as telas, carregado de `vlibras.gov.br`. Se o serviço estiver fora do ar, o site segue normal. Os testes automatizados não o carregam.

## Verificação

Automatizada com axe-core (regras WCAG 2.0/2.1/2.2 A e AA e boas práticas) via `npm run audit:a11y`, em desktop, 320 px, tema escuro e alto contraste, nas telas públicas e de cada perfil: **0 violações**.

**Limite:** a verificação automática cobre apenas parte dos critérios. Testes manuais com leitores de tela (NVDA, VoiceOver, TalkBack) e com usuários ainda precisam ser feitos antes de declarar conformidade plena.
