namespace TutorClass.Application.DTOs;

public sealed record DevelopmentTestDataDto(
    DevelopmentUsuarioDto Administrador,
    IReadOnlyCollection<DevelopmentUsuarioDto> Alunos,
    IReadOnlyCollection<DevelopmentResponsavelDto> Responsaveis,
    IReadOnlyCollection<DevelopmentProfessorDto> ProfessoresAprovados,
    IReadOnlyCollection<DevelopmentProfessorDto> ProfessoresPendentes,
    IReadOnlyCollection<DevelopmentPlanoAlunoDto> PlanosAlunos,
    IReadOnlyCollection<DevelopmentMentoriaDto> Mentorias,
    DevelopmentFinanceiroDto? FinanceiroDemonstracao,
    IReadOnlyCollection<DevelopmentGravacaoDto> GravacoesSimuladas,
    IReadOnlyCollection<DevelopmentFaqDto> PerguntasFrequentes,
    IReadOnlyCollection<DevelopmentNotificacaoDto> Notificacoes,
    string SenhaPadrao);

public sealed record DevelopmentUsuarioDto(Guid Id, string Nome, string Email);

public sealed record DevelopmentResponsavelDto(
    Guid Id,
    string Nome,
    string Email,
    IReadOnlyCollection<DevelopmentAlunoVinculadoDto> AlunosVinculados);

public sealed record DevelopmentAlunoVinculadoDto(Guid Id, string Nome, string Email, string Parentesco);

public sealed record DevelopmentProfessorDto(
    Guid Id,
    string Nome,
    string Email,
    string StatusProfessor,
    string StatusCandidatura,
    IReadOnlyCollection<string> Materias,
    IReadOnlyCollection<string> Qualificacoes,
    decimal? PrecoHora);

public sealed record DevelopmentPlanoAlunoDto(Guid AlunoId, string AlunoNome, string Plano);

public sealed record DevelopmentMentoriaDto(Guid AlunoId, string AlunoNome, Guid ProfessorId, string ProfessorNome, int Slot, bool Ativo);

public sealed record DevelopmentFinanceiroDto(
    Guid AlunoId,
    string AlunoNome,
    Guid PagadorUsuarioId,
    string PagadorNome,
    Guid ProfessorId,
    string ProfessorNome,
    Guid AulaId,
    decimal ValorBrutoAula,
    decimal ValorComissaoTutorClass,
    decimal ValorLiquidoProfessor,
    decimal AdicionalMentoria);

public sealed record DevelopmentGravacaoDto(
    Guid AulaId,
    Guid GravacaoId,
    string Titulo,
    string AlunoNome,
    string ProfessorNome,
    string PlanoAluno,
    bool Disponivel,
    string UrlVideo);

public sealed record DevelopmentFaqDto(Guid Id, string Pergunta, string Categoria, bool Ativo, int OrdemExibicao);

public sealed record DevelopmentNotificacaoDto(Guid Id, Guid UsuarioId, string UsuarioNome, string Tipo, string Titulo, bool Lida);
