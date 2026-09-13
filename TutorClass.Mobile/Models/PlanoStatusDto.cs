namespace TutorClass.Mobile.Models;

public sealed class PlanoStatusDto
{
    public string PlanoAtual { get; init; } = "";
    public bool? AssinaturaAtiva { get; init; }
    public bool? PossuiIaEducacional { get; init; }
    public int? LimiteMensagensIaDia { get; init; }
    public bool? PossuiMentoria { get; init; }
    public bool? PossuiGravacoesElegiveis { get; init; }
}
