using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Teachers;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class AvaliacaoProfessorConfiguration : IEntityTypeConfiguration<AvaliacaoProfessor>
{
    public void Configure(EntityTypeBuilder<AvaliacaoProfessor> builder)
    {
        builder.ToTable("avaliacoes_professor", table =>
            table.HasCheckConstraint("ck_avaliacoes_professor_nota", "nota >= 1 AND nota <= 5"));
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Comentario).HasMaxLength(1500);

        builder.HasIndex(a => a.AulaId).IsUnique();
        builder.HasIndex(a => new { a.ProfessorId, a.CreatedAtUtc });
        builder.HasIndex(a => a.AlunoId);

        builder.HasOne(a => a.Aula).WithOne().HasForeignKey<AvaliacaoProfessor>(a => a.AulaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Aluno).WithMany().HasForeignKey(a => a.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Professor).WithMany().HasForeignKey(a => a.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}
