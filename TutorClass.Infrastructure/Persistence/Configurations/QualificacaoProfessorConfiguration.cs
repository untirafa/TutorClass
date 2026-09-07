using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Teachers;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class QualificacaoProfessorConfiguration : IEntityTypeConfiguration<QualificacaoProfessor>
{
    public void Configure(EntityTypeBuilder<QualificacaoProfessor> builder)
    {
        builder.ToTable("qualificacoes_professor");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Tipo).HasConversion<string>().HasMaxLength(40);
        builder.Property(q => q.Titulo).HasMaxLength(180).IsRequired();
        builder.Property(q => q.Instituicao).HasMaxLength(180);
        builder.Property(q => q.Descricao).HasMaxLength(2000);
        builder.Property(q => q.DocumentoComprovacao).HasMaxLength(500);
        builder.Property(q => q.StatusVerificacao).HasConversion<string>().HasMaxLength(40);
        builder.HasIndex(q => new { q.ProfessorId, q.StatusVerificacao });

        builder.HasOne(q => q.Professor)
            .WithMany(p => p.Qualificacoes)
            .HasForeignKey(q => q.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
