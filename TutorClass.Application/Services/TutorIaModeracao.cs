using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace TutorClass.Application.Services;

// Conservative local rules: match a whole abusive utterance, not isolated keywords
// inside a lesson, quotation, report of bullying or request for help.
public static class TutorIaModeracao
{
    public sealed record Resultado(string Categoria, string Mensagem);
    public const string DadosPessoais = "DADOS_PESSOAIS";
    public const string Ameaca = "AMEACA_OU_ASSEDIO";
    public const string AcessoIndevido = "TENTATIVA_ACESSO_INDEVIDO";
    public const string Motivo = "MENSAGEM_OFENSIVA";
    public const string Aviso = "Essa mensagem contém uma ofensa direta. Reformule sua pergunta com respeito para continuar. Nenhuma mensagem da sua quota foi consumida.";
    private static readonly Regex Ofensa = new(
        @"^(?:(?:voce|vc|tu|tutoria)(?: e| eh| es)?\s+)?(?:(?:seu|sua|um|uma)\s+)?(?:idiota|imbecil|burro|burra|otario|otaria|inutil|estupido|estupida|filho da puta|filha da puta)$|^(?:vai|va) (?:se foder|tomar no cu)$",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    public static bool DeveBloquear(string mensagem)
        => Avaliar(mensagem) is not null;

    public static Resultado? Avaliar(string mensagem)
    {
        var normalized = new StringBuilder();
        foreach (var c in mensagem.ToLowerInvariant().Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                normalized.Append(c);
        var text = Regex.Replace(normalized.ToString(), @"\s+", " ").Trim().TrimEnd('!', '.', '?').Trim();
        if (PossuiDadosPessoais(text))
            return new(DadosPessoais, "Sua mensagem parece conter informações pessoais. Remova esses dados e envie sua pergunta novamente. Você não precisa compartilhar documentos ou números pessoais para estudar com a TutorIA.");
        if (Match(text, @"^(?:eu )?vou (?:te matar|matar voce|matar (?:o|a|meu|minha) (?:professor|professora|colega)|te perseguir|divulgar suas fotos intimas)(?: hoje| amanha| depois da aula)?$") ||
            Match(text, @"^(?:me ajude a|quero) (?:ameacar|assediar|chantagear|humilhar) (?:meu|minha|o|a) (?:colega|professor|professora|aluno|aluna)(?:\s.*)?$"))
            return new(Ameaca, "Esta mensagem não pôde ser enviada porque pode violar as regras de segurança do TutorClass. Reformule sua mensagem com respeito e segurança.");
        if (Match(text, @"^(?:ignore|desconsidere) (?:todas )?(?:as )?(?:regras|restricoes|instrucoes)(?:\s.*)?$") ||
            Match(text, @"^(?:me (?:ensine|ajude) a|quero|como (?:posso )?)\s*(?:burlar (?:a cota|o limite|as restricoes)|invadir (?:a conta|o sistema)|acessar (?:a conta|as conversas) de outr[ao])(?:\s.*)?$"))
            return new(AcessoIndevido, "Não posso atender a essa solicitação. Reformule sua mensagem dentro das funcionalidades e das regras de uso da TutorIA.");
        // A word alone can be a vocabulary prompt. Require a directed expression.
        if ((text.Contains(' ') && Ofensa.IsMatch(text)) ||
            Match(text, @"^(?:meu|minha|o|a) (?:professor|professora|colega|aluno|aluna) e (?:um |uma )?(?:idiota|imbecil|burro|burra|inutil)$"))
            return new(Motivo, "Vamos manter uma comunicação respeitosa. Reformule sua mensagem para que eu possa ajudar com seus estudos. Nenhuma mensagem da sua quota foi consumida.");
        return null;
    }

    private static bool Match(string text, string pattern) => Regex.IsMatch(text, pattern,
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100));

    private static bool PossuiDadosPessoais(string text)
    {
        if (Match(text, @"\b[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}\b")) return true;
        // CPF: require formatting or an explicit label, plus both check digits.
        foreach (Match m in Regex.Matches(text, @"\b\d{3}\.\d{3}\.\d{3}-\d{2}\b|\bcpf\s*(?:e|eh|:|=)?\s*\d{11}\b",
                     RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100)))
        {
            var digits = new string(m.Value.Where(char.IsAsciiDigit).ToArray());
            if (CpfValido(digits)) return true;
        }
        // RG has regional formats: a label and document-shaped value are required.
        if (Match(text, @"\brg\s*(?:e|eh|:|=|numero)?\s*[0-9][0-9. -]{3,18}[0-9x]\b")) return true;
        foreach (Match m in Regex.Matches(text, @"\b(?:telefone|celular|whatsapp|whats|contato)\s*(?:e|eh|:|=)?\s*(?:\+55\s*)?\(?[1-9][0-9]\)?[ .-]*[2-9][0-9 .-]{7,11}[0-9]\b",
                     RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100)))
        {
            var digits = new string(m.Value.Where(char.IsAsciiDigit).ToArray());
            if (digits.Length is 10 or 11 || (digits.StartsWith("55") && digits.Length is 12 or 13)) return true;
        }
        return Match(text, @"\b(?:minha senha|meu token|minha chave de api|meu passaporte)\s*(?:e|eh|:|=)\s*\S+");
    }

    private static bool CpfValido(string digits)
    {
        if (digits.Length != 11 || digits.Distinct().Count() == 1) return false;
        for (var length = 9; length <= 10; length++)
        {
            var sum = 0;
            for (var i = 0; i < length; i++) sum += (digits[i] - '0') * (length + 1 - i);
            var check = (sum * 10) % 11;
            if (check == 10) check = 0;
            if (digits[length] - '0' != check) return false;
        }
        return true;
    }
}
