using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TutorClass.Domain.Mentoring;

namespace TutorClass.Infrastructure.Persistence.Configurations;

public sealed class MentoriaAlunoProfessorConfiguration : IEntityTypeConfiguration<MentoriaAlunoProfessor>
{
    public void Configure(EntityTypeBuilder<MentoriaAlunoProfessor> builder)
    {
        builder.ToTable("mentorias_aluno_professor");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MotivoEncerramento).HasConversion<string>().HasMaxLength(60);

        builder.HasIndex(m => new { m.AlunoId, m.Ativo });
        builder.HasIndex(m => new { m.ProfessorId, m.Ativo });
        builder.HasIndex(m => new { m.AlunoId, m.Slot, m.Ativo });
        builder.HasIndex(m => new { m.AlunoId, m.ProfessorId, m.Ativo });

        builder.HasOne(m => m.Aluno).WithMany().HasForeignKey(m => m.AlunoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(m => m.Professor).WithMany().HasForeignKey(m => m.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}
