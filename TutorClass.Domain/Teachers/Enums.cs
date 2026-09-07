namespace TutorClass.Domain.Teachers;

public enum StatusProfessor
{
    Candidato = 1,
    Aprovado = 2,
    Reprovado = 3,
    Suspenso = 4
}

public enum StatusCandidaturaProfessor
{
    Pendente = 1,
    EmAnalise = 2,
    AguardandoDocumentacao = 3,
    Aprovada = 4,
    Reprovada = 5
}

public enum TipoQualificacaoProfessor
{
    Graduacao = 1,
    PosGraduacao = 2,
    Curso = 3,
    Certificacao = 4,
    Experiencia = 5,
    Outro = 6
}

public enum StatusVerificacaoQualificacao
{
    Pendente = 1,
    Verificada = 2,
    Rejeitada = 3
}

public enum NivelReputacaoProfessor
{
    Novo = 1,
    Verificado = 2,
    Experiente = 3,
    Destaque = 4,
    Referencia = 5
}
