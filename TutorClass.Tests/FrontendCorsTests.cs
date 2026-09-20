using System.Net;
using System.Net.Http.Json;

namespace TutorClass.Tests;

public sealed class FrontendCorsTests
{
    [Theory]
    [InlineData("http://localhost:5242")]
    [InlineData("http://127.0.0.1:5242")]
    public async Task Frontend_local_pode_enviar_Json_e_Bearer(string origin)
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Development");
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        var headers = string.Join(",", response.Headers.GetValues("Access-Control-Allow-Headers"));
        Assert.Contains("content-type", headers);
        Assert.Contains("authorization", headers);
    }

    [Theory]
    [InlineData("https://untrusted.example")]
    [InlineData("http://localhost:9999")]
    [InlineData("null")]
    public async Task Outra_origem_nao_recebe_permissao(string origin)
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Development");
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        var response = await client.SendAsync(request);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Erro_de_login_permanece_legivel_para_o_frontend()
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Development");
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:5242");
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "nonexistent@example.test", senha = Guid.NewGuid().ToString() });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("http://localhost:5242", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Contains("Email ou senha invalidos.", await response.Content.ReadAsStringAsync());
    }
}

