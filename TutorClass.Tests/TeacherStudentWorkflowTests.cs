using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class TeacherStudentWorkflowTests
{
    private const string Password = "Workflow-tests@123";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Cadastro_aprovacao_mentoria_atividade_e_chat_funcionam_por_http_com_isolamento()
    {
        var store = new WorkflowChatStore();
        await using var factory = CreateFactory(store);
        using var scenario = await CreateWorkflowAsync(factory);

        var professorMentorias = await ReadAsync<MentoriaResponse[]>(
            await scenario.Professor.GetAsync("/api/mentorias/me"));
        var alunoMentorias = await ReadAsync<MentoriaResponse[]>(
            await scenario.Aluno.GetAsync("/api/mentorias/me"));
        Assert.Equal(scenario.MentoriaId, Assert.Single(professorMentorias).Id);
        Assert.Equal(scenario.MentoriaId, Assert.Single(alunoMentorias).Id);
        var destinatario = Assert.Single(await ReadAsync<DestinatarioMaterialResponse[]>(
            await scenario.Professor.GetAsync("/api/materiais/destinatarios")));
        Assert.Equal(scenario.AlunoId, destinatario.AlunoId);
        Assert.Equal("Aluno Workflow", destinatario.AlunoNome);
        Assert.Equal(scenario.MateriaId, destinatario.MateriaId);
        Assert.Equal("Matematica Workflow", destinatario.MateriaNome);
        Assert.True(destinatario.MentoriaAtiva);
        Assert.Empty(destinatario.Aulas);
        await AssertStatusAsync(await scenario.Aluno.GetAsync("/api/materiais/destinatarios"), HttpStatusCode.Forbidden);

        // A mentor can send an activity before the first lesson has been booked.
        var request = CreateActivity(scenario.AlunoId, scenario.MateriaId);
        var material = await ReadAsync<MaterialResponse>(
            await scenario.Professor.PostAsJsonAsync("/api/materiais", request, Json), HttpStatusCode.Created);
        Assert.Null(material.AulaId);
        Assert.Equal(scenario.ProfessorId, material.ProfessorId);
        Assert.Equal(scenario.AlunoId, material.AlunoId);
        Assert.Equal("Exercicio", material.Tipo);
        Assert.Equal(request.UrlArquivo, material.UrlArquivo);

        var materiaisAluno = await ReadAsync<MaterialResponse[]>(await scenario.Aluno.GetAsync("/api/materiais/me"));
        var materiaisProfessor = await ReadAsync<MaterialResponse[]>(await scenario.Professor.GetAsync("/api/materiais/me"));
        Assert.Equal(material.Id, Assert.Single(materiaisAluno).Id);
        Assert.Equal(material.Id, Assert.Single(materiaisProfessor).Id);

        var update = new AtualizarMaterialRequest(
            "Exercicios de fracao revisados", "Resolver os exercicios de 1 a 4.", "Exercicio",
            "fracoes-revisadas.pdf", "https://example.com/atividades/fracoes-revisadas.pdf");
        await ReadAsync<MaterialResponse>(
            await scenario.Professor.PutAsJsonAsync($"/api/materiais/{material.Id}", update, Json));
        var materialAtualizado = await ReadAsync<MaterialResponse>(
            await scenario.Aluno.GetAsync($"/api/materiais/{material.Id}"));
        Assert.Equal(update.Titulo, materialAtualizado.Titulo);
        Assert.Equal(update.UrlArquivo, materialAtualizado.UrlArquivo);

        var chatPath = $"/api/mentorias/{scenario.MentoriaId}/chat";
        var chat = await ReadAsync<MentoriaChatResponse>(await scenario.Professor.GetAsync(chatPath));
        Assert.False(chat.SomenteLeitura);
        Assert.Equal(scenario.AlunoId, chat.AlunoId);
        Assert.Equal(scenario.ProfessorId, chat.ProfessorId);
        var enviada = await ReadAsync<MensagemMentoriaResponse>(
            await scenario.Professor.PostAsJsonAsync($"{chatPath}/mensagens",
                new EnviarMensagemMentoriaRequest("Enviei a atividade de fracoes."), Json), HttpStatusCode.Created);
        var resposta = await ReadAsync<MensagemMentoriaResponse>(
            await scenario.Aluno.PostAsJsonAsync($"{chatPath}/mensagens",
                new EnviarMensagemMentoriaRequest("Recebi a atividade, professor."), Json), HttpStatusCode.Created);
        Assert.Equal(scenario.ProfessorId, enviada.RemetenteUsuarioId);
        Assert.Equal(scenario.AlunoId, resposta.RemetenteUsuarioId);
        Assert.Equal(chat.Id, enviada.ChatMentoriaId);
        var historico = await ReadAsync<MensagensMentoriaPageResponse>(
            await scenario.Aluno.GetAsync($"{chatPath}/mensagens?limit=30"));
        Assert.Equal(new[] { enviada.Id, resposta.Id }, historico.Mensagens.Select(m => m.Id));
        Assert.Null(historico.ProximoBeforeUtc);

        var lidas = await ReadAsync<MensagensLidasResponse>(
            await scenario.Aluno.PostAsync($"{chatPath}/mensagens/lidas", null));
        Assert.Equal(1, lidas.QuantidadeMarcada);
        var historicoProfessor = await ReadAsync<MensagensMentoriaPageResponse>(
            await scenario.Professor.GetAsync($"{chatPath}/mensagens"));
        Assert.True(historicoProfessor.Mensagens.Single(m => m.Id == enviada.Id).Lida);
        Assert.False(historicoProfessor.Mensagens.Single(m => m.Id == resposta.Id).Lida);

        using var outroAluno = factory.CreateClient();
        var outro = await RegisterAlunoAsync(outroAluno, "outro.workflow@teste.local");
        await AssertStatusAsync(await outroAluno.GetAsync($"/api/materiais/{material.Id}"), HttpStatusCode.Forbidden);
        Assert.Empty(await ReadAsync<MaterialResponse[]>(await outroAluno.GetAsync("/api/materiais/me")));
        await AssertStatusAsync(await outroAluno.GetAsync($"{chatPath}/mensagens"), HttpStatusCode.Forbidden);
        await AssertStatusAsync(await outroAluno.PostAsJsonAsync($"{chatPath}/mensagens",
            new EnviarMensagemMentoriaRequest("Mensagem de fora do vinculo."), Json), HttpStatusCode.Forbidden);
        await AssertStatusAsync(await scenario.Professor.PostAsJsonAsync("/api/materiais",
            CreateActivity(outro.Id, scenario.MateriaId), Json), HttpStatusCode.BadRequest);
        await AssertStatusAsync(await scenario.Aluno.PostAsJsonAsync("/api/materiais", request, Json), HttpStatusCode.Forbidden);
        var destinatarios = await ReadAsync<DestinatarioMaterialResponse[]>(
            await scenario.Professor.GetAsync("/api/materiais/destinatarios"));
        Assert.Equal(scenario.AlunoId, Assert.Single(destinatarios).AlunoId);
        Assert.DoesNotContain(destinatarios, d => d.AlunoId == outro.Id);

        using var outroProfessor = factory.CreateClient();
        await ReadAsync<UsuarioAutenticadoDto>(await outroProfessor.PostAsJsonAsync("/api/auth/registrar/professor",
            new RegistrarProfessorRequest("Outro Professor", "outro.professor.workflow@teste.local", Password,
                "Professor de outra turma.", 90m), Json), HttpStatusCode.Created);
        await AuthorizeAsync(outroProfessor, "outro.professor.workflow@teste.local");
        Assert.Empty(await ReadAsync<DestinatarioMaterialResponse[]>(await outroProfessor.GetAsync("/api/materiais/destinatarios")));
        await AssertStatusAsync(await outroProfessor.GetAsync(chatPath), HttpStatusCode.Forbidden);
        await AssertStatusAsync(await outroProfessor.PutAsJsonAsync($"/api/materiais/{material.Id}", update, Json), HttpStatusCode.Forbidden);
        await AssertStatusAsync(await outroProfessor.DeleteAsync($"/api/materiais/{material.Id}"), HttpStatusCode.Forbidden);

        Guid outraMateriaId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            var outraMateria = new Materia("Materia nao lecionada pelo mentor");
            db.Add(outraMateria);
            await db.SaveChangesAsync();
            outraMateriaId = outraMateria.Id;
        }
        await AssertStatusAsync(await scenario.Professor.PostAsJsonAsync("/api/materiais",
            CreateActivity(scenario.AlunoId, outraMateriaId), Json), HttpStatusCode.BadRequest);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            Assert.Equal(StatusProfessor.Aprovado, db.Professores.Single(p => p.Id == scenario.ProfessorId).Status);
            Assert.True(db.AssinaturasAluno.Single(a => a.AlunoId == scenario.AlunoId).Ativa);
            Assert.True(db.MentoriasAlunoProfessor.Single(m => m.Id == scenario.MentoriaId).Ativo);
            var persistido = Assert.Single(db.MateriaisPedagogicos);
            Assert.Equal(material.Id, persistido.Id);
            Assert.Equal(update.Titulo, persistido.Titulo);
            Assert.True(db.Notificacoes.Any(n => n.UsuarioId == scenario.AlunoId && n.ReferenciaId == material.Id));
        }
        Assert.Equal(2, store.Messages.Count);

        await AssertStatusAsync(await scenario.Professor.DeleteAsync($"/api/materiais/{material.Id}"), HttpStatusCode.NoContent);
        Assert.Empty(await ReadAsync<MaterialResponse[]>(await scenario.Aluno.GetAsync("/api/materiais/me")));
        await AssertStatusAsync(await scenario.Aluno.GetAsync($"/api/materiais/{material.Id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancelar_plano_preserva_historico_e_bloqueia_nova_atividade_e_mensagem_sem_relacao_ativa()
    {
        await using var factory = CreateFactory(new WorkflowChatStore());
        using var scenario = await CreateWorkflowAsync(factory);
        var material = await ReadAsync<MaterialResponse>(
            await scenario.Professor.PostAsJsonAsync("/api/materiais", CreateActivity(scenario.AlunoId, scenario.MateriaId), Json),
            HttpStatusCode.Created);
        var chatPath = $"/api/mentorias/{scenario.MentoriaId}/chat";
        var mensagem = await ReadAsync<MensagemMentoriaResponse>(
            await scenario.Professor.PostAsJsonAsync($"{chatPath}/mensagens",
                new EnviarMensagemMentoriaRequest("Atividade enviada durante a mentoria."), Json), HttpStatusCode.Created);

        var plano = await ReadAsync<PlanoAlunoStatusResponse>(await scenario.Aluno.PostAsync("/api/planos/me/cancelar", null));
        Assert.False(plano.PossuiMentoria);
        Assert.Equal(PlanoAluno.Gratuito, plano.PlanoAtual);
        Assert.Empty(await ReadAsync<DestinatarioMaterialResponse[]>(
            await scenario.Professor.GetAsync("/api/materiais/destinatarios")));
        var chat = await ReadAsync<MentoriaChatResponse>(await scenario.Professor.GetAsync(chatPath));
        Assert.True(chat.SomenteLeitura);
        var historico = await ReadAsync<MensagensMentoriaPageResponse>(await scenario.Aluno.GetAsync($"{chatPath}/mensagens"));
        Assert.Equal(mensagem.Id, Assert.Single(historico.Mensagens).Id);
        Assert.Equal(material.Id, (await ReadAsync<MaterialResponse>(await scenario.Aluno.GetAsync($"/api/materiais/{material.Id}"))).Id);
        await AssertStatusAsync(await scenario.Professor.PostAsJsonAsync("/api/materiais",
            CreateActivity(scenario.AlunoId, scenario.MateriaId), Json), HttpStatusCode.BadRequest);
        await AssertStatusAsync(await scenario.Professor.PostAsJsonAsync($"{chatPath}/mensagens",
            new EnviarMensagemMentoriaRequest("Nova mensagem."), Json), HttpStatusCode.BadRequest);
    }

    private static TutorClassApiFactory CreateFactory(WorkflowChatStore store) => new(services =>
    {
        services.RemoveAll<IMentoriaChatStore>();
        services.AddSingleton<IMentoriaChatStore>(store);
    });

    private static async Task<Workflow> CreateWorkflowAsync(TutorClassApiFactory factory)
    {
        Guid materiaId;
        using (var scope = factory.Services.CreateScope())
        {
            // Only the catalog and administrator are seeded; enrollment and teaching use the public API.
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
            var admin = new Administrador("Administrador Workflow", "admin.workflow@teste.local", "pending");
            admin.DefinirPasswordHash(hasher.HashPassword(admin, Password));
            var materia = new Materia("Matematica Workflow");
            materiaId = materia.Id;
            db.AddRange(admin, materia);
            await db.SaveChangesAsync();
        }

        var professor = factory.CreateClient();
        var professorUsuario = await ReadAsync<UsuarioAutenticadoDto>(
            await professor.PostAsJsonAsync("/api/auth/registrar/professor",
                new RegistrarProfessorRequest("Professor Workflow", "professor.workflow@teste.local", Password,
                    "Professor de matematica e mentor de alunos.", 80m), Json), HttpStatusCode.Created);
        Assert.Equal("Professor", professorUsuario.Perfil);
        await AuthorizeAsync(professor, professorUsuario.Email);
        await AssertStatusAsync(await professor.GetAsync($"/api/professores/{professorUsuario.Id}"), HttpStatusCode.NotFound);
        var catalogo = await ReadAsync<MateriaDto[]>(await professor.GetAsync("/api/materias"));
        Assert.Contains(catalogo, m => m.Id == materiaId);
        await AssertStatusAsync(await professor.PostAsJsonAsync($"/api/professores/{professorUsuario.Id}/materias",
            new VincularMateriaProfessorRequest(materiaId), Json), HttpStatusCode.NoContent);
        var qualificacao = await ReadAsync<QualificacaoProfessorDto>(
            await professor.PostAsJsonAsync($"/api/professores/{professorUsuario.Id}/qualificacoes",
                new AdicionarQualificacaoProfessorRequest(TipoQualificacaoProfessor.Curso, "Ensino de matematica",
                    "Instituicao de teste", null, null, "https://example.com/comprovante.pdf"), Json), HttpStatusCode.Created);
        var candidaturaId = await ReadAsync<Guid>(
            await professor.PostAsJsonAsync($"/api/professores/{professorUsuario.Id}/candidaturas",
                new EnviarCandidaturaProfessorRequest("Quero lecionar matematica e acompanhar alunos."), Json), HttpStatusCode.Created);
        await AssertStatusAsync(await professor.PostAsync($"/api/admin/qualificacoes/{qualificacao.Id}/verificar", null), HttpStatusCode.Forbidden);

        using var adminClient = factory.CreateClient();
        await AuthorizeAsync(adminClient, "admin.workflow@teste.local");
        var verificada = await ReadAsync<QualificacaoProfessorDto>(
            await adminClient.PostAsync($"/api/admin/qualificacoes/{qualificacao.Id}/verificar", null));
        Assert.Equal(StatusVerificacaoQualificacao.Verificada, verificada.StatusVerificacao);
        await AssertStatusAsync(await adminClient.PostAsJsonAsync($"/api/admin/candidaturas-professor/{candidaturaId}/iniciar-analise",
            new AdminCandidaturaDecisionRequest(null), Json), HttpStatusCode.NoContent);
        await AssertStatusAsync(await adminClient.PostAsJsonAsync($"/api/admin/candidaturas-professor/{candidaturaId}/aprovar",
            new AdminCandidaturaDecisionRequest(null), Json), HttpStatusCode.NoContent);
        var candidatura = await ReadAsync<CandidaturaProfessorDto>(
            await professor.GetAsync($"/api/candidaturas-professor/{candidaturaId}"));
        Assert.Equal(StatusCandidaturaProfessor.Aprovada, candidatura.Status);
        var configurado = await ReadAsync<ProfessorDetalhesDto>(
            await professor.PutAsJsonAsync("/api/professores/me/mentoria", new ConfigurarMentoriaProfessorRequest(true, 5), Json));
        Assert.True(configurado.OfereceMentoria);
        var professores = await ReadAsync<ProfessorResumoDto[]>(await professor.GetAsync($"/api/professores?materiaId={materiaId}"));
        Assert.Contains(professores, p => p.Id == professorUsuario.Id && p.OfereceMentoria);

        var aluno = factory.CreateClient();
        var alunoUsuario = await RegisterAlunoAsync(aluno, "aluno.workflow@teste.local");
        var selecionar = new SelecionarMentorRequest(professorUsuario.Id, 1);
        await AssertStatusAsync(await aluno.PostAsJsonAsync("/api/mentorias/me", selecionar, Json), HttpStatusCode.BadRequest);
        var assinatura = await ReadAsync<AssinaturaAlunoResponse>(
            await aluno.PostAsJsonAsync("/api/planos/me/assinatura", new AssinarPlanoRequest(PlanoAluno.Mentoria), Json), HttpStatusCode.Created);
        Assert.True(assinatura.Ativa);
        Assert.Equal(PlanoAluno.Mentoria, assinatura.Plano);
        var mentoria = await ReadAsync<MentoriaResponse>(
            await aluno.PostAsJsonAsync("/api/mentorias/me", selecionar, Json), HttpStatusCode.Created);
        Assert.True(mentoria.Ativo);
        Assert.Equal(alunoUsuario.Id, mentoria.AlunoId);
        Assert.Equal(professorUsuario.Id, mentoria.ProfessorId);
        return new Workflow(professor, aluno, professorUsuario.Id, alunoUsuario.Id, materiaId, mentoria.Id);
    }

    private static CriarMaterialRequest CreateActivity(Guid alunoId, Guid materiaId) => new(
        alunoId, null, materiaId, "Atividade de fracoes", "Resolver e enviar as duvidas no chat.",
        "Exercicio", "fracoes.pdf", "https://example.com/atividades/fracoes.pdf");

    private static async Task<UsuarioAutenticadoDto> RegisterAlunoAsync(HttpClient client, string email)
    {
        var usuario = await ReadAsync<UsuarioAutenticadoDto>(
            await client.PostAsJsonAsync("/api/auth/registrar/aluno",
                new RegistrarAlunoRequest("Aluno Workflow", email, Password, null), Json), HttpStatusCode.Created);
        Assert.Equal("Aluno", usuario.Perfil);
        await AuthorizeAsync(client, email);
        return usuario;
    }

    private static async Task AuthorizeAsync(HttpClient client, string email)
    {
        var login = await ReadAsync<LoginResponse>(
            await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password), Json));
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using (response)
        {
            await EnsureStatusAsync(response, expected);
            var value = await response.Content.ReadFromJsonAsync<T>(Json);
            Assert.NotNull(value);
            return value!;
        }
    }

    private static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        using (response)
        {
            await EnsureStatusAsync(response, expected);
        }
    }

    private static async Task EnsureStatusAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.True(response.StatusCode == expected,
            $"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri}: esperado {(int)expected}, recebido {(int)response.StatusCode}. {await response.Content.ReadAsStringAsync()}");
    }

    private sealed record Workflow(HttpClient Professor, HttpClient Aluno, Guid ProfessorId, Guid AlunoId, Guid MateriaId, Guid MentoriaId) : IDisposable
    {
        public void Dispose()
        {
            Professor.Dispose();
            Aluno.Dispose();
        }
    }

    private sealed class WorkflowChatStore : IMentoriaChatStore
    {
        private readonly Dictionary<Guid, MentoriaChatResponse> _chats = [];
        private readonly DateTime _startedAt = DateTime.UtcNow;
        public List<MensagemMentoriaResponse> Messages { get; } = [];
        public bool Configurado => true;

        public Task<MentoriaChatResponse> ObterOuCriarChatAsync(MentoriaAlunoProfessor vinculo, CancellationToken cancellationToken)
        {
            if (!_chats.TryGetValue(vinculo.Id, out var chat))
            {
                chat = new MentoriaChatResponse(Guid.NewGuid().ToString("N"), vinculo.Id, vinculo.AlunoId,
                    vinculo.ProfessorId, !vinculo.Ativo, DateTime.UtcNow);
                _chats.Add(vinculo.Id, chat);
            }
            return Task.FromResult(chat with { SomenteLeitura = !vinculo.Ativo });
        }

        public Task<MensagemMentoriaResponse> SalvarMensagemAsync(MentoriaChatResponse chat, Guid remetenteUsuarioId,
            string remetenteNome, string conteudo, CancellationToken cancellationToken)
        {
            var mensagem = new MensagemMentoriaResponse(Guid.NewGuid().ToString("N"), chat.Id, chat.VinculoMentoriaId,
                remetenteUsuarioId, remetenteNome, conteudo, _startedAt.AddTicks(Messages.Count), false, null);
            Messages.Add(mensagem);
            return Task.FromResult(mensagem);
        }

        public Task<IReadOnlyCollection<MensagemMentoriaResponse>> ListarMensagensAsync(Guid vinculoMentoriaId, int limit,
            DateTime? beforeUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyCollection<MensagemMentoriaResponse>>(Messages
                .Where(m => m.VinculoMentoriaId == vinculoMentoriaId && (beforeUtc is null || m.DataEnvioUtc < beforeUtc))
                .OrderByDescending(m => m.DataEnvioUtc).Take(limit).OrderBy(m => m.DataEnvioUtc).ToArray());

        public Task<int> MarcarComoLidasAsync(Guid vinculoMentoriaId, Guid leitorUsuarioId, CancellationToken cancellationToken)
        {
            var count = 0;
            for (var index = 0; index < Messages.Count; index++)
            {
                var message = Messages[index];
                if (message.VinculoMentoriaId == vinculoMentoriaId && message.RemetenteUsuarioId != leitorUsuarioId && !message.Lida)
                {
                    Messages[index] = message with { Lida = true, DataLeituraUtc = DateTime.UtcNow };
                    count++;
                }
            }
            return Task.FromResult(count);
        }

        public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }
}
