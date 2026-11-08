import { h, mount } from '../core/dom.js';
import { pageHeader, section, link } from '../ui/components.js';
import { accessibilityPanel } from '../shell.js';

export default function accessibility({ host }) {
  mount(host, h('div', { class: 'container stack prose' },
    pageHeader('Acessibilidade', 'O TutorClass é projetado para ser usado por todas as pessoas, em qualquer dispositivo.'),
    section('Nosso compromisso', h('p', 'Seguimos as Diretrizes de Acessibilidade para Conteúdo Web (WCAG) 2.2, nível AA, publicadas pelo W3C, e as boas práticas de design responsivo e inclusivo previstas no projeto.')),
    section('O que já está disponível',
      h('ul', null,
        h('li', 'Navegação completa por teclado, com foco sempre visível e links de salto para o conteúdo e para a navegação.'),
        h('li', 'Estrutura semântica (cabeçalho, navegação, conteúdo principal, rodapé), títulos hierárquicos e rótulos em todos os campos.'),
        h('li', 'Mensagens de erro ligadas ao campo, com resumo no topo do formulário e foco levado ao primeiro problema.'),
        h('li', 'Avisos de status e de carregamento anunciados por leitores de tela, sem tirar o foco de onde você está.'),
        h('li', 'Contraste de pelo menos 4,5:1 para textos, alvos de toque de pelo menos 44 × 44 px e fonte-base de 16 px.'),
        h('li', 'Layout responsivo (celular, tablet e desktop), zoom até 400% sem rolagem horizontal e suporte a orientação retrato e paisagem.'),
        h('li', 'Preferências suas: tamanho do texto, tema claro/escuro, alto contraste e redução de animações — além de respeitar as configurações do sistema.'),
        h('li', 'Os avisos no topo da tela somem em 4 segundos, mas o tempo pausa enquanto você passa o mouse ou foca neles, e todos têm botão para fechar; também são anunciados por leitores de tela. Nenhuma informação depende apenas de cor.'))),
    h('div', null, h('button', { type: 'button', class: 'btn primary', onclick: accessibilityPanel }, 'Abrir preferências de acessibilidade')),
    section('Atalhos de teclado', h('ul', null,
      h('li', h('kbd', 'Tab'), ' e ', h('kbd', 'Shift'), '+', h('kbd', 'Tab'), ': avançar e voltar entre os controles.'),
      h('li', h('kbd', 'Enter'), ' ou ', h('kbd', 'Espaço'), ': ativar botões e links.'),
      h('li', h('kbd', 'Esc'), ': fechar janelas.'),
      h('li', 'Abas: setas ', h('kbd', '←'), ' ', h('kbd', '→'), ', ', h('kbd', 'Home'), ' e ', h('kbd', 'End'), '.'))),
    section('Limitações conhecidas', h('p', 'Vídeos de gravações e documentos externos abertos em outra aba pertencem a terceiros e podem não atender aos mesmos critérios. A recuperação de senha ainda não está disponível. A validação com pessoas usuárias de tecnologias assistivas está prevista no plano de testes de usabilidade.')),
    section('Fale com a gente', h('p', null, 'Encontrou uma barreira? Descreva-a na ', link('Central de ajuda', '#ajuda'), ' ou use o contato informado em ', link('Privacidade e termos', '#privacidade'), '. Respondemos o mais rápido possível.'))));
}
