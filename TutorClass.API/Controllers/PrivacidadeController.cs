using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorClass.Application.DTOs;

namespace TutorClass.API.Controllers;

[ApiController]
[Route("api/privacidade")]
public sealed class PrivacidadeController(IConfiguration configuration) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("documentos")]
    [ProducesResponseType(typeof(PrivacidadeResponse), StatusCodes.Status200OK)]
    public PrivacidadeResponse Documentos()
    {
        var result = new List<DocumentoLegalResponse>();
        foreach (var tipo in new[] { "TermosUso", "PoliticaPrivacidade" })
        {
            var section = configuration.GetSection("DocumentosLegais:" + tipo);
            if (section.GetValue<bool>("Publicado") && !string.IsNullOrWhiteSpace(section["Versao"]) &&
                Uri.TryCreate(section["Url"], UriKind.Absolute, out var url) && url.Scheme == "https" && string.IsNullOrEmpty(url.UserInfo))
                result.Add(new(tipo, section["Versao"]!, url.AbsoluteUri));
        }
        // Only explicitly approved public text belongs in this configuration section.
        return new(result, configuration["DocumentosLegais:ContatoPrivacidade"],
            configuration["DocumentosLegais:Orientacoes"], false);
    }
}
