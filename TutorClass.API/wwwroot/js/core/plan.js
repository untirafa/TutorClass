// Regras de plano no front: quem está no Gratuito recebe um convite amigável (aviso no topo) em vez de uma tela bloqueada.
import { get } from './api.js';
import { notice } from '../ui/toast.js';

// Recurso → plano mínimo que o inclui (Gratuito < Foco < Mentoria).
export const PLAN_FEATURES = {
  tutoria: { need: 'Foco', flag: 'possuiIaEducacional', text: 'A TutorIA tira dúvidas de estudo a qualquer hora e chega com o plano Foco. Que tal conhecer e estudar com mais apoio?' },
  mentoria: { need: 'Mentoria', flag: 'possuiMentoria', text: 'A mentoria com chat direto com o professor faz parte do plano Mentoria. Quer dar esse passo e aprender ainda melhor?' },
  gravacoes: { need: 'Mentoria', flag: 'possuiGravacoesElegiveis', text: 'As gravações das aulas ficam disponíveis no plano Mentoria. Que tal explorar essa e outras vantagens?' }
};

export async function planAllows(moduleId) {
  const feature = PLAN_FEATURES[moduleId];
  if (!feature) return true;
  try {
    const plan = await get('/api/planos/me');
    return !!plan?.[feature.flag];
  } catch { return true; } // sem resposta do servidor, quem decide é a API
}

export function upgradeNotice(moduleId) {
  const feature = PLAN_FEATURES[moduleId];
  notice(feature.text, { kind: 'upgrade', title: `Recurso do plano ${feature.need}`, action: { label: 'Ver planos', href: '#app/planos' }, duration: 6000 });
}
