using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Mentoring;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class AcompanhamentoMentoriaConfiguration : IEntityTypeConfiguration<AcompanhamentoMentoria>
{
    public void Configure(EntityTypeBuilder<AcompanhamentoMentoria> builder)
    {
        builder.ToTable("acompanhamentos_mentoria");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Resumo).HasMaxLength(2000);
        builder.Property(a => a.PontosFortes).HasMaxLength(1000);
        builder.Property(a => a.PontosMelhorar).HasMaxLength(1000);
        builder.Property(a => a.ProximosPassos).HasMaxLength(1000);
        builder.Property(a => a.MetaAtual).HasMaxLength(1000);
        builder.Property(a => a.Observacoes).HasMaxLength(1500);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(a => a.AulaId).IsUnique();
        builder.HasIndex(a => new { a.AlunoId, a.CreatedAtUtc });
        builder.HasIndex(a => new { a.ProfessorId, a.CreatedAtUtc });
        builder.HasIndex(a => new { a.ProfessorId, a.Status });

        builder.HasOne(a => a.Aula).WithOne().HasForeignKey<AcompanhamentoMentoria>(a => a.AulaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Aluno).WithMany().HasForeignKey(a => a.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Professor).WithMany().HasForeignKey(a => a.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.VinculoMentoria).WithMany().HasForeignKey(a => a.VinculoMentoriaId).OnDelete(DeleteBehavior.Restrict);
    }
}
