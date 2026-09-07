using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Materials;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class MaterialPedagogicoConfiguration : IEntityTypeConfiguration<MaterialPedagogico>
{
    public void Configure(EntityTypeBuilder<MaterialPedagogico> builder)
    {
        builder.ToTable("materiais");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Titulo).HasMaxLength(160).IsRequired();
        builder.Property(m => m.Descricao).HasMaxLength(1000);
        builder.Property(m => m.Tipo).HasMaxLength(60).IsRequired();
        builder.Property(m => m.NomeArquivo).HasMaxLength(255);
        builder.Property(m => m.UrlArquivo).HasMaxLength(2000).IsRequired();

        builder.HasIndex(m => new { m.AlunoId, m.CreatedAtUtc });
        builder.HasIndex(m => new { m.ProfessorId, m.CreatedAtUtc });
        builder.HasIndex(m => m.AulaId);
        builder.HasIndex(m => m.MateriaId);

        builder.HasOne(m => m.Professor).WithMany().HasForeignKey(m => m.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Aluno).WithMany().HasForeignKey(m => m.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Materia).WithMany().HasForeignKey(m => m.MateriaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Aula).WithMany().HasForeignKey(m => m.AulaId).OnDelete(DeleteBehavior.SetNull);
    }
}
