using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Mentoring;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class MentoriaService
{
    private const int MaxMentoresPorAluno = 2;
    private const decimal ValorAdicionalAcompanhamento = 20m;
    private readonly IApplicationDbContext _db;
    private readonly PlanoAlunoService _planos;
    private readonly NotificacaoService _notificacoes;

    public MentoriaService(IApplicationDbContext db)
        : this(db, new PlanoAlunoService(db), new NotificacaoService(db))
    {
    }

    public MentoriaService(IApplicationDbContext db, PlanoAlunoService planos)
        : this(db, planos, new NotificacaoService(db))
    {
    }

    public MentoriaService(IApplicationDbContext db, PlanoAlunoService planos, NotificacaoService notificacoes)
    {
        _db = db;
        _planos = planos;
        _notificacoes = notificacoes;
    }

    public async Task<MentoriaResponse> SelecionarMentorAsync(Guid alunoId, SelecionarMentorRequest request, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);
        EnsureAlunoPodeUsarMentoria(alunoId);
        EnsureProfessorPodeSerMentor(request.ProfessorId);
        EnsureProfessorTemVaga(request.ProfessorId);

        if (_db.MentoriasAlunoProfessor.Any(m => m.AlunoId == alunoId && m.ProfessorId == request.ProfessorId && m.Ativo))
        {
            throw new DomainException("Este professor ja e mentor ativo do aluno.");
        }

        var vinculosAtivos = _db.MentoriasAlunoProfessor.Where(m => m.AlunoId == alunoId && m.Ativo).ToArray();
        if (vinculosAtivos.Length >= MaxMentoresPorAluno)
        {
            throw new DomainException("Voce ja possui dois mentores ativos.");
        }

        var slot = request.Slot ?? Enumerable.Range(1, MaxMentoresPorAluno).First(s => vinculosAtivos.All(m => m.Slot != s));
        if (vinculosAtivos.Any(m => m.Slot == slot))
        {
            throw new DomainException("Esta vaga de mentor ja esta ocupada. Use o fluxo de troca.");
        }

        var vinculo = new MentoriaAlunoProfessor(alunoId, request.ProfessorId, slot);
        _db.AddEntity(vinculo);
        NotificarMentoriaIniciada(vinculo, alunoId);
        await _db.SaveChangesAsync(cancellationToken);
        return ToMentoriaResponse(vinculo);
    }

    public async Task<MentoriaResponse> TrocarMentorAsync(Guid alunoId, int slot, TrocarMentorRequest request, CancellationToken cancellationToken)
    {
        EnsureAlunoAtivo(alunoId);
        EnsureAlunoPodeUsarMentoria(alunoId);
        EnsureProfessorPodeSerMentor(request.NovoProfessorId);
        EnsureProfessorTemVaga(request.NovoProfessorId);

        var atual = _db.MentoriasAlunoProfessor.SingleOrDefault(m => m.AlunoId == alunoId && m.Slot == slot && m.Ativo)
            ?? throw new DomainException("Vaga de mentor ativa nao encontrada.");

        if (atual.ProfessorId == request.NovoProfessorId)
        {
            throw new DomainException("Novo mentor deve ser diferente do mentor atual.");
        }

        if (_db.MentoriasAlunoProfessor.Any(m => m.AlunoId == alunoId && m.ProfessorId == request.NovoProfessorId && m.Ativo))
        {
            throw new DomainException("Um mesmo professor nao pode ocupar duas vagas de Mentoria do mesmo aluno.");
        }

        if (atual.DataUltimaTrocaUtc > DateTime.UtcNow.AddDays(-30))
        {
            throw new DomainException("Esta vaga de mentor ainda nao pode ser alterada.");
        }

        atual.Encerrar(MotivoEncerramentoMentoria.TrocaSolicitadaPeloAluno);
        var novo = new MentoriaAlunoProfessor(alunoId, request.NovoProfessorId, slot);
        _db.AddEntity(novo);
        NotificarMentoriaEncerrada(atual, alunoId);
        NotificarMentoriaIniciada(novo, alunoId);
        NotificarMentorAlterado(alunoId, novo.Id);
        await _db.SaveChangesAsync(cancellationToken);
        return ToMentoriaResponse(novo);
    }

    public Task<IReadOnlyCollection<MentoriaResponse>> ListarMinhasMentoriasAsync(Guid usuarioId, bool professor, CancellationToken cancellationToken)
    {
        var query = _db.MentoriasAlunoProfessor.AsEnumerable();
        query = professor
            ? query.Where(m => m.ProfessorId == usuarioId)
            : query.Where(m => m.AlunoId == usuarioId);

        var result = query
            .OrderByDescending(m => m.Ativo)
            .ThenBy(m => m.Slot)
            .ThenByDescending(m => m.DataInicioUtc)
            .Select(ToMentoriaResponse)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<MentoriaResponse>>(result);
    }

    public async Task<Guid?> CriarAcompanhamentoPendenteParaAulaAsync(Aula aula, CancellationToken cancellationToken)
    {
        if (_planos.ObterPlanoAtual(aula.AlunoId) != PlanoAluno.Mentoria)
        {
            return null;
        }

        var vinculo = _db.MentoriasAlunoProfessor.SingleOrDefault(m =>
            m.AlunoId == aula.AlunoId &&
            m.ProfessorId == aula.ProfessorId &&
            m.Ativo);

        if (vinculo is null)
        {
            return null;
        }

        var existente = _db.AcompanhamentosMentoria.SingleOrDefault(a => a.AulaId == aula.Id);
        if (existente is not null)
        {
            return existente.Id;
        }

        var acompanhamento = new AcompanhamentoMentoria(aula.Id, aula.AlunoId, aula.ProfessorId, vinculo.Id);
        _db.AddEntity(acompanhamento);
        await _db.SaveChangesAsync(cancellationToken);
        return acompanhamento.Id;
    }

    public Task<IReadOnlyCollection<AcompanhamentoMentoriaResponse>> ListarAcompanhamentosAsync(Guid usuarioId, bool professor, bool somentePendentes, CancellationToken cancellationToken)
    {
        var query = _db.AcompanhamentosMentoria.AsEnumerable();
        query = professor
            ? query.Where(a => a.ProfessorId == usuarioId)
            : query.Where(a => a.AlunoId == usuarioId);

        if (somentePendentes)
        {
            query = query.Where(a => a.Status == StatusAcompanhamentoMentoria.Pendente);
        }

        var result = query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(ToAcompanhamentoResponse)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AcompanhamentoMentoriaResponse>>(result);
    }

    public async Task<AcompanhamentoMentoriaResponse> AtualizarAcompanhamentoAsync(Guid acompanhamentoId, Guid professorId, AtualizarAcompanhamentoMentoriaRequest request, CancellationToken cancellationToken)
    {
        var acompanhamento = await _db.FindAsync<AcompanhamentoMentoria>(acompanhamentoId, cancellationToken)
            ?? throw new DomainException("Acompanhamento de mentoria nao encontrado.");

        if (acompanhamento.ProfessorId != professorId)
        {
            throw new UnauthorizedAccessException("Somente o mentor da aula pode registrar este acompanhamento.");
        }

        acompanhamento.Atualizar(request.Resumo, request.PontosFortes, request.PontosMelhorar, request.ProximosPassos, request.MetaAtual, request.Observacoes);
        await _db.SaveChangesAsync(cancellationToken);
        return ToAcompanhamentoResponse(acompanhamento);
    }

    public async Task<ConclusaoAcompanhamentoMentoriaResponse> ConcluirAcompanhamentoAsync(Guid acompanhamentoId, Guid professorId, AtualizarAcompanhamentoMentoriaRequest? request, CancellationToken cancellationToken)
    {
        var acompanhamento = await _db.FindAsync<AcompanhamentoMentoria>(acompanhamentoId, cancellationToken)
            ?? throw new DomainException("Acompanhamento de mentoria nao encontrado.");

        if (acompanhamento.ProfessorId != professorId)
        {
            throw new UnauthorizedAccessException("Somente o mentor da aula pode concluir este acompanhamento.");
        }

        if (request is not null)
        {
            acompanhamento.Atualizar(request.Resumo, request.PontosFortes, request.PontosMelhorar, request.ProximosPassos, request.MetaAtual, request.Observacoes);
        }

        acompanhamento.Concluir();
        var adicionalGerado = GerarAdicionalSeElegivel(acompanhamento);
        NotificarAcompanhamentoConcluido(acompanhamento);
        await _db.SaveChangesAsync(cancellationToken);

        return new ConclusaoAcompanhamentoMentoriaResponse(
            ToAcompanhamentoResponse(acompanhamento),
            adicionalGerado,
            adicionalGerado ? ValorAdicionalAcompanhamento : null,
            adicionalGerado
                ? "Acompanhamento registrado com sucesso. Adicional simulado gerado."
                : "Acompanhamento registrado com sucesso. Limite mensal de adicionais ja atingido.");
    }

    private bool GerarAdicionalSeElegivel(AcompanhamentoMentoria acompanhamento)
    {
        if (_db.AdicionaisMentoria.Any(a => a.AcompanhamentoMentoriaId == acompanhamento.Id))
        {
            return false;
        }

        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var fimMes = inicioMes.AddMonths(1);
        var quantidadeMes = _db.AdicionaisMentoria.Count(a =>
            a.ProfessorId == acompanhamento.ProfessorId &&
            a.AlunoId == acompanhamento.AlunoId &&
            a.DataGeracaoUtc >= inicioMes &&
            a.DataGeracaoUtc < fimMes);

        if (quantidadeMes >= 2)
        {
            return false;
        }

        _db.AddEntity(new AdicionalMentoria(
            acompanhamento.ProfessorId,
            acompanhamento.AlunoId,
            acompanhamento.AulaId,
            acompanhamento.Id,
            ValorAdicionalAcompanhamento));

        return true;
    }

    private void NotificarMentoriaIniciada(MentoriaAlunoProfessor vinculo, Guid alunoId)
    {
        var alunoNome = GetAlunoNome(alunoId);
        var professorNome = GetProfessorNome(vinculo.ProfessorId);

        _notificacoes.CriarSeNaoExistir(
            vinculo.ProfessorId,
            TipoNotificacao.MentoriaIniciada,
            "Novo aluno de Mentoria",
            $"{alunoNome} selecionou voce como mentor.",
            "/professor/mentoria",
            vinculo.Id,
            $"MENTORIA_INICIADA:{vinculo.Id}:{vinculo.ProfessorId}",
            PrioridadeNotificacao.Importante);

        _notificacoes.CriarSeNaoExistir(
            alunoId,
            TipoNotificacao.MentoriaIniciada,
            "Mentoria iniciada",
            $"{professorNome} agora e seu mentor no TutorClass.",
            "/mentoria",
            vinculo.Id,
            $"MENTORIA_INICIADA:{vinculo.Id}:{alunoId}");
    }

    private void NotificarMentoriaEncerrada(MentoriaAlunoProfessor vinculo, Guid alunoId)
    {
        var alunoNome = GetAlunoNome(alunoId);
        var professorNome = GetProfessorNome(vinculo.ProfessorId);

        _notificacoes.CriarSeNaoExistir(
            vinculo.ProfessorId,
            TipoNotificacao.MentoriaEncerrada,
            "Vinculo de Mentoria encerrado",
            $"Seu vinculo de Mentoria com {alunoNome} foi encerrado.",
            "/professor/mentoria",
            vinculo.Id,
            $"MENTORIA_ENCERRADA:{vinculo.Id}:{vinculo.ProfessorId}");

        _notificacoes.CriarSeNaoExistir(
            alunoId,
            TipoNotificacao.MentoriaEncerrada,
            "Vinculo de Mentoria encerrado",
            $"Seu vinculo de Mentoria com {professorNome} foi encerrado.",
            "/mentoria",
            vinculo.Id,
            $"MENTORIA_ENCERRADA:{vinculo.Id}:{alunoId}");
    }

    private void NotificarMentorAlterado(Guid alunoId, Guid novoVinculoId)
    {
        _notificacoes.CriarSeNaoExistir(
            alunoId,
            TipoNotificacao.MentorAlterado,
            "Mentor alterado",
            "Seu mentor foi alterado com sucesso.",
            "/mentoria",
            novoVinculoId,
            $"MENTOR_ALTERADO:{novoVinculoId}:{alunoId}");
    }

    private void NotificarAcompanhamentoConcluido(AcompanhamentoMentoria acompanhamento)
    {
        _notificacoes.CriarSeNaoExistir(
            acompanhamento.AlunoId,
            TipoNotificacao.AcompanhamentoMentoriaConcluido,
            "Novo acompanhamento da Mentoria",
            "Seu mentor concluiu o acompanhamento da ultima aula.",
            "/mentoria",
            acompanhamento.Id,
            $"ACOMPANHAMENTO_CONCLUIDO:{acompanhamento.Id}:{acompanhamento.AlunoId}");
    }

    private void EnsureAlunoPodeUsarMentoria(Guid alunoId)
    {
        if (!_planos.PossuiMentoriaAtiva(alunoId))
        {
            throw new DomainException("Seu plano atual nao permite Mentoria.");
        }
    }

    private void EnsureProfessorPodeSerMentor(Guid professorId)
    {
        var professor = _db.Professores.SingleOrDefault(p => p.Id == professorId && p.Ativo)
            ?? throw new DomainException("Professor nao encontrado ou inativo.");

        if (professor.Status != StatusProfessor.Aprovado)
        {
            throw new DomainException("Professor precisa estar aprovado para oferecer Mentoria.");
        }

        if (!professor.OfereceMentoria)
        {
            throw new DomainException("Este professor nao oferece Mentoria.");
        }
    }

    private void EnsureProfessorTemVaga(Guid professorId)
    {
        var professor = _db.Professores.Single(p => p.Id == professorId);
        var mentoradosAtivos = _db.MentoriasAlunoProfessor.Count(m => m.ProfessorId == professorId && m.Ativo);
        if (mentoradosAtivos >= professor.LimiteMentorados)
        {
            throw new DomainException("Este professor esta temporariamente sem vagas para Mentoria.");
        }
    }

    private void EnsureAlunoAtivo(Guid alunoId)
    {
        if (!_db.Usuarios.OfType<Aluno>().Any(a => a.Id == alunoId && a.Ativo))
        {
            throw new DomainException("Aluno nao encontrado ou inativo.");
        }
    }

    private MentoriaResponse ToMentoriaResponse(MentoriaAlunoProfessor mentoria)
    {
        var alunoNome = _db.Usuarios.OfType<Aluno>().Where(a => a.Id == mentoria.AlunoId).Select(a => a.Nome).SingleOrDefault() ?? "Aluno";
        var professorNome = _db.Professores.Where(p => p.Id == mentoria.ProfessorId).Select(p => p.Nome).SingleOrDefault() ?? "Professor";

        return new MentoriaResponse(
            mentoria.Id,
            mentoria.AlunoId,
            alunoNome,
            mentoria.ProfessorId,
            professorNome,
            mentoria.Slot,
            mentoria.Ativo,
            mentoria.DataInicioUtc,
            mentoria.DataFimUtc,
            mentoria.DataUltimaTrocaUtc,
            mentoria.MotivoEncerramento?.ToString());
    }

    private string GetAlunoNome(Guid alunoId) => _db.Usuarios.OfType<Aluno>().Where(a => a.Id == alunoId).Select(a => a.Nome).SingleOrDefault() ?? "Aluno";
    private string GetProfessorNome(Guid professorId) => _db.Professores.Where(p => p.Id == professorId).Select(p => p.Nome).SingleOrDefault() ?? "Professor";

    private AcompanhamentoMentoriaResponse ToAcompanhamentoResponse(AcompanhamentoMentoria acompanhamento)
    {
        var aula = _db.Aulas.Single(a => a.Id == acompanhamento.AulaId);
        var alunoNome = _db.Usuarios.OfType<Aluno>().Where(a => a.Id == acompanhamento.AlunoId).Select(a => a.Nome).SingleOrDefault() ?? "Aluno";
        var professorNome = _db.Professores.Where(p => p.Id == acompanhamento.ProfessorId).Select(p => p.Nome).SingleOrDefault() ?? "Professor";
        var materiaNome = _db.Materias.Where(m => m.Id == aula.MateriaId).Select(m => m.Nome).SingleOrDefault() ?? "Materia";

        return new AcompanhamentoMentoriaResponse(
            acompanhamento.Id,
            acompanhamento.AulaId,
            acompanhamento.AlunoId,
            alunoNome,
            acompanhamento.ProfessorId,
            professorNome,
            acompanhamento.VinculoMentoriaId,
            materiaNome,
            aula.InicioUtc,
            acompanhamento.Resumo,
            acompanhamento.PontosFortes,
            acompanhamento.PontosMelhorar,
            acompanhamento.ProximosPassos,
            acompanhamento.MetaAtual,
            acompanhamento.Observacoes,
            acompanhamento.Status,
            acompanhamento.CreatedAtUtc,
            acompanhamento.DataConclusaoUtc);
    }
}
