using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Lessons;
using TutorClass.Domain.Materials;
using TutorClass.Domain.Users;

namespace TutorClass.Application.Services;

public sealed class MaterialPedagogicoService
{
    private readonly IApplicationDbContext _db;

    public MaterialPedagogicoService(IApplicationDbContext db)
    {
        _db = db;
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

        if (!possuiAulaValida)
        {
            throw new DomainException("Professor nao possui relacao pedagogica valida com o aluno para a materia informada.");
        }
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
