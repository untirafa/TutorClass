using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Views;

public sealed class PerfilPage : ComunicacaoPage
{
    private readonly PerfilViewModel vm;
    public PerfilPage(PerfilViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "Perfil";
        var update = new Button { Text = "Atualizar", AutomationId = "PerfilAtualizar" };
        update.SetBinding(IsEnabledProperty, "CanLoad");
        update.Clicked += async (_, _) => await vm.CarregarAsync(Token);
        var data = new VerticalStackLayout
        {
            Spacing = 12, Children =
            {
                new Label { Text = "Nome" }, Texto("Usuario.Nome", 24),
                new Label { Text = "Email" }, Texto("Usuario.Email"),
                new Label { Text = "Perfil" }, Texto("Usuario.Perfil")
            }
        };
        data.SetBinding(IsVisibleProperty, "TemUsuario");
        var logout = new Button { Text = "Sair da conta", AutomationId = "PerfilSair" };
        logout.Clicked += (_, _) => vm.Logout();
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 20, Children = { Estado(), update, data, logout } } };
    }
    protected override Task CarregarAsync() => vm.CarregarAsync(Token);
}

public sealed class PlanoPage : ComunicacaoPage
{
    private readonly PlanoViewModel vm;
    public PlanoPage(PlanoViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "Plano / Assinatura";
        var update = new Button { Text = "Atualizar", AutomationId = "PlanoAtualizar" };
        update.SetBinding(IsEnabledProperty, "CanLoad");
        update.Clicked += async (_, _) => await vm.CarregarAsync(Token);
        var data = new VerticalStackLayout
        {
            Spacing = 16, Children =
            {
                new Label { Text = "Plano atual" }, Texto("Nome", 26),
                Texto("Assinatura", 20), Texto("Beneficios")
            }
        };
        data.SetBinding(IsVisibleProperty, "TemPlano");
        Content = new ScrollView { Content = new VerticalStackLayout { Padding = 24, Spacing = 20, Children = { Estado(), update, data } } };
    }
    protected override Task CarregarAsync() => vm.CarregarAsync(Token);
}
