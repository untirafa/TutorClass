using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Teachers;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class CandidaturaProfessorConfiguration : IEntityTypeConfiguration<CandidaturaProfessor>
{
    public void Configure(EntityTypeBuilder<CandidaturaProfessor> builder)
    {
        builder.ToTable("candidaturas_professor");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Mensagem).HasMaxLength(1000).IsRequired();
        builder.Property(c => c.Motivo).HasMaxLength(1000);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(40);
        builder.Ignore(c => c.EstaAtiva);
        builder.HasIndex(c => new { c.ProfessorId, c.Status });

        builder.HasOne(c => c.Professor)
            .WithMany(p => p.Candidaturas)
            .HasForeignKey(c => c.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
