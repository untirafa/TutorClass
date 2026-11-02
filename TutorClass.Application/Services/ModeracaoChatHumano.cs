using TutorClass.Application.Abstractions;

namespace TutorClass.Application.Services;

public static class ModeracaoChatHumano
{
    public static void Validar(string conteudo)
    {
        var categoria = ModeracaoLinguagem.Avaliar(conteudo);
        if (categoria == ModeracaoLinguagem.Categoria.Ameaca)
            throw new MensagemMentoriaBloqueadaException("AMEACA_EXPLICITA");
        if (categoria == ModeracaoLinguagem.Categoria.Ofensa)
            throw new MensagemMentoriaBloqueadaException("OFENSA_DIRECIONADA");
    }
}
