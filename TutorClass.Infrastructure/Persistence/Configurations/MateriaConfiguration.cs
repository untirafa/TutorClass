using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Subjects;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class MateriaConfiguration : IEntityTypeConfiguration<Materia>
{
    private static readonly Guid PortuguesId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid LiteraturaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid InglesId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid EspanholId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid ArtesId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid HistoriaId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly Guid GeografiaId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private static readonly Guid FilosofiaId = Guid.Parse("88888888-8888-8888-8888-888888888888");
    private static readonly Guid SociologiaId = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly Guid FisicaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid QuimicaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid BiologiaId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public void Configure(EntityTypeBuilder<Materia> builder)
    {
        builder.ToTable("materias");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Nome).HasMaxLength(80).IsRequired();
        builder.HasIndex(m => m.Nome).IsUnique();

        builder.HasData(
            Create(PortuguesId, "Portugues"),
            Create(LiteraturaId, "Literatura"),
            Create(InglesId, "Ingles"),
            Create(EspanholId, "Espanhol"),
            Create(ArtesId, "Artes"),
            Create(HistoriaId, "Historia"),
            Create(GeografiaId, "Geografia"),
            Create(FilosofiaId, "Filosofia"),
            Create(SociologiaId, "Sociologia"),
            Create(FisicaId, "Fisica"),
            Create(QuimicaId, "Quimica"),
            Create(BiologiaId, "Biologia"));
    }

    private static object Create(Guid id, string nome)
    {
        return new { Id = id, Nome = nome, CreatedAtUtc = new DateTime(2026, 8, 23, 0, 0, 0, DateTimeKind.Utc) };
    }
}
