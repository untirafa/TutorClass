using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Mentoring;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class AdicionalMentoriaConfiguration : IEntityTypeConfiguration<AdicionalMentoria>
{
    public void Configure(EntityTypeBuilder<AdicionalMentoria> builder)
    {
        builder.ToTable("adicionais_mentoria");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Valor).HasPrecision(10, 2);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(a => a.AcompanhamentoMentoriaId).IsUnique();
        builder.HasIndex(a => new { a.ProfessorId, a.AlunoId, a.DataGeracaoUtc });

        builder.HasOne(a => a.Professor).WithMany().HasForeignKey(a => a.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Aluno).WithMany().HasForeignKey(a => a.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Aula).WithMany().HasForeignKey(a => a.AulaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AcompanhamentoMentoria).WithOne().HasForeignKey<AdicionalMentoria>(a => a.AcompanhamentoMentoriaId).OnDelete(DeleteBehavior.Cascade);
    }
}
