using TutorClass.Mobile.PrototypeUI.Components;
using TutorClass.Mobile.PrototypeUI.Styles;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.PrototypeUI.Pages;

public sealed class HomePage : PrototypePage
{
    public HomePage(HomeViewModel vm)
    {
        BindingContext = vm;
        Title = "Início";
        var name = new Label { FontSize = 28, FontAttributes = FontAttributes.Bold, AutomationId = "HomeName" };
        name.SetBinding(Label.TextProperty, nameof(vm.Nome));
        var logout = new Button { Text = "Sair da conta", AutomationId = "Logout" };
        logout.Clicked += (_, _) => vm.Logout();
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(20, 16, 20, 32), Spacing = 12,
            Children =
            {
                new Label { Text = "Interface temporária para testes", FontSize = 13, TextColor = Color.FromArgb("#64748B") },
                new Label { Text = "Olá,", FontSize = 18 }, name,
                PrototypeElements.Heading("Estudos"),
                PrototypeElements.Card(new VerticalStackLayout { Spacing = 12, Children =
                {
                    PrototypeElements.Action("Professores", "HomeProfessores", vm.AbrirProfessoresAsync),
                    PrototypeElements.Action("Matérias", "HomeMaterias", vm.AbrirMateriasAsync),
                    PrototypeElements.Action("Minhas aulas", "HomeMinhasAulas", vm.AbrirMinhasAulasAsync)
                }}),
                PrototypeElements.Heading("Apoio"),
                PrototypeElements.Card(new VerticalStackLayout { Spacing = 12, Children =
                {
                    PrototypeElements.Action("TutorIA", "HomeTutorIa", vm.AbrirTutorIaAsync),
                    PrototypeElements.Action("Notificações", "HomeNotificacoes", vm.AbrirNotificacoesAsync)
                }}),
                PrototypeElements.Heading("Conta"),
                PrototypeElements.Card(new VerticalStackLayout { Spacing = 12, Children =
                {
                    PrototypeElements.Action("Perfil", "HomePerfil", vm.AbrirPerfilAsync),
                    PrototypeElements.Action("Plano / Assinatura", "HomePlano", vm.AbrirPlanoAsync),
                    logout
                }})
            }
        };
        Content = new ScrollView { Content = stack };
    }
}
