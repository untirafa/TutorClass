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
        var name = new Label { FontSize = 28, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#174BB8"), AutomationId = "HomeName" };
        name.SetBinding(Label.TextProperty, nameof(vm.Nome));
        var logout = new Button { Text = "Sair da conta", AutomationId = "Logout", BackgroundColor = Color.FromArgb("#FFFFFF"), TextColor = Color.FromArgb("#174BB8"), BorderColor = Color.FromArgb("#C7D8F5"), BorderWidth = 1, CornerRadius = 24 };
        logout.Clicked += (_, _) => vm.Logout();
        var logo = new Image { Source = "logo-tutorclass.jpeg", WidthRequest = 66, HeightRequest = 50, Aspect = Aspect.AspectFit, VerticalOptions = LayoutOptions.Center };
        var brand = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center, Children = { logo, new Label { Text = "TutorClass", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#101A2E"), VerticalTextAlignment = TextAlignment.Center } } };
        var welcome = PrototypeElements.Card(new VerticalStackLayout { Spacing = 5, Children =
        {
            new Label { Text = "SEU ESPAÇO DE APRENDIZAGEM", FontSize = 11, CharacterSpacing = 1.5, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#1D5FE8") },
            new Label { Text = "Olá,", FontSize = 18 }, name,
            new Label { Text = "Aulas, materiais e apoio para avançar no seu ritmo.", FontSize = 14, TextColor = Color.FromArgb("#5E6C82") }
        }});
        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(20, 18, 20, 32), Spacing = 14,
            Children =
            {
                brand, welcome,
                PrototypeElements.Heading("Estudos"),
                PrototypeElements.Card(new VerticalStackLayout { Spacing = 12, Children =
                {
                    PrototypeElements.Action("⌕   Encontrar professores", "HomeProfessores", vm.AbrirProfessoresAsync),
                    PrototypeElements.Action("▤   Matérias e estudos", "HomeMaterias", vm.AbrirMateriasAsync),
                    PrototypeElements.Action("▣   Minhas aulas", "HomeMinhasAulas", vm.AbrirMinhasAulasAsync)
                }}),
                PrototypeElements.Heading("Apoio"),
                PrototypeElements.Card(new VerticalStackLayout { Spacing = 12, Children =
                {
                    PrototypeElements.Action("✦   TutorIA", "HomeTutorIa", vm.AbrirTutorIaAsync),
                    PrototypeElements.Action("♧   Notificações", "HomeNotificacoes", vm.AbrirNotificacoesAsync)
                }}),
                PrototypeElements.Heading("Conta"),
                PrototypeElements.Card(new VerticalStackLayout { Spacing = 12, Children =
                {
                    PrototypeElements.Action("♙   Perfil", "HomePerfil", vm.AbrirPerfilAsync),
                    PrototypeElements.Action("◇   Plano e assinatura", "HomePlano", vm.AbrirPlanoAsync)
                }}),
                logout
            }
        };
        Content = new ScrollView { Content = stack };
    }
}
