using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);

        builder.HasDiscriminator<string>("tipo_usuario")
            .HasValue<Aluno>("Aluno")
            .HasValue<Responsavel>("Responsavel")
            .HasValue<Professor>("Professor")
            .HasValue<Administrador>("Administrador");

        builder.Property(u => u.Nome).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(180).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
