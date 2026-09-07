using TutorClass.Domain.Common;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Plans;

public sealed class UsoIaAluno : Entity
{
    private UsoIaAluno()
    {
    }

    public UsoIaAluno(Guid alunoId, DateOnly data)
    {
        if (alunoId == Guid.Empty)
        {
            throw new DomainException("Aluno e obrigatorio.");
        }

        AlunoId = alunoId;
        Data = data;
    }

    public Guid AlunoId { get; private set; }
    public Aluno? Aluno { get; private set; }
    public DateOnly Data { get; private set; }
    public int Quantidade { get; private set; }

    public void RegistrarUso()
    {
        Quantidade++;
        MarkUpdated();
    }
}
