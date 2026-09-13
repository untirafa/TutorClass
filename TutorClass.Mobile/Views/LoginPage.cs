using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Views;

public sealed class LoginPage : ContentPage
{
    private readonly LoginViewModel vm;
    private bool initialized;
    public LoginPage(LoginViewModel vm)
    {
        this.vm = vm;
        BindingContext = vm;
        Title = "Entrar";
        BackgroundColor = Color.FromArgb("#F5F8F6");
        var email = new Entry { Placeholder = "seu@email.com", Keyboard = Keyboard.Email, AutomationId = "LoginEmail" };
        email.SetBinding(Entry.TextProperty, nameof(vm.Email));
        var password = new Entry { Placeholder = "Sua senha", IsPassword = true, AutomationId = "LoginPassword" };
        password.SetBinding(Entry.TextProperty, nameof(vm.Senha));
        var submit = new Button { Text = "Entrar", AutomationId = "LoginSubmit", BackgroundColor = Color.FromArgb("#125C50"), TextColor = Colors.White };
        submit.SetBinding(IsEnabledProperty, nameof(vm.CanSubmit));
        submit.Clicked += async (_, _) => await vm.LoginAsync();
        var error = new Label { TextColor = Color.FromArgb("#A12424"), AutomationId = "LoginError" };
        error.SetBinding(Label.TextProperty, nameof(vm.Erro));
        var progress = new ActivityIndicator { Color = Color.FromArgb("#125C50") };
        progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(vm.IsBusy));
        progress.SetBinding(IsVisibleProperty, nameof(vm.IsBusy));
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(28, 64, 28, 32), Spacing = 18,
                Children =
                {
                    new Label { Text = "TutorClass", FontSize = 36, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#125C50") },
                    new Label { Text = "Seu próximo passo começa aqui.", FontSize = 18, TextColor = Colors.Black },
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
