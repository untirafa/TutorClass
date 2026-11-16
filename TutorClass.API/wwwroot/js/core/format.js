// Formatação e conversão de datas/valores. A API trabalha em UTC; o usuário vê Brasília.
const ZONE = 'America/Sao_Paulo';
const BRL = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

export const money = (n) => n == null || Number.isNaN(Number(n)) ? 'Não informado' : BRL.format(n);
export const number = (n) => new Intl.NumberFormat('pt-BR').format(n ?? 0);

export function when(value, style = 'short') {
  if (!value) return '—';
  const d = new Date(value);
  if (!Number.isFinite(d.getTime())) return '—';
  return new Intl.DateTimeFormat('pt-BR', style === 'long'
    ? { dateStyle: 'full', timeStyle: 'short', timeZone: ZONE }
    : { dateStyle: 'short', timeStyle: 'short', timeZone: ZONE }).format(d);
}
export function dayOnly(value) {
  if (!value) return '—';
  const d = new Date(/^\d{4}-\d{2}-\d{2}$/.test(value) ? value + 'T12:00:00Z' : value);
  return Number.isFinite(d.getTime()) ? new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeZone: ZONE }).format(d) : '—';
}
export const hhmm = (t) => String(t ?? '').slice(0, 5);

export const timeWithSeconds = (value) => (value.length === 5 ? value + ':00' : value);
export const DAYS = [
  ['Sunday', 'Domingo'], ['Monday', 'Segunda-feira'], ['Tuesday', 'Terça-feira'], ['Wednesday', 'Quarta-feira'],
  ['Thursday', 'Quinta-feira'], ['Friday', 'Sexta-feira'], ['Saturday', 'Sábado']
];
export const dayLabel = (v) => DAYS.find(([k]) => k === v)?.[1] || v;

export function initials(name) {
  return String(name || '?').split(/\s+/).filter(Boolean).slice(0, 2).map(s => s[0].toUpperCase()).join('') || '?';
}
export function estimateTotal(pricePerHour, minutes) {
  if (pricePerHour == null || !minutes) return null;
  return Math.round(Number(pricePerHour) * (Number(minutes) / 60) * 100) / 100;
}
export function durationLabel(seconds) {
  const m = Math.round((seconds || 0) / 60);
  return m >= 60 ? `${Math.floor(m / 60)} h${m % 60 ? ` ${m % 60} min` : ''}` : `${m} min`;
}
export const STATUS_LABEL = {
  Solicitada: 'Solicitada', Confirmada: 'Confirmada', Concluida: 'Concluída', Cancelada: 'Cancelada',
  Pendente: 'Pendente', EmAnalise: 'Em análise', AguardandoDocumentacao: 'Aguardando documentação',
  Aprovada: 'Aprovada', Recusada: 'Recusada', Reprovada: 'Reprovada', Verificada: 'Verificada', Rejeitada: 'Rejeitada',
  Concluido: 'Concluído', Pago: 'Pago', Cancelado: 'Cancelado', Reembolsado: 'Reembolsado',
  Candidato: 'Candidato', Aprovado: 'Aprovado', Reprovado: 'Reprovado', Suspenso: 'Suspenso',
  Aberta: 'Aberta', Procedente: 'Procedente', Improcedente: 'Improcedente'
};
export const statusLabel = (s) => STATUS_LABEL[s] || s || '—';
export const statusTone = (s) => ({
  Confirmada: 'ok', Concluida: 'ok', Aprovada: 'ok', Verificada: 'ok', Pago: 'ok', Concluido: 'ok', Aprovado: 'ok',
  Cancelada: 'bad', Recusada: 'bad', Reprovada: 'bad', Rejeitada: 'bad', Cancelado: 'bad', Reprovado: 'bad', Suspenso: 'bad',
  Aberta: 'warn', Procedente: 'ok', Solicitada: 'warn', Pendente: 'warn', EmAnalise: 'warn', AguardandoDocumentacao: 'warn', Candidato: 'warn'
}[s] || 'neutral');
export const paymentLabel = (s) => ({ Cartao: 'Cartão', Pix: 'Pix' }[s] || s);
export const typeLabel = (s) => ({
  Graduacao: 'Graduação', PosGraduacao: 'Pós-graduação', Curso: 'Curso', Certificacao: 'Certificação',
  Experiencia: 'Experiência', Outro: 'Outro', Exercicio: 'Atividade / exercício', Video: 'Vídeo', Documento: 'Documento'
}[s] || s);

/* ---------- Horário de Brasília (UTC-3, sem horário de verão desde 2019) ----------
   A API guarda disponibilidades e aulas em UTC; a pessoa vê e digita sempre o horário de Brasília. */
export const BRT_OFFSET_MIN = 180;
export const WEEK = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
const pad2 = (n) => String(n).padStart(2, '0');
// "HH:MM[:SS]" -> minutos desde 00:00; "23:59:59" vale como fim do dia (1440).
export function timeToMin(t) {
  const m = /^(\d{1,2}):(\d{2})(?::(\d{2}))?/.exec(String(t ?? '')); if (!m) return NaN;
  const min = Number(m[1]) * 60 + Number(m[2]);
  return min === 1439 && Number(m[3] || 0) >= 59 ? 1440 : min;
}
export const minToTime = (min) => `${pad2(Math.floor(min / 60))}:${pad2(min % 60)}`;
const dayAt = (day, shift) => WEEK[(((WEEK.indexOf(day) + shift) % 7) + 7) % 7];

// Intervalo semanal digitado em Brasília -> 1 ou 2 registros em UTC (2 quando cruza a meia-noite UTC, às 21h de Brasília).
export function brasiliaToUtcSlots(day, inicio, fim) {
  const a = timeToMin(inicio) + BRT_OFFSET_MIN, b = timeToMin(fim) + BRT_OFFSET_MIN;
  const slot = (d, x, y) => ({ diaSemana: d, inicio: `${minToTime(x)}:00`, fim: y >= 1440 ? '23:59:59' : `${minToTime(y)}:00` });
  if (b <= 1440) return [slot(day, a, b)];
  if (a >= 1440) return [slot(dayAt(day, 1), a - 1440, b - 1440)];
  return [slot(day, a, 1440), slot(dayAt(day, 1), 0, b - 1440)];
}
// Registro semanal em UTC -> pedaços em Brasília ({ dia, ini, fim } em minutos).
export function utcToBrasiliaPieces(row) {
  const a = timeToMin(row.inicio) - BRT_OFFSET_MIN, b = timeToMin(row.fim) - BRT_OFFSET_MIN;
  if (b <= 0) return [{ dia: dayAt(row.diaSemana, -1), ini: a + 1440, fim: b + 1440 }];
  if (a >= 0) return [{ dia: row.diaSemana, ini: a, fim: b }];
  return [{ dia: dayAt(row.diaSemana, -1), ini: a + 1440, fim: 1440 }, { dia: row.diaSemana, ini: 0, fim: b }];
}
// Disponibilidades da API -> grupos em Brasília, já juntando os pedaços que a divisão em UTC separou.
export function brasiliaAvailability(rows) {
  const pieces = [];
  for (const row of rows || []) for (const p of utcToBrasiliaPieces(row)) pieces.push({ ...p, rows: [row] });
  pieces.sort((x, y) => WEEK.indexOf(x.dia) - WEEK.indexOf(y.dia) || x.ini - y.ini);
  const groups = [];
  for (const p of pieces) {
    const last = groups[groups.length - 1];
    if (last && last.dia === p.dia && last.fim === p.ini) { last.fim = p.fim; last.rows.push(...p.rows); }
    else groups.push({ ...p, rows: [...p.rows] });
  }
  return groups.map(g => ({ dia: g.dia, inicio: minToTime(g.ini), fim: minToTime(g.fim), rows: [...new Set(g.rows)] }));
}
export const brasiliaSlotText = (g) => `${dayLabel(g.dia)}: ${g.inicio} às ${g.fim} (horário de Brasília)`;

// Data (AAAA-MM-DD) e hora de Brasília -> ISO em UTC. Recusa horários passados.
export function brasiliaInputToIso(date, time, now = new Date()) {
  if (!date || !time) throw new Error('Informe a data e o horário da aula.');
  const d = new Date(`${date}T${time.slice(0, 5)}:00-03:00`);
  if (!Number.isFinite(d.getTime())) throw new Error('Data ou horário inválido.');
  if (d <= now) throw new Error('Escolha um horário futuro.');
  return d.toISOString();
}
// ISO em UTC -> { date, time } em Brasília (para preencher formulários).
export function isoToBrasiliaParts(iso) {
  const d = new Date(new Date(iso).getTime() - BRT_OFFSET_MIN * 60000);
  if (!Number.isFinite(d.getTime())) return { date: '', time: '' };
  const s = d.toISOString(); return { date: s.slice(0, 10), time: s.slice(11, 16) };
}
// Data de hoje em Brasília (AAAA-MM-DD).
export const todayBrasilia = (now = new Date()) => new Date(now.getTime() - BRT_OFFSET_MIN * 60000).toISOString().slice(0, 10);
