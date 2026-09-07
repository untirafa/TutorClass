using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Help;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class PerguntaFrequenteConfiguration : IEntityTypeConfiguration<PerguntaFrequente>
{
    public void Configure(EntityTypeBuilder<PerguntaFrequente> builder)
    {
        builder.ToTable("perguntas_frequentes");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Pergunta).HasMaxLength(240).IsRequired();
        builder.Property(f => f.Resposta).HasMaxLength(2000).IsRequired();
        builder.Property(f => f.Categoria).HasConversion<string>().HasMaxLength(60).IsRequired();
        builder.Property(f => f.PalavrasChave).HasMaxLength(500);
        builder.Property(f => f.TextoAcao).HasMaxLength(120);
        builder.Property(f => f.RotaAcao).HasMaxLength(240);

        builder.HasIndex(f => new { f.Ativo, f.Categoria, f.OrdemExibicao });
        builder.HasIndex(f => f.Pergunta).IsUnique();
    }
}
