using TutorClass.Mobile.ViewModels;

using TutorClass.Mobile.PrototypeUI.Styles;
namespace TutorClass.Mobile.PrototypeUI.Pages;

public sealed class ProfessoresPage : PrototypePage
{
    private readonly ProfessoresViewModel vm;
    private CancellationTokenSource? loading;
    public ProfessoresPage(ProfessoresViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "Professores";
        BackgroundColor = Color.FromArgb("#F4F5F7");
        var progress = new ActivityIndicator();
        progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(vm.IsBusy));
        progress.SetBinding(IsVisibleProperty, nameof(vm.IsBusy));
        var error = new Label { TextColor = Color.FromArgb("#9F1239"), AutomationId = "ProfessoresErro" };
        error.SetBinding(Label.TextProperty, nameof(vm.Erro));
        error.SetBinding(IsVisibleProperty, nameof(vm.HasError));
        var empty = new Label { Text = "Nenhum professor disponível no momento.", AutomationId = "ProfessoresVazio" };
        empty.SetBinding(IsVisibleProperty, nameof(vm.IsEmpty));
        var retry = new Button { Text = "Atualizar", AutomationId = "ProfessoresAtualizar" };
        retry.SetBinding(IsEnabledProperty, nameof(vm.CanLoad));
        retry.Clicked += async (_, _) => await ReloadAsync();
        var list = new CollectionView { SelectionMode = SelectionMode.Single, AutomationId = "ProfessoresLista" };
        list.SetBinding(ItemsView.ItemsSourceProperty, nameof(vm.Professores));
        list.ItemTemplate = new DataTemplate(() =>
        {
            var stack = new VerticalStackLayout { Padding = 16, Spacing = 10 };
            foreach (var field in new[] { "Nome", "Materias", "Preco", "Avaliacao" })
            {
                var label = new Label { FontSize = field == "Nome" ? 21 : 15, TextColor = Colors.Black };
                if (field == "Nome") label.FontAttributes = FontAttributes.Bold;
                label.SetBinding(Label.TextProperty, field);
                stack.Children.Add(label);
            }
            return new Border { Content = stack, Margin = new Thickness(0, 4), BackgroundColor = Colors.White };
        });
        list.SelectionChanged += async (_, args) =>
        {
            if (args.CurrentSelection.FirstOrDefault() is not ProfessorItemViewModel professor) return;
            list.SelectedItem = null;
            await vm.SelecionarAsync(professor);
        };
        var header = new VerticalStackLayout { Spacing = 10, Children = { progress, error, empty, retry } };
        var grid = new Grid { Padding = 20, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
        grid.Add(header, 0, 0);
        grid.Add(list, 0, 1);
        Content = grid;
    }
    private async Task ReloadAsync()
    {
        if (vm.IsBusy) return;
        loading?.Dispose();
        loading = new();
        await vm.CarregarAsync(loading.Token);
    }
    protected override async void OnAppearing() { base.OnAppearing(); await ReloadAsync(); }
    protected override void OnDisappearing() { loading?.Cancel(); base.OnDisappearing(); }
}

