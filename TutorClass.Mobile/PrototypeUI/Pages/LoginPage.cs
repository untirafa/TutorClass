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
        BackgroundColor = Color.FromArgb("#F5F8FF");
        var email = new Entry { Placeholder = "seu@email.com", Keyboard = Keyboard.Email, ReturnType = ReturnType.Next, IsTextPredictionEnabled = false, AutomationId = "LoginEmail" };
        email.SetBinding(Entry.TextProperty, nameof(vm.Email));
        var password = new Entry { Placeholder = "Sua senha", IsPassword = true, ReturnType = ReturnType.Done, AutomationId = "LoginPassword" };
        password.SetBinding(Entry.TextProperty, nameof(vm.Senha));
        email.Completed += (_, _) => password.Focus();
        var submit = new Button { Text = "Entrar na minha conta  →", AutomationId = "LoginSubmit", BackgroundColor = Color.FromArgb("#1D5FE8"), TextColor = Colors.White, CornerRadius = 26, HeightRequest = 56, FontAttributes = FontAttributes.Bold };
        submit.SetBinding(IsEnabledProperty, nameof(vm.CanSubmit));
        submit.Clicked += async (_, _) => await vm.LoginAsync();
        password.Completed += async (_, _) => await vm.LoginAsync();
        var error = new Label { TextColor = Color.FromArgb("#9F1239"), AutomationId = "LoginError" };
        error.SetBinding(Label.TextProperty, nameof(vm.Erro));
        var progress = new ActivityIndicator { Color = Color.FromArgb("#1D5FE8") };
        progress.SetBinding(ActivityIndicator.IsRunningProperty, nameof(vm.IsBusy));
        progress.SetBinding(IsVisibleProperty, nameof(vm.IsBusy));
        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(24, 24, 24, 32), Spacing = 16,
                Children =
                {
                    new Image { Source = "logo-tutorclass.jpeg", WidthRequest = 104, HeightRequest = 82, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Start },
                    new Label { Text = "Aprenda melhor.\nVá mais longe.", FontSize = 32, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#101A2E") },
                    new Label { Text = "Entre para continuar sua jornada.", FontSize = 16, TextColor = Color.FromArgb("#5E6C82") },
                    new Label { Text = "Entrar", Margin = new Thickness(0,16,0,0), FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#101A2E") },
                    new Label { Text = "E-mail", TextColor = Color.FromArgb("#33445F"), FontAttributes = FontAttributes.Bold }, email,
                    new Label { Text = "Senha", TextColor = Color.FromArgb("#33445F"), FontAttributes = FontAttributes.Bold }, password,
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


