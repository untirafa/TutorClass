// Administração das perguntas frequentes (CRUD sobre /api/admin/faq).
import { h } from '../core/dom.js';
import { get, post, put, del } from '../core/api.js';
import { pageHeader, card, badge, btn, pagedList } from '../ui/components.js';
import { field, optional } from '../ui/form.js';
import { formDialog, confirmDialog } from '../ui/dialogs.js';
import { FAQ_CATEGORIES } from './help.js';

const CATS = FAQ_CATEGORIES.map(c => ({ value: c, label: c === 'Gravacoes' ? 'Gravações' : c }));
const FIELDS = [
  field('pergunta', 'Pergunta', 'text', { maxLength: 300 }), field('resposta', 'Resposta', 'textarea', { maxLength: 2000 }), field('categoria', 'Categoria', 'text', { options: CATS }),
  optional('palavrasChave', 'Palavras-chave', 'text', { help: 'Separe por vírgulas. Ajuda a busca e o assistente.' }),
  field('ordemExibicao', 'Ordem de exibição', 'number', { min: 0, step: 1, value: 0 }), optional('textoAcao', 'Texto do atalho', 'text'), optional('rotaAcao', 'Rota do atalho', 'text', { help: 'Ex.: app/planos' })
];
const deactivated = new Map(); // a listagem pública só traz ativas: guardamos as desativadas nesta sessão para permitir reativar

export default async function adminFaq(ctx) {
  const pub = await get('/api/faq', { auth: false, signal: ctx.signal }).catch(() => get('/api/faq'));
  const reload = () => ctx.router.refresh();
  const rows = [...pub.map(f => ({ ...f, ativo: true })), ...[...deactivated.values()].filter(d => !pub.some(p => p.id === d.id))];
  const save = (f) => async (v) => { const body = { ...v, ordemExibicao: v.ordemExibicao ?? 0 }; if (f) await put(`/api/admin/faq/${f.id}`, { ...body, ativo: f.ativo }); else await post('/api/admin/faq', body); reload(); };
  ctx.host.append(pageHeader('Perguntas frequentes', 'Gerencie o conteúdo da central de ajuda e do assistente.', btn('Nova pergunta', () => formDialog('Nova pergunta', FIELDS, {}, save(null), { success: 'Pergunta criada.' }), { kind: 'primary' })),
    h('p', { class: 'muted small' }, 'Perguntas desativadas deixam de aparecer no site. Nesta sessão elas ficam listadas aqui para você poder reativá-las.'),
    pagedList(rows.sort((a, b) => (a.ordemExibicao ?? 0) - (b.ordemExibicao ?? 0)), (f) => card({ title: f.pergunta, subtitle: f.resposta }, h('p', null, badge(f.categoria === 'Gravacoes' ? 'Gravações' : f.categoria, 'neutral'), ' ', badge(f.ativo ? 'Ativa' : 'Desativada', f.ativo ? 'ok' : 'warn')),
      h('div', { class: 'actions' }, btn('Editar', () => formDialog('Editar pergunta', FIELDS, f, save(f), { success: 'Pergunta atualizada.' })),
        btn(f.ativo ? 'Desativar' : 'Ativar', async () => {
          const r = await post(`/api/admin/faq/${f.id}/${f.ativo ? 'desativar' : 'ativar'}`);
          if (f.ativo) deactivated.set(f.id, { ...f, ...r, ativo: false }); else deactivated.delete(f.id);
          reload();
        }, { kind: f.ativo ? 'danger' : 'primary' }),
        btn('Apagar', () => confirmDialog('Apagar pergunta?',
          `“${f.pergunta}” será removida definitivamente, inclusive da central de ajuda e do assistente. Essa ação não pode ser desfeita.`,
          async () => { await del(`/api/admin/faq/${f.id}`); deactivated.delete(f.id); reload(); },
          { confirm: 'Apagar definitivamente', danger: true, success: 'Pergunta apagada.' }), { kind: 'danger' }))), { empty: 'Nenhuma pergunta cadastrada.', label: 'perguntas' }));
}
