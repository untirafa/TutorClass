namespace TutorClass.Domain.Moderation;

public enum TipoAlvoDenuncia
{
    Professor = 1,
    Avaliacao = 2,
    Mensagem = 3
}

public enum MotivoDenuncia
{
    ConteudoOfensivo = 1,
    AssedioOuAmeaca = 2,
    Discriminacao = 3,
    PerfilFalsoOuGolpe = 4,
    DadosPessoais = 5,
    Outro = 6
}

public enum StatusDenuncia
{
    Aberta = 1,
    EmAnalise = 2,
    Procedente = 3,
    Improcedente = 4
}

public enum SancaoDenuncia
{
    Nenhuma = 0,
    Advertencia = 1,
    Suspensao = 2
}
