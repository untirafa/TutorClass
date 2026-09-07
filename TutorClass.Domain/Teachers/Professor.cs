using TutorClass.Domain.Common;
using TutorClass.Domain.Users;

namespace TutorClass.Domain.Teachers;

public sealed class Professor : Usuario
{
    private readonly List<ProfessorMateria> _materias = [];
    private readonly List<QualificacaoProfessor> _qualificacoes = [];
    private readonly List<CandidaturaProfessor> _candidaturas = [];
    private readonly List<DisponibilidadeProfessor> _disponibilidades = [];

    private Professor()
    {
    }

    public Professor(string nome, string email, string passwordHash)
        : base(nome, email, passwordHash)
    {
    }

    public string? Biografia { get; private set; }
    public decimal? PrecoHora { get; private set; }
    public StatusProfessor Status { get; private set; } = StatusProfessor.Candidato;
    public NivelReputacaoProfessor NivelReputacao { get; private set; } = NivelReputacaoProfessor.Novo;
    public bool IdentidadeVerificada { get; private set; }
    public decimal? MediaAvaliacoes { get; private set; }
    public int QuantidadeAvaliacoes { get; private set; }
    public int AulasConcluidas { get; private set; }
    public bool OfereceMentoria { get; private set; }
    public int LimiteMentorados { get; private set; } = 2;
    public IReadOnlyCollection<ProfessorMateria> Materias => _materias.AsReadOnly();
    public IReadOnlyCollection<QualificacaoProfessor> Qualificacoes => _qualificacoes.AsReadOnly();
    public IReadOnlyCollection<CandidaturaProfessor> Candidaturas => _candidaturas.AsReadOnly();
    public IReadOnlyCollection<DisponibilidadeProfessor> Disponibilidades => _disponibilidades.AsReadOnly();

    public void AtualizarPerfilProfissional(string biografia, decimal precoHora)
    {
        if (string.IsNullOrWhiteSpace(biografia))
        {
            throw new DomainException("Biografia profissional e obrigatoria.");
        }

        if (precoHora <= 0)
        {
            throw new DomainException("Preco por hora deve ser maior que zero.");
        }

        Biografia = biografia.Trim();
        PrecoHora = precoHora;
        MarkUpdated();
    }

    public CandidaturaProfessor SolicitarCandidatura(string mensagem)
    {
        if (_candidaturas.Any(c => c.EstaAtiva))
        {
            throw new DomainException("Professor ja possui uma candidatura ativa.");
        }

        if (string.IsNullOrWhiteSpace(Biografia) || PrecoHora is null)
        {
            throw new DomainException("Perfil profissional deve estar preenchido antes da candidatura.");
        }

        var candidatura = new CandidaturaProfessor(Id, mensagem);
        _candidaturas.Add(candidatura);
        MarkUpdated();
        return candidatura;
    }

    public ProfessorMateria VincularMateria(Guid materiaId)
    {
        var materiaExistente = _materias.FirstOrDefault(m => m.MateriaId == materiaId);
        if (materiaExistente is not null)
        {
            return materiaExistente;
        }

        var professorMateria = new ProfessorMateria(Id, materiaId);
        _materias.Add(professorMateria);
        MarkUpdated();
        return professorMateria;
    }

    public QualificacaoProfessor AdicionarQualificacao(
        TipoQualificacaoProfessor tipo,
        string titulo,
        string? instituicao,
        string? descricao,
        DateOnly? dataConclusao,
        string? documentoComprovacao)
    {
        var qualificacao = new QualificacaoProfessor(Id, tipo, titulo, instituicao, descricao, dataConclusao, documentoComprovacao);
        _qualificacoes.Add(qualificacao);
        MarkUpdated();
        return qualificacao;
    }

    public DisponibilidadeProfessor CadastrarDisponibilidade(DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fim)
    {
        var disponibilidade = new DisponibilidadeProfessor(Id, diaSemana, inicio, fim);
        _disponibilidades.Add(disponibilidade);
        MarkUpdated();
        return disponibilidade;
    }

    public void Aprovar()
    {
        Status = StatusProfessor.Aprovado;
        if (NivelReputacao == NivelReputacaoProfessor.Novo && IdentidadeVerificada)
        {
            NivelReputacao = NivelReputacaoProfessor.Verificado;
        }

        MarkUpdated();
    }

    public void ValidarElegibilidadeParaAprovacao(bool possuiMateriaVinculada, bool possuiQualificacaoVerificada)
    {
        if (!possuiMateriaVinculada)
        {
            throw new DomainException("Professor precisa possuir pelo menos uma materia vinculada para ser aprovado.");
        }

        if (!possuiQualificacaoVerificada)
        {
            throw new DomainException("Professor precisa possuir pelo menos uma qualificacao verificada para ser aprovado.");
        }
    }


    public void RegistrarAulaConcluida()
    {
        AulasConcluidas++;
        RecalcularNivelReputacao();
        MarkUpdated();
    }

    public void AtualizarAvaliacoes(decimal? mediaAvaliacoes, int quantidadeAvaliacoes)
    {
        if (quantidadeAvaliacoes < 0)
        {
            throw new DomainException("Quantidade de avaliacoes nao pode ser negativa.");
        }

        if (mediaAvaliacoes is < 1 or > 5)
        {
            throw new DomainException("Media de avaliacoes deve estar entre 1 e 5.");
        }

        MediaAvaliacoes = quantidadeAvaliacoes == 0 ? null : mediaAvaliacoes;
        QuantidadeAvaliacoes = quantidadeAvaliacoes;
        RecalcularNivelReputacao();
        MarkUpdated();
    }

    public void ConfigurarMentoria(bool ofereceMentoria, int limiteMentorados)
    {
        if (limiteMentorados < 0 || limiteMentorados > 50)
        {
            throw new DomainException("Limite de mentorados deve estar entre 0 e 50.");
        }

        OfereceMentoria = ofereceMentoria;
        LimiteMentorados = ofereceMentoria ? limiteMentorados : 0;
        MarkUpdated();
    }

    private void RecalcularNivelReputacao()
    {
        if (QuantidadeAvaliacoes >= 20 && AulasConcluidas >= 30 && MediaAvaliacoes >= 4.7m)
        {
            NivelReputacao = NivelReputacaoProfessor.Referencia;
            return;
        }

        if (QuantidadeAvaliacoes >= 10 && AulasConcluidas >= 15 && MediaAvaliacoes >= 4.5m)
        {
            NivelReputacao = NivelReputacaoProfessor.Destaque;
            return;
        }

        if (QuantidadeAvaliacoes >= 3 && AulasConcluidas >= 5 && MediaAvaliacoes >= 4.0m)
        {
            NivelReputacao = NivelReputacaoProfessor.Experiente;
            return;
        }

        if (QuantidadeAvaliacoes >= 1)
        {
            NivelReputacao = NivelReputacaoProfessor.Verificado;
            return;
        }

        NivelReputacao = NivelReputacaoProfessor.Novo;
    }

    public void Reprovar()
    {
        Status = StatusProfessor.Reprovado;
        MarkUpdated();
    }
}
