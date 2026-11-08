// Cliente da API com autenticação explícita por requisição: endpoints públicos nunca recebem o JWT.
import { request, stream } from './http.js';
import { session, resetSession } from './session.js';

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
