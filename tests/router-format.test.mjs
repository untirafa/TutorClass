import {test} from 'node:test';
import assert from 'node:assert/strict';
import {compile, match} from '../TutorClass.API/wwwroot/js/core/router.js';
import {utcInputToIso, estimateTotal, money, statusLabel} from '../TutorClass.API/wwwroot/js/core/format.js';

test('roteador casa rotas sem barra, parâmetros e consulta no hash', () => {
  const routes = [{ path: 'home' }, { path: 'professores/:id' }, { path: 'professores' }, { path: 'app/:modulo/:id?' }];
  assert.equal(match(routes, '#home').route.path, 'home');
  const prof = match(routes, '#professores/abc');
  assert.equal(prof.route.path, 'professores/:id'); assert.equal(prof.params.id, 'abc');
  const busca = match(routes, '#professores?materia=m1');
  assert.equal(busca.route.path, 'professores'); assert.equal(busca.query.materia, 'm1');
  assert.equal(match(routes, '#app/aulas').params.modulo, 'aulas');
  assert.equal(match(routes, '#nada').route, null);
});
test('formatação: moeda, estimativa e status', () => {
  assert.match(money(10), /R\$\s?10,00/);
  assert.equal(money(null), 'Não informado');
  assert.equal(estimateTotal(60, 90), 90);
  assert.ok(statusLabel('Agendada'));
});
test('data digitada em UTC vira ISO com Z', () => {
  const iso = utcInputToIso('2030-01-02T10:30', new Date('2029-01-01T00:00:00Z'));
  assert.match(iso, /^2030-01-02T10:30/); assert.match(iso, /Z$/);
});
