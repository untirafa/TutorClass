using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class ContextoIaService(IApplicationDbContext db) : IContextoIaService
{
    public Task<ContextoIaAluno> ObterContextoAsync(Guid alunoId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var nome = db.Usuarios.OfType<Aluno>().Where(a => a.Id == alunoId && a.Ativo)
            .Select(a => a.Nome).SingleOrDefault() ?? throw new DomainException("Aluno nao encontrado ou inativo.");
        var plano = db.AssinaturasAluno.Where(a => a.AlunoId == alunoId && a.Ativa)
            .OrderByDescending(a => a.DataInicioUtc).Select(a => a.Plano).FirstOrDefault();
        var hoje = TutorIaService.GetHojeBrasil();
        var usadas = db.UsosIaAluno.Where(u => u.AlunoId == alunoId && u.Data == hoje)
            .Select(u => u.Quantidade).SingleOrDefault();
        var agora = DateTime.UtcNow;
        var aulas = db.Aulas.Where(a => a.AlunoId == alunoId && a.Status != StatusAula.Cancelada);
        var contexto = new ContextoIaAluno
        {
            AlunoId = alunoId, Nome = nome, Plano = plano.ToString(), MensagensUsadasHoje = usadas,
            MensagensRestantesHoje = plano == PlanoAluno.Mentoria ? null :
                plano == PlanoAluno.Foco ? Math.Max(0, PlanoAlunoService.LimiteIaFocoPorDia - usadas) : 0,
            Materias = (from a in aulas join m in db.Materias on a.MateriaId equals m.Id
                        select m.Nome).Distinct().OrderBy(n => n).Take(5).ToList(),
            ProximasAulas = (from a in aulas
                            join m in db.Materias on a.MateriaId equals m.Id
                            join p in db.Professores on a.ProfessorId equals p.Id
                            where a.InicioUtc >= agora && a.Status == StatusAula.Confirmada
                            orderby a.InicioUtc, a.Id
                            select new AulaContextoIa(a.Id, m.Nome, a.InicioUtc, p.Nome)).Take(5).ToList(),
            MentoresAtivos = (from v in db.MentoriasAlunoProfessor
                             join p in db.Professores on v.ProfessorId equals p.Id
                             where v.AlunoId == alunoId && v.Ativo && p.Ativo
                             orderby v.Slot, v.Id
                             select new MentorContextoIa(p.Id, p.Nome, null)).Take(2).ToList()
        };
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(contexto);
    }
}
