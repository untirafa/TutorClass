// VLibras: widget oficial do Governo Federal que traduz o texto da página para Libras (WCAG 1.2.6 é AAA; aqui é um apoio adicional).
// O widget NÃO é carregado ao abrir o site: só depois que a pessoa toca no botão “Libras” (abaixo). Assim ele nunca abre sozinho
// e nenhum serviço externo é consultado por quem não usa Libras. O marcador ([vw]) fica em index.html, fora da área que o roteador troca.
// Se o script do governo não carregar (sem internet, bloqueio de rede), o site continua funcionando normalmente.
const APP = 'https://vlibras.gov.br/app';
let started = false;

function activate(trigger) {
  if (started) return;
  started = true;
  trigger.disabled = true;
  trigger.textContent = 'Carregando…';
  const fail = () => { started = false; trigger.disabled = false; trigger.textContent = 'Libras'; };
  const script = document.createElement('script');
  script.src = `${APP}/vlibras-plugin.js`;
  script.async = true;
  script.dataset.vlibras = '';
  script.addEventListener('error', fail);
  script.addEventListener('load', () => {
    try { new window.VLibras.Widget(APP); } catch { fail(); return; }
    document.body.classList.add('vlibras-aberto');
    // Quando o widget termina de montar o próprio botão, ele é acionado uma vez para abrir o tradutor.
    let tries = 0;
    const timer = setInterval(() => {
      const button = document.querySelector('[vw-access-button]');
      if (button && button.children.length) { clearInterval(timer); trigger.remove(); button.click(); }
      else if (++tries > 40) { clearInterval(timer); trigger.remove(); }
    }, 250);
  });
  document.head.append(script);
}

export function setupVLibras() {
  if (document.getElementById('vlibras-trigger')) return;
  const trigger = document.createElement('button');
  trigger.type = 'button';
  trigger.id = 'vlibras-trigger';
  trigger.className = 'vlibras-trigger';
  trigger.textContent = 'Libras';
  trigger.setAttribute('aria-label', 'Ativar a tradução da página para Libras (VLibras)');
  trigger.addEventListener('click', () => activate(trigger));
  document.body.append(trigger);
}
