using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Teachers;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class ProfessorConfiguration : IEntityTypeConfiguration<Professor>
{
    public void Configure(EntityTypeBuilder<Professor> builder)
    {
        builder.Property(p => p.Biografia).HasMaxLength(2000);
        builder.Property(p => p.PrecoHora).HasPrecision(10, 2);
        builder.Property(p => p.MediaAvaliacoes).HasPrecision(3, 2);
        builder.Property(p => p.LimiteMentorados).HasDefaultValue(2);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(40);
        builder.Property(p => p.NivelReputacao).HasConversion<string>().HasMaxLength(40);
        builder.Navigation(p => p.Materias).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Qualificacoes).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Candidaturas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Disponibilidades).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.OfereceMentoria);
    }
}
