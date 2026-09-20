using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Materials;
using TutorClass.Domain.Notifications;
using TutorClass.Domain.Users;
using TutorClass.Domain.Plans;
using TutorClass.Domain.Teachers;

namespace TutorClass.Application.Services;

public sealed class MaterialPedagogicoService
{
    private readonly IApplicationDbContext _db;
    private readonly NotificacaoService _notificacoes;

    public MaterialPedagogicoService(IApplicationDbContext db)
        : this(db, new NotificacaoService(db))
    {
    }

    public MaterialPedagogicoService(IApplicationDbContext db, NotificacaoService notificacoes)
    {
        _db = db;
        _notificacoes = notificacoes;
    }

    public async Task<MaterialResponse> CriarAsync(Guid professorId, CriarMaterialRequest request, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        EnsureAlunoAtivo(request.AlunoId);

        var aula = ResolveAula(professorId, request.AlunoId, request.AulaId, request.MateriaId);
        var materiaId = aula?.MateriaId ?? request.MateriaId ?? throw new DomainException("Materia e obrigatoria quando AulaId nao e informado.");

        EnsureMateriaExists(materiaId);
        EnsureRelacaoPedagogica(professorId, request.AlunoId, materiaId, aula);

        var material = new MaterialPedagogico(
            professorId,
            request.AlunoId,
            materiaId,
            aula?.Id,
            request.Titulo,
            request.Descricao,
            request.Tipo,
            request.NomeArquivo,
            request.UrlArquivo);

        _db.AddEntity(material);
        CriarNotificacaoNovoMaterial(material);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(material);
    }

    public async Task<MaterialResponse> AtualizarAsync(Guid materialId, Guid professorId, AtualizarMaterialRequest request, CancellationToken cancellationToken)
    {
        var material = await _db.FindAsync<MaterialPedagogico>(materialId, cancellationToken)
            ?? throw new DomainException("Material nao encontrado.");

        if (material.ProfessorId != professorId)
        {
            throw new UnauthorizedAccessException("Professor nao pode editar material de outro professor.");
        }

        material.Atualizar(request.Titulo, request.Descricao, request.Tipo, request.NomeArquivo, request.UrlArquivo);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(material);
    }

    public async Task ExcluirAsync(Guid materialId, Guid professorId, CancellationToken cancellationToken)
    {
        var material = await _db.FindAsync<MaterialPedagogico>(materialId, cancellationToken)
            ?? throw new DomainException("Material nao encontrado.");

        if (material.ProfessorId != professorId)
        {
            throw new UnauthorizedAccessException("Professor nao pode excluir material de outro professor.");
        }

        _db.RemoveEntity(material);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<MaterialResponse?> BuscarAsync(Guid materialId, Guid usuarioId, bool administrador, bool responsavel, CancellationToken cancellationToken)
    {
        var material = _db.MateriaisPedagogicos.SingleOrDefault(m => m.Id == materialId);
        if (material is null)
        {
            return Task.FromResult<MaterialResponse?>(null);
        }

        EnsureCanView(material, usuarioId, administrador, responsavel);
        return Task.FromResult<MaterialResponse?>(ToResponse(material));
    }

    public Task<IReadOnlyCollection<MaterialResponse>> ListarMeusAsync(Guid usuarioId, bool administrador, bool responsavel, CancellationToken cancellationToken)
    {
        var materiais = _db.MateriaisPedagogicos.AsEnumerable();

        if (!administrador)
        {
            materiais = responsavel
                ? materiais.Where(m => _db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == usuarioId && ra.AlunoId == m.AlunoId))
                : materiais.Where(m => m.AlunoId == usuarioId || m.ProfessorId == usuarioId);
        }

        var result = materiais
            .OrderByDescending(m => m.CreatedAtUtc)
            .Select(ToResponse)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<MaterialResponse>>(result);
    }

    private Aula? ResolveAula(Guid professorId, Guid alunoId, Guid? aulaId, Guid? materiaId)
    {
        if (aulaId is null)
        {
            return null;
        }

        var aula = _db.Aulas.SingleOrDefault(a => a.Id == aulaId.Value)
            ?? throw new DomainException("Aula nao encontrada.");

        if (aula.ProfessorId != professorId || aula.AlunoId != alunoId)
        {
            throw new DomainException("Aula informada nao corresponde ao professor e aluno do material.");
        }

        if (materiaId is not null && aula.MateriaId != materiaId.Value)
        {
            throw new DomainException("Materia informada nao corresponde a aula.");
        }

        if (aula.Status == StatusAula.Cancelada)
        {
            throw new DomainException("Material nao pode ser vinculado a aula cancelada.");
        }

        return aula;
    }

    private void EnsureRelacaoPedagogica(Guid professorId, Guid alunoId, Guid materiaId, Aula? aula)
    {
        if (aula is not null)
        {
            return;
        }

        var possuiAulaValida = _db.Aulas.Any(a =>
            a.ProfessorId == professorId &&
            a.AlunoId == alunoId &&
            a.MateriaId == materiaId &&
            a.Status != StatusAula.Cancelada);

        if (!possuiAulaValida && !PossuiMentoriaPedagogica(professorId, alunoId, materiaId))
        {
            throw new DomainException("Professor nao possui relacao pedagogica valida com o aluno para a materia informada.");
        }
    }

    private bool PossuiMentoriaPedagogica(Guid professorId, Guid alunoId, Guid materiaId)
    {
        return _db.Professores.Any(p => p.Id == professorId && p.Ativo && p.Status == StatusProfessor.Aprovado && p.OfereceMentoria)
            && _db.ProfessoresMaterias.Any(pm => pm.ProfessorId == professorId && pm.MateriaId == materiaId)
            && _db.MentoriasAlunoProfessor.Any(m => m.ProfessorId == professorId && m.AlunoId == alunoId && m.Ativo)
            && _db.AssinaturasAluno.Any(a => a.AlunoId == alunoId && a.Ativa && a.Plano == PlanoAluno.Mentoria);
    }

    public Task<IReadOnlyCollection<DestinatarioMaterialResponse>> ListarDestinatariosAsync(Guid professorId, CancellationToken cancellationToken)
    {
        EnsureProfessorAtivo(professorId);
        var aulas = _db.Aulas.Where(a => a.ProfessorId == professorId && a.Status != StatusAula.Cancelada).ToArray();
        var pares = aulas.Select(a => (a.AlunoId, a.MateriaId)).ToHashSet();
        var materias = _db.ProfessoresMaterias.Where(m => m.ProfessorId == professorId).Select(m => m.MateriaId).ToArray();
        var mentorados = _db.MentoriasAlunoProfessor.Where(m => m.ProfessorId == professorId && m.Ativo).Select(m => m.AlunoId).ToArray();
        foreach (var alunoId in mentorados)
            foreach (var materiaId in materias)
                if (PossuiMentoriaPedagogica(professorId, alunoId, materiaId))
                    pares.Add((alunoId, materiaId));

        var result = new List<DestinatarioMaterialResponse>();
        foreach (var (alunoId, materiaId) in pares)
        {
            var aluno = _db.Usuarios.OfType<Aluno>().SingleOrDefault(a => a.Id == alunoId && a.Ativo);
            var materia = _db.Materias.SingleOrDefault(m => m.Id == materiaId);
            if (aluno is null || materia is null) continue;
            result.Add(new DestinatarioMaterialResponse(aluno.Id, aluno.Nome, materia.Id, materia.Nome,
                PossuiMentoriaPedagogica(professorId, aluno.Id, materia.Id),
                aulas.Where(a => a.AlunoId == aluno.Id && a.MateriaId == materia.Id).OrderByDescending(a => a.InicioUtc)
                    .Select(a => new AulaMaterialOpcao(a.Id, a.InicioUtc, a.Status.ToString())).ToArray()));
        }
        return Task.FromResult<IReadOnlyCollection<DestinatarioMaterialResponse>>(result.OrderBy(r => r.AlunoNome).ThenBy(r => r.MateriaNome).ToArray());
    }

    private void EnsureCanView(MaterialPedagogico material, Guid usuarioId, bool administrador, bool responsavel)
    {
        if (administrador || material.AlunoId == usuarioId || material.ProfessorId == usuarioId)
        {
            return;
        }

        if (responsavel && _db.ResponsaveisAlunos.Any(ra => ra.ResponsavelId == usuarioId && ra.AlunoId == material.AlunoId))
        {
            return;
        }

        throw new UnauthorizedAccessException("Usuario nao pode consultar material de outro aluno.");
    }

    private void EnsureProfessorAtivo(Guid professorId)
    {
        if (!_db.Professores.Any(p => p.Id == professorId && p.Ativo))
        {
            throw new DomainException("Professor nao encontrado ou inativo.");
        }
    }

    private void EnsureAlunoAtivo(Guid alunoId)
    {
        if (!_db.Usuarios.OfType<Aluno>().Any(a => a.Id == alunoId && a.Ativo))
        {
            throw new DomainException("Aluno nao encontrado ou inativo.");
        }
    }

    private void EnsureMateriaExists(Guid materiaId)
    {
        if (!_db.Materias.Any(m => m.Id == materiaId))
        {
            throw new DomainException("Materia nao encontrada.");
        }
    }

    private void CriarNotificacaoNovoMaterial(MaterialPedagogico material)
    {
        var professorNome = _db.Professores
            .Where(p => p.Id == material.ProfessorId)
            .Select(p => p.Nome)
            .SingleOrDefault() ?? "Seu professor";

        _notificacoes.CriarSeNaoExistir(
            material.AlunoId,
            TipoNotificacao.NovoMaterial,
            "Novo material disponivel",
            $"{professorNome} disponibilizou o material \"{material.Titulo}\" para voce.",
            "/materiais",
            material.Id,
            $"NOVO_MATERIAL:{material.Id}:{material.AlunoId}");
    }

    private MaterialResponse ToResponse(MaterialPedagogico material)
    {
        var professorNome = _db.Professores.Where(p => p.Id == material.ProfessorId).Select(p => p.Nome).SingleOrDefault() ?? "Professor";
        var alunoNome = _db.Usuarios.OfType<Aluno>().Where(a => a.Id == material.AlunoId).Select(a => a.Nome).SingleOrDefault() ?? "Aluno";
        var materiaNome = _db.Materias.Where(m => m.Id == material.MateriaId).Select(m => m.Nome).SingleOrDefault() ?? "Materia";

        return new MaterialResponse(
            material.Id,
            material.ProfessorId,
            professorNome,
            material.AlunoId,
            alunoNome,
            material.MateriaId,
            materiaNome,
            material.AulaId,
            material.Titulo,
            material.Descricao,
            material.Tipo,
            material.NomeArquivo,
            material.UrlArquivo,
            material.CreatedAtUtc,
            material.UpdatedAtUtc);
    }
}
