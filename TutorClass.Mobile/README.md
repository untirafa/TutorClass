# TutorClass Mobile — Android

Primeira etapa em .NET 10 / MAUI 10: Login e Home. Projeto separado da solução do backend.

## Estrutura
- Models: contratos JSON da API.
- Services: SessionService (HTTP) e SecureTokenStore (SecureStorage do MAUI).
- ViewModels: estado do login, restauração da sessão e saída.
- Views: LoginPage e HomePage; os atalhos futuros estão desabilitados.
- Platforms/Android e Resources: inicialização, permissões e identidade visual.
- ../TutorClass.Mobile.Tests: testes do mesmo código de serviços e ViewModels, sem dependência do emulador.

## API e sessão
Base: https://tutorclass.onrender.com/
- POST /api/auth/login: email e senha.
- GET /api/auth/me: Authorization: Bearer, confirma identidade antes de abrir Home e ao restaurar sessão.

HttpClient único, timeout de 60 segundos, somente HTTPS. A senha fica somente em memória durante o login e é limpa após sucesso. Apenas o token é salvo no SecureStorage, protegido pelo Android. Backup do app desabilitado para evitar restauração de credenciais criptografadas em outro dispositivo. Não há logs de credenciais ou token. Logout remove o token. Resposta 401 na restauração limpa a sessão. Falhas de rede exibem erro controlado e preservam o token para uma nova tentativa. Não há refresh token no contrato usado.

A navegação substitui a página raiz, evitando voltar ao Login após sucesso ou à Home após sair. A Home mostra o nome retornado por /me. Nenhum banco é acessado diretamente.

## Build
Instale .NET SDK 10 e o workload:
```powershell
dotnet workload install maui-android
dotnet build TutorClass.Mobile/TutorClass.Mobile.csproj -t:InstallAndroidDependencies -f net10.0-android -p:AndroidSdkDirectory="$env:LOCALAPPDATA/Android/Sdk" -p:JavaSdkDirectory="$env:LOCALAPPDATA/Android/jdk" -p:AcceptAndroidSDKLicenses=True
dotnet build TutorClass.Mobile/TutorClass.Mobile.csproj -f net10.0-android -p:AndroidSdkDirectory="$env:LOCALAPPDATA/Android/Sdk" -p:JavaSdkDirectory="$env:LOCALAPPDATA/Android/jdk"
```
APK de desenvolvimento em bin/Debug/net10.0-android/*-Signed.apk. Não é um pacote de publicação na Play Store.

## Testes
```powershell
dotnet test TutorClass.Mobile.Tests
```
O teste Render é opt-in. Defina TUTORCLASS_TEST_EMAIL e TUTORCLASS_TEST_PASSWORD somente no ambiente do processo para executá-lo:
```powershell
dotnet test TutorClass.Mobile.Tests --filter FullyQualifiedName~RenderTests
```
Nunca versione credenciais. Sem essas variáveis, o teste real é marcado como ignorado.

## Validação em dispositivo
Instale o APK em um Android 6 ou superior. Abra o app, informe uma conta existente e toque Entrar. Confirme o nome na Home; reabra o app para verificar restauração e toque Sair para retornar ao Login. Teste também senha incorreta e ausência de conexão.

Os testes de serviço/ViewModel não substituem validação visual, teclado e armazenamento seguro em dispositivo.

## Validação desta etapa (10/09/2026)
- 8 testes locais aprovados.
- Teste real opt-in aprovado separadamente: login no Render, /me com JWT, restauração e transição de ViewModel Login → Home.
- Na execução padrão, o teste real fica ignorado por não haver credenciais no ambiente.
- Sem celular/emulador disponível: navegação visual e SecureStorage no dispositivo ainda precisam de validação.
- Caso o OneDrive bloqueie arquivos em obj, acrescente ao comando de build: --artifacts-path "$env:TEMP/TutorClassMobileArtifacts".
- Build Android Debug concluído com 0 avisos e 0 erros. APK copiado para ../artifacts/mobile/com.tutorclass.mobile-Signed.apk.

## Etapa 2 — Professores
Home → Professores → Detalhes do professor, com retorno pela NavigationPage existente na mesma implementação de IAppNavigation. SessionService e SecureStorage permanecem os mesmos; ProfessorService recebe o HttpClient já registrado.

Endpoints públicos:
- GET /api/professores
- GET /api/professores/{id}

O resumo fornece nome, matérias, preço por hora e avaliação. Detalhes acrescentam reputação, qualificações e disponibilidades. Campos opcionais ausentes recebem indicação de informação não disponível; não são inferidos. Qualificações usam apenas título, instituição, descrição e data de conclusão, sem abrir documentos. Horários semanais são exibidos como retornados pela API, sem conversão de fuso não especificado pelo contrato.

As telas têm carregamento, erro controlado com botão Atualizar, lista vazia e professor não encontrado (404). Requisições são canceladas ao sair da tela. A lista usa CollectionView; detalhes usam rolagem. Não há agendamento ou pagamento.

Arquivos criados nesta etapa:
- Models/ProfessorDtos.cs
- Services/ProfessorService.cs
- ViewModels/ProfessoresViewModels.cs
- Views/ProfessoresPage.cs
- Views/ProfessorDetalhesPage.cs
- ../TutorClass.Mobile.Tests/ProfessoresTests.cs

Arquivos alterados:
- App.xaml.cs, MauiProgram.cs, TutorClass.Mobile.csproj (assemblies incorporadas no APK)
- ViewModels/LoginViewModel.cs (interface de navegação) e HomeViewModel.cs
- Views/HomePage.cs
- ../TutorClass.Mobile.Tests/SessionTests.cs e TutorClass.Mobile.Tests.csproj
- README.md

Validação:
- Build Android: 0 erros, 0 avisos; APK atualizado em ../artifacts/mobile/.
- 19 testes locais aprovados, incluindo os 8 anteriores de sessão.
- Testes de rede ficam ignorados por padrão. Para lista/detalhes reais:
```powershell
$env:TUTORCLASS_TEST_PUBLIC_API = '1'
dotnet test TutorClass.Mobile.Tests --filter FullyQualifiedName~ProfessoresRenderTests
Remove-Item Env:TUTORCLASS_TEST_PUBLIC_API
```
- Validação visual pendente por ausência de celular/emulador: conferir rolagem da lista/detalhes, textos extensos, botão Voltar e estados de carregamento/erro em Android.
- Teste real Render aprovado nesta etapa: lista não vazia, detalhes com ID/nome correspondentes e 404 para ID inexistente.

Para gerar explicitamente o APK instalável, acrescente -t:SignAndroidPackage ao comando de build. EmbedAssembliesIntoApk está ativado para instalação direta sem fast deployment.

## Etapa 3 — Matérias e Minhas Aulas
A Home abre Matérias e Minhas Aulas na mesma NavigationPage, preservando Professores e seus detalhes.

- GET /api/materias: exibe apenas o nome; o ID permanece no DTO.
- GET /api/aulas/minhas: JWT enviado por SessionService a partir do SecureStorage, sem ID de aluno no request. A API determina as aulas do usuário autenticado.
- Aulas exibem matéria, professor, início no fuso local do dispositivo (indicado na tela), duração, status e preço acordado em reais quando presente.
- Sem agendamento, pagamento ou ações sobre aulas.
- Ambas as telas oferecem carregamento, erro controlado, Atualizar e estado vazio. Cancelam a consulta ao sair e limpam resultados anteriores antes de recarregar.
- 401 ou token ausente limpa a sessão e retorna ao Login; 403/5xx não removem o token e são apresentados como erro de consulta.

Reutilização: HttpClient registrado, SessionService/ITokenStore, IAppNavigation, ConsultaViewModel e o estilo CollectionView. ListaConsultaViewModel e ListaConsultaPage compartilham o comportamento das duas novas listas.

Criados:
- Models/EstudosDtos.cs
- Services/EstudosServices.cs
- ViewModels/EstudosViewModels.cs
- Views/EstudosPages.cs
- ../TutorClass.Mobile.Tests/EstudosTests.cs

Alterados:
- Services/SessionService.cs
- ViewModels/ProfessoresViewModels.cs, LoginViewModel.cs e HomeViewModel.cs
- Views/HomePage.cs
- App.xaml.cs e MauiProgram.cs
- ../TutorClass.Mobile.Tests/SessionTests.cs e TutorClass.Mobile.Tests.csproj
- README.md

Testes locais: 31 aprovados, 4 testes de rede ignorados por padrão.
Testes reais desta etapa podem ser executados com o filtro FullyQualifiedName~EstudosRenderTests, usando as variáveis de teste já documentadas. Não gravam tokens nem acessam bancos diretamente.

Pendência visual: validar Matérias, Minhas Aulas, rolagem, botão Voltar, formatação do horário no fuso do Android e estados vazio/erro em dispositivo ou emulador.
- Render: 2 testes reais aprovados nesta etapa (matérias e aulas com login Aluno/JWT). O teste de aulas aceita lista vazia e verifica os campos quando existem aulas.
- Build Android e assinatura concluídos: 0 avisos, 0 erros. APK instalável atualizado em ../artifacts/mobile/com.tutorclass.mobile-Signed.apk.

## Etapa 4 — TutorIA e Notificações
Novas rotas na Home, preservando as telas anteriores. O SessionService agora compartilha GET e POST autenticados no mesmo HttpClient/ITokenStore.

Endpoints:
- GET /api/tutoria/status
- POST /api/tutoria/chat (somente mensagem no JSON)
- GET /api/notificacoes?page=1&pageSize=20
- GET /api/notificacoes/resumo
- POST /api/notificacoes/{id}/marcar-lida
- POST /api/notificacoes/marcar-todas-lidas

TutorIA:
- Exibe plano e contadores exatamente como retornados pela API; não calcula quota nem infere acesso pelo plano.
- Enviar depende de PossuiAcesso e LimiteAtingido retornados pelo backend.
- Exibe a mensagem de bloqueio/indisponibilidade, inclusive nas respostas HTTP 200.
- Resposta da última mensagem permitida é exibida mesmo quando o retorno já sinaliza limite atingido.
- Sem acesso direto ao Gemini, retry de chat no app ou persistência local da conversa.
- As mensagens visíveis vivem apenas na instância da tela; nova entrada cria nova lista visual. Eventual contexto mantido pelo backend continua sendo responsabilidade da API.
- Falha de transporte mantém o texto e exige atualizar o status antes de reenviar, pois a operação pode ter sido processada no servidor.
- Token ausente/401 encerra sessão; erro de rede, timeout e demais erros são controlados.

Notificações:
- Mostra título, mensagem, data local e lida/não lida.
- Carregar mais lê a próxima página de 20 itens, sem duplicar IDs.
- Marca uma notificação com a resposta confirmada pela API e atualiza o resumo.
- Marcar todas recarrega a lista e o resumo depois da confirmação.
- Não abre RotaAcao nem cria telas fora desta etapa.
- Trata carregamento, erro, vazio e sessão expirada.

Criados:
- Models/ComunicacaoDtos.cs
- Services/ComunicacaoServices.cs
- ViewModels/ComunicacaoViewModels.cs
- Views/ComunicacaoPages.cs
- ../TutorClass.Mobile.Tests/ComunicacaoTests.cs

Alterados:
- Services/SessionService.cs
- ViewModels/LoginViewModel.cs e HomeViewModel.cs
- Views/HomePage.cs
- App.xaml.cs e MauiProgram.cs
- ../TutorClass.Mobile.Tests/SessionTests.cs e TutorClass.Mobile.Tests.csproj
- README.md

Teste real: aprovado login, status, lista/resumo de notificações e POST chat com resposta não vazia. A chamada de chat consumiu um uso da conta de desenvolvimento. Marcações de leitura foram validadas com HTTP simulado, sem modificar notificações reais.
Para executar esse teste, defina as credenciais de teste somente no processo e TUTORCLASS_TEST_CHAT=1 para autorizar explicitamente a mensagem real, usando o filtro FullyQualifiedName~ComunicacaoRenderTests. Sem essa última variável, o teste faz apenas consultas.

Pendências visuais: dispositivo/emulador indisponível. Conferir teclado e rolagem da TutorIA, mensagens longas, mensagens de bloqueio, lista de notificações e botões de leitura em Android.
- Suíte final desta etapa: 45 testes locais aprovados; 5 testes de rede ignorados por padrão. O teste real desta etapa foi executado separadamente e passou.
- Build Android assinado desta etapa aprovado: 0 avisos e 0 erros. APK instalável atualizado em ../artifacts/mobile/com.tutorclass.mobile-Signed.apk.

## Etapa 5 — Perfil e Plano/Assinatura
Novos acessos na Home, mantendo todas as rotas anteriores.

Endpoints de consulta:
- GET /api/auth/me: Perfil usa UsuarioAutenticadoDto e SessionService já existentes para exibir nome, email e perfil.
- GET /api/planos/me: mostra PlanoAtual, AssinaturaAtiva e os indicadores de benefícios recebidos (TutorIA, limite diário quando informado, Mentoria e gravações elegíveis).

O endpoint de consulta do plano não retorna preço, datas da assinatura ou cobrança. Esses dados não são inventados, e endpoints POST de contratação/cancelamento não são chamados. Não há tabela de regras de planos no app.

Logout usa SessionService.Logout: remove o JWT pelo ITokenStore/SecureStorage, limpa o usuário da sessão e o perfil visível, e substitui a raiz pela tela de Login. A restauração posterior sem token não consulta a API. Sessão expirada durante as consultas também volta ao Login.

Ambas as telas têm atualização, carregamento, erro controlado e cancelamento ao sair. Campos ausentes não geram benefícios ou status presumidos.

Criados:
- Models/PlanoStatusDto.cs
- Services/PlanoService.cs
- ViewModels/ContaViewModels.cs
- Views/ContaPages.cs
- ../TutorClass.Mobile.Tests/ContaTests.cs

Alterados:
- App.xaml.cs, MauiProgram.cs
- ViewModels/HomeViewModel.cs, LoginViewModel.cs e ComunicacaoViewModels.cs (reutilização do retorno ao Login)
- Views/HomePage.cs
- ../TutorClass.Mobile.Tests/SessionTests.cs e TutorClass.Mobile.Tests.csproj
- README.md

Validação local: 56 testes aprovados, 6 testes de rede ignorados por padrão.
Teste real opt-in: use as variáveis de credenciais já documentadas e o filtro FullyQualifiedName~ContaRenderTests. Faz login, consulta perfil/plano e valida logout com armazenamento em memória; não modifica assinatura.

Pendências de dispositivo: confirmar visualmente Perfil/Plano, rolagem, Voltar, logout e ausência de sessão ao reabrir o app. O teste em memória verifica a chamada de remoção e o fluxo; a integração com SecureStorage precisa de Android disponível.
- Teste real desta etapa aprovado: login, GET /api/auth/me, GET /api/planos/me, logout e verificação de sessão não restaurável sem token.
- Build Android final desta etapa: 0 avisos e 0 erros. APK instalável atualizado em ../artifacts/mobile/com.tutorclass.mobile-Signed.apk.
