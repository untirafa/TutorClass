using TutorClass.Mobile.Models;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Views;

// Shared loading/list layout for these two read-only screens.
public abstract class ListaConsultaPage<T> : ContentPage
{
    private readonly ListaConsultaViewModel<T> vm;
    private CancellationTokenSource? loading;
    protected ListaConsultaPage(ListaConsultaViewModel<T> vm, string title, string emptyText, params string[] fields)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = title;
        BackgroundColor = Color.FromArgb("#F5F8F6");
        var progress = new ActivityIndicator();
        progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(vm.IsBusy));
        progress.SetBinding(IsVisibleProperty, nameof(vm.IsBusy));
        var error = new Label { TextColor = Color.FromArgb("#A12424"), AutomationId = "ListaErro" };
        error.SetBinding(Label.TextProperty, nameof(vm.Erro));
        error.SetBinding(IsVisibleProperty, nameof(vm.HasError));
        var empty = new Label { Text = emptyText, AutomationId = "ListaVazia" };
        empty.SetBinding(IsVisibleProperty, nameof(vm.IsEmpty));
        var reload = new Button { Text = "Atualizar", AutomationId = "ListaAtualizar" };
        reload.SetBinding(IsEnabledProperty, nameof(vm.CanLoad));
        reload.Clicked += async (_, _) => await ReloadAsync();
        var list = new CollectionView { SelectionMode = SelectionMode.None, AutomationId = "ListaItens" };
        list.SetBinding(ItemsView.ItemsSourceProperty, nameof(vm.Itens));
        list.ItemTemplate = new DataTemplate(() =>
        {
            var stack = new VerticalStackLayout { Padding = 16, Spacing = 8 };
            foreach (var field in fields)
            {
                var label = new Label { TextColor = Colors.Black, FontSize = field == fields[0] ? 21 : 15 };
                if (field == fields[0]) label.FontAttributes = FontAttributes.Bold;
                label.SetBinding(Label.TextProperty, field);
                stack.Children.Add(label);
            }
            return new Border { Content = stack, Margin = new Thickness(0, 4), BackgroundColor = Colors.White };
        });
        var grid = new Grid { Padding = 20, RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) } };
        grid.Add(new VerticalStackLayout { Spacing = 10, Children = { progress, error, empty, reload } }, 0, 0);
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

public sealed class MateriasPage(MateriasViewModel vm)
    : ListaConsultaPage<MateriaDto>(vm, "Matérias", "Nenhuma matéria disponível no momento.", "Nome");

public sealed class MinhasAulasPage(MinhasAulasViewModel vm)
    : ListaConsultaPage<AulaItemViewModel>(vm, "Minhas Aulas", "Você ainda não tem aulas.", "Materia", "Professor", "Horario", "Duracao", "Status", "Valor");
