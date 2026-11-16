// Cliente da API com autenticação explícita por requisição: endpoints públicos nunca recebem o JWT.
import { request, stream } from './http.js';
import { session, resetSession, storedToken, clearStoredSession } from './session.js';

let onExpired = () => {};
export const setExpiredHandler = (fn) => { onExpired = fn; };
const LONG = new Set(['/api/tutoria/chat', '/api/tutoria/chat/stream']);

export async function api(path, { method = 'GET', body, auth = true, signal, timeout } = {}) {
  if (auth && !session.token) throw new Error('Entre na sua conta para continuar.');
  const epoch = session.epoch;
  const stale = () => new DOMException('Sessão alterada.', 'AbortError');
  try {
    const data = await request(session.base, path, {
      method, body, signal, token: auth ? session.token : undefined, timeout: timeout ?? (LONG.has(path) ? 125000 : 60000)
    });
    if (epoch !== session.epoch) throw stale();
    return data;
  } catch (error) {
    if (epoch !== session.epoch) throw stale();
    if (error.status === 401 && auth) {
      resetSession(); onExpired(error.message);
      throw new DOMException('Sessão expirada.', 'AbortError');
    }
    throw error;
  }
}
export const get = (path, opts) => api(path, opts);
export const publicGet = (path, opts) => api(path, { ...opts, auth: false });
export const post = (path, body, opts) => api(path, { ...opts, method: 'POST', body });
export const put = (path, body, opts) => api(path, { ...opts, method: 'PUT', body });
export const del = (path, opts) => api(path, { ...opts, method: 'DELETE' });
export const q = (params) => {
  const s = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) if (v !== '' && v != null) s.set(k, v);
  const text = s.toString();
  return text ? `?${text}` : '';
};

export function tutorStream(body, { onDelta, signal }) {
  return stream(session.base, '/api/tutoria/chat/stream', { body, token: session.token, onDelta, signal });
}

// Catálogo de matérias é público e estável durante a sessão: uma consulta por carregamento.
export async function subjects() {
  if (!session.subjects) session.subjects = await publicGet('/api/materias');
  return session.subjects;
}
export const isAbort = (e) => e?.name === 'AbortError';
export const errorText = (e) => (isAbort(e) ? '' : e?.message || 'Não foi possível concluir a operação.');

// Reabre a sessão depois de recarregar a página (F5): valida o token guardado na aba consultando /api/auth/me.
// Não usa o tratador de sessão expirada, pois a tela ainda não foi montada. Token recusado (401/403) é descartado;
// falha de rede mantém a cópia para uma nova tentativa, mas a pessoa vê a tela de entrada.
export async function restoreSession() {
  const token = storedToken();
  if (!token || session.token) return false;
  const epoch = session.epoch;
  try {
    const user = await request(session.base, '/api/auth/me', { token, timeout: 60000 });
    if (epoch !== session.epoch) return false;
    if (!user?.id || !user.nome || !user.perfil) { clearStoredSession(); return false; }
    session.token = token; session.user = user;
    return true;
  } catch (error) {
    if (error?.status === 401 || error?.status === 403) clearStoredSession();
    return false;
  }
}
