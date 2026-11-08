// Acessibilidade transversal (WCAG 2.2): anúncios, foco, preferências do usuário e atalhos.
const KEY = 'tutorclass.prefs';
export const DEFAULT_PREFS = Object.freeze({ scale: '100', contrast: 'auto', motion: 'auto', theme: 'auto' });
let prefs = { ...DEFAULT_PREFS };

const storage = {
  get() { try { return JSON.parse(localStorage.getItem(KEY) || 'null'); } catch { return null; } },
  set(value) { try { localStorage.setItem(KEY, JSON.stringify(value)); } catch { /* modo privado */ } }
};

export function loadPrefs() {
  const saved = storage.get();
  prefs = { ...DEFAULT_PREFS, ...(saved && typeof saved === 'object' ? sanitize(saved) : {}) };
  applyPrefs();
  return prefs;
}
function sanitize(value) {
  const ok = { scale: ['100', '115', '130'], contrast: ['auto', 'high'], motion: ['auto', 'reduce'], theme: ['auto', 'light', 'dark'] };
  return Object.fromEntries(Object.entries(value).filter(([k, v]) => ok[k]?.includes(String(v))).map(([k, v]) => [k, String(v)]));
}
export const getPrefs = () => ({ ...prefs });
export function setPrefs(next) {
  prefs = { ...prefs, ...sanitize(next) };
  storage.set(prefs); applyPrefs();
}
export function applyPrefs(root = document.documentElement) {
  root.dataset.textScale = prefs.scale; root.dataset.contrast = prefs.contrast;
  root.dataset.motion = prefs.motion; root.dataset.theme = prefs.theme;
}

// Regiões ao vivo permanentes no index.html: politeness separada para status e alertas.
export function announce(message, { urgent = false } = {}) {
  const region = document.getElementById(urgent ? 'live-assertive' : 'live-polite');
  if (!region) return;
  region.textContent = '';
  // Alterar o texto em outro quadro faz leitores de tela anunciarem mensagens repetidas.
  requestAnimationFrame(() => { region.textContent = message; });
}

// Move o foco para o título da nova tela (2.4.3 Ordem do foco; 3.2.2 Na entrada).
export function focusHeading(root = document.getElementById('main')) {
  const target = root?.querySelector('h1') || root;
  if (!target) return;
  if (!target.hasAttribute('tabindex')) target.setAttribute('tabindex', '-1');
  target.focus({ preventScroll: true });
}

export function trapEscape(node, onEscape) {
  node.addEventListener('keydown', (event) => { if (event.key === 'Escape') { event.stopPropagation(); onEscape(event); } });
}

// Roving tabindex para abas e barras de ferramentas (padrão WAI-ARIA APG).
export function rovingTabs(tablist, { onSelect }) {
  const tabs = () => [...tablist.querySelectorAll('[role="tab"]')];
  const select = (tab, focus = true) => {
    for (const t of tabs()) { const on = t === tab; t.setAttribute('aria-selected', String(on)); t.tabIndex = on ? 0 : -1; }
    if (focus) tab.focus();
    onSelect(tab);
  };
  tablist.addEventListener('click', (e) => { const t = e.target.closest('[role="tab"]'); if (t) select(t, false); });
  tablist.addEventListener('keydown', (e) => {
    const list = tabs(); const i = list.indexOf(document.activeElement);
    if (i < 0) return;
    const move = { ArrowRight: i + 1, ArrowLeft: i - 1, Home: 0, End: list.length - 1 }[e.key];
    if (move == null) return;
    e.preventDefault(); select(list[(move + list.length) % list.length]);
  });
  return { select };
}
