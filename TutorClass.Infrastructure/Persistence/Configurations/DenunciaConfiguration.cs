using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Moderation;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class DenunciaConfiguration : IEntityTypeConfiguration<Denuncia>
{
    public void Configure(EntityTypeBuilder<Denuncia> builder)
    {
        builder.ToTable("denuncias");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TipoAlvo).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(d => d.AlvoId).HasMaxLength(64).IsRequired();
        builder.Property(d => d.Motivo).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(d => d.Descricao).HasMaxLength(Denuncia.DescricaoMaxLength);
        builder.Property(d => d.ResumoAlvo).HasMaxLength(Denuncia.ResumoMaxLength).IsRequired();
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.Sancao).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.ObservacaoModeracao).HasMaxLength(Denuncia.ObservacaoMaxLength);

        builder.HasIndex(d => new { d.Status, d.CreatedAtUtc });
        builder.HasIndex(d => new { d.DenuncianteId, d.TipoAlvo, d.AlvoId });
        builder.HasIndex(d => d.AlvoUsuarioId);

        builder.HasOne<TutorClass.Domain.Users.Usuario>()
            .WithMany()
            .HasForeignKey(d => d.DenuncianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TutorClass.Domain.Users.Usuario>()
            .WithMany()
            .HasForeignKey(d => d.AlvoUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
