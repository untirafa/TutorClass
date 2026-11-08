// Despachante da área interna: escolhe o módulo pelo perfil e entrega um contêiner com cabeçalho próprio.
import { h, mount } from '../core/dom.js';
import { role, isMinorStudent, isStudent } from '../core/session.js';
import { notice } from '../ui/toast.js';
import { findModule } from '../modules.js';
import { loading, errorState } from '../ui/components.js';
import { planAllows, upgradeNotice } from '../core/plan.js';
import { errorText, isAbort } from '../core/api.js';

export default async function appHost(ctx) {
  const id = ctx.params.modulo || 'inicio';
  const module = findModule(id, role());
  if (!module) { ctx.router.go('app/inicio'); return; }
  // Menores de 18 anos não acessam o financeiro: aviso no topo e retorno à tela inicial.
  if (module.id === 'financeiro' && isMinorStudent()) {
    notice('Alunos menores de 18 anos não têm acesso à área financeira. Peça ao seu responsável para cuidar dos pagamentos.', { kind: 'warning', title: 'Área restrita' });
    ctx.router.go('app/inicio');
    return;
  }
  // Plano Gratuito: convite amigável para conhecer o plano que inclui o recurso.
  if (isStudent() && !(await planAllows(module.id))) {
    if (!ctx.isCurrent()) return;
    upgradeNotice(module.id);
    ctx.router.go('app/planos');
    return;
  }
  ctx.setTitle(module.label);
  const box = h('div', { class: 'container stack' });
  // append tolerante: ignora null/false (condicionais nas telas) em vez de escrever "null" na página
  const nativeAppend = box.append.bind(box);
  box.append = (...items) => nativeAppend(...items.flat(Infinity).filter(x => x != null && x !== false));
  mount(ctx.host, box);
  box.append(loading());
  const view = await module.load();
  const inner = { ...ctx, host: box, module, params: { ...ctx.params, id: ctx.params.id }, reload: () => ctx.router.refresh() };
  try {
    box.replaceChildren();
    await view(inner);
  } catch (error) {
    if (isAbort(error) || !ctx.isCurrent()) return;
    box.replaceChildren(errorState(errorText(error), ctx.router.refresh));
  }
}
