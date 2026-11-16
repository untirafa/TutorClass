using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Users;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class SolicitacaoVinculoResponsavelConfiguration : IEntityTypeConfiguration<SolicitacaoVinculoResponsavel>
{
    public void Configure(EntityTypeBuilder<SolicitacaoVinculoResponsavel> builder)
    {
        builder.ToTable("solicitacoes_vinculo");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.NomeAlunoInformado).HasMaxLength(SolicitacaoVinculoResponsavel.NomeAlunoMaxLength).IsRequired();
        builder.Property(s => s.Parentesco).HasMaxLength(SolicitacaoVinculoResponsavel.ParentescoMaxLength).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Motivo).HasMaxLength(SolicitacaoVinculoResponsavel.MotivoMaxLength);

        builder.HasIndex(s => new { s.Status, s.CreatedAtUtc });
        builder.HasIndex(s => new { s.ResponsavelId, s.Status });
        builder.HasIndex(s => s.AlunoId);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(s => s.ResponsavelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(s => s.AlunoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
