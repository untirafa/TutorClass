namespace TutorClass.Tests;

public static class ModeracaoCorpus
{
    private static readonly string[] Ofensas =
    [
        "sua p@ta", @"sua p\@ta", "SÚÚÁ P@@TAAA!!!", "sua p.u.t.a", "sua p u t a",
        "Professora, sua p@ta, não gostei da aula", "TutorIA, você é uma v4g4bund4",
        "Você é um iiiidiotaaa mesmo", "professora vai se fuder agora", "vai se foder",
        "Você é um i d i o t a", "sua p_u_t_a", "Seu filho da puta",
        "O que significa idiota? Você é uma idiota!",
        "Um colega me chamou de idiota. Mas você é uma vagabunda"
    ];
    private static readonly string[] Ameacas =
    [
        "vou te bater", "Amanhã eu vou bater em você na saída", "Professora, vou te agredir hoje",
        "Eu irei te espancar depois da aula", "Vou acabar com você", "Vou divulgar suas fotos íntimas",
        "vou matar meu professor", "Eu vou te maaatar", "Eu vou te m a t a r"
    ];
    private static readonly string[] Permitidas =
    [
        "oi", "Explique a fotossíntese", "O que significa essa palavra ofensiva?", "O que significa puta?",
        "Um colega me chamou de sua p@ta. O que devo fazer?", "Um colega disse que vai me bater. O que faço?",
        "Analise a frase você é uma vagabunda", "Ela disse: professora vai se foder. Como lidar com isso?",
        "\"sua p@ta\"", "“Vou te bater”", "Não diga você é um idiota aos colegas",
        "vagabundo", "puta", "sua pauta está boa", "A disputa foi equilibrada", "Explique casamento putativo",
        "Sua pata está machucada", "Seu burro adoeceu", "Vou matar o processo no Linux",
        "Vou te matar de rir com essa piada", "Calcule 12345678909 dividido por 3"
    ];
    public static IEnumerable<object[]> ChatBloqueadas() => Ofensas.Select(s => new object[] { s, "OFENSA_DIRECIONADA" })
        .Concat(Ameacas.Select(s => new object[] { s, "AMEACA_EXPLICITA" }));
    public static IEnumerable<object[]> IaBloqueadas() => Ofensas.Select(s => new object[] { s, "MENSAGEM_OFENSIVA" })
        .Concat(Ameacas.Select(s => new object[] { s, "AMEACA_OU_ASSEDIO" }));
    public static IEnumerable<object[]> Legitimas() => Permitidas.Select(s => new object[] { s });
}
