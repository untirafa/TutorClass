'use strict';
// Frontend e API são servidos pelo mesmo ASP.NET Core em todos os ambientes.
// Use a raiz da API, sem /swagger ou /api. Nunca inclua segredos neste arquivo.
window.TUTORCLASS_CONFIG = Object.freeze({
  apiBaseUrl: window.location.origin
});
