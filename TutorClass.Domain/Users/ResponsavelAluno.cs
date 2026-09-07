using TutorClass.Domain.Common;

namespace TutorClass.Domain.Users;

public sealed class ResponsavelAluno : Entity
{
    private ResponsavelAluno()
    {
    }

    public ResponsavelAluno(Guid responsavelId, Guid alunoId, string parentesco)
    {
        ResponsavelId = responsavelId;
        AlunoId = alunoId;
        Parentesco = string.IsNullOrWhiteSpace(parentesco) ? "Nao informado" : parentesco.Trim();
    }

    public Guid ResponsavelId { get; private set; }
    public Responsavel? Responsavel { get; private set; }
    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public string Parentesco { get; private set; } = string.Empty;
}
