using TutorClass.Domain.Common;

namespace TutorClass.Domain.Help;

public sealed class PerguntaFrequente : Entity
{
    private PerguntaFrequente()
    {
    }

    public PerguntaFrequente(
        string pergunta,
        string resposta,
        CategoriaFaq categoria,
        string? palavrasChave,
        int ordemExibicao,
        string? textoAcao = null,
        string? rotaAcao = null)
    {
        Atualizar(pergunta, resposta, categoria, palavrasChave, ordemExibicao, textoAcao, rotaAcao, ativo: true);
        UpdatedAtUtc = null;
    }

    public string Pergunta { get; private set; } = string.Empty;
    public string Resposta { get; private set; } = string.Empty;
    public CategoriaFaq Categoria { get; private set; }
    public string? PalavrasChave { get; private set; }
    public bool Ativo { get; private set; }
    public int OrdemExibicao { get; private set; }
    public string? TextoAcao { get; private set; }
    public string? RotaAcao { get; private set; }

    public void Atualizar(
        string pergunta,
        string resposta,
        CategoriaFaq categoria,
        string? palavrasChave,
        int ordemExibicao,
        string? textoAcao,
        string? rotaAcao,
        bool ativo)
    {
        if (string.IsNullOrWhiteSpace(pergunta))
        {
            throw new DomainException("Pergunta e obrigatoria.");
        }

        if (string.IsNullOrWhiteSpace(resposta))
        {
            throw new DomainException("Resposta e obrigatoria.");
        }

        if (ordemExibicao < 0)
        {
            throw new DomainException("Ordem de exibicao nao pode ser negativa.");
        }

        Pergunta = pergunta.Trim();
        Resposta = resposta.Trim();
        Categoria = categoria;
        PalavrasChave = string.IsNullOrWhiteSpace(palavrasChave) ? null : palavrasChave.Trim();
        OrdemExibicao = ordemExibicao;
        TextoAcao = string.IsNullOrWhiteSpace(textoAcao) ? null : textoAcao.Trim();
        RotaAcao = string.IsNullOrWhiteSpace(rotaAcao) ? null : rotaAcao.Trim();
        Ativo = ativo;
        MarkUpdated();
    }

    public void Ativar()
    {
        if (Ativo)
        {
            return;
        }

        Ativo = true;
        MarkUpdated();
    }

    public void Desativar()
    {
        if (!Ativo)
        {
            return;
        }

        Ativo = false;
        MarkUpdated();
    }
}
