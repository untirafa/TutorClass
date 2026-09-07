using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Lessons;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class GravacaoAulaConfiguration : IEntityTypeConfiguration<GravacaoAula>
{
    public void Configure(EntityTypeBuilder<GravacaoAula> builder)
    {
        builder.ToTable("gravacoes_aula");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Titulo).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Descricao).HasMaxLength(1000);
        builder.Property(g => g.UrlVideo).HasMaxLength(1000).IsRequired();
        builder.Property(g => g.MiniaturaUrl).HasMaxLength(1000);

        builder.HasIndex(g => g.AulaId).IsUnique();
        builder.HasIndex(g => new { g.Disponivel, g.DataDisponibilizacaoUtc });

        builder.HasOne(g => g.Aula)
            .WithMany()
            .HasForeignKey(g => g.AulaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
