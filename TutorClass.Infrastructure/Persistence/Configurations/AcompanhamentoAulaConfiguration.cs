using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Lessons;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class AcompanhamentoAulaConfiguration : IEntityTypeConfiguration<AcompanhamentoAula>
{
    public void Configure(EntityTypeBuilder<AcompanhamentoAula> builder)
    {
        builder.ToTable("acompanhamentos_aula");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Resumo).HasMaxLength(2000).IsRequired();
        builder.Property(a => a.PontosFortes).HasMaxLength(1000);
        builder.Property(a => a.PontosMelhorar).HasMaxLength(1000);
        builder.Property(a => a.ProximosPassos).HasMaxLength(1000);

        builder.HasIndex(a => a.AulaId).IsUnique();
        builder.HasIndex(a => new { a.AlunoId, a.CreatedAtUtc });
        builder.HasIndex(a => new { a.ProfessorId, a.CreatedAtUtc });

        builder.HasOne(a => a.Aula).WithOne().HasForeignKey<AcompanhamentoAula>(a => a.AulaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Aluno).WithMany().HasForeignKey(a => a.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Professor).WithMany().HasForeignKey(a => a.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}
