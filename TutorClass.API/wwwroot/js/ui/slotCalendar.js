// Calendário de horários livres: só os dias em que o professor atende ficam habilitados e, ao escolher o dia,
// aparecem somente os intervalos realmente livres. Tudo em horário de Brasília; a API continua em UTC.
import { h } from '../core/dom.js';
import { publicGet, q, isAbort } from '../core/api.js';
import { announce } from '../core/a11y.js';
import { WEEK, BRT_OFFSET_MIN, brasiliaAvailability, minToTime, timeToMin, todayBrasilia } from '../core/format.js';

const MONTHS = ['janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho', 'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'];
const WEEKDAYS = ['dom', 'seg', 'ter', 'qua', 'qui', 'sex', 'sáb'];
const WEEKDAY_FULL = ['domingo', 'segunda-feira', 'terça-feira', 'quarta-feira', 'quinta-feira', 'sexta-feira', 'sábado'];
const STEP = 30;          // passo dos horários de início (minutos)
const MIN_DURATION = 30;  // menor aula aceita pela API
const MONTHS_AHEAD = 5;

const pad = (n) => String(n).padStart(2, '0');
const isoDate = (y, m, d) => `${y}-${pad(m + 1)}-${pad(d)}`;
const addDays = (date, n) => new Date(Date.parse(date + 'T00:00:00Z') + n * 86400000).toISOString().slice(0, 10);
const weekdayOf = (date) => new Date(date + 'T12:00:00Z').getUTCDay();
const longDate = (date) => { const [y, m, d] = date.split('-').map(Number); return `${WEEKDAY_FULL[weekdayOf(date)]}, ${d} de ${MONTHS[m - 1]} de ${y}`; };

// Intervalos livres de uma data de Brasília: junta as consultas das duas datas UTC que ela cobre.
export async function freeIntervals(professorId, date, { signal, now = new Date() } = {}) {
  const path = (d) => `/api/professores/${encodeURIComponent(professorId)}/horarios-livres` + q({ data: d });
  const [a, b] = await Promise.all([publicGet(path(date), { signal }), publicGet(path(addDays(date, 1)), { signal })]);
  const dayStart = Date.parse(date + 'T00:00:00Z') + BRT_OFFSET_MIN * 60000;
  let floor = 0;
  if (date === todayBrasilia(now)) floor = Math.ceil(((now.getTime() - dayStart) / 60000) / STEP) * STEP;
  const segs = [];
  for (const r of [...a, ...b]) {
    const base = Date.parse(String(r.data).slice(0, 10) + 'T00:00:00Z');
    const s = Math.max((base + timeToMin(r.inicio) * 60000 - dayStart) / 60000, 0, floor);
    const e = Math.min((base + timeToMin(r.fim) * 60000 - dayStart) / 60000, 1440);
    if (e - s >= MIN_DURATION) segs.push([s, e]);
  }
  segs.sort((x, y) => x[0] - y[0]);
  const merged = [];
  for (const [s, e] of segs) {
    const last = merged[merged.length - 1];
    if (last && s <= last[1]) last[1] = Math.max(last[1], e); else merged.push([s, e]);
  }
  return merged.map(([s, e]) => ({ ini: s, fim: e }));
}

export function slotCalendar({ professorId, disponibilidades = [], pick = false, onChange } = {}) {
  const days = new Set(brasiliaAvailability(disponibilidades).map(g => g.dia));
  const today = todayBrasilia();
  const [ty, tm] = today.split('-').map(Number);
  let year = ty, month = tm - 1, selected = null, intervals = [], chosen = null, start = null, duration = 60, token = 0;

  const heading = h('h3', { class: 'cal-title', 'aria-live': 'polite' });
  const prev = h('button', { type: 'button', class: 'btn secondary', 'aria-label': 'Mês anterior' }, '‹');
  const next = h('button', { type: 'button', class: 'btn secondary', 'aria-label': 'Próximo mês' }, '›');
  const grid = h('div', { class: 'cal-grid', role: 'group' });
  const result = h('div', { class: 'stack-sm', 'aria-live': 'polite' });
  const el = h('div', { class: 'cal stack-sm' }, h('div', { class: 'cal-head' }, prev, heading, next), grid, result);

  const monthIndex = () => (year - ty) * 12 + (month - (tm - 1));
  const enabled = (date) => date >= today && days.has(WEEK[weekdayOf(date)]);

  function renderMonth() {
    heading.textContent = `${MONTHS[month][0].toUpperCase()}${MONTHS[month].slice(1)} de ${year}`;
    grid.setAttribute('aria-label', `Dias de ${MONTHS[month]} de ${year}. Só os dias com horários do professor estão habilitados.`);
    prev.disabled = monthIndex() <= 0; next.disabled = monthIndex() >= MONTHS_AHEAD;
    const first = new Date(Date.UTC(year, month, 1)).getUTCDay(), total = new Date(Date.UTC(year, month + 1, 0)).getUTCDate();
    const cells = WEEKDAYS.map(w => h('span', { class: 'cal-wd', 'aria-hidden': 'true' }, w));
    for (let i = 0; i < first; i++) cells.push(h('span', { class: 'cal-blank', 'aria-hidden': 'true' }));
    for (let d = 1; d <= total; d++) {
      const date = isoDate(year, month, d), ok = enabled(date);
      const b = h('button', { type: 'button', class: 'cal-day', 'aria-pressed': String(date === selected), 'aria-label': `${longDate(date)}${ok ? ', com horários' : ', indisponível'}` }, String(d));
      b.disabled = !ok;
      if (ok) b.addEventListener('click', () => choose(date));
      cells.push(b);
    }
    grid.replaceChildren(...cells);
  }

  const starts = () => {
    if (!chosen) return [];
    const out = [];
    for (let m = chosen.ini; m + duration <= chosen.fim; m += STEP) out.push(m);
    return out;
  };

  function emit() { onChange?.(pick && selected && chosen && start != null ? { date: selected, time: minToTime(start) } : null); }

  function renderResult() {
    if (!selected) { result.replaceChildren(h('p', { class: 'muted small' }, 'Escolha um dia habilitado para ver os horários livres.')); return; }
    const title = h('h4', null, `Horários livres: ${longDate(selected)} (Brasília)`);
    if (!intervals.length) { result.replaceChildren(title, h('p', { class: 'muted' }, 'Não há horários livres neste dia. Escolha outro dia.')); return; }
    const list = h('ul', { class: 'slot-list' }, intervals.map(iv => {
      const b = h('button', { type: 'button', class: 'btn secondary', 'aria-pressed': String(chosen === iv) }, `${minToTime(iv.ini)} às ${minToTime(iv.fim)}`);
      if (pick) b.addEventListener('click', () => { chosen = iv; start = null; renderResult(); selectFirstStart(); });
      else b.disabled = true;
      return h('li', null, b);
    }));
    const parts = [title, list];
    if (pick && chosen) {
      const options = starts();
      if (!options.length) parts.push(h('p', { class: 'state-error small', role: 'alert' }, `Este intervalo é menor que a duração escolhida (${duration} min). Escolha outro intervalo ou reduza a duração.`));
      else {
        const sel = h('select', { id: 'cal-start' }, options.map(m => h('option', { value: String(m) }, minToTime(m))));
        sel.value = String(start ?? options[0]); start = Number(sel.value);
        sel.addEventListener('change', () => { start = Number(sel.value); emit(); announce(`Início às ${minToTime(start)}.`); });
        parts.push(h('div', { class: 'field' }, h('label', { for: 'cal-start' }, 'Horário de início (Brasília)'), sel,
          h('p', { class: 'small muted' }, `A aula termina às ${minToTime(start + duration)}.`)));
      }
    }
    result.replaceChildren(...parts);
    emit();
  }
  function selectFirstStart() { const o = starts(); start = o.length ? o[0] : null; renderResult(); }

  async function choose(date) {
    selected = date; chosen = null; start = null; intervals = []; renderMonth();
    result.replaceChildren(h('p', { class: 'muted' }, 'Consultando horários…'));
    const mine = ++token;
    try {
      const rows = await freeIntervals(professorId, date);
      if (mine !== token) return;
      intervals = rows;
      if (pick && rows.length === 1) chosen = rows[0];
      renderResult(); if (chosen) selectFirstStart();
      announce(rows.length ? `${rows.length} intervalo(s) livre(s) em ${longDate(date)}.` : 'Nenhum horário livre neste dia.');
    } catch (error) {
      if (mine !== token || isAbort(error)) return;
      result.replaceChildren(h('p', { class: 'state-error', role: 'alert' }, error.message || 'Não foi possível consultar os horários.'));
    }
  }

  prev.addEventListener('click', () => { month -= 1; if (month < 0) { month = 11; year -= 1; } renderMonth(); });
  next.addEventListener('click', () => { month += 1; if (month > 11) { month = 0; year += 1; } renderMonth(); });
  renderMonth(); renderResult();
  return {
    el,
    value: () => (selected && chosen && start != null ? { date: selected, time: minToTime(start) } : null),
    setDuration(minutes) { const n = Number(minutes); if (Number.isFinite(n) && n > 0 && n !== duration) { duration = n; if (chosen) { const o = starts(); if (!o.includes(start)) start = o.length ? o[0] : null; renderResult(); } } }
  };
}
