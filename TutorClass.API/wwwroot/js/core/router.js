// Roteamento por fragmento (#rota). O fragmento não é enviado ao servidor, então o ASP.NET
// continua devolvendo 404 real para caminhos inexistentes. Padrões: "professores/:id", "app/:modulo/:id?".

export function compile(pattern) {
  const keys = [];
  const source = pattern.split('/').map(part => {
    if (part.startsWith(':')) {
      const optional = part.endsWith('?');
      keys.push(part.slice(1).replace('?', ''));
      return optional ? '(?:/([^/]+))?' : '/([^/]+)';
    }
    return '/' + part.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }).join('');
  return { re: new RegExp('^' + source.replace(/^\//, '/?') + '$'), keys };
}

export function match(routes, hash) {
  const [rawPath, rawQuery = ''] = (hash || '').replace(/^#\/?/, '').split('?');
  let path; try { path = decodeURIComponent(rawPath) || 'home'; } catch { path = rawPath || 'home'; }
  const query = Object.fromEntries(new URLSearchParams(rawQuery));
  for (const route of routes) {
    const c = route._c ||= compile(route.path);
    const m = c.re.exec(path);
    if (m) return { route, params: Object.fromEntries(c.keys.map((k, i) => [k, m[i + 1]])), path, query };
  }
  return { route: null, params: {}, path, query };
}

export function createRouter(routes, onChange) {
  const run = () => onChange(match(routes, location.hash));
  addEventListener('hashchange', run);
  return {
    start: run,
    go(path) { const target = '#' + path.replace(/^#/, ''); if (location.hash === target) run(); else location.hash = target; },
    refresh: run
  };
}
