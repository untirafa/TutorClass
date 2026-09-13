using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;

namespace TutorClass.Mobile.ViewModels;

public sealed class PerfilViewModel(SessionService session, IAppNavigation navigation) : ConsultaAutenticadaViewModel(navigation)
{
    public UsuarioAutenticadoDto? Usuario { get; private set; }
    public bool TemUsuario => Usuario is not null;
    public Task CarregarAsync(CancellationToken ct = default) => ExecutarAsync(async () =>
    {
        Usuario = null;
        Refresh();
        var usuario = await session.GetAuthenticatedAsync<UsuarioAutenticadoDto>("api/auth/me", ct);
        ct.ThrowIfCancellationRequested();
        Usuario = usuario;
    }, ct, "seu perfil");
    public void Logout()
    {
        session.Logout();
        Usuario = null;
        Refresh();
        VoltarAoLogin();
    }
}

public sealed class PlanoViewModel(PlanoService service, IAppNavigation navigation) : ConsultaAutenticadaViewModel(navigation)
{
    public PlanoStatusDto? Plano { get; private set; }
    public bool TemPlano => Plano is not null;
    public string Nome => Plano?.PlanoAtual ?? "";
    public string Assinatura => Plano?.AssinaturaAtiva switch
    {
        true => "Assinatura ativa",
        false => "Sem assinatura ativa",
        _ => "Status da assinatura não informado"
    };
    public string Beneficios => Plano is null ? "" : string.Join("\n", new[]
    {
        Beneficio("TutorIA", Plano.PossuiIaEducacional),
        Plano.LimiteMensagensIaDia is { } limite ? $"Limite diário de mensagens da IA: {limite}" : null,
        Beneficio("Mentoria", Plano.PossuiMentoria),
        Beneficio("Gravações elegíveis", Plano.PossuiGravacoesElegiveis)
    }.Where(s => s is not null));
    private static string? Beneficio(string nome, bool? disponivel) => disponivel is { } valor
        ? $"{nome}: {(valor ? "disponível" : "não disponível")}" : null;
    public Task CarregarAsync(CancellationToken ct = default) => ExecutarAsync(async () =>
    {
        Plano = null;
        Refresh();
        var plano = await service.ObterAsync(ct);
        ct.ThrowIfCancellationRequested();
        Plano = plano;
    }, ct, "seu plano");
}

