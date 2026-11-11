// Construtor de elementos seguro: texto sempre entra como nó de texto, nunca como HTML.
// h('button', {class:'btn', onclick: fn, 'aria-label': 'Fechar'}, 'Texto', outroNo)

const SVG_NS = 'http://www.w3.org/2000/svg';
let seq = 0;
export const uid = (prefix = 'id') => `${prefix}-${++seq}`;

export function h(tag, attrs, ...children) {
  const node = document.createElement(tag);
  if (attrs && (typeof attrs !== 'object' || attrs.nodeType || Array.isArray(attrs))) { children.unshift(attrs); attrs = null; }
  for (const [key, value] of Object.entries(attrs || {})) {
    if (value === false || value == null) continue;
    if (key === 'class') node.className = value;
    else if (key === 'dataset') Object.assign(node.dataset, value);
    else if (key.startsWith('on') && typeof value === 'function') node.addEventListener(key.slice(2).toLowerCase(), value);
    else if (key === 'value' || key === 'checked' || key === 'selected' || key === 'disabled' || key === 'hidden') node[key] = value;
    else node.setAttribute(key, value === true ? '' : String(value));
  }
  append(node, ...children);
  return node;
}

export function append(parent, ...children) {
  for (const child of children.flat(Infinity)) {
    if (child == null || child === false) continue;
    parent.append(child.nodeType ? child : document.createTextNode(String(child)));
  }
  return parent;
}

export const clear = (node) => { node.replaceChildren(); return node; };
export const mount = (node, ...children) => { node.replaceChildren(); return append(node, ...children); };

// Ícones: SVG inline decorativo (aria-hidden). O nome acessível fica no texto do controle.
const ICONS = {
  home: 'M3 11l9-8 9 8M5 10v10h5v-6h4v6h5V10',
  search: 'M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14zM21 21l-4.5-4.5',
  calendar: 'M4 6h16v14H4zM4 10h16M8 3v4M16 3v4',
  book: 'M4 5a2 2 0 0 1 2-2h13v16H6a2 2 0 0 0-2 2zM4 19a2 2 0 0 1 2-2h13',
  chat: 'M4 5h16v11H9l-5 4z',
  chart: 'M4 20V4M4 20h16M8 16v-5M12 16V8M16 16v-8',
  spark: 'M12 3l2.2 6.8L21 12l-6.8 2.2L12 21l-2.2-6.8L3 12l6.8-2.2z',
  layers: 'M12 3l9 5-9 5-9-5zM3 13l9 5 9-5M3 17.5l9 5 9-5',
  play: 'M6 4l14 8-14 8z',
  pause: 'M7 4h4v16H7zM13 4h4v16h-4z',
  wallet: 'M3 7h17a1 1 0 0 1 1 1v11a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1zM3 7V5a1 1 0 0 1 1-1h13M16 14h2',
  flag: 'M5 21V4M5 4h12l-2.5 4L17 12H5',
  bell: 'M6 16V11a6 6 0 1 1 12 0v5l2 2H4zM10 21h4',
  id: 'M3 5h18v14H3zM7 10a2 2 0 1 0 4 0 2 2 0 0 0-4 0M6 16c.5-2 5.5-2 6 0M14 10h4M14 14h4',
  clock: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18zM12 7v5l3 2',
  user: 'M12 4a4 4 0 1 0 0 8 4 4 0 0 0 0-8zM4 21c1-5 15-5 16 0',
  users: 'M9 5a3.5 3.5 0 1 0 0 7 3.5 3.5 0 0 0 0-7zM2 20c1-5.5 12-5.5 13 0M17 6a3 3 0 0 1 0 6M18 14c3 .5 4 2.5 4 5',
  help: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18zM9.5 9.5a2.5 2.5 0 1 1 3.5 2.3c-.7.4-1 1-1 1.7M12 17h.01',
  menu: 'M4 6h16M4 12h16M4 18h16',
  close: 'M6 6l12 12M18 6L6 18',
  more: 'M5 12h.01M12 12h.01M19 12h.01',
  filter: 'M4 5h16l-6 8v6l-4-2v-4z',
  star: 'M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z',
  check: 'M5 12.5l4.5 4.5L19 7',
  alert: 'M12 3l10 18H2zM12 10v5M12 18h.01',
  access: 'M12 4a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3zM5 9h14M12 9v5M9 21l3-7 3 7',
  logout: 'M10 4H5v16h5M15 8l4 4-4 4M19 12H9',
  refresh: 'M20 11a8 8 0 1 0-2.3 6M20 4v7h-7',
  shield: 'M12 3l8 3v6c0 5-3.5 8-8 9-4.5-1-8-4-8-9V6z',
  arrow: 'M5 12h14M13 6l6 6-6 6',
  external: 'M14 4h6v6M20 4l-9 9M18 14v6H4V6h6',
  plus: 'M12 5v14M5 12h14',
  edit: 'M4 20h4L19 9l-4-4L4 16zM13 7l4 4',
  trash: 'M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13',
  eye: 'M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12zM12 9a3 3 0 1 0 0 6 3 3 0 0 0 0-6z',
  eyeoff: 'M3 3l18 18M10.6 6.1A9.8 9.8 0 0 1 12 5c6 0 10 7 10 7a17 17 0 0 1-3.2 4M6.4 7.6A17 17 0 0 0 2 12s4 7 10 7c1.6 0 3-.4 4.3-1M9.9 9.9a3 3 0 0 0 4.2 4.2',
  lock: 'M6 11h12v9H6zM8 11V8a4 4 0 0 1 8 0v3M12 15v2',
  info: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18zM12 11v6M12 7.5h.01',
  sun: 'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8zM12 2v2M12 20v2M2 12h2M20 12h2M5 5l1.5 1.5M17.5 17.5L19 19M5 19l1.5-1.5M17.5 6.5L19 5'
};
export function icon(name, size = 20) {
  const svg = document.createElementNS(SVG_NS, 'svg');
  svg.setAttribute('viewBox', '0 0 24 24'); svg.setAttribute('width', size); svg.setAttribute('height', size);
  svg.setAttribute('aria-hidden', 'true'); svg.setAttribute('focusable', 'false'); svg.setAttribute('class', 'icon');
  const path = document.createElementNS(SVG_NS, 'path');
  path.setAttribute('d', ICONS[name] || ICONS.help);
  path.setAttribute('fill', 'none'); path.setAttribute('stroke', 'currentColor');
  path.setAttribute('stroke-width', '1.8'); path.setAttribute('stroke-linecap', 'round'); path.setAttribute('stroke-linejoin', 'round');
  svg.append(path);
  return svg;
}
