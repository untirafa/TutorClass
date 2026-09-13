using System.Collections.ObjectModel;
using TutorClass.Mobile.Models;
using TutorClass.Mobile.Services;

namespace TutorClass.Mobile.ViewModels;

public abstract class ConsultaAutenticadaViewModel(IAppNavigation navigation) : ConsultaViewModel
{
    protected void VoltarAoLogin() => navigation.ShowLogin();
    protected Task ExecutarAsync(Func<Task> action, CancellationToken ct, string recurso)
        => LoadAsync(async () =>
        {
            try { await action(); }
            catch (UnauthorizedAccessException) { VoltarAoLogin(); throw; }
        }, ct, recurso);
}

public sealed class TutorIaViewModel(TutorIaService service, IAppNavigation navigation) : ConsultaAutenticadaViewModel(navigation)
{
    private string mensagem = "";
    public string Mensagem { get => mensagem; set { mensagem = value; Refresh(); } }
    public TutorIaResponse? Status { get; private set; }
    public ObservableCollection<MensagemIaItem> Mensagens { get; } = [];
    public bool PodeEnviar => !IsBusy && Status is { PossuiAcesso: true, LimiteAtingido: false } && !string.IsNullOrWhiteSpace(Mensagem);
    public string Plano => Status is null ? "" : $"Plano: {Status.Plano}";
    public string Quota => Status is null ? "" : string.Join(" • ", new[]
    {
        $"Usadas hoje: {Status.UsadasHoje}",
        Status.LimiteDiario is { } limite ? $"Limite diário: {limite}" : null,
        Status.RestantesHoje is { } restantes ? $"Restantes: {restantes}" : null
    }.Where(x => x is not null));
    public string Aviso => Status?.Mensagem ?? "";
    public Task CarregarAsync(CancellationToken ct = default) => ExecutarAsync(async () =>
    {
        Status = null;
        Refresh();
        Status = await service.StatusAsync(ct);
    }, ct, "a TutorIA");

    public Task EnviarAsync(CancellationToken ct = default)
    {
        if (!PodeEnviar) return Task.CompletedTask;
        var pergunta = Mensagem;
        return ExecutarAsync(async () =>
        {
            // Never resend automatically: a lost response may already have consumed usage.
            Status = null;
            Refresh();
            Mensagens.Add(new("Você", pergunta));
            var result = await service.EnviarAsync(pergunta, ct);
            Status = result;
            if (!string.IsNullOrWhiteSpace(result.Resposta))
            {
                Mensagens.Add(new(result.Nome, result.Resposta));
                Mensagem = "";
            }
        }, ct, "a TutorIA. Atualize o status antes de tentar novamente");
    }
}

public sealed class NotificacaoItemViewModel(NotificacaoDto dto)
{
    public Guid Id => dto.Id;
    public string Titulo => dto.Titulo;
    public string Mensagem => dto.Mensagem;
    public bool Lida => dto.Lida;
    public bool NaoLida => !dto.Lida;
    public string Leitura => dto.Lida ? "Lida" : "Não lida";
    public string Data => dto.DataCriacaoUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public sealed class NotificacoesViewModel(NotificacaoService service, IAppNavigation navigation) : ConsultaAutenticadaViewModel(navigation)
{
    private int page = 1;
    public NotificacaoItemViewModel[] Itens { get; private set; } = [];
    public int? NaoLidas { get; private set; }
    public string Resumo => NaoLidas is { } count ? $"{count} não lidas" : "";
    public bool IsEmpty { get; private set; }
    public bool HasMore { get; private set; }
    public bool PodeMarcarTodas => !IsBusy && NaoLidas is > 0;
    public Task CarregarAsync(CancellationToken ct = default) => ExecutarAsync(() => RecarregarAsync(ct), ct, "as notificações");
    private async Task RecarregarAsync(CancellationToken ct)
    {
        Itens = [];
        IsEmpty = HasMore = false;
        NaoLidas = null;
        Refresh();
        var items = await service.ListarAsync(1, ct);
        Itens = items.Select(n => new NotificacaoItemViewModel(n)).ToArray();
        page = 1;
        IsEmpty = Itens.Length == 0;
        HasMore = items.Length == NotificacaoService.PageSize;
        Refresh();
        NaoLidas = (await service.ResumoAsync(ct)).NaoLidas;
    }
    public Task MaisAsync(CancellationToken ct = default)
    {
        if (!HasMore) return Task.CompletedTask;
        return ExecutarAsync(async () =>
        {
            var items = await service.ListarAsync(page + 1, ct);
            Itens = Itens.Concat(items.Select(n => new NotificacaoItemViewModel(n))).DistinctBy(n => n.Id).ToArray();
            page++;
            HasMore = items.Length == NotificacaoService.PageSize;
        }, ct, "as notificações");
    }
    public Task MarcarAsync(NotificacaoItemViewModel item, CancellationToken ct = default)
    {
        if (item.Lida) return Task.CompletedTask;
        return ExecutarAsync(async () =>
        {
            var updated = await service.MarcarLidaAsync(item.Id, ct);
            Itens = Itens.Select(n => n.Id == updated.Id ? new NotificacaoItemViewModel(updated) : n).ToArray();
            Refresh();
            NaoLidas = (await service.ResumoAsync(ct)).NaoLidas;
        }, ct, "as notificações");
    }
    public Task MarcarTodasAsync(CancellationToken ct = default)
    {
        if (!PodeMarcarTodas) return Task.CompletedTask;
        return ExecutarAsync(async () =>
        {
            await service.MarcarTodasAsync(ct);
            await RecarregarAsync(ct);
        }, ct, "as notificações");
    }
}

