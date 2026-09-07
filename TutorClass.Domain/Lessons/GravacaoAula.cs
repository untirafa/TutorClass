using TutorClass.Domain.Common;

namespace TutorClass.Domain.Lessons;

public sealed class GravacaoAula : Entity
{
    private GravacaoAula()
    {
    }

    public GravacaoAula(Aula aula, string titulo, string urlVideo, int duracaoSegundos, string? descricao = null, string? miniaturaUrl = null)
    {
        if (aula is null)
        {
            throw new DomainException("Aula e obrigatoria.");
        }

        EnsureAulaConcluida(aula);
        AulaId = aula.Id;
        DefinirMetadados(titulo, urlVideo, duracaoSegundos, descricao, miniaturaUrl);
        Disponivel = true;
        DataDisponibilizacaoUtc = DateTime.UtcNow;
    }

    public Guid AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public string UrlVideo { get; private set; } = string.Empty;
    public string? MiniaturaUrl { get; private set; }
    public int DuracaoSegundos { get; private set; }
    public DateTime? DataDisponibilizacaoUtc { get; private set; }
    public bool Disponivel { get; private set; }

    public void AtualizarReferencia(Aula aula, string titulo, string urlVideo, int duracaoSegundos, string? descricao = null, string? miniaturaUrl = null)
    {
        EnsureMesmaAula(aula);
        EnsureAulaConcluida(aula);
        DefinirMetadados(titulo, urlVideo, duracaoSegundos, descricao, miniaturaUrl);

        if (!Disponivel)
        {
            Disponivel = true;
            DataDisponibilizacaoUtc ??= DateTime.UtcNow;
        }

        MarkUpdated();
    }

    public void Desativar()
    {
        if (!Disponivel)
        {
            return;
        }

        Disponivel = false;
        MarkUpdated();
    }

    public void GarantirAcessoDisponivel(Aula aula)
    {
        EnsureMesmaAula(aula);
        EnsureAulaConcluida(aula);

        if (!Disponivel)
        {
            throw new DomainException("Gravacao da aula nao esta disponivel.");
        }
    }

    private void EnsureMesmaAula(Aula aula)
    {
        if (aula is null || aula.Id != AulaId)
        {
            throw new DomainException("Gravacao nao pertence a aula informada.");
        }
    }

    private static void EnsureAulaConcluida(Aula aula)
    {
        if (aula.Status != StatusAula.Concluida)
        {
            throw new DomainException("Gravacao so pode ser disponibilizada para aula concluida.");
        }
    }

    private void DefinirMetadados(string titulo, string urlVideo, int duracaoSegundos, string? descricao, string? miniaturaUrl)
    {
        if (string.IsNullOrWhiteSpace(titulo))
        {
            throw new DomainException("Titulo da gravacao e obrigatorio.");
        }

        if (!IsValidHttpUrl(urlVideo))
        {
            throw new DomainException("URL do video deve ser uma URL HTTP ou HTTPS valida.");
        }

        if (!string.IsNullOrWhiteSpace(miniaturaUrl) && !IsValidHttpUrl(miniaturaUrl))
        {
            throw new DomainException("URL da miniatura deve ser uma URL HTTP ou HTTPS valida.");
        }

        if (duracaoSegundos <= 0)
        {
            throw new DomainException("Duracao da gravacao deve ser maior que zero.");
        }

        Titulo = titulo.Trim();
        UrlVideo = urlVideo.Trim();
        DuracaoSegundos = duracaoSegundos;
        Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        MiniaturaUrl = string.IsNullOrWhiteSpace(miniaturaUrl) ? null : miniaturaUrl.Trim();
    }

    private static bool IsValidHttpUrl(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
