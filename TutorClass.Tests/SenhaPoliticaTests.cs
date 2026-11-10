using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TutorClass.Application.DTOs;

namespace TutorClass.Tests;

public sealed class SenhaPoliticaTests
{
    private static async Task<HttpResponseMessage> RegistrarAsync(string senha)
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Production");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        return await client.PostAsJsonAsync("/api/auth/registrar/aluno",
            new RegistrarAlunoRequest("Aluno Senha", $"senha{Guid.NewGuid():N}@example.test", senha, null));
    }

    [Theory]
    [InlineData("curta1!")]          // menos de 8 caracteres
    [InlineData("SemNumero!!")]      // sem numero
    [InlineData("12345678!")]        // sem letra
    [InlineData("SenhaSem1234")]     // sem caractere especial
    [InlineData("        ")]         // so espacos
    public async Task Cadastro_recusa_senha_fraca(string senha)
    {
        var resposta = await RegistrarAsync(senha);
        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
        Assert.Contains("Senha deve possuir pelo menos 8 caracteres", await resposta.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("TutorClass@123")]
    [InlineData("Segura#2026")]
    [InlineData("a1_b2-c3")]
    public async Task Cadastro_aceita_senha_com_letra_numero_e_caractere_especial(string senha)
    {
        Assert.Equal(HttpStatusCode.Created, (await RegistrarAsync(senha)).StatusCode);
    }
}
