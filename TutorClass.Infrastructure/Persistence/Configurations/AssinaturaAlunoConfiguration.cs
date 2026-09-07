using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Plans;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class AssinaturaAlunoConfiguration : IEntityTypeConfiguration<AssinaturaAluno>
{
    public void Configure(EntityTypeBuilder<AssinaturaAluno> builder)
    {
        builder.ToTable("assinaturas_aluno");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Plano).HasConversion<string>().HasMaxLength(40);
        builder.Property(a => a.ValorMensal).HasPrecision(10, 2);
        builder.Property(a => a.ValorCobradoPrimeiroMes).HasPrecision(10, 2);

        builder.HasIndex(a => new { a.AlunoId, a.Ativa });
        builder.HasIndex(a => a.PagadorUsuarioId);
        builder.HasIndex(a => new { a.AlunoId, a.UsouPromocaoPrimeiraAssinatura });

        builder.HasOne(a => a.Aluno).WithMany().HasForeignKey(a => a.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.PagadorUsuario).WithMany().HasForeignKey(a => a.PagadorUsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
