using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Materials;

public sealed class MaterialPedagogico : Entity
{
    private MaterialPedagogico()
    {
    }

    public MaterialPedagogico(
        Guid professorId,
        Guid alunoId,
        Guid materiaId,
        Guid? aulaId,
        string titulo,
        string? descricao,
        string tipo,
        string? nomeArquivo,
        string urlArquivo)
    {
        if (professorId == Guid.Empty)
        {
            throw new DomainException("Professor e obrigatorio.");
        }

        if (alunoId == Guid.Empty)
        {
            throw new DomainException("Aluno e obrigatorio.");
        }

        if (materiaId == Guid.Empty)
        {
            throw new DomainException("Materia e obrigatoria.");
        }

        ProfessorId = professorId;
        AlunoId = alunoId;
        MateriaId = materiaId;
        AulaId = aulaId;
        Atualizar(titulo, descricao, tipo, nomeArquivo, urlArquivo);
        UpdatedAtUtc = null;
    }

    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public Guid MateriaId { get; private set; }
    public Materia? Materia { get; private set; }
    public Guid? AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public string? NomeArquivo { get; private set; }
    public string UrlArquivo { get; private set; } = string.Empty;

    public void Atualizar(string titulo, string? descricao, string tipo, string? nomeArquivo, string urlArquivo)
    {
        Titulo = NormalizeRequired(titulo, "Titulo", 160);
        Descricao = NormalizeOptional(descricao, "Descricao", 1000);
        Tipo = NormalizeRequired(tipo, "Tipo", 60);
        NomeArquivo = NormalizeOptional(nomeArquivo, "Nome do arquivo", 255);
        UrlArquivo = NormalizeRequired(urlArquivo, "URL ou referencia do arquivo", 2000);

        if (!IsValidReference(UrlArquivo))
        {
            throw new DomainException("URL ou referencia do arquivo deve ser uma URL http/https valida ou uma referencia externa simples.");
        }

        MarkUpdated();
    }

    private static string NormalizeRequired(string value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} e obrigatorio.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} deve ter no maximo {maxLength} caracteres.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} deve ter no maximo {maxLength} caracteres.");
        }

        return normalized;
    }

    private static bool IsValidReference(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.Scheme is "http" or "https";
        }

        return value.Length >= 3 &&
            value.Length <= 2000 &&
            value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '/' or '.' or ':');
    }
}
