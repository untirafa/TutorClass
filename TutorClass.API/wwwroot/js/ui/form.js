// Formulários acessíveis: rótulo visível ligado ao controle, ajuda e erro associados por aria-describedby,
// resumo de erros com links, foco no primeiro problema e estado de envio sem perder o foco.
import { h, uid, icon } from '../core/dom.js';
import { announce } from '../core/a11y.js';
import { errorText, isAbort } from '../core/api.js';

export const field = (name, label, type = 'text', extra = {}) => ({ name, label, type, ...extra });
export const optional = (name, label, type = 'textarea', extra = {}) => field(name, label, type, { required: false, ...extra });

const ATTRS = ['min', 'max', 'step', 'maxLength', 'minLength', 'placeholder', 'pattern', 'autocomplete', 'inputMode', 'rows'];

function buildControl(f, id, describedBy, value) {
  let control;
  if (f.type === 'textarea') control = h('textarea', { rows: f.rows || 4 });
  else if (f.options) {
    control = h('select', null, f.options.map(o => {
      const v = typeof o === 'object' ? o.value : o, l = typeof o === 'object' ? o.label : o;
      return h('option', { value: v }, l);
    }));
  } else control = h('input', { type: f.type === 'checkbox' ? 'checkbox' : f.type });
  control.name = f.name; control.id = id;
  for (const key of ATTRS) if (f[key] !== undefined && !(key === 'rows' && f.type !== 'textarea')) control[key] = f[key];
  if (f.type === 'checkbox') control.checked = !!value;
  else control.value = value ?? f.value ?? (f.options ? (typeof f.options[0] === 'object' ? f.options[0].value : f.options[0]) : '');
  if (describedBy.length) control.setAttribute('aria-describedby', describedBy.join(' '));
  if (f.required !== false && f.type !== 'checkbox') control.setAttribute('aria-required', 'true');
  if (f.disabled) control.disabled = true;
  return control;
}

// Campo de senha com botão "olhinho": alterna entre ocultar e mostrar (botão com aria-pressed e nome acessível).
function passwordBox(control, label) {
  const eye = icon('eye', 20), eyeOff = icon('eyeoff', 20); eyeOff.setAttribute('hidden', '');
  const button = h('button', { type: 'button', class: 'pw-toggle', 'aria-pressed': 'false', 'aria-label': `Mostrar ${label.toLowerCase()}` }, eye, eyeOff);
  button.addEventListener('click', () => {
    const show = control.type === 'password';
    control.type = show ? 'text' : 'password';
    button.setAttribute('aria-pressed', String(show));
    button.setAttribute('aria-label', `${show ? 'Ocultar' : 'Mostrar'} ${label.toLowerCase()}`);
    eye.toggleAttribute('hidden', show); eyeOff.toggleAttribute('hidden', !show);
    announce(show ? 'Senha visível.' : 'Senha oculta.');
  });
  return h('div', { class: 'pw-box' }, control, button);
}

export function messageFor(control, f) {
  const v = control.type === 'checkbox' ? control.checked : String(control.value ?? '').trim();
  if (f.type !== 'checkbox' && f.required !== false && v === '') return `Preencha o campo “${f.label}”.`;
  if (v === '' || f.type === 'checkbox') return '';
  if (f.type === 'email' && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return `Informe um e-mail válido em “${f.label}”.`;
  if (f.type === 'url') { try { const u = new URL(v); if (!/^https?:$/.test(u.protocol)) throw 0; } catch { return `Informe um endereço http(s) válido em “${f.label}”.`; } }
  if (f.minLength && v.length < f.minLength) return `“${f.label}” precisa ter ao menos ${f.minLength} caracteres.`;
  if (f.maxLength && v.length > f.maxLength) return `“${f.label}” aceita no máximo ${f.maxLength} caracteres.`;
  if (f.type === 'number') {
    const n = Number(v);
    if (!Number.isFinite(n)) return `Informe um número em “${f.label}”.`;
    if (f.min !== undefined && n < Number(f.min)) return `“${f.label}” deve ser no mínimo ${f.min}.`;
    if (f.max !== undefined && n > Number(f.max)) return `“${f.label}” deve ser no máximo ${f.max}.`;
  }
  if (f.pattern && !new RegExp(`^(?:${f.pattern})$`).test(v)) return f.patternMessage || `Confira o formato de “${f.label}”.`;
  if (f.validate) { const m = f.validate(v); if (m) return m; }
  return '';
}

export function readValues(controls, fields) {
  const values = {};
  for (const f of fields) {
    const c = controls[f.name]; if (!c) continue;
    values[f.name] = f.type === 'checkbox' ? c.checked
      : f.type === 'number' ? (c.value === '' ? null : Number(c.value))
      : (f.trim === false ? c.value : c.value.trim()) || null;
  }
  return values;
}

/**
 * createForm({fields, values, submit, onSubmit, onCancel, cancel, class})
 * onSubmit(values) pode lançar Error: a mensagem aparece no resumo de erros (role=alert).
 */
export function createForm({ fields, values = {}, submit = 'Salvar', onSubmit, onCancel, cancel = 'Cancelar', danger = false, className = '' }) {
  const controls = {}, errors = {}, wrappers = {};
  const form = h('form', { class: `form stack ${className}`.trim(), novalidate: true });
  const summary = h('div', { class: 'form-errors', role: 'alert', tabindex: '-1', hidden: true });
  form.append(summary);

  for (const f of fields) {
    const id = uid(`f-${f.name}`);
    const hintId = f.help ? `${id}-hint` : null, liveId = f.liveHint ? `${id}-live` : null, errId = `${id}-err`;
    const describedBy = [hintId, liveId, errId].filter(Boolean);
    const control = buildControl(f, id, describedBy, values[f.name]);
    controls[f.name] = control;
    const err = h('p', { class: 'field-error', id: errId, hidden: true });
    errors[f.name] = err;
    const opt = f.required === false && f.type !== 'checkbox' ? h('span', { class: 'opt' }, ' (opcional)') : null;
    const live = f.liveHint ? h('p', { class: 'hint', id: liveId, 'aria-live': 'polite' }) : null;
    if (live) {
      const refresh = () => { live.textContent = f.liveHint(control.value) || ''; };
      control.addEventListener('input', refresh); control.addEventListener('change', refresh); refresh();
    }
    const wrap = f.type === 'checkbox'
      ? h('div', { class: 'field field-check', dataset: { name: f.name } },
          control, h('label', { for: id }, f.label), hintId ? h('p', { class: 'hint', id: hintId }, f.help) : null, err)
      : h('div', { class: 'field', dataset: { name: f.name } },
          h('label', { for: id }, f.label, opt),
          hintId ? h('p', { class: 'hint', id: hintId }, f.help) : null, f.type === 'password' ? passwordBox(control, f.label) : control, live, err);
    wrappers[f.name] = wrap;
    control.addEventListener('input', () => setFieldError(f.name, ''));
    if (f.type === 'date') {
      // Ano com no máximo 4 dígitos (dd/mm/aaaa) e conferência imediata ao sair do campo (data futura, antiga demais etc.).
      control.addEventListener('input', () => { const m = /^\+?(\d{4})\d+(-\d\d-\d\d)$/.exec(control.value); if (m) control.value = m[1] + m[2]; });
      control.addEventListener('blur', () => { if (control.value) { const msg = messageFor(control, f); setFieldError(f.name, msg); if (msg) announce(msg, { urgent: true }); } });
    }
    form.append(wrap);
  }

  function setFieldError(name, message) {
    const c = controls[name], e = errors[name];
    if (!c) return;
    e.textContent = message; e.hidden = !message;
    if (message) c.setAttribute('aria-invalid', 'true'); else c.removeAttribute('aria-invalid');
    wrappers[name].classList.toggle('has-error', !!message);
  }
  function showSummary(messages, links = []) {
    summary.replaceChildren(
      h('p', { class: 'form-errors-title' }, icon('alert', 18), messages.length > 1 || links.length ? ` Revise ${links.length || messages.length} ponto(s) antes de continuar:` : ' Não foi possível concluir:'),
      h('ul', null, messages.map((m, i) => h('li', null, links[i]
        ? h('a', { href: `#${controls[links[i]].id}`, onclick: (ev) => { ev.preventDefault(); controls[links[i]].focus(); } }, m) : m))));
    summary.hidden = false;
  }
  const clearSummary = () => { summary.hidden = true; summary.replaceChildren(); };
  function setError(message) { showSummary([message]); summary.focus(); announce(message, { urgent: true }); }

  function validate() {
    const bad = [];
    for (const f of fields) {
      if (wrappers[f.name].hidden) { setFieldError(f.name, ''); continue; }
      const m = messageFor(controls[f.name], f); setFieldError(f.name, m);
      if (m) bad.push([f.name, m]);
    }
    if (bad.length) { showSummary(bad.map(b => b[1]), bad.map(b => b[0])); summary.focus(); announce(`Há ${bad.length} campo(s) para corrigir.`, { urgent: true }); }
    else clearSummary();
    return bad.length === 0;
  }

  const submitButton = h('button', { type: 'submit', class: `btn ${danger ? 'danger' : 'primary'}` }, submit);
  const actions = h('div', { class: 'form-actions' }, submitButton,
    onCancel ? h('button', { type: 'button', class: 'btn ghost', onclick: onCancel }, cancel) : null);
  form.append(actions);

  let busy = false;
  form.addEventListener('submit', async (event) => {
    event.preventDefault();
    if (busy) return;
    if (!validate()) return;
    busy = true; submitButton.setAttribute('aria-disabled', 'true'); submitButton.classList.add('is-busy');
    const label = submitButton.textContent; submitButton.textContent = 'Enviando…';
    try { await onSubmit(readValues(controls, fields), api); clearSummary(); }
    catch (error) { if (!isAbort(error)) setError(errorText(error)); }
    finally { busy = false; submitButton.removeAttribute('aria-disabled'); submitButton.classList.remove('is-busy'); submitButton.textContent = label; }
  });

  const api = {
    el: form, controls, wrappers, fields, submitButton, setError, setFieldError, validate, clearSummary,
    values: () => readValues(controls, fields),
    show(name, visible) { wrappers[name].hidden = !visible; controls[name].disabled = !visible; },
    reset() { form.reset(); clearSummary(); }
  };
  return api;
}
