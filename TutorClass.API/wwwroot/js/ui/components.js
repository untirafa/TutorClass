// Componentes visuais reutilizáveis. Todo texto dinâmico entra como nó de texto (h()).
import { h, icon, uid } from '../core/dom.js';
import { initials, statusLabel, statusTone } from '../core/format.js';
import { announce } from '../core/a11y.js';

export const badge = (text, tone = 'neutral') => h('span', { class: `badge tone-${tone}` }, text);
export const statusBadge = (s) => badge(statusLabel(s), statusTone(s));
export const avatar = (name) => h('span', { class: 'avatar', 'aria-hidden': 'true' }, initials(name));

export function stars(n, count) {
  const v = Math.max(0, Math.min(5, Math.round(Number(n) || 0)));
  return h('span', { class: 'stars', role: 'img', 'aria-label': `Nota ${v} de 5${count != null ? `, ${count} avaliação(ões)` : ''}` },
    h('span', { 'aria-hidden': 'true' }, '★'.repeat(v) + '☆'.repeat(5 - v)));
}

export function loading(text = 'Carregando…') {
  return h('div', { class: 'state state-loading', role: 'status' }, h('span', { class: 'spinner', 'aria-hidden': 'true' }), h('span', null, text));
}
export const emptyState = (text, action) => h('div', { class: 'state state-empty' }, h('p', null, text), action || null);
export function errorState(text, retry) {
  return h('div', { class: 'state state-error', role: 'alert' }, icon('alert', 22), h('p', null, text),
    retry ? h('button', { type: 'button', class: 'btn secondary', onclick: retry }, 'Tentar novamente') : null);
}

export function dl(data) {
  const list = h('dl', { class: 'details' });
  for (const [k, v] of Object.entries(data)) {
    if (v && v.nodeType) list.append(h('div', null, h('dt', k), h('dd', v)));
    else list.append(h('div', null, h('dt', k), h('dd', v == null || v === '' ? '—' : String(v))));
  }
  return list;
}

export function link(label, href, cls = 'text-link') {
  return h('a', { href, class: cls }, label);
}
export function externalLink(label, url) {
  try {
    const u = new URL(url);
    if (['http:', 'https:'].includes(u.protocol))
      return h('a', { href: u.href, class: 'text-link', target: '_blank', rel: 'noopener noreferrer' },
        label, h('span', { class: 'sr-only' }, ' (abre em nova aba)'), icon('external', 16));
  } catch { /* cai no texto simples */ }
  return h('span', { class: 'muted' }, url || 'Referência indisponível');
}

export function card({ title, subtitle, level = 3, className = '' } = {}, ...children) {
  return h('article', { class: `card ${className}`.trim() },
    title ? h(`h${level}`, { class: 'card-title' }, title) : null,
    subtitle ? h('p', { class: 'muted' }, subtitle) : null, ...children);
}
export const grid = (items, className = '') => h('div', { class: `grid ${className}`.trim() }, items);
export const actions = (...btns) => h('div', { class: 'actions' }, btns);
export function btn(label, onClick, { kind = 'secondary', iconName, type = 'button', ariaLabel } = {}) {
  return h('button', { type, class: `btn ${kind}`, onclick: onClick, 'aria-label': ariaLabel }, iconName ? icon(iconName, 18) : null, label);
}
export function linkButton(label, href, kind = 'primary') { return h('a', { href, class: `btn ${kind}` }, label); }

// Executa uma ação de botão mostrando "ocupado" sem desabilitar (preserva o foco do teclado).
export async function runAction(button, fn, onError) {
  if (button.getAttribute('aria-disabled') === 'true') return;
  button.setAttribute('aria-disabled', 'true'); button.classList.add('is-busy');
  try { await fn(); } catch (error) { onError?.(error); } finally { button.removeAttribute('aria-disabled'); button.classList.remove('is-busy'); }
}

// Lista com estados vazio/erro e "carregar mais" (paginação para conexões móveis).
export function pagedList(items, renderItem, { size = 9, empty = 'Nenhum registro encontrado.', gridClass = 'grid cards', label = 'itens' } = {}) {
  const host = h('div', { class: 'paged' });
  if (!items.length) { host.append(emptyState(empty)); return host; }
  const list = h('div', { class: gridClass }), status = h('p', { class: 'muted small', 'aria-live': 'polite' });
  const more = h('button', { type: 'button', class: 'btn secondary' }, 'Carregar mais');
  let shown = 0;
  const showMore = () => {
    const first = shown; const next = items.slice(shown, shown + size);
    next.forEach(i => list.append(renderItem(i))); shown += next.length;
    status.textContent = `Mostrando ${shown} de ${items.length} ${label}.`;
    more.hidden = shown >= items.length;
    if (first > 0) { const target = list.children[first]?.querySelector('a,button'); target?.focus(); announce(`${next.length} ${label} adicionados.`); }
  };
  more.addEventListener('click', showMore); showMore();
  host.append(list, status, more);
  return host;
}

export function disclosure(summary, ...content) {
  return h('details', { class: 'disclosure' }, h('summary', null, summary), h('div', { class: 'disclosure-body' }, content));
}

export function tabs(items, { label, initial = 0 } = {}) {
  // items: [{id, label, render:()=>Node}] -> tablist/tabpanel com navegação por setas.
  const base = uid('tabs');
  const list = h('div', { role: 'tablist', 'aria-label': label, class: 'tablist' });
  const panels = items.map((it, i) => h('div', { role: 'tabpanel', id: `${base}-p${i}`, 'aria-labelledby': `${base}-t${i}`, tabindex: '0', class: 'tabpanel stack', hidden: i !== initial }));
  items.forEach((it, i) => list.append(h('button', {
    type: 'button', role: 'tab', id: `${base}-t${i}`, 'aria-controls': `${base}-p${i}`,
    'aria-selected': String(i === initial), tabindex: i === initial ? '0' : '-1', class: 'tab'
  }, it.label)));
  const rendered = new Set();
  const show = (i) => {
    panels.forEach((p, k) => { p.hidden = k !== i; });
    if (!rendered.has(i)) { rendered.add(i); panels[i].append(items[i].render()); }
  };
  list.addEventListener('keydown', (e) => {
    const tabsEls = [...list.querySelectorAll('[role=tab]')], i = tabsEls.indexOf(document.activeElement);
    if (i < 0) return;
    const to = { ArrowRight: i + 1, ArrowLeft: i - 1, Home: 0, End: tabsEls.length - 1 }[e.key];
    if (to == null) return;
    e.preventDefault(); const t = tabsEls[(to + tabsEls.length) % tabsEls.length]; t.focus(); t.click();
  });
  list.addEventListener('click', (e) => {
    const t = e.target.closest('[role=tab]'); if (!t) return;
    const tabsEls = [...list.querySelectorAll('[role=tab]')]; const i = tabsEls.indexOf(t);
    tabsEls.forEach((el, k) => { el.setAttribute('aria-selected', String(k === i)); el.tabIndex = k === i ? 0 : -1; });
    show(i);
  });
  show(initial);
  return h('div', { class: 'tabs' }, list, panels);
}

export function pageHeader(title, subtitle, ...extra) {
  return h('header', { class: 'page-header' }, h('div', null, h('h1', { tabindex: '-1' }, title), subtitle ? h('p', { class: 'lead' }, subtitle) : null), ...extra);
}
export function section(title, ...children) {
  const id = uid('sec');
  return h('section', { class: 'section stack', 'aria-labelledby': id }, h('h2', { id }, title), ...children);
}
export const stat = (label, value) => h('div', { class: 'stat' }, h('span', { class: 'stat-value' }, String(value)), h('span', { class: 'stat-label' }, label));

// Tabela semântica com legenda e cabeçalhos de coluna; rolagem horizontal focável em telas estreitas.
export function dataTable({ caption, columns, rows, empty = 'Nenhum registro.' }) {
  if (!rows.length) return emptyState(empty);
  const id = uid('tbl');
  return h('div', { class: 'table-wrap', role: 'region', 'aria-labelledby': id, tabindex: '0' },
    h('table', null, h('caption', { id }, caption),
      h('thead', null, h('tr', null, columns.map(c => h('th', { scope: 'col' }, c.label)))),
      h('tbody', null, rows.map(r => h('tr', null, columns.map(c => h('td', null, c.render ? c.render(r) : (r[c.key] ?? '—'))))))));
}
