using System.ComponentModel;
using System.Runtime.CompilerServices;
using TutorClass.Mobile.Services;

namespace TutorClass.Mobile.ViewModels;

public interface IAppNavigation
{
    void ShowHome();
    void ShowLogin();
    Task ShowProfessoresAsync();
    Task ShowMateriasAsync();
    Task ShowPerfilAsync();
    Task ShowPlanoAsync();
    Task ShowTutorIaAsync();
    Task ShowNotificacoesAsync();
    Task ShowMinhasAulasAsync();
    Task ShowProfessorAsync(Guid id);
}

public sealed class LoginViewModel(SessionService session, IAppNavigation navigation) : INotifyPropertyChanged
{
    private string email = "", senha = "", erro = "";
    private bool isBusy;
    public string Email { get => email; set { email = value; Changed(); } }
    public string Senha { get => senha; set { senha = value; Changed(); } }
    public string Erro { get => erro; private set { erro = value; Changed(); } }
    public bool IsBusy { get => isBusy; private set { isBusy = value; Changed(); Changed(nameof(CanSubmit)); } }
    public bool CanSubmit => !IsBusy;
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));

    public Task LoginAsync() => RunAsync(async () =>
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
        {
            Erro = "Informe seu email e sua senha.";
            return;
        }
        await session.LoginAsync(Email, Senha);
        Senha = "";
        navigation.ShowHome();
    });

    public Task RestoreAsync() => RunAsync(async () =>
    {
        if (await session.RestoreAsync()) navigation.ShowHome();
    });

    private async Task RunAsync(Func<Task> action)
    {
        if (IsBusy) return;
        IsBusy = true;
        Erro = "";
        try { await action(); }
        catch (SessionException ex) { Erro = ex.Message; }
        catch (UnauthorizedAccessException) { Erro = "Sua sessão expirou. Entre novamente."; }
        catch (OperationCanceledException) { Erro = "A conexão demorou demais. Tente novamente."; }
        catch (HttpRequestException) { Erro = "Não foi possível conectar. Verifique sua conexão e tente novamente."; }
        catch { Erro = "Não foi possível iniciar a sessão com segurança. Tente novamente."; }
        finally { IsBusy = false; }
    }
}




