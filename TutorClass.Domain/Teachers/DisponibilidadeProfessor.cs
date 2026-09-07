using TutorClass.Domain.Common;

namespace TutorClass.Domain.Teachers;

public sealed class DisponibilidadeProfessor : Entity
{
    private DisponibilidadeProfessor()
    {
    }

    public DisponibilidadeProfessor(Guid professorId, DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fim)
    {
        if (fim <= inicio)
        {
            throw new DomainException("Horario final deve ser maior que o inicial.");
        }

        ProfessorId = professorId;
        DiaSemana = diaSemana;
        Inicio = inicio;
        Fim = fim;
    }

    public Guid ProfessorId { get; private set; }
    public Professor? Professor { get; private set; }
    public DayOfWeek DiaSemana { get; private set; }
    public TimeOnly Inicio { get; private set; }
    public TimeOnly Fim { get; private set; }

    public void Atualizar(DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fim)
    {
        if (fim <= inicio)
        {
            throw new DomainException("Horario final deve ser maior que o inicial.");
        }

        DiaSemana = diaSemana;
        Inicio = inicio;
        Fim = fim;
        MarkUpdated();
    }

    public bool Sobrepoe(DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fim)
    {
        return DiaSemana == diaSemana && Inicio < fim && inicio < Fim;
    }
}
