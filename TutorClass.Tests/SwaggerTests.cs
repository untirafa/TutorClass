using System.Net;
using System.Text.Json;

namespace TutorClass.Tests;

public sealed class SwaggerTests
{
    [Theory]
    [InlineData("/api/auth/login", "post")]
    [InlineData("/api/auth/registrar/aluno", "post")]
    [InlineData("/api/auth/registrar/professor", "post")]
    [InlineData("/api/auth/registrar/responsavel", "post")]
    [InlineData("/api/materias", "get")]
    [InlineData("/api/financeiro/aulas/comissao", "get")]
    [InlineData("/health", "get")]
    public async Task Endpoints_publicos_nao_exigem_Bearer_no_documento(string path, string method)
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method);

        Assert.Empty(GetEffectiveSecurity(document.RootElement, operation).EnumerateArray());
    }

    [Theory]
    [InlineData("/api/auth/me", "get")]
    [InlineData("/api/financeiro/aulas/{aulaId}/pagar", "post")]
    public async Task Endpoints_protegidos_exigem_token_Bearer_no_documento(string path, string method)
    {
        await using var factory = new TutorClassApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var scheme = root.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());

        var operation = root.GetProperty("paths").GetProperty(path).GetProperty(method);
        var requirement = Assert.Single(GetEffectiveSecurity(root, operation).EnumerateArray());
        Assert.Empty(requirement.GetProperty("Bearer").EnumerateArray());
    }

    private static JsonElement GetEffectiveSecurity(JsonElement document, JsonElement operation)
    {
        return operation.TryGetProperty("security", out var security)
            ? security
            : document.GetProperty("security");
    }
}
