'use strict';
// Transporte compartilhado: o frontend acessa somente a API, nunca o banco.
globalThis.TutorClassHttp = (() => {
  function normalizeBaseUrl(value, pageProtocol = globalThis.location?.protocol) {
    let url;
    try { url = new URL(value); } catch { throw new Error('Configure uma URL válida para a API em config.js ou em Configurar conexão.'); }
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
    const fallback = {401: 'Sua sessão expirou. Entre novamente.', 403: 'Seu perfil não tem permissão para esta ação.',
      404: 'Registro ou endpoint não encontrado.', 503: 'Serviço temporariamente indisponível.'}[response.status];
    const error = new Error(message || plainText || fallback || `Não foi possível concluir (HTTP ${response.status}).`);
    error.status = response.status;
    return error;
  }

  async function request(base, path, {method = 'GET', body, token, timeout = 20000} = {}) {
    const url = normalizeBaseUrl(base);
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeout);
    const headers = {Accept: 'application/json'};
    if (token) headers.Authorization = `Bearer ${token}`;
    if (body !== undefined) headers['Content-Type'] = 'application/json';
    try {
      const response = await fetch(url + path, {method, headers,
        body: body === undefined ? undefined : JSON.stringify(body), signal: controller.signal});
      const raw = await response.text();
      let data = null;
      let invalidJson = false;
      try { data = raw ? JSON.parse(raw) : null; } catch { invalidJson = true; }
      if (!response.ok) throw responseError(data, raw, response);
      if (invalidJson) throw new Error(`A API retornou uma resposta que não é JSON (HTTP ${response.status}). Confira o endereço em Configurar conexão.`);
      return data;
    } catch (error) {
      if (controller.signal.aborted) throw new Error('O servidor demorou para responder. Consulte o resultado antes de repetir uma alteração.');
      if (error instanceof TypeError) throw new Error(`Não foi possível conectar à API ${url}. Verifique a disponibilidade da API e se ela permite a origem ${globalThis.location?.origin || 'do frontend'} no CORS.`);
      throw error;
    } finally { clearTimeout(timer); }
  }
  return Object.freeze({normalizeBaseUrl, request});
})();
