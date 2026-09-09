using System.Collections;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Tests;

public class AulasMinhasRegressionTests
{
    [Fact]
    public async Task Aluno_lista_somente_suas_aulas_sem_comando_durante_reader_aberto()
    {
        await using var factory = new TutorClassApiFactory(services =>
            services.AddScoped<IApplicationDbContext>(sp =>
            {
                var proxy = DispatchProxy.Create<IApplicationDbContext, ReaderGuard>();
                ((ReaderGuard)(object)proxy).Db = sp.GetRequiredService<TutorClassDbContext>();
                return proxy;
            }));
        using var client = factory.CreateClient();
        Guid aulaId, alunoId;
        var inicio = new DateTime(2030, 1, 2, 18, 0, 0, DateTimeKind.Utc);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
            var aluno = new Aluno("Aluno dono", "aulas.regressao@test.local", "hash");
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>();
            aluno.DefinirPasswordHash(hasher.HashPassword(aluno, "TutorClass@123"));
            var outro = new Aluno("Outro aluno privado", "outro.regressao@test.local", "hash");
            var professor = new Professor("Professor real", "prof.regressao@test.local", "hash");
            var materia = new Materia("Matematica");
            var aula = new Aula(aluno.Id, professor.Id, materia.Id, inicio, 60, 50);
            aula.Confirmar(professor.Id);
            db.AddRange(aluno, outro, professor, materia, aula,
                new Aula(outro.Id, professor.Id, materia.Id, inicio.AddDays(-1), 60, 50));
            await db.SaveChangesAsync(); aulaId = aula.Id; alunoId = aluno.Id;
        }
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("aulas.regressao@test.local", "TutorClass@123"));
        var auth = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var response = await client.GetAsync("/api/aulas/minhas");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Outro aluno privado", json);
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var item = Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(aulaId, item.GetProperty("id").GetGuid());
        Assert.Equal(alunoId, item.GetProperty("solicitadaPorUsuarioId").GetGuid());
        Assert.Equal("Professor real", item.GetProperty("professor").GetString());
        Assert.Equal("Matematica", item.GetProperty("materia").GetString());
        Assert.Equal(inicio, item.GetProperty("inicio").GetDateTime());
        Assert.Equal(60, item.GetProperty("duracaoMinutos").GetInt32());
        Assert.Equal("Confirmada", item.GetProperty("status").GetString());
    }

    // EF InMemory permits nested queries unlike Npgsql. Model the open-reader restriction
    // so the original AsEnumerable -> PossuiVinculo bug fails this HTTP regression test.
    public class ReaderGuard : DispatchProxy
    {
        public IApplicationDbContext Db = null!;
        private bool reading;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (reading) throw new InvalidOperationException("A command is already in progress (regression reader guard).");
            var result = method!.Invoke(Db, args);
                        return method.Name switch
            {
                "get_Aulas" => Enumerate((IQueryable<Aula>)result!).AsQueryable(),
                "get_Usuarios" => ((IQueryable<Usuario>)result!).ToArray().AsQueryable(),
                "get_Professores" => ((IQueryable<Professor>)result!).ToArray().AsQueryable(),
                "get_Materias" => ((IQueryable<Materia>)result!).ToArray().AsQueryable(),
                "get_ResponsaveisAlunos" => ((IQueryable<ResponsavelAluno>)result!).ToArray().AsQueryable(),
                _ => result
            };
        }
        private IEnumerable<Aula> Enumerate(IQueryable<Aula> source)
        {
            reading = true;
            try { foreach (var aula in source) yield return aula; }
            finally { reading = false; }
        }
    }
}
