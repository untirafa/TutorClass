using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TutorClass.Application.Abstractions;

namespace TutorClass.Application.Services;

/// <summary>Conservative whole-utterance rules, independent of TutorIA and external providers.</summary>
public static class ModeracaoChatHumano
{
    private const string Insulto = @"(?:idiota|imbecil|burro|burra|otario|otaria|inutil|estupido|estupida|vagabundo|vagabunda|filho da puta|filha da puta)";
    private static readonly Regex Ofensa = new(
        @"^(?:(?:professor|professora|aluno|aluna),?\s+)?(?:(?:voce|vc|tu)\s+(?:(?:e|eh|es)\s+)?(?:(?:um|uma)\s+)?|(?:seu|sua)\s+)" + Insulto + @"$|^(?:(?:professor|professora|aluno|aluna),?\s+)?(?:vai|va) (?:se f[ou]der|tomar no cu)$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));
    private static readonly Regex Ameaca = new(
        @"^(?:(?:professor|professora|aluno|aluna),?\s+)?(?:eu )?(?:vou|irei) (?:te bater|bater (?:em voce|em vc|em ti|no meu colega|na minha colega)|te matar|matar voce|te agredir|te espancar|te perseguir|divulgar suas fotos intimas|matar (?:o|a|meu|minha) (?:professor|professora|colega))(?: hoje| amanha| depois da aula)?$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    public static void Validar(string conteudo)
    {
        var normalized = new StringBuilder();
        foreach (var c in conteudo.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) normalized.Append(c);
        var texto = Regex.Replace(normalized.ToString(), @"\s+", " ", RegexOptions.NonBacktracking,
            TimeSpan.FromMilliseconds(100)).Trim().TrimEnd('.', '!', '?').Trim();
        // Do not strip quotes or search for sensitive words inside reports/educational text.
        if (Ameaca.IsMatch(texto)) throw new MensagemMentoriaBloqueadaException("AMEACA_EXPLICITA");
        if (Ofensa.IsMatch(texto)) throw new MensagemMentoriaBloqueadaException("OFENSA_DIRECIONADA");
    }
}
