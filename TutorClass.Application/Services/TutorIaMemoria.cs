using TutorClass.Application.DTOs;

namespace TutorClass.Application.Services;

/// <summary>Short-lived TutorIA-only memory. No persistence or mentoring chat access.</summary>
public sealed class TutorIaMemoria
{
    public const int LimiteMensagens = 10;
    public const int LimiteCaracteres = 12000;
    private const int LimiteAlunos = 500;
    private readonly object gate = new();
    private readonly Dictionary<Guid, Entrada> entradas = new();
    private readonly TimeProvider clock;
    public TutorIaMemoria() : this(TimeProvider.System) { }
    public TutorIaMemoria(TimeProvider clock) => this.clock = clock;

    public IReadOnlyList<MensagemTutorIa> Obter(Guid alunoId)
    {
        lock (gate)
        {
            Expirar();
            return entradas.TryGetValue(alunoId, out var entrada) ? entrada.Mensagens.ToArray() : [];
        }
    }

    public void Registrar(Guid alunoId, string pergunta, string resposta)
    {
        lock (gate)
        {
            Expirar();
            // Discard the complete exchange, rather than retaining fragments of sensitive data.
            if (ContemDadoSensivel(pergunta) || ContemDadoSensivel(resposta) ||
                pergunta.Length + resposta.Length > LimiteCaracteres) return;
            if (!entradas.TryGetValue(alunoId, out var entrada))
            {
                if (entradas.Count >= LimiteAlunos)
                    entradas.Remove(entradas.MinBy(e => e.Value.Atualizada).Key);
                entrada = new Entrada();
                entradas[alunoId] = entrada;
            }
            entrada.Mensagens.Add(new("user", pergunta));
            entrada.Mensagens.Add(new("assistant", resposta));
            while (entrada.Mensagens.Count > LimiteMensagens || entrada.Mensagens.Sum(m => m.Texto.Length) > LimiteCaracteres)
                entrada.Mensagens.RemoveRange(0, 2);
            entrada.Atualizada = clock.GetUtcNow();
        }
    }

    private void Expirar()
    {
        var limite = clock.GetUtcNow().AddMinutes(-30);
        foreach (var id in entradas.Where(e => e.Value.Atualizada <= limite).Select(e => e.Key).ToArray())
            entradas.Remove(id);
    }

    private static bool ContemDadoSensivel(string texto)
    {
        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(texto,
        @"(?i)\b(?:senhas?|password|segredo|secret|token|bearer|api.?key|connection.?string|cpf|rg|cart[aã]o|diagn[oó]stico|endere[cç]o|telefone)\b|eyJ[\w-]+\.[\w-]+\.[\w-]+|[\w.+-]+@[\w.-]+\.[a-z]{2,}|\d[\d .()/+-]{8,}\d",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException) { return true; }
    }

    private sealed class Entrada
    {
        public List<MensagemTutorIa> Mensagens { get; } = [];
        public DateTimeOffset Atualizada { get; set; }
    }
}
