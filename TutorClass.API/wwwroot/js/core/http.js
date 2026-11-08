// Transporte HTTP: o front-end conversa somente com a API (mesma origem), nunca com o banco.
// Mensagens reais da API são preservadas; falhas de rede, timeout e HTML inesperado têm texto próprio.

export function normalizeBaseUrl(value, pageProtocol = globalThis.location?.protocol) {
  let url;
  try { url = new URL(value); } catch { throw new Error('Configure uma URL válida para a API em config.js.'); }
  if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.search || url.hash)
    throw new Error('Use uma URL HTTP/HTTPS, sem credenciais, consulta ou fragmentos.');
  if (pageProtocol === 'https:' && url.protocol !== 'https:')
    throw new Error('Uma página HTTPS precisa de uma API HTTPS para evitar bloqueio de conteúdo misto.');
  const path = url.pathname.replace(/\/+$/, '');
  if (/\/(?:api|swagger)(?:\/|$)/i.test(path))
    throw new Error('Informe a raiz da API, sem /api, /swagger ou o caminho de um endpoint.');
  return url.href.replace(/\/+$/, '');
}

function responseError(data, raw, response) {
  const validation = data?.errors && typeof data.errors === 'object'
    ? Object.values(data.errors).flat().filter(value => typeof value === 'string').join(' ') : '';
  const message = [data?.error, validation, data?.detail, data?.message, data?.title,
    typeof data === 'string' ? data : null].find(value => typeof value === 'string' && value.trim());
  const plainText = response.headers.get('content-type')?.startsWith('text/plain') && !/^\s*</.test(raw) ? raw.slice(0, 500) : '';
  const fallback = {
    401: 'Sua sessão expirou. Entre novamente.',
    403: 'Seu perfil não tem permissão para esta ação.',
    404: 'Registro ou endpoint não encontrado.',
    503: 'Serviço temporariamente indisponível.'
  }[response.status];
  const error = new Error(message || plainText || fallback || `Não foi possível concluir (HTTP ${response.status}).`);
  error.status = response.status;
  if (data && typeof data === 'object') { error.code = data.codigo; error.details = data; }
  return error;
}

export async function request(base, path, { method = 'GET', body, token, timeout = 20000, signal } = {}) {
  const url = normalizeBaseUrl(base);
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeout);
  const cancel = () => controller.abort();
  if (signal?.aborted) cancel();
  signal?.addEventListener('abort', cancel, { once: true });
  const headers = { Accept: 'application/json' };
  if (token) headers.Authorization = `Bearer ${token}`;
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  try {
    const response = await fetch(url + path, {
      method, headers, body: body === undefined ? undefined : JSON.stringify(body), signal: controller.signal
    });
    const raw = await response.text();
    let data = null;
    let invalidJson = false;
    try { data = raw ? JSON.parse(raw) : null; } catch { invalidJson = true; }
    if (!response.ok) throw responseError(data, raw, response);
    if (invalidJson) throw new Error(`A API retornou uma resposta que não é JSON (HTTP ${response.status}). Confira o endereço configurado em config.js.`);
    return data;
  } catch (error) {
    if (signal?.aborted) throw new DOMException('Requisição cancelada.', 'AbortError');
    if (controller.signal.aborted) throw new Error('O servidor demorou para responder. Consulte o resultado antes de repetir uma alteração.');
    if (error instanceof TypeError) throw new Error(`Não foi possível conectar à API ${url}. Verifique a disponibilidade da API e se ela permite a origem ${globalThis.location?.origin || 'do front-end'} no CORS.`);
    throw error;
  } finally {
    clearTimeout(timer);
    signal?.removeEventListener('abort', cancel);
  }
}

// Resposta progressiva (SSE) da TutorIA. Nunca repete a pergunta automaticamente.
export async function stream(base, path, { body, token, onDelta, signal, timeout = 125000 } = {}) {
  const controller = new AbortController();
  const cancel = () => controller.abort();
  if (signal?.aborted) cancel();
  signal?.addEventListener('abort', cancel, { once: true });
  const timer = setTimeout(cancel, timeout);
  let reader;
  try {
    const response = await fetch(normalizeBaseUrl(base) + path, {
      method: 'POST',
      headers: { Accept: 'text/event-stream', 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
      body: JSON.stringify(body), signal: controller.signal
    });
    if (!response.ok) {
      const raw = await response.text();
      let data; try { data = JSON.parse(raw); } catch { /* usa mensagem HTTP padrão */ }
      throw responseError(data, raw, response);
    }
    if (!response.headers.get('content-type')?.startsWith('text/event-stream') || !response.body)
      throw new Error('A API não retornou uma resposta progressiva válida.');
    reader = response.body.getReader();
    const decoder = new TextDecoder('utf-8', { fatal: true });
    let buffer = '', event = '', data = [], received = 0;
    for (;;) {
      const chunk = await reader.read();
      buffer += chunk.done ? decoder.decode() : decoder.decode(chunk.value, { stream: true });
      received += chunk.value?.length || 0;
      if (received > 2 * 1024 * 1024) throw new Error('A resposta excedeu o tamanho permitido.');
      let end;
      while ((end = buffer.indexOf('\n')) >= 0) {
        const line = buffer.slice(0, end).replace(/\r$/, ''); buffer = buffer.slice(end + 1);
        if (line === '') {
          if (data.length) {
            const payload = JSON.parse(data.join('\n'));
            if (event === 'delta' && typeof payload.text === 'string') await onDelta?.(payload.text);
            else if (event === 'complete') return payload;
            else if (event === 'error') throw new Error(payload.message || 'A resposta foi interrompida.');
          }
          event = ''; data = [];
        } else if (line.startsWith('event:')) event = line.slice(6).trim();
        else if (line.startsWith('data:')) data.push(line.slice(5).replace(/^ /, ''));
      }
      if (chunk.done) throw new Error('A resposta foi interrompida. Atualize o status antes de tentar novamente.');
    }
  } catch (error) {
    if (signal?.aborted) throw new DOMException('Requisição cancelada.', 'AbortError');
    if (controller.signal.aborted) throw new Error('A resposta demorou demais. Atualize o status antes de tentar novamente.');
    throw error;
  } finally {
    clearTimeout(timer); signal?.removeEventListener('abort', cancel);
    if (reader) { try { await reader.cancel(); } catch { /* já encerrado */ } reader.releaseLock(); }
  }
}
