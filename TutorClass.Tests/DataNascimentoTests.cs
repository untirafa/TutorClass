using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TutorClass.Application.DTOs;

namespace TutorClass.Tests;

public sealed class DataNascimentoTests
{
    private static async Task<HttpResponseMessage> RegistrarAsync(DateOnly? nascimento)
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Production");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var request = new RegistrarAlunoRequest("Aluno Nascimento", $"nasc{Guid.NewGuid():N}@example.test", Guid.NewGuid().ToString(), nascimento);
        return await client.PostAsJsonAsync("/api/auth/registrar/aluno", request);
    }

    [Fact]
    public async Task Cadastro_recusa_data_de_nascimento_futura()
    {
        var resposta = await RegistrarAsync(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(2));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("Data de nascimento nao pode ser futura.", await resposta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Cadastro_recusa_data_de_nascimento_com_mais_de_120_anos()
    {
        var resposta = await RegistrarAsync(DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-130));
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Cadastro_aceita_data_valida_e_ausente()
    {
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(new DateOnly(2010, 5, 20))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(null)).StatusCode);
    }
}
