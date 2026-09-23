import {createServer} from 'node:http';
import {readFile} from 'node:fs/promises';

const files = new Map([
  ['/', ['index.html', 'text/html']], ['/index.html', ['index.html', 'text/html']],
  ['/style.css', ['style.css', 'text/css']], ['/app.js', ['app.js', 'text/javascript']],
  ['/api.js', ['api.js', 'text/javascript']], ['/config.js', ['config.js', 'text/javascript']],
  ['/tutor-text.js', ['tutor-text.js', 'text/javascript']]
]);
const port = Number(process.env.PORT || 5242);
const server = createServer(async (request, response) => {
  const file = files.get(new URL(request.url, 'http://localhost').pathname);
  if (!file || !['GET', 'HEAD'].includes(request.method)) {
    response.writeHead(404).end(); return;
  }
  try {
    const body = await readFile(new URL('../TutorClass.API/wwwroot/' + file[0], import.meta.url));
    response.writeHead(200, {'Content-Type': file[1] + '; charset=utf-8', 'Cache-Control': 'no-store'});
    response.end(request.method === 'HEAD' ? undefined : body);
  } catch { response.writeHead(500).end('Não foi possível carregar o arquivo.'); }
});
server.on('error', error => {
  console.error(error.code === 'EADDRINUSE'
    ? 'A porta já está em uso. Se a API local já serve o site nessa porta, use esse servidor ou encerre-o antes de iniciar este.'
    : error.message);
  process.exitCode = 1;
});
server.listen(port, 'localhost', () => console.log('Prévia estática (sem API): http://localhost:' + port + '\nPara cadastro/login, execute dotnet run --project TutorClass.API --launch-profile http.'));

