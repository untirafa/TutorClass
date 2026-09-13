using TutorClass.Mobile.ViewModels;

using TutorClass.Mobile.PrototypeUI.Styles;
namespace TutorClass.Mobile.PrototypeUI.Pages;

public sealed class LoginPage : PrototypePage
{
    private readonly LoginViewModel vm;
    private bool initialized;
    public LoginPage(LoginViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "Entrar";
        BackgroundColor = Color.FromArgb("#F4F5F7");
        var email = new Entry { Placeholder = "seu@email.com", Keyboard = Keyboard.Email, ReturnType = ReturnType.Next, IsTextPredictionEnabled = false, AutomationId = "LoginEmail" };
        email.SetBinding(Entry.TextProperty, nameof(vm.Email));
        var password = new Entry { Placeholder = "Sua senha", IsPassword = true, ReturnType = ReturnType.Done, AutomationId = "LoginPassword" };
        password.SetBinding(Entry.TextProperty, nameof(vm.Senha));
        email.Completed += (_, _) => password.Focus();
        var submit = new Button { Text = "Entrar", AutomationId = "LoginSubmit", BackgroundColor = Color.FromArgb("#334155"), TextColor = Colors.White };
        submit.SetBinding(IsEnabledProperty, nameof(vm.CanSubmit));
        submit.Clicked += async (_, _) => await vm.LoginAsync();
        password.Completed += async (_, _) => await vm.LoginAsync();
        var error = new Label { TextColor = Color.FromArgb("#9F1239"), AutomationId = "LoginError" };
        error.SetBinding(Label.TextProperty, nameof(vm.Erro));
        var progress = new ActivityIndicator { Color = Color.FromArgb("#334155") };
        progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(vm.IsBusy));
        progress.SetBinding(IsVisibleProperty, nameof(vm.IsBusy));
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 24, 24, 32), Spacing = 18,
                Children =
                {
                    new Label { Text = "TutorClass", FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#334155") },
                    new Label { Text = "Interface temporária para testes", FontSize = 18, TextColor = Colors.Black },
                    new Label { Text = "Entre com sua conta", Margin = new Thickness(0,24,0,0), FontSize = 24, TextColor = Colors.Black },
                    new Label { Text = "Email", TextColor = Colors.Black }, email,
                    new Label { Text = "Senha", TextColor = Colors.Black }, password,
                    error, submit, progress
                }
            }
        };
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (initialized) return;
        initialized = true;
        await vm.RestoreAsync();
    }
}


