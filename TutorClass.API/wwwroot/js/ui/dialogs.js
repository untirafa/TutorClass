// Atalhos de janelas: formulário em janela e confirmação de ação.
import { openDialog } from './dialog.js';
import { createForm } from './form.js';
import { notice } from './toast.js';

export function formDialog(title, fields, values, onSave, { description, save = 'Salvar', size = 'md', keepOpen = false, danger = false, success } = {}) {
  const dlg = openDialog(title, { size, description });
  const form = createForm({
    fields, values, submit: save, danger, onCancel: () => dlg.close(),
    onSubmit: async (v) => {
      const result = await onSave(v, dlg, form);
      if (success) notice(typeof success === 'function' ? success(result) : success);
      if (!keepOpen) dlg.close();
    }
  });
  dlg.body.append(form.el);
  const first = form.el.querySelector('input,select,textarea');
  if (!fields.length) form.submitButton.focus(); else if (first && size !== 'lg') requestAnimationFrame(() => first.focus());
  return { dlg, form };
}

// Confirmação explícita para ações irreversíveis ou com efeito financeiro (3.3.4 Prevenção de erros).
export function confirmDialog(title, description, onConfirm, { confirm = 'Confirmar', danger = false, success = 'Alteração concluída.' } = {}) {
  return formDialog(title, [], {}, onConfirm, { description, save: confirm, danger, success });
}
