// VLibras: widget oficial do Governo Federal que traduz o texto da página para Libras (WCAG 1.2.6 é AAA; aqui é um apoio adicional).
// O marcador ([vw]) fica em index.html, fora da área que o roteador troca, e por isso aparece em todas as telas.
// Se o script do governo não carregar (sem internet, bloqueio de rede), o site continua funcionando normalmente.
const APP = 'https://vlibras.gov.br/app';

export function setupVLibras() {
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
