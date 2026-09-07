using TutorClass.Domain.Financial;
using TutorClass.Domain.Help;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Materials;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Abstractions;

public interface IApplicationDbContext
{
    IQueryable<Materia> Materias { get; }
    IQueryable<Professor> Professores { get; }
    IQueryable<CandidaturaProfessor> CandidaturasProfessor { get; }
    IQueryable<QualificacaoProfessor> QualificacoesProfessor { get; }
    IQueryable<DisponibilidadeProfessor> DisponibilidadesProfessor { get; }
    IQueryable<ProfessorMateria> ProfessoresMaterias { get; }
    IQueryable<Administrador> Administradores { get; }
    IQueryable<Usuario> Usuarios { get; }
    IQueryable<ResponsavelAluno> ResponsaveisAlunos { get; }
    IQueryable<Aula> Aulas { get; }
    IQueryable<GravacaoAula> GravacoesAula { get; }
    IQueryable<AulaReagendamento> AulasReagendamentos { get; }
    IQueryable<AcompanhamentoAula> AcompanhamentosAula { get; }
    IQueryable<AvaliacaoProfessor> AvaliacoesProfessor { get; }
    IQueryable<MaterialPedagogico> MateriaisPedagogicos { get; }
    IQueryable<AssinaturaAluno> AssinaturasAluno { get; }
    IQueryable<UsoIaAluno> UsosIaAluno { get; }
    IQueryable<MentoriaAlunoProfessor> MentoriasAlunoProfessor { get; }
    IQueryable<AcompanhamentoMentoria> AcompanhamentosMentoria { get; }
    IQueryable<AdicionalMentoria> AdicionaisMentoria { get; }
    IQueryable<PagamentoAula> PagamentosAula { get; }
    IQueryable<CobrancaAssinatura> CobrancasAssinatura { get; }
    IQueryable<PerguntaFrequente> PerguntasFrequentes { get; }
    IQueryable<Notificacao> Notificacoes { get; }

    void AddEntity<TEntity>(TEntity entity) where TEntity : class;
    void RemoveEntity<TEntity>(TEntity entity) where TEntity : class;
    Task<TEntity?> FindAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
