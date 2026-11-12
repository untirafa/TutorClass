namespace TutorClass.Mobile.Views;

/// <summary>
/// Casca nativa do aplicativo: abre a própria interface Web do TutorClass (todos os perfis, acessibilidade,
/// VLibras, cadastro, TutorIA etc.), com tela de carregamento, aviso de falta de conexão e botão Voltar do Android.
/// </summary>
public sealed class WebShellPage : ContentPage
{
    private static readonly Color Verde = Color.FromArgb("#1C4F96");
    private readonly WebView web;
    private readonly Grid loading;
    private readonly Label loadingText;
    private readonly Grid offline;
    private bool failed;

    public WebShellPage()
    {
        BackgroundColor = Verde;
        SafeAreaEdges = SafeAreaEdges.All;
        NavigationPage.SetHasNavigationBar(this, false);

        web = new WebView
        {
            Source = new UrlWebViewSource { Url = Services.AppConfig.BaseUrl },
            BackgroundColor = Color.FromArgb("#0B1322"), // mesmo fundo do tema escuro do site: evita o clarão branco ao abrir
            AutomationId = "TutorClassWeb"
        };
        web.Navigating += OnNavigating;
        web.Navigated += OnNavigated;

        var spinner = new ActivityIndicator { IsRunning = true, Color = Colors.White, HeightRequest = 36 };
        loadingText = new Label
        {
            Text = "Abrindo o TutorClass…",
            TextColor = Colors.White, FontSize = 16, HorizontalTextAlignment = TextAlignment.Center
        };
        loading = new Grid
        {
            BackgroundColor = Verde, InputTransparent = false,
            Children =
            {
                new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center, Spacing = 16, Padding = new Thickness(32),
                    Children = { new Image { Source = "logo_tutorclass.png", HeightRequest = 120, HorizontalOptions = LayoutOptions.Center, AutomationId = "LogoTutorClass" }, spinner, loadingText }
                }
            }
        };

        var retry = new Button
        {
            Text = "Tentar novamente", AutomationId = "TentarNovamente",
            BackgroundColor = Colors.White, TextColor = Verde, CornerRadius = 12, MinimumHeightRequest = 52,
            HorizontalOptions = LayoutOptions.Fill
        };
        retry.Clicked += (_, _) => Load();
        offline = new Grid
        {
            BackgroundColor = Verde, IsVisible = false,
            Children =
            {
                new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center, Spacing = 16, Padding = new Thickness(32),
                    Children =
                    {
                        new Label { Text = "Sem conexão", TextColor = Colors.White, FontSize = 26, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
                        new Label { Text = "Não foi possível abrir o TutorClass. Verifique sua internet e tente novamente.", TextColor = Colors.White, FontSize = 17, HorizontalTextAlignment = TextAlignment.Center },
                        retry
                    }
                }
            }
        };

        Content = new Grid { Children = { web, loading, offline } };
        Load();
    }

    private void Load()
    {
        failed = false;
        offline.IsVisible = false;
        loading.IsVisible = true;
        loadingText.Text = "Abrindo o TutorClass…";
        web.Source = new UrlWebViewSource { Url = Services.AppConfig.BaseUrl };
        // O servidor gratuito pode levar até um minuto para acordar.
        Dispatcher.StartTimer(TimeSpan.FromSeconds(8), () =>
        {
            if (loading.IsVisible && !failed) loadingText.Text = "Acordando o servidor, pode levar até 1 minuto…";
            return false;
        });
    }

    private async void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!Uri.TryCreate(e.Url, UriKind.Absolute, out var uri)) return;
        var home = new Uri(Services.AppConfig.BaseUrl);
        var interno = uri.Scheme is "about" or "data" or "blob" || uri.Host.Equals(home.Host, StringComparison.OrdinalIgnoreCase);
        if (interno) return;
        // Links para outros sites abrem no navegador do celular.
        e.Cancel = true;
        try { await Launcher.OpenAsync(uri); } catch { /* sem aplicativo para abrir o link */ }
    }

    private void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result == WebNavigationResult.Success)
        {
            failed = false;
            offline.IsVisible = false;
            loading.IsVisible = false;
            return;
        }
        if (e.Result == WebNavigationResult.Cancel) return;
        failed = true;
        loading.IsVisible = false;
        offline.IsVisible = true;
    }

    protected override bool OnBackButtonPressed()
    {
#if ANDROID
        if (web.Handler?.PlatformView is Android.Webkit.WebView native && native.CanGoBack())
        {
            native.GoBack();
            return true;
        }
#endif
        return base.OnBackButtonPressed();
    }
}
