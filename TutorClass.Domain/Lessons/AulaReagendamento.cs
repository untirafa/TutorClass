using TutorClass.Domain.Common;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Lessons;

public sealed class AulaReagendamento : Entity
{
    private AulaReagendamento()
    {
    }

    public AulaReagendamento(
        Guid aulaId,
        DateTime inicioAnteriorUtc,
        DateTime fimAnteriorUtc,
        DateTime inicioNovoUtc,
        DateTime fimNovoUtc,
        Guid solicitadoPorUsuarioId)
    {
        if (aulaId == Guid.Empty || solicitadoPorUsuarioId == Guid.Empty)
        {
            throw new DomainException("Aula e solicitante sao obrigatorios.");
        }

        if (inicioNovoUtc <= DateTime.UtcNow)
        {
            throw new DomainException("Novo horario da aula nao pode estar no passado.");
        }

        AulaId = aulaId;
        InicioAnteriorUtc = NormalizeUtc(inicioAnteriorUtc);
        FimAnteriorUtc = NormalizeUtc(fimAnteriorUtc);
        InicioNovoUtc = NormalizeUtc(inicioNovoUtc);
        FimNovoUtc = NormalizeUtc(fimNovoUtc);
        SolicitadoPorUsuarioId = solicitadoPorUsuarioId;
    }

    public Guid AulaId { get; private set; }
    public Aula? Aula { get; private set; }
    public DateTime InicioAnteriorUtc { get; private set; }
    public DateTime FimAnteriorUtc { get; private set; }
    public DateTime InicioNovoUtc { get; private set; }
    public DateTime FimNovoUtc { get; private set; }
    public Guid SolicitadoPorUsuarioId { get; private set; }
    public Usuario? SolicitadoPorUsuario { get; private set; }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
