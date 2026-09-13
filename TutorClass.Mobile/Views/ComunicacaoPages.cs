using TutorClass.Mobile.Models;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Views;

public abstract class ComunicacaoPage : ContentPage
{
    private CancellationTokenSource? lifetime;
    protected CancellationToken Token => lifetime?.Token ?? CancellationToken.None;
    protected ComunicacaoPage()
    {
        BackgroundColor = Color.FromArgb("#F5F8F6");
    }
    protected abstract Task CarregarAsync();
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        lifetime?.Dispose();
        lifetime = new();
        await CarregarAsync();
    }
    protected override void OnDisappearing() { lifetime?.Cancel(); base.OnDisappearing(); }
    protected static Label Texto(string binding, int size = 16)
    {
        var label = new Label { FontSize = size, TextColor = Colors.Black };
        label.SetBinding(Label.TextProperty, binding);
        return label;
    }
    protected static VerticalStackLayout Estado()
    {
        var spinner = new ActivityIndicator();
        spinner.SetBinding(ActivityIndicator.IsRunningProperty, "IsBusy");
        spinner.SetBinding(IsVisibleProperty, "IsBusy");
        var error = Texto("Erro");
        error.TextColor = Color.FromArgb("#A12424");
        error.SetBinding(IsVisibleProperty, "HasError");
        return new VerticalStackLayout { Children = { spinner, error } };
    }
}

public sealed class TutorIaPage : ComunicacaoPage
{
    private readonly TutorIaViewModel vm;
    public TutorIaPage(TutorIaViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "TutorIA";
        var refresh = new Button { Text = "Atualizar status", AutomationId = "TutorIaAtualizar" };
        refresh.SetBinding(IsEnabledProperty, "CanLoad");
        refresh.Clicked += async (_, _) => await vm.CarregarAsync(Token);
        var header = new VerticalStackLayout { Spacing = 6, Children = { Texto("Plano"), Texto("Quota", 14), Texto("Aviso"), Estado(), refresh } };
        var messages = new CollectionView { SelectionMode = SelectionMode.None, AutomationId = "TutorIaMensagens", ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView };
        messages.SetBinding(ItemsView.ItemsSourceProperty, "Mensagens");
        messages.ItemTemplate = new DataTemplate(() => new Border
        {
            Margin = new Thickness(0, 5), Padding = 12, BackgroundColor = Colors.White,
            Content = new VerticalStackLayout { Spacing = 6, Children = { Texto("Autor", 14), Texto("Texto") } }
        });
        var editor = new Editor { Placeholder = "Escreva sua mensagem", HeightRequest = 95, AutomationId = "TutorIaMensagem" };
        editor.SetBinding(Editor.TextProperty, "Mensagem");
        editor.SetBinding(IsEnabledProperty, "CanLoad");
        var send = new Button { Text = "Enviar", AutomationId = "TutorIaEnviar" };
        send.SetBinding(IsEnabledProperty, "PodeEnviar");
        send.Clicked += async (_, _) => await vm.EnviarAsync(Token);
        var grid = new Grid { Padding = 16, RowSpacing = 8, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(header, 0, 0);
        grid.Add(messages, 0, 1);
        grid.Add(new VerticalStackLayout { Children = { editor, send } }, 0, 2);
        Content = grid;
    }
    protected override Task CarregarAsync() => vm.CarregarAsync(Token);
}

public sealed class NotificacoesPage : ComunicacaoPage
{
    private readonly NotificacoesViewModel vm;
    public NotificacoesPage(NotificacoesViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "Notificações";
        var reload = new Button { Text = "Atualizar", AutomationId = "NotificacoesAtualizar" };
        reload.SetBinding(IsEnabledProperty, "CanLoad");
        reload.Clicked += async (_, _) => await vm.CarregarAsync(Token);
        var all = new Button { Text = "Marcar todas como lidas", AutomationId = "NotificacoesLerTodas" };
        all.SetBinding(IsEnabledProperty, "PodeMarcarTodas");
        all.Clicked += async (_, _) => await vm.MarcarTodasAsync(Token);
        var empty = new Label { Text = "Você não tem notificações.", AutomationId = "NotificacoesVazio" };
        empty.SetBinding(IsVisibleProperty, "IsEmpty");
        var list = new CollectionView { SelectionMode = SelectionMode.None, AutomationId = "NotificacoesLista" };
        list.SetBinding(ItemsView.ItemsSourceProperty, "Itens");
        list.SetBinding(IsEnabledProperty, "CanLoad");
        list.ItemTemplate = new DataTemplate(() =>
        {
            var read = new Button { Text = "Marcar como lida" };
            read.SetBinding(IsVisibleProperty, "NaoLida");
            read.Clicked += async (_, _) =>
            {
                if (read.BindingContext is NotificacaoItemViewModel item) await vm.MarcarAsync(item, Token);
            };
            return new Border
            {
                Margin = new Thickness(0, 5), Padding = 12, BackgroundColor = Colors.White,
                Content = new VerticalStackLayout { Spacing = 7, Children = { Texto("Titulo", 20), Texto("Mensagem"), Texto("Data", 14), Texto("Leitura", 14), read } }
            };
        });
        var more = new Button { Text = "Carregar mais" };
        more.SetBinding(IsVisibleProperty, "HasMore");
        more.SetBinding(IsEnabledProperty, "CanLoad");
        more.Clicked += async (_, _) => await vm.MaisAsync(Token);
        var grid = new Grid { Padding = 16, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(new VerticalStackLayout { Spacing = 6, Children = { Texto("Resumo"), Estado(), reload, all, empty } }, 0, 0);
        grid.Add(list, 0, 1);
        grid.Add(more, 0, 2);
        Content = grid;
    }
    protected override Task CarregarAsync() => vm.CarregarAsync(Token);
}
