// VLibras: widget oficial do Governo Federal que traduz o texto da página para Libras (WCAG 1.2.6 é AAA; aqui é um apoio adicional).
// O widget é carregado depois que a página termina de abrir, sem bloquear o site, e mostra o botão oficial do VLibras em todas as telas.
// O marcador ([vw]) fica em index.html, fora da área que o roteador troca.
// Se o script do governo não carregar (sem internet, bloqueio de rede), o site continua funcionando normalmente.
const APP = 'https://vlibras.gov.br/app';

function load() {
  if (document.querySelector('script[data-vlibras]')) return;
  const script = document.createElement('script');
  script.src = `${APP}/vlibras-plugin.js`;
  script.async = true;
  script.dataset.vlibras = '';
  script.addEventListener('load', () => { try { new window.VLibras.Widget(APP); } catch { /* sem tradutor: o site segue normal */ } });
  document.head.append(script);
}

export function setupVLibras() {
  if (document.readyState === 'complete') load();
  else window.addEventListener('load', load, { once: true });
}
