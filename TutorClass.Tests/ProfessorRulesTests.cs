using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;

namespace TutorClass.Tests;

public sealed class ProfessorRulesTests
{
    [Fact]
    public void Preco_por_hora_deve_ser_maior_que_zero()
    {
        var professor = new Professor("Carlos Souza", "carlos@email.com", "hash");

        Assert.Throws<DomainException>(() => professor.AtualizarPerfilProfissional("Professor de fisica.", 0));
    }

    [Fact]
    public void Qualificacao_nova_comeca_pendente()
    {
        var professor = new Professor("Carlos Souza", "carlos@email.com", "hash");

        var qualificacao = professor.AdicionarQualificacao(
            TipoQualificacaoProfessor.Certificacao,
            "Certificado TOEFL",
            "ETS",
            "Certificacao de idioma.",
            new DateOnly(2025, 6, 10),
            "https://storage.example/certificado.pdf");

        Assert.Equal(StatusVerificacaoQualificacao.Pendente, qualificacao.StatusVerificacao);
    }

    [Fact]
    public void Qualificacao_pendente_pode_virar_verificada()
    {
        var professor = new Professor("Carlos Souza", "carlos@email.com", "hash");
        var qualificacao = professor.AdicionarQualificacao(
            TipoQualificacaoProfessor.Curso,
            "Curso de Quimica",
            "Instituicao Teste",
            null,
            null,
            null);

        qualificacao.Verificar();

        Assert.Equal(StatusVerificacaoQualificacao.Verificada, qualificacao.StatusVerificacao);
        Assert.NotNull(qualificacao.DataVerificacaoUtc);
    }

    [Fact]
    public void Qualificacao_pendente_pode_virar_rejeitada()
    {
        var professor = new Professor("Carlos Souza", "carlos@email.com", "hash");
        var qualificacao = professor.AdicionarQualificacao(
            TipoQualificacaoProfessor.Curso,
            "Curso de Quimica",
            "Instituicao Teste",
            null,
            null,
            null);

        qualificacao.Rejeitar();

        Assert.Equal(StatusVerificacaoQualificacao.Rejeitada, qualificacao.StatusVerificacao);
        Assert.NotNull(qualificacao.DataVerificacaoUtc);
    }

    [Fact]
    public void Qualificacao_verificada_nao_pode_ser_verificada_novamente()
    {
        var professor = new Professor("Carlos Souza", "carlos@email.com", "hash");
        var qualificacao = professor.AdicionarQualificacao(
            TipoQualificacaoProfessor.Curso,
            "Curso de Quimica",
            "Instituicao Teste",
            null,
            null,
            null);

        qualificacao.Verificar();

        Assert.Throws<DomainException>(() => qualificacao.Verificar());
    }

    [Fact]
    public void Disponibilidade_deve_ter_horario_final_maior_que_inicial()
    {
        var professor = new Professor("Carlos Souza", "carlos@email.com", "hash");

        Assert.Throws<DomainException>(() => professor.CadastrarDisponibilidade(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(9, 0)));
    }
}
