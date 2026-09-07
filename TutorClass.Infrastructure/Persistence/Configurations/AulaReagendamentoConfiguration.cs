using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Lessons;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class AulaReagendamentoConfiguration : IEntityTypeConfiguration<AulaReagendamento>
{
    public void Configure(EntityTypeBuilder<AulaReagendamento> builder)
    {
        builder.ToTable("aulas_reagendamentos");
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.AulaId, r.CreatedAtUtc });
        builder.HasIndex(r => r.SolicitadoPorUsuarioId);

        builder.HasOne(r => r.Aula).WithMany().HasForeignKey(r => r.AulaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(r => r.SolicitadoPorUsuario).WithMany().HasForeignKey(r => r.SolicitadoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
