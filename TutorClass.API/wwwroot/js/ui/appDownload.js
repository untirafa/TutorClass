// Divulgação do aplicativo Android: faixa na página inicial, passo a passo na ajuda e link no rodapé.
// Dentro do próprio aplicativo (WebView com "TutorClassApp" no user agent) nada disso aparece.
import { h } from '../core/dom.js';

// O APK é anexado a uma Release do GitHub com o nome TutorClass.apk; este link sempre aponta para a mais recente.
export const APP_APK_URL = 'https://github.com/untirafa/TutorClass/releases/latest/download/TutorClass.apk';
export const isNativeApp = () => /TutorClassApp/.test(navigator.userAgent || '');

const downloadLink = (text, cls = 'btn') =>
  h('a', { class: cls, href: APP_APK_URL, rel: 'noopener', download: 'TutorClass.apk' }, text);

export function appBand() {
  if (isNativeApp()) return null;
  return h('section', { class: 'container section', 'aria-labelledby': 'app-title' }, h('div', { class: 'band' },
    h('div', null, h('h2', { id: 'app-title' }, 'Leve o TutorClass no celular'),
      h('p', null, 'Baixe o aplicativo para Android e use tudo o que existe no site, inclusive a tradução em Libras.')),
    downloadLink('Baixar o app Android')));
}

export function appInstallGuide() {
  if (isNativeApp()) return null;
  return h('section', { class: 'stack', 'aria-labelledby': 'app-guide' },
    h('h2', { id: 'app-guide' }, 'Instalar o aplicativo Android'),
    h('p', { class: 'muted' }, 'O aplicativo é uma versão acadêmica, fora da loja do Google. Por isso o Android pede uma permissão na primeira instalação.'),
    h('ol', { class: 'stack-sm' },
      h('li', null, 'Toque em “Baixar o app Android” e aguarde o arquivo TutorClass.apk (cerca de 85 MB).'),
      h('li', null, 'Abra o arquivo baixado. Se o Android avisar sobre “fontes desconhecidas”, toque em Configurações e permita a instalação pelo navegador.'),
      h('li', null, 'Confirme a instalação. Se o Google Play Protect perguntar, escolha “Instalar mesmo assim”.'),
      h('li', null, 'Abra o TutorClass e entre com a sua conta. O app precisa de internet.')),
    h('p', null, downloadLink('Baixar o app Android', 'btn primary')));
}
