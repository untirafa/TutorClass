using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Teachers;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class ProfessorMateriaConfiguration : IEntityTypeConfiguration<ProfessorMateria>
{
    public void Configure(EntityTypeBuilder<ProfessorMateria> builder)
    {
        builder.ToTable("professores_materias");
        builder.HasKey(pm => pm.Id);
        builder.HasIndex(pm => new { pm.ProfessorId, pm.MateriaId }).IsUnique();

        builder.HasOne(pm => pm.Professor)
            .WithMany(p => p.Materias)
            .HasForeignKey(pm => pm.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pm => pm.Materia)
            .WithMany(m => m.Professores)
            .HasForeignKey(pm => pm.MateriaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
