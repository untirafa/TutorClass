namespace TutorClass.API.Security;

/// <summary>Process-local, short-lived counters. No message bodies or device fingerprints.</summary>
public sealed class ControleAbuso(TimeProvider clock)
{
    private readonly object gate = new();
    private readonly Dictionary<(string Grupo, string Identidade), (DateTimeOffset Inicio, int Usos)> counters = [];
    public int Tentar(string grupo, string identidade, int limite)
    {
        lock (gate)
        {
            var now = clock.GetUtcNow();
            foreach (var key in counters.Where(p => now - p.Value.Inicio >= TimeSpan.FromMinutes(1)).Select(p => p.Key).ToArray())
                counters.Remove(key);
            var keyAtual = (grupo, identidade);
            if (!counters.TryGetValue(keyAtual, out var counter))
            {
                // Bound memory even if many distinct clients arrive. No permanent lockouts.
                if (counters.Count >= 10000) return 60;
                counter = (now, 0);
            }
            if (counter.Usos >= limite)
                return Math.Max(1, (int)Math.Ceiling((counter.Inicio.AddMinutes(1) - now).TotalSeconds));
            counters[keyAtual] = (counter.Inicio, counter.Usos + 1);
            return 0;
        }
    }
}
