using Microsoft.EntityFrameworkCore;
using TutorClass.Application.Abstractions;
using TutorClass.Domain.Financial;
using TutorClass.Domain.Help;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Materials;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Infrastructure.Persistence;

public sealed class TutorClassDbContext : DbContext, IApplicationDbContext
{
    public TutorClassDbContext(DbContextOptions<TutorClassDbContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Aluno> Alunos => Set<Aluno>();
    public DbSet<Responsavel> Responsaveis => Set<Responsavel>();
    public DbSet<Professor> Professores => Set<Professor>();
    public DbSet<Administrador> Administradores => Set<Administrador>();
    public DbSet<ResponsavelAluno> ResponsaveisAlunos => Set<ResponsavelAluno>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<ProfessorMateria> ProfessoresMaterias => Set<ProfessorMateria>();
    public DbSet<CandidaturaProfessor> CandidaturasProfessor => Set<CandidaturaProfessor>();
    public DbSet<QualificacaoProfessor> QualificacoesProfessor => Set<QualificacaoProfessor>();
    public DbSet<DisponibilidadeProfessor> DisponibilidadesProfessor => Set<DisponibilidadeProfessor>();
    public DbSet<Aula> Aulas => Set<Aula>();
    public DbSet<GravacaoAula> GravacoesAula => Set<GravacaoAula>();
    public DbSet<AulaReagendamento> AulasReagendamentos => Set<AulaReagendamento>();
    public DbSet<AcompanhamentoAula> AcompanhamentosAula => Set<AcompanhamentoAula>();
    public DbSet<AvaliacaoProfessor> AvaliacoesProfessor => Set<AvaliacaoProfessor>();
    public DbSet<MaterialPedagogico> MateriaisPedagogicos => Set<MaterialPedagogico>();
    public DbSet<AssinaturaAluno> AssinaturasAluno => Set<AssinaturaAluno>();
    public DbSet<UsoIaAluno> UsosIaAluno => Set<UsoIaAluno>();
    public DbSet<MentoriaAlunoProfessor> MentoriasAlunoProfessor => Set<MentoriaAlunoProfessor>();
    public DbSet<AcompanhamentoMentoria> AcompanhamentosMentoria => Set<AcompanhamentoMentoria>();
    public DbSet<AdicionalMentoria> AdicionaisMentoria => Set<AdicionalMentoria>();
    public DbSet<PagamentoAula> PagamentosAula => Set<PagamentoAula>();
    public DbSet<CobrancaAssinatura> CobrancasAssinatura => Set<CobrancaAssinatura>();
    public DbSet<PerguntaFrequente> PerguntasFrequentes => Set<PerguntaFrequente>();
    public DbSet<Notificacao> Notificacoes => Set<Notificacao>();

    IQueryable<Materia> IApplicationDbContext.Materias => Materias;
    IQueryable<Professor> IApplicationDbContext.Professores => Professores;
    IQueryable<CandidaturaProfessor> IApplicationDbContext.CandidaturasProfessor => CandidaturasProfessor;
    IQueryable<QualificacaoProfessor> IApplicationDbContext.QualificacoesProfessor => QualificacoesProfessor;
    IQueryable<DisponibilidadeProfessor> IApplicationDbContext.DisponibilidadesProfessor => DisponibilidadesProfessor;
    IQueryable<ProfessorMateria> IApplicationDbContext.ProfessoresMaterias => ProfessoresMaterias;
    IQueryable<Administrador> IApplicationDbContext.Administradores => Administradores;
    IQueryable<Usuario> IApplicationDbContext.Usuarios => Usuarios;
    IQueryable<ResponsavelAluno> IApplicationDbContext.ResponsaveisAlunos => ResponsaveisAlunos;
    IQueryable<Aula> IApplicationDbContext.Aulas => Aulas;
    IQueryable<GravacaoAula> IApplicationDbContext.GravacoesAula => GravacoesAula;
    IQueryable<AulaReagendamento> IApplicationDbContext.AulasReagendamentos => AulasReagendamentos;
    IQueryable<AcompanhamentoAula> IApplicationDbContext.AcompanhamentosAula => AcompanhamentosAula;
    IQueryable<AvaliacaoProfessor> IApplicationDbContext.AvaliacoesProfessor => AvaliacoesProfessor;
    IQueryable<MaterialPedagogico> IApplicationDbContext.MateriaisPedagogicos => MateriaisPedagogicos;
    IQueryable<AssinaturaAluno> IApplicationDbContext.AssinaturasAluno => AssinaturasAluno;
    IQueryable<UsoIaAluno> IApplicationDbContext.UsosIaAluno => UsosIaAluno;
    IQueryable<MentoriaAlunoProfessor> IApplicationDbContext.MentoriasAlunoProfessor => MentoriasAlunoProfessor;
    IQueryable<AcompanhamentoMentoria> IApplicationDbContext.AcompanhamentosMentoria => AcompanhamentosMentoria;
    IQueryable<AdicionalMentoria> IApplicationDbContext.AdicionaisMentoria => AdicionaisMentoria;
    IQueryable<PagamentoAula> IApplicationDbContext.PagamentosAula => PagamentosAula;
    IQueryable<CobrancaAssinatura> IApplicationDbContext.CobrancasAssinatura => CobrancasAssinatura;
    IQueryable<PerguntaFrequente> IApplicationDbContext.PerguntasFrequentes => PerguntasFrequentes;
    IQueryable<Notificacao> IApplicationDbContext.Notificacoes => Notificacoes;

    public void AddEntity<TEntity>(TEntity entity) where TEntity : class
    {
        Set<TEntity>().Add(entity);
    }

    public void RemoveEntity<TEntity>(TEntity entity) where TEntity : class
    {
        Set<TEntity>().Remove(entity);
    }

    public async Task<TEntity?> FindAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : class
    {
        return await Set<TEntity>().FindAsync([id], cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TutorClassDbContext).Assembly);
        modelBuilder.ApplySnakeCaseNamingConvention();
    }
}
