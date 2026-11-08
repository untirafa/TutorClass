// Estado da sessão. O JWT existe somente em memória: recarregar a página encerra a sessão.
// Nada sensível é gravado em localStorage/sessionStorage/cookies.
import { normalizeBaseUrl } from './http.js';

export const session = {
  base: '', token: null, user: null, epoch: 0, returnTo: null,
  subjects: null, tutorHistory: [], tutorStream: null
};

export function configure(config = globalThis.TUTORCLASS_CONFIG) {
  session.base = normalizeBaseUrl(config?.apiBaseUrl || globalThis.location?.origin || '');
  return session.base;
}
export const role = () => session.user?.perfil ?? null;
export const isStudent = () => role() === 'Aluno';
export const isTeacher = () => role() === 'Professor';
export const isGuardian = () => role() === 'Responsavel';
export const isAdmin = () => role() === 'Administrador';
export const isSignedIn = () => !!(session.token && session.user);

const hooks = new Set();
export const onSessionReset = (fn) => { hooks.add(fn); return () => hooks.delete(fn); };
export function resetSession() {
  session.epoch++;
  session.token = null; session.user = null; session.tutorHistory = []; session.returnTo = null;
  session.tutorStream?.abort(); session.tutorStream = null;
  for (const fn of hooks) { try { fn(); } catch { /* observador não deve impedir o logout */ } }
}

// Idade completa a partir da data de nascimento (AAAA-MM-DD) devolvida por /api/auth/me. Sem data, não há como bloquear.
export function ageOf(dateText, today = new Date()) {
  const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(dateText || ''); if (!m) return null;
  let age = today.getFullYear() - Number(m[1]);
  if (today.getMonth() + 1 < Number(m[2]) || (today.getMonth() + 1 === Number(m[2]) && today.getDate() < Number(m[3]))) age -= 1;
  return age;
}
export const isMinorStudent = () => isStudent() && (ageOf(session.user?.dataNascimento) ?? 99) < 18;
export const roleLabel = (r) => (r === 'Responsavel' ? 'Responsável' : r || '');
