using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TutorClass.Application.Services;

// Pure, local analysis. Never return normalized user text or log the input.
internal static partial class ModeracaoLinguagem
{
    internal enum Categoria { Ofensa, Ameaca }
    private static readonly HashSet<string> Ofensas = Words("idiota imbecil burro burra otario otaria inutil estupido estupida vagabundo vagabunda puta puto");
    private static readonly HashSet<string> Alvos = Words("voce vc tu tutoria professora professor aluno aluna colega seu sua");
    private static readonly HashSet<string> Ligacoes = Words("e eh es um uma muito mesmo totalmente tao seu sua voce vc tu tutoria professora professor aluno aluna colega meu minha o a");
    private static readonly HashSet<string> Violencia = Words("bater matar agredir espancar perseguir ameacar assediar chantagear humilhar");
    private static readonly HashSet<string> Relato = Words("disse disseram dizia chamou chamaram chamando ameacou ameacaram ameacando ouvi citar citou frase palavra exemplo significa significado");
    private static readonly HashSet<string> Negacao = Words("nao nunca jamais evite");
    private static readonly HashSet<string> Vocabulos = new(Ofensas.Concat(Alvos).Concat(Violencia).Concat(Words("vai va se foder fuder cu filho filha da te vou irei")));

    internal static Categoria? Avaliar(string mensagem)
    {
        // Retain sentence scope so a legitimate report cannot exempt a later attack.
        var normalized = new StringBuilder(mensagem.Length);
        foreach (var c in mensagem.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark &&
                CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.Format) normalized.Append(c);
        var unquoted = Citacoes().Replace(normalized.ToString(), " ");
        Categoria? result = null;
        foreach (var sentence in Frases().Split(unquoted))
        {
            var tokens = Tokens(sentence);
            for (var i = 0; i < tokens.Count; i++)
            {
                if (ContextoLegitimo(tokens, i)) continue;
                if (Ameaca(tokens, i)) return Categoria.Ameaca;
                if (Ataque(tokens, i)) result = Categoria.Ofensa;
            }
        }
        return result;
    }

    private static bool ContextoLegitimo(List<string> t, int i)
    {
        if (i > 2 && t[0] is "analise" or "explique" && t.Take(3).Contains("ameaca")) return true;
        // Scoped to the preceding clause, not a global bypass word anywhere in a message.
        var start = Math.Max(0, i - 12);
        for (var k = start; k < i; k++)
        {
            if (t[k] is "mas" or "porem") start = k + 1;
        }
        return t.Skip(start).Take(i - start).Any(w => Relato.Contains(w) || Negacao.Contains(w));
    }

    private static bool Ameaca(List<string> t, int i)
    {
        if (t[i] is not ("vou" or "irei")) return false;
        var j = i + 1;
        var te = j < t.Count && t[j] == "te";
        if (te) j++;
        if (j >= t.Count) return false;
        if (t[j] == "divulgar")
            return Sequence(t, j + 1, "suas", "fotos", "intimas");
        if (t[j] == "acabar") return Sequence(t, j + 1, "com", "voce") || Sequence(t, j + 1, "com", "vc");
        if (!Violencia.Contains(t[j])) return false;
        if (Sequence(t, j + 1, "de", "rir")) return false;
        if (te) return true;
        j++;
        while (j < t.Count && t[j] is "em" or "no" or "na" or "o" or "a" or "meu" or "minha") j++;
        return j < t.Count && (Alvos.Contains(t[j]) || t[j] == "ti");
    }

    private static bool Ataque(List<string> t, int i)
    {
        if (t[i] is "vai" or "va")
            return Sequence(t, i + 1, "se", "foder") || Sequence(t, i + 1, "se", "fuder") || Sequence(t, i + 1, "tomar", "no", "cu");
        var insulto = Ofensas.Contains(t[i]) ||
            ((t[i] is "filho" or "filha") && Sequence(t, i + 1, "da", "puta"));
        if (!insulto) return false;
        // The animal is not an insult just because it follows a possessive.
        if (t[i] is "buro" or "bura" && i + 1 < t.Count && t[i + 1] is "adoeceu" or "esta" or "comeu" or "animal") return false;
        for (var k = i - 1; k >= Math.Max(0, i - 6); k--)
        {
            if (!Ligacoes.Contains(t[k])) break;
            if (Alvos.Contains(t[k])) return true;
        }
        return false;
    }

    private static bool Sequence(List<string> t, int start, params string[] words)
        => start + words.Length <= t.Count && words.Select((w, i) => t[start + i] == Compact(w)).All(x => x);

    private static List<string> Tokens(string text)
    {
        var raw = Palavras().Matches(text).SelectMany(m => Vocabulos.Contains(Canonical(m.Value))
            ? new[] { m.Value } : Separadores().Split(m.Value)).ToArray();
        var result = new List<string>(raw.Length);
        for (var i = 0; i < raw.Length; i++)
        {
            // Join artificial letter separation only when the result belongs to the known lexicon.
            var word = Canonical(raw[i]); var consumed = 0;
            if (raw[i].Length == 1)
            {
                var joined = new StringBuilder(raw[i]);
                for (var j = i + 1; j < Math.Min(raw.Length, i + 16) && raw[j].Length == 1; j++)
                {
                    joined.Append(raw[j]);
                    var candidate = Canonical(joined.ToString());
                    if (Vocabulos.Contains(candidate)) { word = candidate; consumed = j - i; }
                }
            }
            result.Add(word); i += consumed;
        }
        return result;
    }

    private static string Canonical(string token)
    {
        token = new string(token.Where(c => char.IsAsciiLetterOrDigit(c) || c is '@' or '$').ToArray());
        // Here @ is a masked vowel in a known insult; plain 'pata' stays an animal word.
        if (Compact(token) == "p@ta") return "puta";
        var mapped = new string(token.Select(c => c switch
        { '@' or '4' => 'a', '0' => 'o', '1' => 'i', '3' => 'e', '$' or '5' => 's', '7' => 't', _ => c }).ToArray());
        return Compact(mapped);
    }

    private static HashSet<string> Words(string value) => value.Split(' ').Select(Compact).ToHashSet(StringComparer.Ordinal);
    private static string Compact(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value) if (sb.Length == 0 || sb[^1] != c) sb.Append(c);
        return sb.ToString();
    }

    [GeneratedRegex("[\"“][^\"”\\r\\n]{0,4000}[\"”]|'[^'\\r\\n]{0,4000}'", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Citacoes();
    [GeneratedRegex(@"(?<=[.!?;])\s+|[\r\n]+", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Frases();
    [GeneratedRegex(@"[a-z0-9@$]+(?:[\\._*\-][a-z0-9@$]+)*", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Palavras();
    [GeneratedRegex(@"[\\._*\-]+", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Separadores();
}
