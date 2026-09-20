using Microsoft.AspNetCore.DataProtection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public sealed class AuthFlowTests
{
    [Fact]
    public async Task Senha_nao_e_armazenada_em_texto_puro()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var senha = "TutorClass@123";

        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Teste", "aluno@teste.local", senha, null));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var usuario = db.Usuarios.Single(u => u.Email == "aluno@teste.local");
        Assert.NotEqual(senha, usuario.PasswordHash);
        Assert.StartsWith("AQAAAA", usuario.PasswordHash);
    }

    [Fact]
    public async Task Login_valido_retorna_token()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Teste", "login@teste.local", "TutorClass@123", null));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("login@teste.local", "TutorClass@123"));
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(login!.AccessToken));
        Assert.Equal("Aluno", login.Usuario.Perfil);
    }

    [Fact]
    public async Task Cadastro_de_professor_recebe_senha_e_armazena_hash()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var senha = "TutorClass@123";

        var response = await client.PostAsJsonAsync(
            "/api/auth/registrar/professor",
            new RegistrarProfessorRequest("Professor Novo", "professor.novo@teste.local", senha, "Professor candidato para teste.", 80));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var professor = db.Professores.Single(p => p.Email == "professor.novo@teste.local");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(StatusProfessor.Candidato, professor.Status);
        Assert.NotEqual(senha, professor.PasswordHash);
        Assert.StartsWith("AQAAAA", professor.PasswordHash);
    }

    [Fact]
    public async Task Login_invalido_falha()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Teste", "invalido@teste.local", "TutorClass@123", null));

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("invalido@teste.local", "senha-errada"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Email_duplicado_e_rejeitado()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var request = new RegistrarAlunoRequest("Aluno Teste", "duplicado@teste.local", "TutorClass@123", null);

        await client.PostAsJsonAsync("/api/auth/registrar/aluno", request);
        var response = await client.PostAsJsonAsync("/api/auth/registrar/aluno", request with { Email = "DUPLICADO@teste.local" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Endpoint_administrativo_rejeita_usuario_sem_autenticacao()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/admin/qualificacoes/{Guid.NewGuid()}/verificar", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Aluno_nao_consegue_executar_endpoint_administrativo()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Teste", "aluno.admin@teste.local", "TutorClass@123", null));
        await AuthorizeAsync(client, "aluno.admin@teste.local", "TutorClass@123");

        var response = await client.PostAsync($"/api/admin/qualificacoes/{Guid.NewGuid()}/verificar", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Professor_nao_consegue_executar_endpoint_administrativo()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await factory.SeedProfessorAsync("professor.admin@teste.local");
        await AuthorizeAsync(client, "professor.admin@teste.local", "TutorClass@123");

        var response = await client.PostAsync($"/api/admin/qualificacoes/{Guid.NewGuid()}/verificar", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrador_consegue_executar_endpoint_autorizado()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var qualificacaoId = await factory.SeedAdminProfessorAndQualificationAsync();
        await AuthorizeAsync(client, "admin@teste.local", "TutorClass@123");

        var response = await client.PostAsync($"/api/admin/qualificacoes/{qualificacaoId}/verificar", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Usuario_autenticado_consegue_consultar_me()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Me", "me@teste.local", "TutorClass@123", null));
        await AuthorizeAsync(client, "me@teste.local", "TutorClass@123");

        var response = await client.GetAsync("/api/auth/me");
        var me = await response.Content.ReadFromJsonAsync<UsuarioAutenticadoDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("me@teste.local", me!.Email);
    }

    [Fact]
    public async Task Professor_nao_consegue_agir_em_nome_de_outro_professor()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var professorA = await factory.SeedProfessorAsync("professor.a@teste.local");
        var professorB = await factory.SeedProfessorAsync("professor.b@teste.local");
        await AuthorizeAsync(client, "professor.a@teste.local", "TutorClass@123");

        var response = await client.PostAsJsonAsync(
            $"/api/professores/{professorB}/qualificacoes",
            new AdicionarQualificacaoProfessorRequest(TipoQualificacaoProfessor.Curso, "Curso", "Instituicao", null, null, null));

        Assert.NotEqual(professorA, professorB);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Aluno_nao_consulta_candidatura_privada_de_professor()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var dados = await factory.SeedPrivateProfessorDataAsync();
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Privacidade", "aluno.privacidade@teste.local", "TutorClass@123", null));
        await AuthorizeAsync(client, "aluno.privacidade@teste.local", "TutorClass@123");

        var response = await client.GetAsync($"/api/candidaturas-professor/{dados.CandidaturaId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Aluno_nao_consulta_qualificacoes_privadas_de_professor()
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var dados = await factory.SeedPrivateProfessorDataAsync();
        await client.PostAsJsonAsync("/api/auth/registrar/aluno", new RegistrarAlunoRequest("Aluno Qualificacao", "aluno.qualificacao@teste.local", "TutorClass@123", null));
        await AuthorizeAsync(client, "aluno.qualificacao@teste.local", "TutorClass@123");

        var response = await client.GetAsync($"/api/professores/{dados.ProfessorId}/qualificacoes");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task AuthorizeAsync(HttpClient client, string email, string senha)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, senha));
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }
}

public sealed class TutorClassApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly Action<IServiceCollection>? _configureTestServices;
    private readonly string _environmentName;

    public TutorClassApiFactory(Action<IServiceCollection>? configureTestServices = null, string environmentName = "Testing")
    {
        _configureTestServices = configureTestServices;
        _environmentName = environmentName;
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TutorClass.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TutorClass.Tests");
        Environment.SetEnvironmentVariable("Jwt__Secret", "tests-only-secret-with-more-than-thirty-two-characters");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environmentName);
        // Registro Npgsql válido, substituído pelo banco InMemory antes de qualquer consulta.
        if (_environmentName != "Testing")
            builder.UseSetting("ConnectionStrings:TutorClassDb", "Host=127.0.0.1;Database=unused;Username=unused");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["TutorIA:Provider"] = "Simulado" }));

        builder.ConfigureTestServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration<TutorClassDbContext>>();
            services.RemoveAll<DbContextOptions<TutorClassDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDatabaseProvider>();
            services.RemoveAll<TutorClassDbContext>();
            services.RemoveAll<IApplicationDbContext>();
            services.AddDbContext<TutorClassDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TutorClassDbContext>());
            _configureTestServices?.Invoke(services);
        });
    }

    public async Task<Guid> SeedProfessorAsync(string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var professor = new Professor("Professor Teste", email, "pending");
        professor.DefinirPasswordHash(hasher.HashPassword(professor, "TutorClass@123"));
        professor.AtualizarPerfilProfissional("Professor para teste de autorizacao.", 80);
        db.Add(professor);
        await db.SaveChangesAsync();
        return professor.Id;
    }

    public async Task<Guid> SeedAdminProfessorAndQualificationAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var admin = new Administrador("Admin Teste", "admin@teste.local", "pending");
        admin.DefinirPasswordHash(hasher.HashPassword(admin, "TutorClass@123"));
        var professor = new Professor("Professor Teste", "professor.qualificacao@teste.local", "pending");
        professor.DefinirPasswordHash(hasher.HashPassword(professor, "TutorClass@123"));
        professor.AtualizarPerfilProfissional("Professor para teste de qualificacao.", 80);
        var qualificacao = professor.AdicionarQualificacao(TipoQualificacaoProfessor.Curso, "Curso Teste", "Instituicao", null, null, null);
        db.AddRange(admin, professor, qualificacao);
        await db.SaveChangesAsync();
        return qualificacao.Id;
    }

    public async Task<PrivateProfessorData> SeedPrivateProfessorDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
        var professor = new Professor("Professor Privado", "professor.privado@teste.local", "pending");
        professor.DefinirPasswordHash(hasher.HashPassword(professor, "TutorClass@123"));
        professor.AtualizarPerfilProfissional("Professor para teste de privacidade.", 80);
        var qualificacao = professor.AdicionarQualificacao(TipoQualificacaoProfessor.Curso, "Curso Privado", "Instituicao", null, null, "https://example.com/privado.pdf");
        var candidatura = professor.SolicitarCandidatura("Candidatura privada.");

        db.AddRange(professor, qualificacao, candidatura);
        await db.SaveChangesAsync();
        return new PrivateProfessorData(professor.Id, candidatura.Id, qualificacao.Id);
    }
}

public sealed record PrivateProfessorData(Guid ProfessorId, Guid CandidaturaId, Guid QualificacaoId);
