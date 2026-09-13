using TutorClass.Mobile.Services;

namespace TutorClass.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular")
                .AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold"));
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri("https://tutorclass.onrender.com/"),
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




