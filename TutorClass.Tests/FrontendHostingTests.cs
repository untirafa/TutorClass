using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TutorClass.Application.DTOs;

namespace TutorClass.Tests;

public sealed class FrontendHostingTests
{
    [Theory]
    [InlineData("/","text/html")]
    [InlineData("/index.html","text/html")]
    [InlineData("/style.css","text/css")]
    [InlineData("/config.js","text/javascript")]
    [InlineData("/api.js","text/javascript")]
    [InlineData("/app.js","text/javascript")]
    [InlineData("/tutor-text.js","text/javascript")]
    public async Task Production_entrega_arquivos_publicos(string path, string mediaType)
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Production");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        Assert.True(factory.Services.GetRequiredService<IWebHostEnvironment>().IsProduction());
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(body);
        if (path == "/") Assert.Contains("id=\"auth-form\"", body);
        if (path == "/config.js") {
            Assert.Contains("window.location.origin", body);
            Assert.DoesNotContain("onrender.com", body);
        }
    }

    [Theory]
    [InlineData("/api/inexistente")]
    [InlineData("/swagger/inexistente")]
    [InlineData("/health/inexistente")]
    [InlineData("/appsettings.json")]
    [InlineData("/.git/config")]
    [InlineData("/.local/tutorclass.db")]
    [InlineData("/missing.js")]
    [InlineData("/login")]
    public async Task Production_nao_substitui_404_por_HTML_nem_expoe_arquivos_privados(string path)
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Production");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("id=\"auth-form\"", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/health","application/json")]
    [InlineData("/swagger","text/html")]
    [InlineData("/swagger/v1/swagger.json","application/json")]
    public async Task Production_preserva_health_e_swagger(string path, string mediaType)
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Production");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var response = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Production_preserva_cadastro_login_me_e_erro_real_na_mesma_origem()
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Production");
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        var password = Guid.NewGuid().ToString();
        var registration = new RegistrarAlunoRequest("Teste Publicação", "hosting@example.test", password, null);
        var created = await client.PostAsJsonAsync("/api/auth/registrar/aluno", registration);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var logged = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(registration.Email, password));
        Assert.Equal(HttpStatusCode.OK, logged.StatusCode);
        var login = await logged.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", login!.AccessToken);
        var me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(registration.Email, (await me.Content.ReadFromJsonAsync<UsuarioAutenticadoDto>())!.Email);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        var error = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(registration.Email, Guid.NewGuid().ToString()));
        Assert.Equal(HttpStatusCode.BadRequest, error.StatusCode);
        Assert.Contains("Email ou senha invalidos.", await error.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Production_ignora_CORS_local_mesmo_se_variavel_antiga_existir()
    {
        await using var original = new TutorClassApiFactory(environmentName: "Production");
        await using var factory = original.WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5242"));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:5242");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        var response = await client.SendAsync(request);
        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Development_entrega_site_com_a_API_local()
    {
        await using var factory = new TutorClassApiFactory(environmentName: "Development");
        var client = factory.CreateClient();
        Assert.True(factory.Services.GetRequiredService<IWebHostEnvironment>().IsDevelopment());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        Assert.Contains("window.location.origin", await client.GetStringAsync("/config.js"));
    }
}

