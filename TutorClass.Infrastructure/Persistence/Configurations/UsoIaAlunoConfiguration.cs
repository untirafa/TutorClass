using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Plans;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class UsoIaAlunoConfiguration : IEntityTypeConfiguration<UsoIaAluno>
{
    public void Configure(EntityTypeBuilder<UsoIaAluno> builder)
    {
        builder.ToTable("usos_ia_aluno");
        builder.HasKey(u => u.Id);

        builder.HasIndex(u => new { u.AlunoId, u.Data }).IsUnique();
        builder.HasOne(u => u.Aluno).WithMany().HasForeignKey(u => u.AlunoId).OnDelete(DeleteBehavior.Restrict);
    }
}
