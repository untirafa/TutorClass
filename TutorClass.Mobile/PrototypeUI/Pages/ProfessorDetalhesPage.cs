using TutorClass.Mobile.ViewModels;

using TutorClass.Mobile.PrototypeUI.Styles;
namespace TutorClass.Mobile.PrototypeUI.Pages;

public sealed class ProfessorDetalhesPage : PrototypePage
{
    private readonly ProfessorDetalhesViewModel vm;
    private CancellationTokenSource? loading;
    public ProfessorDetalhesPage(ProfessorDetalhesViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "Detalhes do professor";
        BackgroundColor = Color.FromArgb("#F4F5F7");
        var progress = new ActivityIndicator();
        progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(vm.IsBusy));
        progress.SetBinding(IsVisibleProperty, nameof(vm.IsBusy));
        var error = new Label { TextColor = Color.FromArgb("#9F1239"), AutomationId = "ProfessorErro" };
        error.SetBinding(Label.TextProperty, nameof(vm.Erro));
        error.SetBinding(IsVisibleProperty, nameof(vm.HasError));
        var missing = new Label { Text = "Professor não encontrado. Volte à lista para escolher outro professor.", AutomationId = "ProfessorNaoEncontrado" };
        missing.SetBinding(IsVisibleProperty, nameof(vm.NotFound));
        var retry = new Button { Text = "Atualizar", AutomationId = "ProfessorAtualizar" };
        retry.SetBinding(IsEnabledProperty, nameof(vm.CanLoad));
        retry.Clicked += async (_, _) => await ReloadAsync();
        var data = new VerticalStackLayout { Spacing = 14, AutomationId = "ProfessorDados" };
        data.SetBinding(IsVisibleProperty, nameof(vm.HasProfessor));
        foreach (var (title, path) in new[]
        {
            ("", "Professor.Nome"), ("Matérias", "Professor.Materias"),
            ("Preço", "Professor.Preco"), ("Avaliações", "Professor.Avaliacao"),
            ("Reputação", "Professor.Reputacao"), ("Qualificações", "Qualificacoes"),
            ("Disponibilidades", "Disponibilidades")
        })
        {
            if (title.Length > 0) data.Children.Add(new Label { Text = title, FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#334155") });
            var label = new Label { FontSize = title.Length == 0 ? 28 : 16, TextColor = Colors.Black };
            label.SetBinding(Label.TextProperty, path);
            data.Children.Add(label);
        }
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 16, Children = { progress, error, missing, retry, data } } };
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

