import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';
import {extname, join, normalize, sep} from 'node:path';
import {fileURLToPath} from 'node:url';

const root = fileURLToPath(new URL('../TutorClass.API/wwwroot/', import.meta.url));
const types = {'.html':'text/html','.css':'text/css','.js':'text/javascript','.svg':'image/svg+xml','.webp':'image/webp','.png':'image/png','.ico':'image/x-icon'};
const port = Number(process.env.PORT || 5242);
const server = createServer(async (request, response) => {
  if (!['GET', 'HEAD'].includes(request.method)) { response.writeHead(405).end(); return; }
  let path;
  try { path = decodeURIComponent(new URL(request.url, 'http://localhost').pathname); } catch { response.writeHead(400).end(); return; }
  if (path === '/') path = '/index.html';
  const file = normalize(join(root, path));
  const type = types[extname(file)];
  // Somente arquivos estáticos dentro de wwwroot; o roteamento é por #hash, então não há fallback para 404.
  if (!file.startsWith(root) || !type) { response.writeHead(404).end(); return; }
  try {
    const body = await readFile(file);
    response.writeHead(200, {'Content-Type': type + '; charset=utf-8', 'Cache-Control': 'no-store'});
    response.end(request.method === 'HEAD' ? undefined : body);
  } catch { response.writeHead(404).end(); }
});
server.on('error', error => {
  console.error(error.code === 'EADDRINUSE'
    ? 'A porta já está em uso. Se a API local já serve o site nessa porta, use esse servidor ou encerre-o antes de iniciar este.'
    : error.message);
  process.exitCode = 1;
});
server.listen(port, 'localhost', () => console.log('Prévia estática (sem API): http://localhost:' + port + '\nPara cadastro/login reais, execute dotnet run --project TutorClass.API --launch-profile http (ou npm run mock para uma API simulada).'));
