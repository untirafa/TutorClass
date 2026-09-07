using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Financial;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class CobrancaAssinaturaConfiguration : IEntityTypeConfiguration<CobrancaAssinatura>
{
    public void Configure(EntityTypeBuilder<CobrancaAssinatura> builder)
    {
        builder.ToTable("cobrancas_assinatura");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Valor).HasPrecision(10, 2);
        builder.Property(c => c.FormaPagamento).HasConversion<string>().HasMaxLength(40);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(c => new { c.AssinaturaId, c.AnoCompetencia, c.MesCompetencia }).IsUnique();
        builder.HasIndex(c => new { c.PagadorUsuarioId, c.CreatedAtUtc });
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Assinatura).WithMany().HasForeignKey(c => c.AssinaturaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(c => c.PagadorUsuario).WithMany().HasForeignKey(c => c.PagadorUsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
