using TutorClass.Mobile.Models;
using TutorClass.Mobile.ViewModels;

using TutorClass.Mobile.PrototypeUI.Styles;
namespace TutorClass.Mobile.PrototypeUI.Pages;

public abstract class ComunicacaoPage : PrototypePage
{
    private CancellationTokenSource? lifetime;
    protected CancellationToken Token => lifetime?.Token ?? CancellationToken.None;
    protected ComunicacaoPage()
    {
        BackgroundColor = Color.FromArgb("#F4F5F7");
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
        error.TextColor = Color.FromArgb("#9F1239");
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
        // Status details scroll with the conversation instead of consuming keyboard space.
        var header = new VerticalStackLayout
        {
            Padding = new Thickness(0, 4, 0, 12), Spacing = 8,
            Children = { Texto("Plano", 18), Texto("Quota", 14), refresh }
        };
        var messages = new CollectionView
        {
            SelectionMode = SelectionMode.None, AutomationId = "TutorIaMensagens",
            ItemsUpdatingScrollMode = ItemsUpdatingScrollMode.KeepLastItemInView,
            ItemSizingStrategy = ItemSizingStrategy.MeasureAllItems,
            Header = header
        };
        messages.SetBinding(ItemsView.ItemsSourceProperty, "Mensagens");
        messages.ItemTemplate = new DataTemplate(() =>
        {
            var bubble = new Border
            {
                Padding = 14, Margin = new Thickness(0, 6, 24, 6), BackgroundColor = Colors.White,
                Content = new VerticalStackLayout { Spacing = 8, Children = { Texto("Autor", 14), Texto("Texto", 17) } }
            };
            var user = new DataTrigger(typeof(Border)) { Binding = new Binding("Autor"), Value = "Você" };
            user.Setters.Add(new Setter { Property = BackgroundColorProperty, Value = Color.FromArgb("#E8EEF5") });
            user.Setters.Add(new Setter { Property = View.MarginProperty, Value = new Thickness(24, 6, 0, 6) });
            bubble.Triggers.Add(user);
            return bubble;
        });

        // No truncation: longer notices remain scrollable even with the keyboard open.
        var notices = new ScrollView
        {
            MaximumHeightRequest = 110,
            Content = new VerticalStackLayout { Spacing = 6, Children = { Texto("Aviso", 14), Estado() } }
        };
        var editor = new Editor
        {
            Placeholder = "Escreva sua mensagem", HeightRequest = 76,
            AutomationId = "TutorIaMensagem", AutoSize = EditorAutoSizeOption.Disabled
        };
        editor.SetBinding(Editor.TextProperty, "Mensagem");
        editor.SetBinding(IsEnabledProperty, "CanLoad");
        SemanticProperties.SetDescription(editor, "Mensagem para a TutorIA");
        var send = new Button { Text = "Enviar", AutomationId = "TutorIaEnviar", VerticalOptions = LayoutOptions.Center };
        send.SetBinding(IsEnabledProperty, "PodeEnviar");
        send.Clicked += async (_, _) => await vm.EnviarAsync(Token);
        var composer = new Grid { ColumnSpacing = 10, ColumnDefinitions = { new(GridLength.Star), new(GridLength.Auto) } };
        composer.Add(editor, 0, 0);
        composer.Add(send, 1, 0);
        var grid = new Grid
        {
            Padding = new Thickness(12, 8, 12, 12), RowSpacing = 8,
            RowDefinitions = { new(GridLength.Star), new(GridLength.Auto), new(GridLength.Auto) }
        };
        grid.Add(messages, 0, 0);
        grid.Add(notices, 0, 1);
        grid.Add(composer, 0, 2);
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
                Content = new VerticalStackLayout { Spacing = 10, Children = { Texto("Titulo", 20), Texto("Mensagem"), Texto("Data", 14), Texto("Leitura", 14), read } }
            };
        });
        var more = new Button { Text = "Carregar mais" };
        more.SetBinding(IsVisibleProperty, "HasMore");
        more.SetBinding(IsEnabledProperty, "CanLoad");
        more.Clicked += async (_, _) => await vm.MaisAsync(Token);
        var grid = new Grid { Padding = 16, RowDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(new VerticalStackLayout { Spacing = 10, Children = { Texto("Resumo"), Estado(), reload, all, empty } }, 0, 0);
        grid.Add(list, 0, 1);
        grid.Add(more, 0, 2);
        Content = grid;
    }
    protected override Task CarregarAsync() => vm.CarregarAsync(Token);
}


