using TutorClass.Domain.Common;
using TutorClass.Domain.Teachers;

namespace TutorClass.Tests;

public sealed class CandidaturaProfessorTests
{
    [Fact]
    public void Professor_nao_pode_solicitar_candidatura_sem_perfil_profissional()
    {
        var professor = new Professor("Ana Silva", "ana@email.com", "hash");

        var exception = Assert.Throws<DomainException>(() => professor.SolicitarCandidatura("Quero lecionar."));

        Assert.Equal("Perfil profissional deve estar preenchido antes da candidatura.", exception.Message);
    }

    [Fact]
    public void Professor_nao_pode_ter_duas_candidaturas_ativas()
    {
        var professor = CriarProfessorComPerfil();
        professor.SolicitarCandidatura("Primeira candidatura.");

        Assert.Throws<DomainException>(() => professor.SolicitarCandidatura("Segunda candidatura."));
    }

    [Fact]
    public void Aprovar_candidatura_altera_status_e_encerra_fluxo()
    {
        var professor = CriarProfessorComPerfil();
        var candidatura = professor.SolicitarCandidatura("Tenho experiencia com aulas.");
        var administradorId = Guid.NewGuid();

        candidatura.IniciarAnalise(administradorId);
        candidatura.Aprovar(administradorId);

        Assert.Equal(StatusCandidaturaProfessor.Aprovada, candidatura.Status);
        Assert.False(candidatura.EstaAtiva);
        Assert.Equal(administradorId, candidatura.AdministradorAnaliseId);
    }

    [Fact]
    public void Candidatura_aprovada_nao_pode_ser_aprovada_de_novo()
    {
        var candidatura = CriarProfessorComPerfil().SolicitarCandidatura("Tenho certificados.");
        candidatura.IniciarAnalise(Guid.NewGuid());

        candidatura.Aprovar(Guid.NewGuid());

        Assert.Throws<DomainException>(() => candidatura.Aprovar(Guid.NewGuid()));
    }

    [Fact]
    public void Candidatura_nao_pode_ser_aprovada_sem_estar_em_analise()
    {
        var candidatura = CriarProfessorComPerfil().SolicitarCandidatura("Tenho certificados.");

        Assert.Throws<DomainException>(() => candidatura.Aprovar(Guid.NewGuid()));
    }

    [Fact]
    public void Solicitar_complementacao_exige_motivo()
    {
        var candidatura = CriarProfessorComPerfil().SolicitarCandidatura("Tenho experiencia.");

        Assert.Throws<DomainException>(() => candidatura.SolicitarComplementacao(Guid.NewGuid(), ""));
    }

    private static Professor CriarProfessorComPerfil()
    {
        var professor = new Professor("Ana Silva", "ana@email.com", "hash");
        professor.AtualizarPerfilProfissional("Professora de biologia com experiencia em ensino medio.", 80);
        return professor;
    }
}
