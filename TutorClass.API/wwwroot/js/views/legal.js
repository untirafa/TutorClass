// Termos de uso e Política de privacidade do TutorClass (LGPD – Lei nº 13.709/2018).
// Texto elaborado pela equipe do projeto, conforme o que a plataforma realmente faz. Revise sempre que o sistema mudar.
import { h, mount } from '../core/dom.js';
import { publicGet } from '../core/api.js';
import { pageHeader, section, link } from '../ui/components.js';

export const LEGAL_VERSION = '1.1';
export const LEGAL_DATE = '11 de novembro de 2026';

const note = () => h('p', { class: 'badge tone-warn' },
  'Projeto acadêmico (Projeto Integrado Multidisciplinar – UNIP). Este texto foi escrito pela equipe e não substitui parecer jurídico.');
const meta = () => h('p', { class: 'small muted' }, `Versão ${LEGAL_VERSION} · atualizado em ${LEGAL_DATE}`);
const ul = (...items) => h('ul', null, items.map(t => h('li', null, t)));
const p = (t) => h('p', null, t);

async function contact(ctx) {
  try { const d = await publicGet('/api/privacidade/documentos', { signal: ctx.signal }); return d?.contatoPrivacidade || ''; } catch { return ''; }
}
const contactSection = (email) => section('Contato',
  p(email ? `Para dúvidas ou pedidos sobre seus dados, fale com a equipe do projeto: ${email}.`
          : 'Para dúvidas ou pedidos sobre seus dados, use o canal de contato da equipe do projeto. O endereço de e-mail será divulgado nesta página.'));
const back = () => h('p', null, link('Voltar para Privacidade e termos', '#privacidade'));

export async function policy(ctx) {
  const email = await contact(ctx);
  mount(ctx.host, h('div', { class: 'container stack prose' },
    pageHeader('Política de privacidade', 'Como o TutorClass coleta, usa, compartilha e protege seus dados pessoais.'), meta(), note(),
    section('1. Quem trata os seus dados',
      p('O TutorClass é uma plataforma de aulas particulares desenvolvida como projeto acadêmico. A equipe responsável pelo projeto é a controladora dos dados tratados aqui, nos termos da Lei Geral de Proteção de Dados Pessoais (LGPD, Lei nº 13.709/2018).')),
    section('2. Quais dados coletamos', ul(
      'Cadastro: nome, e-mail e senha. A senha é guardada somente em forma de código criptográfico (hash); ninguém consegue lê-la.',
      'Alunos: data de nascimento (opcional), usada para saber se a pessoa é menor de 18 anos. Responsáveis: telefone (opcional) e os alunos vinculados.',
      'Professores: apresentação, preço por hora, matérias, qualificações e a referência dos documentos que comprovam a formação.',
      'Uso da plataforma: aulas solicitadas e realizadas, avaliações, notificações, plano contratado, perguntas à TutorIA e mensagens do chat de Mentoria.',
      'Denúncias: quando você denuncia um perfil, uma avaliação ou uma mensagem, guardamos o motivo, o que você escreveu, um trecho do conteúdo denunciado e a decisão da moderação. Sua identidade não é revelada à pessoa denunciada.',
      'Pagamentos e gravações são simulados nesta versão: nenhum dado de cartão ou de conta bancária é solicitado.',
      'Dados técnicos mínimos de funcionamento (como registros de acesso do servidor), usados para segurança e correção de falhas.')),
    section('3. Para que usamos e em que base legal', ul(
      'Criar e manter sua conta, agendar aulas e prestar os serviços que você pediu (execução de contrato – art. 7º, V).',
      'Oferecer recursos opcionais, como a TutorIA e a Mentoria, e tratar dados de menores de idade com o consentimento de um responsável (art. 7º, I e art. 14).',
      'Proteger a plataforma contra fraude e abuso e melhorar o serviço (legítimo interesse – art. 7º, IX), sempre com o mínimo de dados necessário.',
      'Cumprir obrigações legais e atender pedidos de autoridades, quando houver.')),
    section('4. Crianças e adolescentes',
      p('Alunos menores de 18 anos usam o TutorClass com o acompanhamento de um responsável vinculado à conta. O tratamento dos dados deles busca sempre o seu melhor interesse (art. 14 da LGPD). Menores não têm acesso à área financeira: os pagamentos são feitos pelo responsável.')),
    section('5. Com quem compartilhamos', ul(
      'Professor: vê os dados necessários para dar a aula (nome, matéria, horários e mensagens da Mentoria do vínculo).',
      'Responsável: vê os dados do aluno que está vinculado a ele, como aulas, plano e financeiro.',
      'Prestadores de infraestrutura (operadores): hospedagem do servidor, banco de dados relacional (PostgreSQL) e banco do chat de Mentoria (MongoDB). Eles tratam os dados apenas para manter o serviço no ar.',
      'TutorIA: quando o provedor de IA generativa (Google Gemini) está habilitado, a pergunta do aluno é enviada a esse serviço para gerar a resposta. Não envie dados sensíveis nas perguntas.',
      'VLibras (Governo Federal): o tradutor para Libras é carregado a partir do serviço do Governo Federal (vlibras.gov.br) quando a página abre, e o navegador pode enviar a esse serviço dados técnicos da conexão, como o endereço IP. O TutorClass não envia seus dados de cadastro nem o conteúdo da sua conta ao VLibras.',
      'Não vendemos nem alugamos dados pessoais.')),
    section('6. Cookies e armazenamento no navegador', ul(
      'Não usamos cookies de rastreamento, nem serviços externos de estatísticas, fontes ou imagens.',
      'O token de acesso fica apenas na memória da aba: ao recarregar a página ou sair, você precisa entrar novamente.',
      'Suas preferências de acessibilidade (tamanho do texto, tema, contraste e movimento) e o aviso de download do aplicativo ficam guardados só neste navegador.')),
    section('7. Como protegemos seus dados', ul(
      'Conexão protegida por HTTPS e controle de acesso por perfil (aluno, responsável, professor e administrador).',
      'Senhas com hash e regra mínima de senha: 8 caracteres, com letra, número e caractere especial.',
      'Moderação do conteúdo da TutorIA e limites de uso por plano.',
      'Nenhum sistema é totalmente imune a falhas. Se houver um incidente que possa causar risco relevante, avisaremos as pessoas afetadas e a Autoridade Nacional de Proteção de Dados (ANPD), como a lei determina.')),
    section('8. Por quanto tempo guardamos',
      p('Guardamos os dados enquanto a sua conta existir e for necessário para os fins descritos acima. Por ser uma versão acadêmica, dados de demonstração podem ser apagados a qualquer momento. Dados que a lei obrigue a manter serão guardados pelo prazo legal.')),
    section('9. Seus direitos',
      p('Você pode pedir, a qualquer momento (art. 18 da LGPD):'),
      ul('confirmação de que tratamos seus dados e acesso a eles;', 'correção de dados incompletos, inexatos ou desatualizados;',
        'anonimização, bloqueio ou eliminação de dados desnecessários ou tratados em desconformidade;', 'portabilidade dos dados;',
        'informação sobre com quem os dados foram compartilhados;', 'eliminação dos dados tratados com consentimento e revogação desse consentimento;',
        'oposição a um tratamento, nos casos previstos em lei.'),
      p('Se não ficar satisfeito com a resposta, você também pode recorrer à ANPD (www.gov.br/anpd).')),
    section('10. Mudanças nesta política', p('Podemos atualizar este texto quando a plataforma mudar. A versão e a data ficam no topo da página.')),
    contactSection(email), back()));
}

export async function terms(ctx) {
  const email = await contact(ctx);
  mount(ctx.host, h('div', { class: 'container stack prose' },
    pageHeader('Termos de uso', 'As regras para usar o TutorClass.'), meta(), note(),
    section('1. Aceitação', p('Ao criar uma conta ou usar o TutorClass, você declara que leu e concorda com estes termos e com a Política de privacidade.')),
    section('2. O que é o TutorClass',
      p('O TutorClass conecta alunos, responsáveis e professores particulares, permitindo buscar professores, agendar aulas, usar a TutorIA e a Mentoria, conforme o plano. Esta é uma versão acadêmica: pagamentos e gravações de aula são simulados e não geram cobrança nem arquivo real.')),
    section('3. Conta e segurança', ul(
      'Informe dados verdadeiros e mantenha-os atualizados.',
      'Use uma senha forte (8 caracteres ou mais, com letra, número e caractere especial) e não a compartilhe. Você responde pelo que for feito com a sua conta.',
      'Alunos menores de 18 anos devem usar o TutorClass com o acompanhamento de um responsável.')),
    section('4. Perfis', ul(
      'Aluno: busca professores, solicita aulas e usa os recursos do seu plano.',
      'Responsável: acompanha os alunos vinculados e cuida dos pagamentos deles.',
      'Professor: a candidatura e as qualificações são analisadas pela administração antes de o perfil ser aprovado. Informe apenas dados e documentos verdadeiros.',
      'Administrador: opera a plataforma, modera conteúdo e mantém as perguntas frequentes.')),
    section('5. Aulas, planos e avaliações', ul(
      'O preço de uma aula é calculado pela plataforma a partir do valor por hora do professor e da duração.',
      'Os recursos disponíveis dependem do plano contratado (por exemplo, TutorIA, gravações e Mentoria).',
      'Alunos podem avaliar o professor depois de uma aula concluída. Avaliações devem ser honestas e respeitosas.')),
    section('6. TutorIA',
      p('A TutorIA é um apoio de estudo. Ela pode errar ou ser incompleta e não substitui um professor. Evite enviar dados pessoais ou sensíveis. Pedidos inadequados são bloqueados pela moderação.')),
    section('7. O que não é permitido', ul(
      'Usar a plataforma para fins ilegais, para assediar, discriminar ou expor outras pessoas.',
      'Tentar acessar contas ou dados de terceiros, burlar regras de acesso ou sobrecarregar o serviço.',
      'Enviar conteúdo ofensivo, falso, que viole direitos de autor ou que contenha dados pessoais de terceiros sem autorização.')),
    section('8. Conteúdo e propriedade intelectual',
      p('O TutorClass, sua marca, interface e código pertencem à equipe do projeto. Os materiais enviados por professores continuam sendo deles, que declaram ter o direito de usá-los. Você não pode copiar ou redistribuir a plataforma sem autorização.')),
    section('9. Disponibilidade e responsabilidade',
      p('Por ser um projeto acadêmico, o TutorClass é oferecido como está, sem garantia de funcionamento contínuo, e pode ficar indisponível ou ter dados de demonstração apagados. A equipe não se responsabiliza por danos decorrentes do uso indevido da plataforma, de falhas de terceiros ou do conteúdo gerado pela TutorIA, nos limites da lei.')),
    section('10. Suspensão e encerramento', p('Contas que descumpram estes termos ou as Regras de convivência podem receber advertência, ser suspensas ou encerradas, depois da análise de uma denúncia pela administração. Você pode pedir a exclusão da sua conta pelo contato abaixo.'),
      p(link('Leia as Regras de convivência.', '#regras-de-convivencia'))),
    section('11. Mudanças e lei aplicável', p('Podemos atualizar estes termos; a versão e a data ficam no topo da página. Aplica-se a legislação brasileira.')),
    contactSection(email), back()));
}

export async function rules(ctx) {
  const email = await contact(ctx);
  mount(ctx.host, h('div', { class: 'container stack prose' },
    pageHeader('Regras de convivência', 'Como convivemos no TutorClass, como denunciar e o que a moderação faz.'), meta(), note(),
    section('1. Respeito em primeiro lugar', ul(
      'Trate todas as pessoas com respeito, no cadastro, na busca, no agendamento, nas avaliações e nas mensagens.',
      'Não é permitido ofender, humilhar, ameaçar, perseguir ou assediar ninguém.',
      'Não é permitida discriminação por origem, raça, cor, etnia, religião, gênero, orientação sexual, deficiência, idade ou condição social.')),
    section('2. Proteção de crianças e adolescentes', ul(
      'Conteúdo sexual ou inadequado para menores é proibido e pode ser comunicado às autoridades.',
      'Não peça nem ofereça fotos, endereços ou contatos de menores fora da plataforma.',
      'Responsáveis acompanham os alunos menores vinculados à sua conta.')),
    section('3. Dados pessoais e honestidade', ul(
      'Não exponha dados pessoais seus ou de outras pessoas (CPF, endereço, telefone, senhas) em mensagens ou avaliações.',
      'Use informações verdadeiras: perfis falsos, qualificações inventadas e tentativas de golpe não são aceitos.',
      'Não combine pagamentos fora da plataforma. Avaliações devem falar da aula e do ensino, sem ataques pessoais.')),
    section('4. Como denunciar', ul(
      'Use o botão “Denunciar” no perfil do professor, nas avaliações e nas mensagens da Mentoria.',
      'Escolha o motivo e, se quiser, conte o que aconteceu. Para o motivo “Outro”, a descrição é obrigatória.',
      'Acompanhe o andamento em “Minhas denúncias”, na área logada. Você recebe um aviso quando a análise termina.',
      'Sua identidade não é revelada à pessoa denunciada. Denúncias de má-fé ou em excesso (o limite é de 10 por dia) podem levar a sanções.')),
    section('5. O que a moderação faz', ul(
      'A administração analisa cada denúncia e registra uma justificativa para a decisão.',
      'Denúncia improcedente: nenhuma providência é tomada contra a pessoa denunciada.',
      'Denúncia procedente: pode haver advertência (aviso dentro da plataforma) ou suspensão da conta, que bloqueia o acesso até a administração reativá-la.',
      'Casos graves, como ameaças ou crimes, podem ser comunicados às autoridades competentes.',
      'A TutorIA e o chat da Mentoria também têm filtros automáticos que bloqueiam mensagens ofensivas, ameaças e dados pessoais.')),
    section('6. Pedir revisão', p('Se você discorda de uma sanção, fale com a equipe do projeto pelo contato abaixo, informando o que aconteceu.')),
    contactSection(email),
    h('p', null, link('Ver Termos de uso', '#termos-de-uso'), ' · ', link('Voltar para Privacidade e termos', '#privacidade'))));
}
