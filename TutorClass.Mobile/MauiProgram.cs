using TutorClass.Mobile.Services;

namespace TutorClass.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("TutorClassWeb", (handler, _) =>
        {
            var settings = handler.PlatformView.Settings;
            settings.JavaScriptEnabled = true;
            settings.DomStorageEnabled = true;
            settings.MediaPlaybackRequiresUserGesture = false;
            settings.MixedContentMode = Android.Webkit.MixedContentHandling.NeverAllow;
            settings.SetSupportMultipleWindows(false);
        });
#endif
        builder.UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular")
                .AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold"));
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(AppConfig.BaseUrl),
            Timeout = TimeSpan.FromSeconds(60)
        });
        builder.Services.AddSingleton<ITokenStore, SecureTokenStore>();
        builder.Services.AddSingleton<SessionService>();
        builder.Services.AddSingleton<ProfessorService>();
        builder.Services.AddSingleton<MateriaService>();
        builder.Services.AddSingleton<AulaService>();
        builder.Services.AddSingleton<PlanoService>();
        builder.Services.AddSingleton<TutorIaService>();
        builder.Services.AddSingleton<NotificacaoService>();
        return builder.Build();
    }
}




