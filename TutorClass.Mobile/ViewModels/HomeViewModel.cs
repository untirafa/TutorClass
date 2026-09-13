using TutorClass.Mobile.Services;

namespace TutorClass.Mobile.ViewModels;

public sealed class HomeViewModel(SessionService session, IAppNavigation navigation)
{
    public string Nome => session.Usuario?.Nome ?? "";
    public Task AbrirProfessoresAsync() => navigation.ShowProfessoresAsync();
    public Task AbrirMateriasAsync() => navigation.ShowMateriasAsync();
    public Task AbrirMinhasAulasAsync() => navigation.ShowMinhasAulasAsync();
    public Task AbrirTutorIaAsync() => navigation.ShowTutorIaAsync();
    public Task AbrirNotificacoesAsync() => navigation.ShowNotificacoesAsync();
    public Task AbrirPerfilAsync() => navigation.ShowPerfilAsync();
    public Task AbrirPlanoAsync() => navigation.ShowPlanoAsync();
    public void Logout()
    {
        session.Logout();
        navigation.ShowLogin();
    }
}




