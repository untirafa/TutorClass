using System.Text.RegularExpressions;

namespace TutorClass.Mobile.Models;

public static class TutorIaTexto
{
    private static string Replace(string text, string pattern, string replacement)
        => Regex.Replace(text, pattern, replacement, RegexOptions.None, TimeSpan.FromMilliseconds(100));

    public static string Formatar(string texto)
    {
        // Keep fenced code verbatim; only prose gets Markdown/math presentation.
        var blocks = Regex.Split(texto, @"(?m)^\s*```[^\r\n]*\r?\n?", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        for (var index = 0; index < blocks.Length; index += 2)
            blocks[index] = FormatarProsa(blocks[index]);
        return string.Concat(blocks);
    }

    private static string FormatarProsa(string text)
    {
        for (var i = 0; i < 12; i++)
        {
            var next = Replace(text, @"\\(?:dfrac|tfrac|frac)\s*\{([^{}]*)\}\s*\{([^{}]*)\}", "($1)/($2)");
            next = Replace(next, @"\\sqrt\s*\{([^{}]*)\}", "√($1)");
            next = Replace(next, @"\\(?:text|mathrm|mathbf)\s*\{([^{}]*)\}", "$1");
            if (next == text) break;
            text = next;
        }
        text = Replace(text, @"\\(?:left|right)(?=[()[\]{}|])", "");
        var symbols = new Dictionary<string, string>
        {
            ["times"]="×", ["cdot"]="·", ["div"]="÷", ["pm"]="±", ["leq"]="≤", ["geq"]="≥",
            ["neq"]="≠", ["approx"]="≈", ["pi"]="π", ["theta"]="θ", ["alpha"]="α", ["beta"]="β",
            ["Delta"]="Δ", ["infty"]="∞", ["sum"]="∑", ["to"]="→"
        };
        text = Regex.Replace(text, @"\\([A-Za-z]+)\b", m => symbols.GetValueOrDefault(m.Groups[1].Value, m.Value),
            RegexOptions.None, TimeSpan.FromMilliseconds(100));
        text = Replace(text, @"\\\(([\s\S]*?)\\\)|\\\[([\s\S]*?)\\\]", "$1$2");
        text = Replace(text, @"\$\$([\s\S]*?)\$\$", "$1");
        text = Replace(text, @"(^|[^\w])\$([^$\n]+)\$", "$1$2");
        text = Replace(text, @"([_^])\{([^{}]*)\}", "$1($2)");
        text = Replace(text, @"(?m)^\s*#{1,6}\s+", "");
        text = Replace(text, @"\*\*([^*\n]+)\*\*|__([^_\n]+)__", "$1$2");
        text = Replace(text, @"`([^`\n]+)`", "$1");
        return Replace(text, @"(?m)^\s*[-+*]\s+", "• ");
    }
}
