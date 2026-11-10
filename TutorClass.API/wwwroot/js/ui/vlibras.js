// VLibras: widget oficial do Governo Federal que traduz o texto da página para Libras (WCAG 1.2.6 é AAA; aqui é um apoio adicional).
// O marcador ([vw]) fica em index.html, fora da área que o roteador troca, e por isso aparece em todas as telas.
// Se o script do governo não carregar (sem internet, bloqueio de rede), o site continua funcionando normalmente.
const APP = 'https://vlibras.gov.br/app';

// O widget não deve abrir sozinho: só fica visível depois que a pessoa aciona o botão de Libras.
// Antes disso a janela do widget fica escondida (CSS: body:not(.vlibras-aberto)) e, se ele tentar abrir sozinho, é fechada.
function keepClosedUntilRequested() {
  const isButton = (e) => e.target instanceof Element && e.target.closest('[vw-access-button]');
  const open = (e) => { if (isButton(e) && (e.type !== 'keydown' || e.key === 'Enter' || e.key === ' ')) document.body.classList.add('vlibras-aberto'); };
  document.addEventListener('click', open, true);
  document.addEventListener('keydown', open, true);
  const root = document.querySelector('[vw]');
  if (!root) return;
  new MutationObserver(() => {
    if (document.body.classList.contains('vlibras-aberto')) return;
    const wrapper = root.querySelector('[vw-plugin-wrapper]'), button = root.querySelector('[vw-access-button]');
    // Só altera quando necessário, para não disparar o próprio observador em laço.
    if (wrapper?.classList.contains('active')) wrapper.classList.remove('active');
    if (button && !button.classList.contains('active')) button.classList.add('active');
  }).observe(root, { attributes: true, attributeFilter: ['class'], subtree: true });
}

export function setupVLibras() {
  keepClosedUntilRequested();
  // Navegadores automatizados (testes e auditoria) não precisam do widget e não devem depender de um serviço externo.
  if (navigator.webdriver || document.querySelector('script[data-vlibras]')) return;
  const script = document.createElement('script');
  script.src = `${APP}/vlibras-plugin.js`;
  script.async = true;
  script.dataset.vlibras = '';
  script.addEventListener('load', () => {
    try { new window.VLibras.Widget(APP); } catch { /* widget indisponível: segue sem ele */ }
  });
  script.addEventListener('error', () => document.querySelector('[vw]')?.remove());
  document.head.append(script);
}
