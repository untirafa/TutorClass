using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TutorClass.Application.Services;

// Conservative local rules: match a whole abusive utterance, not isolated keywords
// inside a lesson, quotation, report of bullying or request for help.
public static class TutorIaModeracao
{
    public const string Motivo = "MENSAGEM_OFENSIVA";
    public const string Aviso = "Essa mensagem contém uma ofensa direta. Reformule sua pergunta com respeito para continuar. Nenhuma mensagem da sua quota foi consumida.";
    private static readonly Regex Ofensa = new(
        @"^(?:(?:voce|vc|tu|tutoria)(?: e| eh| es)?\s+)?(?:(?:seu|sua|um|uma)\s+)?(?:idiota|imbecil|burro|burra|otario|otaria|inutil|estupido|estupida|filho da puta|filha da puta)$|^(?:vai|va) (?:se foder|tomar no cu)$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    public static bool DeveBloquear(string mensagem)
    {
        var normalized = new StringBuilder();
        foreach (var c in mensagem.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                normalized.Append(c);
        var text = Regex.Replace(normalized.ToString(), @"\s+", " ").Trim().TrimEnd('!', '.', '?').Trim();
        return Ofensa.IsMatch(text);
    }
}
