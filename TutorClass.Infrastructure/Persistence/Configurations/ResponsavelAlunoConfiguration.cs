using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Users;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class ResponsavelAlunoConfiguration : IEntityTypeConfiguration<ResponsavelAluno>
{
    public void Configure(EntityTypeBuilder<ResponsavelAluno> builder)
    {
        builder.ToTable("responsaveis_alunos");
        builder.HasKey(ra => ra.Id);
        builder.Property(ra => ra.Parentesco).HasMaxLength(80).IsRequired();
        builder.HasIndex(ra => new { ra.ResponsavelId, ra.AlunoId }).IsUnique();

        builder.HasOne(ra => ra.Responsavel)
            .WithMany(r => r.Alunos)
            .HasForeignKey(ra => ra.ResponsavelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ra => ra.Aluno)
            .WithMany(a => a.Responsaveis)
            .HasForeignKey(ra => ra.AlunoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
