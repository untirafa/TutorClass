import { h } from '../core/dom.js';
import { get } from '../core/api.js';
import { when, durationLabel } from '../core/format.js';
import { pageHeader, card, dl, externalLink, btn, emptyState, pagedList } from '../ui/components.js';
import { openDialog } from '../ui/dialog.js';

export function showRecording(r) {
  const d = openDialog(r.titulo);
  d.body.append(h('p', r.descricao || 'Gravação demonstrativa vinculada à sua aula.'),
    dl({ Professor: r.professor, Matéria: r.materia, Aula: when(r.aulaInicio), Duração: durationLabel(r.duracaoSegundos) }),
    r.disponivel ? externalLink('Assistir à gravação (link externo)', r.urlVideo) : h('p', { class: 'muted' }, 'Gravação indisponível.'));
}
export default async function recordings(ctx) {
  const rows = await get('/api/gravacoes/minhas', { signal: ctx.signal });
  ctx.host.append(pageHeader('Gravações', 'As gravações desta versão são mídias demonstrativas. O acesso depende do plano Mentoria e da elegibilidade das suas próprias aulas.'),
    pagedList(rows, (r) => card({ title: r.titulo, subtitle: r.descricao }, h('p', { class: 'small' }, `${r.materia} · ${r.professor}`),
      h('p', { class: 'small muted' }, `Aula: ${when(r.aulaInicio)} · ${durationLabel(r.duracaoSegundos)}`),
      r.disponivel ? h('div', { class: 'actions' }, btn('Abrir gravação', () => showRecording(r))) : h('p', { class: 'small muted' }, 'Ainda indisponível.')),
      { empty: 'Nenhuma gravação elegível disponível.', label: 'gravações' }));
}
