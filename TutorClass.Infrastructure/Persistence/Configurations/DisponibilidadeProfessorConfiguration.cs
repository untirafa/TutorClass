using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Teachers;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class DisponibilidadeProfessorConfiguration : IEntityTypeConfiguration<DisponibilidadeProfessor>
{
    public void Configure(EntityTypeBuilder<DisponibilidadeProfessor> builder)
    {
        builder.ToTable("disponibilidades_professor");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DiaSemana).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(d => new { d.ProfessorId, d.DiaSemana, d.Inicio, d.Fim })
            .HasDatabaseName("ix_disponibilidades_professor_periodo")
            .IsUnique();

        builder.HasOne(d => d.Professor)
            .WithMany(p => p.Disponibilidades)
            .HasForeignKey(d => d.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
