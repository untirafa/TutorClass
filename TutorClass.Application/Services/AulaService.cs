using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Subjects;
using TutorClass.Domain.Teachers;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class AulaService
{
    private readonly IApplicationDbContext _db;
    private readonly MentoriaService _mentorias;
    private readonly PlanoAlunoService _planos;
    private readonly NotificacaoService _notificacoes;
    private readonly ResponsavelAlunoService _responsaveis;

    public AulaService(IApplicationDbContext db)
        : this(db, new MentoriaService(db), new PlanoAlunoService(db), new NotificacaoService(db), new ResponsavelAlunoService(db))
    {
    }

    public AulaService(IApplicationDbContext db, MentoriaService mentorias)
        : this(db, mentorias, new PlanoAlunoService(db), new NotificacaoService(db), new ResponsavelAlunoService(db))
    {
    }

    public AulaService(IApplicationDbContext db, MentoriaService mentorias, PlanoAlunoService planos)
        : this(db, mentorias, planos, new NotificacaoService(db), new ResponsavelAlunoService(db))
    {
    }

    public AulaService(
        IApplicationDbContext db,
        MentoriaService mentorias,
        PlanoAlunoService planos,
        NotificacaoService notificacoes,
        ResponsavelAlunoService responsaveis)
    {
        _db = db;
        _mentorias = mentorias;
        _planos = planos;
        _notificacoes = notificacoes;
        _responsaveis = responsaveis;
    }

    public async Task<AulaDetalhesResponse> SolicitarAsync(Guid alunoId, SolicitarAulaRequest request, CancellationToken cancellationToken)
    {
        return await SolicitarParaAlunoAsync(alunoId, alunoId, request, cancellationToken);
    }

    public async Task<AulaDetalhesResponse> SolicitarParaAlunoAsync(Guid alunoId, Guid solicitadaPorUsuarioId, SolicitarAulaRequest request, CancellationToken cancellationToken)
    {
        var aluno = _db.Usuarios.OfType<Aluno>().SingleOrDefault(a => a.Id == alunoId && a.Ativo)
            ?? throw new DomainException("Aluno nao encontrado ou inativo.");

        if (solicitadaPorUsuarioId != alunoId)
        {
            _responsaveis.ValidarVinculo(solicitadaPorUsuarioId, alunoId);
        }

        var professor = _db.Professores.SingleOrDefault(p => p.Id == request.ProfessorId && p.Ativo)
            ?? throw new DomainException("Professor nao encontrado ou inativo.");

        if (professor.Status != StatusProfessor.Aprovado ||
            !_db.CandidaturasProfessor.Any(c => c.ProfessorId == professor.Id && c.Status == StatusCandidaturaProfessor.Aprovada))
        {
            throw new DomainException("Somente professores aprovados podem receber solicitacoes de aula.");
        }

        var materia = _db.Materias.SingleOrDefault(m => m.Id == request.MateriaId)
            ?? throw new DomainException("Materia nao encontrada.");

        if (!_db.ProfessoresMaterias.Any(pm => pm.ProfessorId == professor.Id && pm.MateriaId == materia.Id))
        {
            throw new DomainException("Professor nao leciona a materia informada.");
        }

        var inicioUtc = NormalizeUtc(request.Inicio);
        if (inicioUtc <= DateTime.UtcNow)
        {
            throw new DomainException("Horario da aula nao pode estar no passado.");
        }

        var fimUtc = inicioUtc.AddMinutes(request.DuracaoMinutos);
        EnsureWithinAvailability(professor.Id, inicioUtc, fimUtc);
        EnsureNoConflicts(aluno.Id, professor.Id, inicioUtc, fimUtc);

        if (professor.PrecoHora is null)
        {
            throw new DomainException("Professor nao possui preco por hora definido.");
        }

        var precoAcordado = Math.Round(professor.PrecoHora.Value * request.DuracaoMinutos / 60m, 2);
        var aula = new Aula(aluno.Id, professor.Id, materia.Id, inicioUtc, request.DuracaoMinutos, precoAcordado, solicitadaPorUsuarioId);

        _db.AddEntity(aula);
        var solicitanteNome = GetUsuarioNome(solicitadaPorUsuarioId);
        var mensagem = solicitadaPorUsuarioId == aluno.Id
            ? $"{aluno.Nome} solicitou uma aula de {materia.Nome} para {FormatDate(inicioUtc)}."
            : $"{solicitanteNome} solicitou uma aula de {materia.Nome} para {aluno.Nome} em {FormatDate(inicioUtc)}.";

        _notificacoes.CriarSeNaoExistir(
            professor.Id,
            TipoNotificacao.AulaSolicitada,
            "Nova aula solicitada",
            mensagem,
            "/professor/aulas",
            aula.Id,
            $"AULA_SOLICITADA:{aula.Id}:{professor.Id}",
            PrioridadeNotificacao.Importante);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDetalhes(aula);
    }

    public Task<AulaDetalhesResponse?> BuscarAsync(Guid aulaId, Guid usuarioId, bool administrador, CancellationToken cancellationToken)
    {
        var aula = _db.Aulas.SingleOrDefault(a => a.Id == aulaId);
        if (aula is null)
        {
            return Task.FromResult<AulaDetalhesResponse?>(null);
        }

        EnsureCanView(aula, usuarioId, administrador);
        return Task.FromResult<AulaDetalhesResponse?>(ToDetalhes(aula));
    }

    public Task<IReadOnlyCollection<AulaResumoResponse>> ListarMinhasAsync(Guid usuarioId, bool administrador, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var alunos = _db.Usuarios.OfType<Aluno>();
        var responsaveis = _db.Usuarios.OfType<Responsavel>();
        var vinculos = _db.ResponsaveisAlunos;
        var query = _db.Aulas;
        if (!administrador)
        {
            query = query.Where(a => a.AlunoId == usuarioId || a.ProfessorId == usuarioId ||
                (responsaveis.Any(r => r.Id == usuarioId && r.Ativo) &&
                 alunos.Any(aluno => aluno.Id == a.AlunoId && aluno.Ativo) &&
                 vinculos.Any(v => v.ResponsavelId == usuarioId && v.AlunoId == a.AlunoId)));
        }

        // Keep authorization and mapping in one query; no nested commands with an open reader.
        var aulas = (from aula in query
                     join aluno in alunos on aula.AlunoId equals aluno.Id
                     join professor in _db.Professores on aula.ProfessorId equals professor.Id
                     join materia in _db.Materias on aula.MateriaId equals materia.Id
                     orderby aula.InicioUtc, aula.Id
                     select new AulaResumoResponse(aula.Id, professor.Nome, aluno.Nome,
                         aula.SolicitadaPorUsuarioId, materia.Nome, aula.InicioUtc,
                         aula.DuracaoMinutos, aula.PrecoAcordado, aula.Status)).ToArray();
        return Task.FromResult<IReadOnlyCollection<AulaResumoResponse>>(aulas);
    }

    public async Task<AulaDetalhesResponse> ConfirmarAsync(Guid aulaId, Guid professorId, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        var aula = await _db.FindAsync<Aula>(aulaId, cancellationToken)
            ?? throw new DomainException("Aula nao encontrada.");

        EnsureProfessorOwner(aula, professorId);
        aula.Confirmar(professorId);
        NotificarAulaConfirmada(aula);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDetalhes(aula);
    }

    public async Task<AulaDetalhesResponse> CancelarAsync(Guid aulaId, Guid usuarioId, string? motivo, CancellationToken cancellationToken)
    {
        var aula = await _db.FindAsync<Aula>(aulaId, cancellationToken)
            ?? throw new DomainException("Aula nao encontrada.");

        var autorizadoPorVinculo = false;
        if (aula.AlunoId != usuarioId && aula.ProfessorId != usuarioId)
        {
            autorizadoPorVinculo = _responsaveis.PossuiVinculo(usuarioId, aula.AlunoId);
            if (!autorizadoPorVinculo)
            {
                throw new UnauthorizedAccessException("Usuario nao pode cancelar aula de outro usuario.");
            }
        }

        aula.Cancelar(usuarioId, motivo, autorizadoPorVinculo);
        NotificarAulaCancelada(aula, usuarioId);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDetalhes(aula);
    }

    public async Task<AulaDetalhesResponse> ReagendarAsync(Guid aulaId, Guid usuarioId, ReagendarAulaRequest request, CancellationToken cancellationToken)
    {
        var aula = await _db.FindAsync<Aula>(aulaId, cancellationToken)
            ?? throw new DomainException("Aula nao encontrada.");

        EnsureCanReschedule(aula, usuarioId);

        var novoInicioUtc = NormalizeUtc(request.Inicio);
        if (novoInicioUtc <= DateTime.UtcNow)
        {
            throw new DomainException("Novo horario da aula nao pode estar no passado.");
        }

        var novaDuracao = request.DuracaoMinutos ?? aula.DuracaoMinutos;
        var novoFimUtc = novoInicioUtc.AddMinutes(novaDuracao);

        EnsureWithinAvailability(aula.ProfessorId, novoInicioUtc, novoFimUtc);
        EnsureNoConflicts(aula.AlunoId, aula.ProfessorId, novoInicioUtc, novoFimUtc, aula.Id);

        var historico = new AulaReagendamento(
            aula.Id,
            aula.InicioUtc,
            aula.FimUtc,
            novoInicioUtc,
            novoFimUtc,
            usuarioId);

        aula.Reagendar(novoInicioUtc, novaDuracao);
        _db.AddEntity(historico);
        NotificarAulaReagendada(aula, historico, usuarioId);
        await _db.SaveChangesAsync(cancellationToken);
        return ToDetalhes(aula);
    }

    public Task<IReadOnlyCollection<AulaReagendamentoResponse>> ListarReagendamentosAsync(Guid aulaId, Guid usuarioId, bool administrador, CancellationToken cancellationToken)
    {
        var aula = _db.Aulas.SingleOrDefault(a => a.Id == aulaId)
            ?? throw new DomainException("Aula nao encontrada.");

        EnsureCanView(aula, usuarioId, administrador);

        var historico = _db.AulasReagendamentos
            .Where(r => r.AulaId == aulaId)
            .OrderBy(r => r.CreatedAtUtc)
            .ToArray()
            .Select(r => new AulaReagendamentoResponse(
                r.Id,
                r.AulaId,
                r.InicioAnteriorUtc,
                r.FimAnteriorUtc,
                r.InicioNovoUtc,
                r.FimNovoUtc,
                r.SolicitadoPorUsuarioId,
                r.CreatedAtUtc))
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<AulaReagendamentoResponse>>(historico);
    }

    public async Task<AulaDetalhesResponse> ConcluirAsync(Guid aulaId, Guid professorId, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        var aula = await _db.FindAsync<Aula>(aulaId, cancellationToken)
            ?? throw new DomainException("Aula nao encontrada.");

        EnsureProfessorOwner(aula, professorId);
        aula.Concluir(professorId);

        var professor = await _db.FindAsync<Professor>(professorId, cancellationToken)
            ?? throw new DomainException("Professor nao encontrado.");
        professor.RegistrarAulaConcluida();

        await _db.SaveChangesAsync(cancellationToken);
        var acompanhamentoMentoriaId = await _mentorias.CriarAcompanhamentoPendenteParaAulaAsync(aula, cancellationToken);
        return ToDetalhes(
            aula,
            aulaConcluida: true,
            requerAcompanhamentoMentoria: acompanhamentoMentoriaId is not null,
            acompanhamentoMentoriaId: acompanhamentoMentoriaId);
    }

    private void EnsureWithinAvailability(Guid professorId, DateTime inicioUtc, DateTime fimUtc)
    {
        var inicio = TimeOnly.FromDateTime(inicioUtc);
        var fim = TimeOnly.FromDateTime(fimUtc);
        var dentroDaDisponibilidade = _db.DisponibilidadesProfessor.Any(d =>
            d.ProfessorId == professorId &&
            d.DiaSemana == inicioUtc.DayOfWeek &&
            d.Inicio <= inicio &&
            d.Fim >= fim);

        if (!dentroDaDisponibilidade)
        {
            throw new DomainException("Horario solicitado esta fora da disponibilidade do professor.");
        }
    }

    private void EnsureNoConflicts(Guid alunoId, Guid professorId, DateTime inicioUtc, DateTime fimUtc, Guid? aulaIgnoradaId = null)
    {
        var aulasAtivas = _db.Aulas
            .Where(a => a.Status == StatusAula.Solicitada || a.Status == StatusAula.Confirmada)
            .Where(a => aulaIgnoradaId == null || a.Id != aulaIgnoradaId.Value)
            .AsEnumerable();

        if (aulasAtivas.Any(a => a.ProfessorId == professorId && a.Sobrepoe(inicioUtc, fimUtc)))
        {
            throw new DomainException("Professor ja possui aula sobreposta nesse horario.");
        }

        if (aulasAtivas.Any(a => a.AlunoId == alunoId && a.Sobrepoe(inicioUtc, fimUtc)))
        {
            throw new DomainException("Aluno ja possui aula sobreposta nesse horario.");
        }
    }

    private void NotificarAulaConfirmada(Aula aula)
    {
        var professorNome = GetProfessorNome(aula.ProfessorId);
        var materiaNome = GetMateriaNome(aula.MateriaId);

        _notificacoes.CriarSeNaoExistir(
            aula.AlunoId,
            TipoNotificacao.AulaConfirmada,
            $"{professorNome} confirmou sua aula",
            $"Sua aula de {materiaNome} foi confirmada para {FormatDate(aula.InicioUtc)}.",
            "/minhas-aulas",
            aula.Id,
            $"AULA_CONFIRMADA:{aula.Id}:{aula.AlunoId}",
            PrioridadeNotificacao.Importante);
    }

    private void NotificarAulaCancelada(Aula aula, Guid canceladaPorUsuarioId)
    {
        var alunoNome = GetAlunoNome(aula.AlunoId);
        var professorNome = GetProfessorNome(aula.ProfessorId);
        var usuarioNome = GetUsuarioNome(canceladaPorUsuarioId);
        var materiaNome = GetMateriaNome(aula.MateriaId);

        if (canceladaPorUsuarioId != aula.AlunoId)
        {
            _notificacoes.CriarSeNaoExistir(
                aula.AlunoId,
                TipoNotificacao.AulaCancelada,
                "Aula cancelada",
                $"Sua aula de {materiaNome} com {professorNome} foi cancelada por {usuarioNome}.",
                "/minhas-aulas",
                aula.Id,
                $"AULA_CANCELADA:{aula.Id}:{aula.AlunoId}",
                PrioridadeNotificacao.Importante);
        }

        if (canceladaPorUsuarioId != aula.ProfessorId)
        {
            _notificacoes.CriarSeNaoExistir(
                aula.ProfessorId,
                TipoNotificacao.AulaCancelada,
                "Aula cancelada",
                $"A aula de {materiaNome} com {alunoNome} foi cancelada por {usuarioNome}.",
                "/professor/aulas",
                aula.Id,
                $"AULA_CANCELADA:{aula.Id}:{aula.ProfessorId}",
                PrioridadeNotificacao.Importante);
        }
    }

    private void NotificarAulaReagendada(Aula aula, AulaReagendamento historico, Guid solicitadoPorUsuarioId)
    {
        var alunoNome = GetAlunoNome(aula.AlunoId);
        var professorNome = GetProfessorNome(aula.ProfessorId);
        var usuarioNome = GetUsuarioNome(solicitadoPorUsuarioId);
        var materiaNome = GetMateriaNome(aula.MateriaId);

        if (solicitadoPorUsuarioId != aula.AlunoId)
        {
            _notificacoes.CriarSeNaoExistir(
                aula.AlunoId,
                TipoNotificacao.AulaReagendada,
                "Aula reagendada",
                $"Sua aula de {materiaNome} com {professorNome} foi reagendada para {FormatDate(aula.InicioUtc)} por {usuarioNome}.",
                "/minhas-aulas",
                aula.Id,
                $"AULA_REAGENDADA:{historico.Id}:{aula.AlunoId}",
                PrioridadeNotificacao.Importante);
        }

        if (solicitadoPorUsuarioId != aula.ProfessorId)
        {
            _notificacoes.CriarSeNaoExistir(
                aula.ProfessorId,
                TipoNotificacao.AulaReagendada,
                "Aula reagendada",
                $"A aula de {materiaNome} com {alunoNome} foi reagendada para {FormatDate(aula.InicioUtc)} por {usuarioNome}.",
                "/professor/aulas",
                aula.Id,
                $"AULA_REAGENDADA:{historico.Id}:{aula.ProfessorId}",
                PrioridadeNotificacao.Importante);
        }
    }

    private void EnsureProfessorAtivo(Guid professorId)
    {
        if (!_db.Professores.Any(p => p.Id == professorId && p.Ativo))
        {
            throw new DomainException("Professor nao encontrado ou inativo.");
        }
    }

    private void EnsureCanView(Aula aula, Guid usuarioId, bool administrador)
    {
        if (!administrador && aula.AlunoId != usuarioId && aula.ProfessorId != usuarioId && !_responsaveis.PossuiVinculo(usuarioId, aula.AlunoId))
        {
            throw new UnauthorizedAccessException("Usuario nao pode consultar aula de outro usuario.");
        }
    }

    private static void EnsureProfessorOwner(Aula aula, Guid professorId)
    {
        if (aula.ProfessorId != professorId)
        {
            throw new UnauthorizedAccessException("Professor nao pode manipular aula de outro professor.");
        }
    }

    private void EnsureCanReschedule(Aula aula, Guid usuarioId)
    {
        if (aula.AlunoId == usuarioId || aula.ProfessorId == usuarioId)
        {
            return;
        }

        try
        {
            _responsaveis.ValidarVinculo(usuarioId, aula.AlunoId);
        }
        catch (UnauthorizedAccessException)
        {
            throw new UnauthorizedAccessException("Usuario nao pode reagendar aula de outro usuario.");
        }
    }

    private AulaDetalhesResponse ToDetalhes(
        Aula aula,
        bool aulaConcluida = false,
        bool requerAcompanhamentoMentoria = false,
        Guid? acompanhamentoMentoriaId = null)
    {
        var aluno = _db.Usuarios.OfType<Aluno>().Single(a => a.Id == aula.AlunoId);
        var professor = _db.Professores.Single(p => p.Id == aula.ProfessorId);
        var materia = _db.Materias.Single(m => m.Id == aula.MateriaId);

        return new AulaDetalhesResponse(
            aula.Id,
            new ParticipanteAulaDto(professor.Id, professor.Nome),
            new ParticipanteAulaDto(aluno.Id, aluno.Nome),
            aula.SolicitadaPorUsuarioId,
            new MateriaAulaDto(materia.Id, materia.Nome),
            aula.InicioUtc,
            aula.DuracaoMinutos,
            aula.PrecoAcordado,
            aula.Status,
            aula.ConfirmadaEmUtc,
            aula.CanceladaEmUtc,
            aula.ConcluidaEmUtc,
            aula.MotivoCancelamento,
            aulaConcluida,
            requerAcompanhamentoMentoria,
            acompanhamentoMentoriaId);
    }

    private AulaResumoResponse ToResumo(Aula aula)
    {
        var detalhes = ToDetalhes(aula);
        return new AulaResumoResponse(
            detalhes.Id,
            detalhes.Professor.Nome,
            detalhes.Aluno.Nome,
            detalhes.SolicitadaPorUsuarioId,
            detalhes.Materia.Nome,
            detalhes.Inicio,
            detalhes.DuracaoMinutos,
            detalhes.PrecoAcordado,
            detalhes.Status);
    }

    private string GetAlunoNome(Guid alunoId) => _db.Usuarios.OfType<Aluno>().Where(a => a.Id == alunoId).Select(a => a.Nome).SingleOrDefault() ?? "Aluno";
    private string GetProfessorNome(Guid professorId) => _db.Professores.Where(p => p.Id == professorId).Select(p => p.Nome).SingleOrDefault() ?? "Professor";
    private string GetMateriaNome(Guid materiaId) => _db.Materias.Where(m => m.Id == materiaId).Select(m => m.Nome).SingleOrDefault() ?? "Materia";
    private string GetUsuarioNome(Guid usuarioId) => _db.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.Nome).SingleOrDefault() ?? "Usuario";

    private static string FormatDate(DateTime value) => $"{value:dd/MM/yyyy HH:mm} UTC";

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
