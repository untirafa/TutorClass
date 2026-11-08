import {test} from 'node:test';
import assert from 'node:assert/strict';
import {ageOf} from '../TutorClass.API/wwwroot/js/core/session.js';

test('idade completa considera mês e dia do aniversário', () => {
  const hoje = new Date(2026, 10, 7); // 7/11/2026
  assert.equal(ageOf('2008-11-07', hoje), 18);
  assert.equal(ageOf('2008-11-08', hoje), 17);
  assert.equal(ageOf('2014-05-10', hoje), 12);
  assert.equal(ageOf(null, hoje), null);
  assert.equal(ageOf('texto', hoje), null);
});
