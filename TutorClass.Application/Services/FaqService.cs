using System.Globalization;
using System.Text;
using TutorClass.Application.Abstractions;
using TutorClass.Application.DTOs;
using TutorClass.Domain.Common;
using TutorClass.Domain.Help;

namespace TutorClass.Application.Services;

public sealed class FaqService
{
    private const int MinimumChatScore = 40;
    private const string FallbackMessage = "Nao encontrei uma resposta exata para essa duvida. Tente reformular sua pergunta ou consulte as categorias de ajuda.";
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a",
        "as",
        "ao",
        "aos",
        "como",
        "com",
        "da",
        "das",
        "de",
        "do",
        "dos",
        "e",
        "em",
        "eu",
        "me",
        "minha",
        "minhas",
        "meu",
        "meus",
        "na",
        "nas",
        "no",
        "nos",
        "o",
        "os",
        "para",
        "por",
        "que",
        "uma",
        "um",
        "uns",
        "umas"
    };

    private readonly IApplicationDbContext _db;

    public FaqService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyCollection<FaqResponse>> ListarAsync(string? categoria, string? termo, CancellationToken cancellationToken)
    {
        var faqs = _db.PerguntasFrequentes
            .Where(f => f.Ativo)
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(categoria))
        {
            if (!Enum.TryParse<CategoriaFaq>(categoria, ignoreCase: true, out var categoriaEnum))
            {
                return Task.FromResult<IReadOnlyCollection<FaqResponse>>([]);
            }

            faqs = faqs.Where(f => f.Categoria == categoriaEnum);
        }

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var termoNormalizado = Normalize(termo);
            faqs = faqs.Where(f => ContainsNormalized(f.Pergunta, termoNormalizado) ||
                ContainsNormalized(f.Resposta, termoNormalizado) ||
                ContainsNormalized(f.PalavrasChave, termoNormalizado) ||
                ContainsNormalized(f.Categoria.ToString(), termoNormalizado));
        }

        var response = faqs
            .OrderBy(f => f.Categoria)
            .ThenBy(f => f.OrdemExibicao)
            .ThenBy(f => f.Pergunta)
            .Select(ToResponse)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<FaqResponse>>(response);
    }

    public Task<FaqChatResponse> ConversarAsync(FaqChatRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Mensagem))
        {
            throw new DomainException("Mensagem e obrigatoria.");
        }

        var mensagem = Normalize(request.Mensagem);
        var tokens = Tokenize(mensagem);

        var match = _db.PerguntasFrequentes
            .Where(f => f.Ativo)
            .AsEnumerable()
            .Select(f => new { Faq = f, Score = Score(f, mensagem, tokens) })
            .Where(result => result.Score >= MinimumChatScore)
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Faq.Categoria)
            .ThenBy(result => result.Faq.OrdemExibicao)
            .ThenBy(result => result.Faq.Pergunta)
            .FirstOrDefault();

        if (match is null)
        {
            return Task.FromResult(new FaqChatResponse(false, FallbackMessage, null, null, null, null, null, null));
        }

        return Task.FromResult(ToChatResponse(match.Faq));
    }

    public async Task<FaqResponse> CriarAsync(CriarFaqRequest request, CancellationToken cancellationToken)
    {
        EnsureUniqueQuestion(request.Pergunta);
        var faq = new PerguntaFrequente(
            request.Pergunta,
            request.Resposta,
            request.Categoria,
            request.PalavrasChave,
            request.OrdemExibicao,
            request.TextoAcao,
            request.RotaAcao);

        _db.AddEntity(faq);
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(faq);
    }

    public async Task<FaqResponse> AtualizarAsync(Guid id, AtualizarFaqRequest request, CancellationToken cancellationToken)
    {
        var faq = _db.PerguntasFrequentes.SingleOrDefault(f => f.Id == id)
            ?? throw new DomainException("FAQ nao encontrado.");

        EnsureUniqueQuestion(request.Pergunta, id);
        faq.Atualizar(
            request.Pergunta,
            request.Resposta,
            request.Categoria,
            request.PalavrasChave,
            request.OrdemExibicao,
            request.TextoAcao,
            request.RotaAcao,
            request.Ativo);

        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(faq);
    }

    public async Task<FaqResponse> DesativarAsync(Guid id, CancellationToken cancellationToken)
    {
        var faq = _db.PerguntasFrequentes.SingleOrDefault(f => f.Id == id)
            ?? throw new DomainException("FAQ nao encontrado.");

        faq.Desativar();
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(faq);
    }

    public async Task<FaqResponse> AtivarAsync(Guid id, CancellationToken cancellationToken)
    {
        var faq = _db.PerguntasFrequentes.SingleOrDefault(f => f.Id == id)
            ?? throw new DomainException("FAQ nao encontrado.");

        faq.Ativar();
        await _db.SaveChangesAsync(cancellationToken);
        return ToResponse(faq);
    }

    public async Task ExcluirAsync(Guid id, CancellationToken cancellationToken)
    {
        var faq = _db.PerguntasFrequentes.SingleOrDefault(f => f.Id == id)
            ?? throw new DomainException("FAQ nao encontrado.");

        _db.RemoveEntity(faq);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void EnsureUniqueQuestion(string pergunta, Guid? ignoredId = null)
    {
        var normalizedQuestion = Normalize(pergunta);
        var duplicated = _db.PerguntasFrequentes
            .AsEnumerable()
            .Any(f => f.Id != ignoredId && Normalize(f.Pergunta) == normalizedQuestion);

        if (duplicated)
        {
            throw new DomainException("Ja existe uma FAQ com esta pergunta.");
        }
    }

    private static int Score(PerguntaFrequente faq, string mensagem, IReadOnlyCollection<string> tokens)
    {
        var pergunta = Normalize(faq.Pergunta);
        var resposta = Normalize(faq.Resposta);
        var palavrasChave = SplitKeywords(faq.PalavrasChave);
        var categoria = Normalize(faq.Categoria.ToString());

        if (pergunta == mensagem)
        {
            return 1000;
        }

        var score = 0;

        foreach (var keyword in palavrasChave)
        {
            if (mensagem == keyword || mensagem.Contains(keyword, StringComparison.Ordinal))
            {
                score += 120;
            }

            if (tokens.Contains(keyword))
            {
                score += 80;
            }
        }

        if (pergunta.Contains(mensagem, StringComparison.Ordinal) || mensagem.Contains(pergunta, StringComparison.Ordinal))
        {
            score += 90;
        }

        if (categoria == mensagem || mensagem.Contains(categoria, StringComparison.Ordinal))
        {
            score += 45;
        }

        foreach (var token in tokens)
        {
            if (token.Length < 3)
            {
                continue;
            }

            if (pergunta.Contains(token, StringComparison.Ordinal))
            {
                score += 25;
            }

            if (palavrasChave.Any(keyword => keyword.Contains(token, StringComparison.Ordinal) || token.Contains(keyword, StringComparison.Ordinal)))
            {
                score += 35;
            }

            if (categoria.Contains(token, StringComparison.Ordinal))
            {
                score += 20;
            }

            if (resposta.Contains(token, StringComparison.Ordinal))
            {
                score += 5;
            }
        }

        return score;
    }

    private static IReadOnlyCollection<string> SplitKeywords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(keyword => keyword.Length > 1)
            .Where(keyword => !StopWords.Contains(keyword))
            .Distinct()
            .ToArray();
    }

    private static IReadOnlyCollection<string> Tokenize(string value)
    {
        return value
            .Split([' ', ',', '.', '?', '!', ';', ':', '/', '\\', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(token => token.Length > 1)
            .Where(token => !StopWords.Contains(token))
            .Distinct()
            .ToArray();
    }

    private static bool ContainsNormalized(string? value, string search)
    {
        return !string.IsNullOrWhiteSpace(value) && Normalize(value).Contains(search, StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static FaqResponse ToResponse(PerguntaFrequente faq) => new(
        faq.Id,
        faq.Pergunta,
        faq.Resposta,
        faq.Categoria,
        faq.PalavrasChave,
        faq.Ativo,
        faq.OrdemExibicao,
        faq.TextoAcao,
        faq.RotaAcao,
        faq.CreatedAtUtc,
        faq.UpdatedAtUtc);

    private static FaqChatResponse ToChatResponse(PerguntaFrequente faq) => new(
        true,
        null,
        faq.Id,
        faq.Pergunta,
        faq.Resposta,
        faq.Categoria,
        faq.TextoAcao,
        faq.RotaAcao);
}
