// Busca de professores (filtros em gaveta no celular / barra lateral no desktop), perfil em abas e solicitação de aula.
import { h, mount, icon } from '../core/dom.js';
import { publicGet, get, post, q, subjects, errorText, isAbort } from '../core/api.js';
import { session, isSignedIn, isGuardian, isStudent } from '../core/session.js';
import { money, hhmm, brasiliaHint, utcInputToIso, estimateTotal, dayOnly } from '../core/format.js';
import { announce } from '../core/a11y.js';
import { pageHeader, pagedList, loading, errorState, emptyState, tabs, stars, badge, avatar, dl, section, linkButton, link } from '../ui/components.js';
import { createForm, field } from '../ui/form.js';
import { openDialog } from '../ui/dialog.js';
import { notice } from '../ui/toast.js';
import { reportButton } from '../ui/report.js';
import { rateLesson } from './lessons.js';
import { teacherCard, reviewCard, qualificationCard, weeklySlotText, subjectOptions, inApp } from './shared.js';

/* ---------- Busca (barra de pesquisa + gaveta de filtros no topo) ---------- */
async function renderSearch(ctx) {
  const wrap = h('div', { class: 'stack' });
  const subs = await subjects();
  const fields = [
    field('materiaId', 'Matéria', 'text', { required: false, options: subjectOptions(subs, 'Todas as matérias') }),
    field('notaMinima', 'Nota mínima', 'text', { required: false, options: [{ value: '', label: 'Qualquer nota' }, { value: '4', label: '4 ou mais' }, { value: '3', label: '3 ou mais' }] }),
    field('precoMax', 'Preço máximo por hora (R$)', 'number', { required: false, min: 0, step: 1 }),
    field('mentoria', 'Somente quem oferece mentoria', 'checkbox'),
    field('ordem', 'Ordenar por', 'text', { required: false, options: [{ value: 'relevancia', label: 'Relevância' }, { value: 'nota', label: 'Melhor avaliação' }, { value: 'preco-asc', label: 'Menor preço' }, { value: 'preco-desc', label: 'Maior preço' }, { value: 'nome', label: 'Nome (A–Z)' }] })
  ];
  let all = [];
  let current = { materiaId: ctx.query.materia || '', ordem: 'relevancia' };
  let term = '';
  const norm = (t) => String(t || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  const results = h('div', { 'aria-live': 'polite' });
  const count = h('p', { class: 'muted', role: 'status' });

  const apply = (v) => {
    let rows = all.slice();
    if (term) { const t = norm(term); rows = rows.filter(p => [p.nome, p.biografia, (p.materias || []).join(' ')].some(x => norm(x).includes(t))); }
    if (v.notaMinima) rows = rows.filter(p => (p.mediaAvaliacoes ?? 0) >= Number(v.notaMinima));
    if (v.precoMax != null) rows = rows.filter(p => (p.precoHora ?? Infinity) <= v.precoMax);
    if (v.mentoria) rows = rows.filter(p => p.ofereceMentoria);
    const sorters = {
      nota: (a, b) => (b.mediaAvaliacoes ?? 0) - (a.mediaAvaliacoes ?? 0), 'preco-asc': (a, b) => (a.precoHora ?? 0) - (b.precoHora ?? 0),
      'preco-desc': (a, b) => (b.precoHora ?? 0) - (a.precoHora ?? 0), nome: (a, b) => a.nome.localeCompare(b.nome, 'pt-BR')
    };
    if (sorters[v.ordem]) rows.sort(sorters[v.ordem]);
    count.textContent = `${rows.length} professor(es) encontrado(s).`;
    mount(results, rows.length ? pagedList(rows, teacherCard, { size: 9, label: 'professores' })
      : emptyState('Nenhum professor encontrado com esses filtros. Tente ampliar a busca: remova o preço máximo ou a nota mínima.',
          h('button', { type: 'button', class: 'btn secondary', onclick: () => { searchInput.value = ''; term = ''; form.api.reset(); } }, 'Limpar filtros')));
  };
  const load = async (v) => {
    mount(results, loading('Buscando professores…'));
    try {
      all = await publicGet('/api/professores' + q({ materiaId: v.materiaId }), { signal: ctx.signal });
      apply(v);
      announce(`${all.length} professor(es) para a busca.`);
    } catch (e) { if (!isAbort(e)) mount(results, errorState(errorText(e), () => load(v))); }
  };

  const form = {};
  form.api = createForm({
    fields, values: { materiaId: current.materiaId, ordem: 'relevancia' }, submit: 'Aplicar filtros',
    onSubmit: async (v) => { const needsFetch = (v.materiaId || '') !== (current.materiaId || ''); current = v; if (needsFetch) await load(v); else apply(v); }
  });
  const reset = form.api.reset;
  form.api.reset = () => { reset(); form.api.controls.materiaId.value = ''; form.api.controls.ordem.value = 'relevancia'; current = { materiaId: '', ordem: 'relevancia' }; load(current); };
  form.api.el.querySelector('.form-actions').append(h('button', { type: 'button', class: 'btn ghost', onclick: () => form.api.reset() }, 'Limpar'));

  // Topo: barra de pesquisa + botão "Filtrar e ordenar", que abre uma gaveta logo abaixo. Os professores ficam embaixo.
  const panelId = 'filters-panel';
  const searchInput = h('input', { type: 'search', id: 'teacher-search', name: 'busca', autocomplete: 'off', placeholder: 'Pesquise por nome, matéria ou assunto…', 'aria-label': 'Pesquisar professores por nome, matéria ou assunto' });
  const toggle = h('button', { type: 'button', class: 'btn secondary filter-toggle', 'aria-expanded': 'false', 'aria-controls': panelId }, icon('filter', 18), 'Filtrar e ordenar');
  const searchForm = h('form', { class: 'search-bar', role: 'search', onsubmit: (e) => { e.preventDefault(); term = searchInput.value.trim(); apply(current); announce(`Busca por “${term || 'todos'}”.`); } },
    h('div', { class: 'search-input' }, icon('search', 20), searchInput),
    h('button', { type: 'submit', class: 'btn primary' }, 'Pesquisar'), toggle);
  searchInput.addEventListener('input', () => { term = searchInput.value.trim(); apply(current); });
  const panel = h('section', { class: 'filters drawer-top', id: panelId, 'aria-label': 'Filtros e ordenação', hidden: true }, form.api.el);
  const setOpen = (open) => { toggle.setAttribute('aria-expanded', String(open)); panel.hidden = !open; if (open) form.api.controls.materiaId.focus(); };
  toggle.addEventListener('click', () => setOpen(toggle.getAttribute('aria-expanded') !== 'true'));
  panel.addEventListener('keydown', (e) => { if (e.key === 'Escape') { setOpen(false); toggle.focus(); } });
  form.api.el.addEventListener('submit', () => setTimeout(() => setOpen(false), 0));

  wrap.append(searchForm, panel, h('div', { class: 'results-bar' }, count), results);
  mount(ctx.host, wrap);
  await load(current);
  return wrap;
}

export async function publicSearch(ctx) {
  const box = h('div', { class: 'container stack' }, pageHeader('Buscar professores', 'Compare professores por matéria, avaliação e preço por hora. O valor total da aula é sempre mostrado antes de você solicitar.'));
  const body = h('div'); box.append(body);
  mount(ctx.host, box);
  await renderSearch({ ...ctx, host: body });
}
export async function searchModule(ctx) {
  ctx.host.append(pageHeader('Encontrar professores', isGuardian() ? 'Escolha um professor e solicite aulas para o aluno que você acompanha.' : 'Escolha um professor, consulte horários e solicite sua aula.'));
  const body = h('div'); ctx.host.append(body);
  await renderSearch({ ...ctx, host: body });
}

/* ---------- Horários livres ---------- */
function slotFinder(professorId, onPick) {
  const list = h('ul', { class: 'slot-list', 'aria-live': 'polite' });
  const form = createForm({
    fields: [field('data', 'Data para consultar (UTC)', 'date', { help: 'Os horários aparecem em UTC e em Brasília.' })],
    submit: 'Consultar horários',
    onSubmit: async (v) => {
      const rows = await publicGet(`/api/professores/${professorId}/horarios-livres` + q({ data: v.data }));
      list.replaceChildren();
      if (!rows.length) { list.append(h('li', { class: 'muted' }, 'Nenhum intervalo livre nesta data.')); announce('Nenhum horário livre nesta data.'); return; }
      rows.forEach(s => {
        const b = h('button', { type: 'button', class: 'btn secondary', 'aria-pressed': 'false' },
          `${hhmm(s.inicio)}–${hhmm(s.fim)} UTC`, h('span', { class: 'small muted' }, ` ${brasiliaHint(`${s.data}T${hhmm(s.inicio)}`).replace(/^\d+\/\d+\/\d+ /, '')}`));
        b.addEventListener('click', () => {
          list.querySelectorAll('button').forEach(x => x.setAttribute('aria-pressed', 'false'));
          b.setAttribute('aria-pressed', 'true'); onPick?.(`${s.data}T${hhmm(s.inicio)}`, s); announce(`Horário ${hhmm(s.inicio)} UTC selecionado.`);
        });
        list.append(h('li', null, b));
      });
      announce(`${rows.length} horário(s) livre(s).`);
    }
  });
  return h('div', { class: 'stack' }, form.el, list);
}

/* ---------- Solicitação de aula ---------- */
export async function openBooking(profile) {
  if (!isSignedIn()) { session.returnTo = location.hash; location.hash = 'login'; announce('Entre na sua conta para solicitar a aula. Você voltará a esta página.'); return; }
  if (!isStudent() && !isGuardian()) throw new Error('Somente alunos e responsáveis solicitam aulas.');
  const subs = (await subjects()).filter(m => (profile.materias || []).includes(m.nome));
  if (!subs.length) throw new Error('Este professor ainda não possui matérias disponíveis.');
  let dependents = [];
  if (isGuardian()) {
    dependents = await get('/api/responsavel/alunos');
    if (!dependents.length) throw new Error('Nenhum aluno está vinculado à sua conta. Peça a vinculação à administração antes de solicitar aulas.');
  }
  const dlg = openDialog(`Solicitar aula com ${profile.nome}`, { description: `Valor por hora: ${money(profile.precoHora)}. O total é calculado pela duração. Não há custos ocultos.` });
  const fields = [
    ...(isGuardian() ? [field('alunoId', 'Aluno que terá a aula', 'text', { options: dependents.map(d => ({ value: d.id, label: d.nome })) })] : []),
    field('materiaId', 'Matéria', 'text', { options: subs.map(m => ({ value: m.id, label: m.nome })) }),
    field('inicio', 'Início da aula (UTC)', 'datetime-local', { help: 'Informe em UTC (15:00 UTC = 12:00 em Brasília) ou escolha um horário livre acima.', liveHint: brasiliaHint }),
    field('duracaoMinutos', 'Duração em minutos', 'number', { min: 30, max: 240, step: 1, value: 60, liveHint: (v) => { const t = estimateTotal(profile.precoHora, Number(v)); return t == null ? '' : `Total estimado: ${money(t)}`; } })
  ];
  const form = createForm({
    fields, submit: 'Solicitar aula', onCancel: () => dlg.close(),
    onSubmit: async (v) => {
      const body = { professorId: profile.id, materiaId: v.materiaId, inicio: utcInputToIso(v.inicio), duracaoMinutos: v.duracaoMinutos };
      if (isGuardian()) await post(`/api/responsavel/alunos/${encodeURIComponent(v.alunoId)}/aulas`, body);
      else await post('/api/aulas', body);
      dlg.close(); notice('Solicitação enviada. Aguarde a confirmação do professor.');
      location.hash = isGuardian() ? `app/dependente/${v.alunoId}` : 'app/aulas';
    }
  });
  dlg.body.append(h('div', { class: 'stack' }, h('h3', 'Horários livres'), slotFinder(profile.id, (iso) => {
    form.controls.inicio.value = iso; form.controls.inicio.dispatchEvent(new Event('input', { bubbles: true }));
  })), h('hr'), form.el);
  return dlg;
}

/* ---------- Perfil ---------- */
async function renderProfile(ctx) {
  const id = ctx.params.id;
  const [profile, reviews] = await Promise.all([
    publicGet(`/api/professores/${encodeURIComponent(id)}`, { signal: ctx.signal }),
    publicGet(`/api/professores/${encodeURIComponent(id)}/avaliacoes`, { signal: ctx.signal }).catch(() => [])
  ]);
  ctx.setTitle(profile.nome);
  const back = link('← Voltar à busca', inApp() ? '#app/professores' : '#professores');
  const head = h('header', { class: 'profile-head' }, avatar(profile.nome),
    h('div', null, h('h1', { tabindex: '-1' }, profile.nome),
      h('p', null, badge(`Reputação: ${profile.nivelReputacao}`, 'neutral'), ' ',
        profile.quantidadeAvaliacoes > 0 ? [stars(profile.mediaAvaliacoes, profile.quantidadeAvaliacoes), ` ${Number(profile.mediaAvaliacoes).toFixed(1).replace('.', ',')} (${profile.quantidadeAvaliacoes})`] : 'Ainda sem avaliações',
        profile.ofereceMentoria ? [' ', badge('Oferece mentoria', 'ok')] : null),
      profile.id !== session.user?.id ? reportButton({ tipo: 'Professor', alvoId: profile.id, alvoNome: `o perfil de ${profile.nome}` }, 'Denunciar perfil') : null));

  const sobre = () => h('div', { class: 'stack' }, h('p', { style: 'white-space: pre-wrap' }, profile.biografia || 'Este professor ainda não escreveu uma apresentação.'),
    dl({ 'Preço por hora': money(profile.precoHora), 'Aulas concluídas': profile.aulasConcluidas, 'Oferece mentoria': profile.ofereceMentoria ? `Sim (até ${profile.limiteMentorados} mentorados)` : 'Não', 'Avaliações': profile.textoAvaliacoes }),
    h('h2', 'Qualificações'), (profile.qualificacoes || []).length ? h('div', { class: 'grid cards' }, profile.qualificacoes.map(qualificationCard)) : emptyState('Nenhuma qualificação pública disponível.'));
  const materias = () => (profile.materias || []).length
    ? h('ul', null, profile.materias.map(m => h('li', m))) : emptyState('Nenhuma matéria vinculada ainda.');
  // Aluno que já concluiu aula com este professor avalia direto pelo perfil.
  const rateBox = () => {
    const box = h('div', { class: 'panel stack-sm' });
    if (!isStudent()) return null;
    box.append(h('p', { class: 'muted' }, 'Verificando suas aulas…'));
    get('/api/aulas/minhas').then((all) => {
      const done = all.filter(l => l.status === 'Concluida' && (l.professorId === profile.id || l.professor === profile.nome));
      box.replaceChildren(h('h2', 'Avalie este professor'), ...(done.length
        ? [h('p', null, `Você concluiu ${done.length} aula(s) com ${profile.nome}. Conte como foi.`),
          h('div', { class: 'actions' }, done.map(l => h('button', { type: 'button', class: 'btn primary', onclick: () => rateLesson(l.id) }, `Avaliar aula de ${dayOnly(l.inicio)}`)))]
        : [h('p', { class: 'muted' }, 'Você poderá avaliar este professor depois de concluir uma aula com ele.')]));
    }).catch(() => box.replaceChildren());
    return box;
  };
  const avaliacoes = () => h('div', { class: 'stack' }, rateBox(), reviews.length ? h('div', { class: 'grid cards' }, reviews.map(r => reviewCard(r, r.alunoId !== session.user?.id ? reportButton({ tipo: 'Avaliacao', alvoId: r.id, alvoNome: `a avaliação de ${r.aluno || 'aluno'}` }, 'Denunciar avaliação') : null))) : emptyState('Nenhuma avaliação publicada.'));
  const agenda = () => h('div', { class: 'stack' },
    h('h2', 'Disponibilidade semanal'),
    (profile.disponibilidades || []).length ? h('ul', null, profile.disponibilidades.map(d => h('li', weeklySlotText(d)))) : emptyState('O professor ainda não cadastrou horários.'),
    h('h2', 'Horários livres em uma data'), slotFinder(profile.id));

  const bookable = isStudent() || isGuardian() || !isSignedIn();
  const bookButton = h('button', { type: 'button', class: 'btn primary block', onclick: async (e) => { try { await openBooking(profile); } catch (err) { if (!isAbort(err)) notice(errorText(err), { error: true }); } } },
    isSignedIn() ? 'Solicitar aula' : 'Entrar para solicitar aula');
  const aside = h('aside', { class: 'profile-aside', 'aria-label': 'Solicitar aula' }, h('div', { class: 'book-bar' },
    h('div', null, h('p', { class: 'small muted' }, 'A partir de'), h('p', { class: 'price' }, money(profile.precoHora), h('small', ' / hora'))),
    bookable ? bookButton : h('p', { class: 'small muted' }, 'Somente alunos e responsáveis solicitam aulas.')));
  document.body.classList.toggle('has-bookbar', bookable);

  mount(ctx.host, h('div', { class: 'stack' }, back, h('div', { class: 'profile-layout' },
    h('div', { class: 'stack' }, head, tabs([
      { label: 'Sobre', render: sobre }, { label: 'Matérias', render: materias },
      { label: `Avaliações (${reviews.length})`, render: avaliacoes }, { label: 'Agenda', render: agenda }], { label: `Perfil de ${profile.nome}` })),
    aside)));
}
export async function publicProfile(ctx) {
  const box = h('div', { class: 'container' }); mount(ctx.host, box);
  await renderProfile({ ...ctx, host: box });
}
export async function profileModule(ctx) { await renderProfile(ctx); }
