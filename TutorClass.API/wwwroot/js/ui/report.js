// Denúncias: botão "Denunciar" e janela com motivo e detalhes (POST /api/denuncias).
import { h } from '../core/dom.js';
import { post } from '../core/api.js';
import { isSignedIn, isAdmin } from '../core/session.js';
import { field, optional } from './form.js';
import { formDialog } from './dialogs.js';
import { btn } from './components.js';

export const REPORT_REASONS = [
  { value: 'ConteudoOfensivo', label: 'Conteúdo ofensivo ou desrespeitoso' },
  { value: 'AssedioOuAmeaca', label: 'Assédio ou ameaça' },
  { value: 'Discriminacao', label: 'Discriminação' },
  { value: 'PerfilFalsoOuGolpe', label: 'Perfil falso ou tentativa de golpe' },
  { value: 'DadosPessoais', label: 'Exposição de dados pessoais' },
  { value: 'Outro', label: 'Outro motivo' }
];
export const REASON_LABEL = Object.fromEntries(REPORT_REASONS.map(r => [r.value, r.label]));
export const TARGET_LABEL = { Professor: 'Perfil de professor', Avaliacao: 'Avaliação', Mensagem: 'Mensagem da Mentoria' };

// Quem está logado (exceto a administração, que decide as denúncias) pode denunciar.
export const canReport = () => isSignedIn() && !isAdmin();

export function openReport({ tipo, alvoId, vinculoMentoriaId = null, alvoNome = 'este conteúdo' }) {
  const { dlg } = formDialog('Denunciar', [
    field('motivo', 'Motivo', 'text', { options: REPORT_REASONS }),
    optional('descricao', 'Detalhes (opcional)', 'textarea', { maxLength: 500, rows: 4, help: 'Conte o que aconteceu. Para o motivo “Outro”, descreva com pelo menos 10 caracteres.' })
  ], {}, async (v) => {
    if (v.motivo === 'Outro' && (v.descricao || '').length < 10) throw new Error('Para o motivo “Outro”, descreva o ocorrido com pelo menos 10 caracteres.');
    await post('/api/denuncias', { tipo, alvoId: String(alvoId), motivo: v.motivo, descricao: v.descricao, vinculoMentoriaId });
  }, {
    description: `Você está denunciando ${alvoNome}. Sua identidade não é revelada à pessoa denunciada.`,
    save: 'Enviar denúncia', success: 'Denúncia enviada. Nossa equipe vai analisar; acompanhe em “Minhas denúncias”.'
  });
  dlg.body.append(h('p', { class: 'small muted' }, 'Dúvidas sobre o que pode ser denunciado? Leia as ',
    h('a', { href: '#regras-de-convivencia', target: '_blank', rel: 'noopener' }, 'Regras de convivência'), ' (abrem em outra aba).'));
  return dlg;
}

export function reportButton(target, label = 'Denunciar') {
  if (!canReport()) return null;
  return btn(label, () => openReport(target), { kind: 'ghost', iconName: 'flag', ariaLabel: `Denunciar ${target.alvoNome || 'este conteúdo'}` });
}
