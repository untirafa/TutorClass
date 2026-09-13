using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Views;

public sealed class HomePage : ContentPage
{
    public HomePage(HomeViewModel vm)
    {
        BindingContext = vm;
        Title = "Início";
        BackgroundColor = Color.FromArgb("#F5F8F6");
        var name = new Label { FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#125C50"), AutomationId = "HomeName" };
        name.SetBinding(Label.TextProperty, nameof(vm.Nome));
        var logout = new Button { Text = "Sair da conta", AutomationId = "Logout", BackgroundColor = Color.FromArgb("#125C50"), TextColor = Colors.White };
        logout.Clicked += (_, _) => vm.Logout();
        var professores = new Button { Text = "Professores", AutomationId = "HomeProfessores" };
        professores.Clicked += async (_, _) => await vm.AbrirProfessoresAsync();
        var materias = new Button { Text = "Matérias", AutomationId = "HomeMaterias" };
        materias.Clicked += async (_, _) => await vm.AbrirMateriasAsync();
        var aulas = new Button { Text = "Minhas Aulas", AutomationId = "HomeMinhasAulas" };
        aulas.Clicked += async (_, _) => await vm.AbrirMinhasAulasAsync();
        var tutorIa = new Button { Text = "TutorIA", AutomationId = "HomeTutorIa" };
        tutorIa.Clicked += async (_, _) => await vm.AbrirTutorIaAsync();
        var notificacoes = new Button { Text = "Notificações", AutomationId = "HomeNotificacoes" };
        notificacoes.Clicked += async (_, _) => await vm.AbrirNotificacoesAsync();
        var perfil = new Button { Text = "Perfil", AutomationId = "HomePerfil" };
        perfil.Clicked += async (_, _) => await vm.AbrirPerfilAsync();
        var plano = new Button { Text = "Plano / Assinatura", AutomationId = "HomePlano" };
        plano.Clicked += async (_, _) => await vm.AbrirPlanoAsync();
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(28,64,28,32), Spacing = 20,
                Children =
                {
                    new Label { Text = "TutorClass", FontSize = 18, TextColor = Colors.Black },
                    new Label { Text = "Bem-vindo(a),", FontSize = 22, TextColor = Colors.Black },
                    name,
                    new Label { Text = "Sua sessão está confirmada. Este é o início do TutorClass no seu celular.", FontSize = 16, TextColor = Colors.Black },
                    professores, materias, aulas, tutorIa, notificacoes, perfil, plano,
                    new Label { Text = "Em breve", FontSize = 22, TextColor = Colors.Black, Margin = new Thickness(0,20,0,0) },
                    new Button { Text = "Estudos • Em breve", IsEnabled = false },
                    new Button { Text = "Agenda • Em breve", IsEnabled = false },
                    logout
                }
            }
        };
    }
}




