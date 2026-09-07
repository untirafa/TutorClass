using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class GravacaoAulaService
{
    private readonly IApplicationDbContext _db;
    private readonly PlanoAlunoService _planos;
    private readonly NotificacaoService _notificacoes;

    public GravacaoAulaService(IApplicationDbContext db)
        : this(db, new PlanoAlunoService(db), new NotificacaoService(db))
    {
    }

    public GravacaoAulaService(IApplicationDbContext db, PlanoAlunoService planos)
        : this(db, planos, new NotificacaoService(db))
    {
    }

    public GravacaoAulaService(IApplicationDbContext db, PlanoAlunoService planos, NotificacaoService notificacoes)
    {
        _db = db;
        _planos = planos;
        _notificacoes = notificacoes;
    }

    public async Task<GravacaoAulaResponse> CriarAsync(Guid aulaId, Guid usuarioId, bool administrador, CriarGravacaoAulaRequest request, CancellationToken cancellationToken)
    {
        var aula = GetAula(aulaId);
        EnsureCanManageRecording(aula, usuarioId, administrador);

        if (_db.GravacoesAula.Any(g => g.AulaId == aula.Id))
        {
            throw new DomainException("Aula ja possui gravacao cadastrada. Use a atualizacao da gravacao.");
        }

        var gravacao = new GravacaoAula(aula, request.Titulo, request.UrlVideo, request.DuracaoSegundos, request.Descricao, request.MiniaturaUrl);
        _db.AddEntity(gravacao);
        NotificarGravacaoDisponivel(gravacao, aula);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(gravacao, aula);
    }

    public async Task<GravacaoAulaResponse> AtualizarAsync(Guid aulaId, Guid usuarioId, bool administrador, CriarGravacaoAulaRequest request, CancellationToken cancellationToken)
    {
        var aula = GetAula(aulaId);
        EnsureCanManageRecording(aula, usuarioId, administrador);

        var gravacao = _db.GravacoesAula.SingleOrDefault(g => g.AulaId == aula.Id)
            ?? throw new DomainException("Gravacao da aula nao encontrada.");

        gravacao.AtualizarReferencia(aula, request.Titulo, request.UrlVideo, request.DuracaoSegundos, request.Descricao, request.MiniaturaUrl);
        NotificarGravacaoDisponivel(gravacao, aula);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(gravacao, aula);
    }

    public async Task<GravacaoAulaResponse> DesativarAsync(Guid aulaId, Guid usuarioId, bool administrador, CancellationToken cancellationToken)
    {
        var aula = GetAula(aulaId);
        EnsureCanManageRecording(aula, usuarioId, administrador);

        var gravacao = _db.GravacoesAula.SingleOrDefault(g => g.AulaId == aula.Id)
            ?? throw new DomainException("Gravacao da aula nao encontrada.");

        gravacao.Desativar();
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(gravacao, aula);
    }

    public Task<GravacaoAulaResponse> BuscarParaAlunoAsync(Guid aulaId, Guid alunoId, CancellationToken cancellationToken)
    {
        var aula = GetAula(aulaId);
        EnsureAlunoPodeAcessarAula(aula, alunoId);
        EnsureMentoriaAtiva(alunoId);

        var gravacao = _db.GravacoesAula.SingleOrDefault(g => g.AulaId == aula.Id)
            ?? throw new DomainException("Gravacao da aula nao esta disponivel.");

        gravacao.GarantirAcessoDisponivel(aula);
        return Task.FromResult(ToResponse(gravacao, aula));
    }

    public Task<IReadOnlyCollection<GravacaoAulaResponse>> ListarMinhasAsync(Guid alunoId, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);
        EnsureMentoriaAtiva(alunoId);

        var aulasDoAluno = _db.Aulas
            .Where(a => a.AlunoId == alunoId && a.Status == StatusAula.Concluida)
            .ToArray();

        var responses = aulasDoAluno
            .Join(
                _db.GravacoesAula.Where(g => g.Disponivel).ToArray(),
                aula => aula.Id,
                gravacao => gravacao.AulaId,
                (aula, gravacao) => ToResponse(gravacao, aula))
            .OrderByDescending(g => g.DataDisponibilizacaoUtc)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<GravacaoAulaResponse>>(responses);
    }

    private Aula GetAula(Guid aulaId)
    {
        return _db.Aulas.SingleOrDefault(a => a.Id == aulaId)
            ?? throw new DomainException("Aula nao encontrada.");
    }

    private void EnsureCanManageRecording(Aula aula, Guid usuarioId, bool administrador)
    {
        if (administrador)
        {
            return;
        }

        throw new UnauthorizedAccessException("Gravacao de aula e uma operacao tecnica do TutorClass. Somente administrador pode gerenciar a referencia manualmente nesta versao.");
    }

    private void EnsureAlunoPodeAcessarAula(Aula aula, Guid alunoId)
    {
        EnsureAlunoAtivo(alunoId);

        if (aula.AlunoId != alunoId)
        {
            throw new UnauthorizedAccessException("Aluno nao pode acessar gravacao de aula de outro aluno.");
        }
    }

    private void EnsureAlunoAtivo(Guid alunoId)
    {
        if (!_db.Usuarios.OfType<Aluno>().Any(a => a.Id == alunoId && a.Ativo))
        {
            throw new DomainException("Aluno nao encontrado ou inativo.");
        }
    }

    private void EnsureMentoriaAtiva(Guid alunoId)
    {
        var plano = _planos.ObterPlanoAtual(alunoId);
        if (plano == PlanoAluno.Mentoria)
        {
            return;
        }

        var message = plano == PlanoAluno.Foco
            ? "Gravacoes de aulas fazem parte do plano Mentoria."
            : "Gravacoes de aulas estao disponiveis apenas no plano Mentoria.";

        throw new UnauthorizedAccessException(message);
    }

    private void NotificarGravacaoDisponivel(GravacaoAula gravacao, Aula aula)
    {
        if (!gravacao.Disponivel || !_planos.PossuiMentoriaAtiva(aula.AlunoId))
        {
            return;
        }

        var materia = _db.Materias.Where(m => m.Id == aula.MateriaId).Select(m => m.Nome).SingleOrDefault() ?? "aula";
        _notificacoes.CriarSeNaoExistir(
            aula.AlunoId,
            TipoNotificacao.GravacaoDisponivel,
            "Gravacao disponivel",
            $"A gravacao da sua aula de {materia} ja esta disponivel para revisao.",
            "/gravacoes",
            gravacao.Id,
            $"GRAVACAO_DISPONIVEL:{gravacao.Id}:{aula.AlunoId}");
    }

    private GravacaoAulaResponse ToResponse(GravacaoAula gravacao, Aula aula)
    {
        var professor = _db.Usuarios.OfType<Professor>().Single(p => p.Id == aula.ProfessorId);
        var materia = _db.Materias.Single(m => m.Id == aula.MateriaId);

        return new GravacaoAulaResponse(
            gravacao.Id,
            gravacao.AulaId,
            gravacao.Titulo,
            gravacao.Descricao,
            gravacao.UrlVideo,
            gravacao.MiniaturaUrl,
            gravacao.DuracaoSegundos,
            gravacao.DataDisponibilizacaoUtc,
            gravacao.Disponivel,
            materia.Nome,
            professor.Nome,
            aula.InicioUtc);
    }
}
