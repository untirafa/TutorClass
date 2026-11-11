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

// "2026-11-10T15:00" (digitado como UTC) -> ISO UTC. Recusa horários passados.
export function utcInputToIso(value, now = new Date()) {
  if (!value) throw new Error('Informe a data e o horário da aula.');
  const d = new Date(value + 'Z');
  if (!Number.isFinite(d.getTime())) throw new Error('Data ou horário inválido.');
  if (d <= now) throw new Error('Escolha um horário futuro.');
  return d.toISOString();
}
export const isoToUtcInput = (iso) => String(iso ?? '').replace(/Z$/, '').slice(0, 16);

// Equivalente em Brasília para uma dica visual ("15:00 UTC = 12:00 em Brasília").
export function brasiliaHint(utcDateTimeLocal) {
  if (!utcDateTimeLocal) return '';
  const d = new Date(utcDateTimeLocal.length === 5 ? `1970-01-01T${utcDateTimeLocal}:00Z` : utcDateTimeLocal + 'Z');
  if (!Number.isFinite(d.getTime())) return '';
  const opts = utcDateTimeLocal.length === 5
    ? { timeStyle: 'short', timeZone: ZONE }
    : { dateStyle: 'short', timeStyle: 'short', timeZone: ZONE };
  return `${new Intl.DateTimeFormat('pt-BR', opts).format(d)} em Brasília`;
}

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
  Aprovada: 'Aprovada', Reprovada: 'Reprovada', Verificada: 'Verificada', Rejeitada: 'Rejeitada',
  Concluido: 'Concluído', Pago: 'Pago', Cancelado: 'Cancelado', Reembolsado: 'Reembolsado',
  Candidato: 'Candidato', Aprovado: 'Aprovado', Reprovado: 'Reprovado', Suspenso: 'Suspenso',
  Aberta: 'Aberta', Procedente: 'Procedente', Improcedente: 'Improcedente'
};
export const statusLabel = (s) => STATUS_LABEL[s] || s || '—';
export const statusTone = (s) => ({
  Confirmada: 'ok', Concluida: 'ok', Aprovada: 'ok', Verificada: 'ok', Pago: 'ok', Concluido: 'ok', Aprovado: 'ok',
  Cancelada: 'bad', Reprovada: 'bad', Rejeitada: 'bad', Cancelado: 'bad', Reprovado: 'bad', Suspenso: 'bad',
  Aberta: 'warn', Procedente: 'ok', Solicitada: 'warn', Pendente: 'warn', EmAnalise: 'warn', AguardandoDocumentacao: 'warn', Candidato: 'warn'
}[s] || 'neutral');
export const paymentLabel = (s) => ({ Cartao: 'Cartão', Pix: 'Pix' }[s] || s);
export const typeLabel = (s) => ({
  Graduacao: 'Graduação', PosGraduacao: 'Pós-graduação', Curso: 'Curso', Certificacao: 'Certificação',
  Experiencia: 'Experiência', Outro: 'Outro', Exercicio: 'Atividade / exercício', Video: 'Vídeo', Documento: 'Documento'
}[s] || s);
