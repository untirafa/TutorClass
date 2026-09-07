using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Notifications;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class NotificacaoConfiguration : IEntityTypeConfiguration<Notificacao>
{
    public void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        builder.ToTable("notificacoes");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Tipo)
            .HasConversion<string>()
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(n => n.Prioridade)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(n => n.Titulo).HasMaxLength(140).IsRequired();
        builder.Property(n => n.Mensagem).HasMaxLength(500).IsRequired();
        builder.Property(n => n.RotaAcao).HasMaxLength(240);
        builder.Property(n => n.ChaveEvento).HasMaxLength(160);

        builder.HasIndex(n => new { n.UsuarioId, n.Lida, n.CreatedAtUtc });
        builder.HasIndex(n => n.ChaveEvento).IsUnique().HasFilter("chave_evento IS NOT NULL");

        builder.HasOne<TutorClass.Domain.Users.Usuario>()
            .WithMany()
            .HasForeignKey(n => n.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
