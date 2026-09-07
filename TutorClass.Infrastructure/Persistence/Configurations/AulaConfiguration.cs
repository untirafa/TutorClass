using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Lessons;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class AulaConfiguration : IEntityTypeConfiguration<Aula>
{
    public void Configure(EntityTypeBuilder<Aula> builder)
    {
        builder.ToTable("aulas");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(a => a.PrecoAcordado).HasPrecision(10, 2);
        builder.Property(a => a.MotivoCancelamento).HasMaxLength(1000);
        builder.Ignore(a => a.FimUtc);

        builder.HasIndex(a => new { a.AlunoId, a.InicioUtc, a.Status });
        builder.HasIndex(a => a.SolicitadaPorUsuarioId);
        builder.HasIndex(a => new { a.ProfessorId, a.InicioUtc, a.Status });
        builder.HasIndex(a => new { a.ProfessorId, a.MateriaId });

        builder.HasOne(a => a.Aluno)
            .WithMany()
            .HasForeignKey(a => a.AlunoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SolicitadaPorUsuario)
            .WithMany()
            .HasForeignKey(a => a.SolicitadaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Professor)
            .WithMany()
            .HasForeignKey(a => a.ProfessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Materia)
            .WithMany()
            .HasForeignKey(a => a.MateriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
