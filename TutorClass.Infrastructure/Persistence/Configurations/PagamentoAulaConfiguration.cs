using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Financial;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class PagamentoAulaConfiguration : IEntityTypeConfiguration<PagamentoAula>
{
    public void Configure(EntityTypeBuilder<PagamentoAula> builder)
    {
        builder.ToTable("pagamentos_aula");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ValorBruto).HasPrecision(10, 2);
        builder.Property(p => p.PercentualComissao).HasPrecision(5, 2);
        builder.Property(p => p.ValorComissao).HasPrecision(10, 2);
        builder.Property(p => p.ValorLiquidoProfessor).HasPrecision(10, 2);
        builder.Property(p => p.FormaPagamento).HasConversion<string>().HasMaxLength(40);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(p => p.AulaId).IsUnique();
        builder.HasIndex(p => new { p.AlunoId, p.CreatedAtUtc });
        builder.HasIndex(p => new { p.PagadorUsuarioId, p.CreatedAtUtc });
        builder.HasIndex(p => new { p.ProfessorId, p.CreatedAtUtc });
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Aula).WithOne().HasForeignKey<PagamentoAula>(p => p.AulaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Aluno).WithMany().HasForeignKey(p => p.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.PagadorUsuario).WithMany().HasForeignKey(p => p.PagadorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(p => p.Professor).WithMany().HasForeignKey(p => p.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}
