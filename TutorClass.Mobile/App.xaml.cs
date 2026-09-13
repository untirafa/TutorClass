using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;
// TEMPORARY UI: restore TutorClass.Mobile.Views before removing PrototypeUI.
using TutorClass.Mobile.PrototypeUI.Pages;

namespace TutorClass.Mobile;

public partial class App : Application, IAppNavigation
{
    private readonly SessionService session;
    private Window? window;
    private readonly ProfessorService professores;
    private bool navigating;
    private readonly PlanoService planos;
    private readonly TutorIaService tutorIa;
    private readonly NotificacaoService notificacoes;
    private readonly MateriaService materias;
    private readonly AulaService aulas;
    public App(SessionService session, ProfessorService professores, MateriaService materias, AulaService aulas, TutorIaService tutorIa, NotificacaoService notificacoes, PlanoService planos)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;
        this.session = session;
        this.professores = professores;
        this.materias = materias;
        this.aulas = aulas;
        this.planos = planos;
        this.tutorIa = tutorIa;
        this.notificacoes = notificacoes;
    }
    protected override Window CreateWindow(IActivationState? activationState)
    {
        window = new Window(new LoginPage(new LoginViewModel(session, this)));
        return window;
    }
    public void ShowHome()
    {
        if (window is not null) window.Page = new NavigationPage(new HomePage(new HomeViewModel(session, this)));
    }
    public Task ShowProfessoresAsync() => PushAsync(() => new ProfessoresPage(new ProfessoresViewModel(professores, this)));
    public Task ShowProfessorAsync(Guid id) => PushAsync(() => new ProfessorDetalhesPage(new ProfessorDetalhesViewModel(professores, id)));
    public Task ShowMateriasAsync() => PushAsync(() => new MateriasPage(new MateriasViewModel(materias)));
    public Task ShowMinhasAulasAsync() => PushAsync(() => new MinhasAulasPage(new MinhasAulasViewModel(aulas, this)));
    public Task ShowTutorIaAsync() => PushAsync(() => new TutorIaPage(new TutorIaViewModel(tutorIa, this)));
    public Task ShowNotificacoesAsync() => PushAsync(() => new NotificacoesPage(new NotificacoesViewModel(notificacoes, this)));
    public Task ShowPerfilAsync() => PushAsync(() => new PerfilPage(new PerfilViewModel(session, this)));
    public Task ShowPlanoAsync() => PushAsync(() => new PlanoPage(new PlanoViewModel(planos, this)));
    private async Task PushAsync(Func<Page> createPage)
    {
        if (navigating || window?.Page is not NavigationPage root) return;
        navigating = true;
        try { await root.PushAsync(createPage()); }
        finally { navigating = false; }
    }
    public void ShowLogin()
    {
        if (window is not null) window.Page = new LoginPage(new LoginViewModel(session, this));
    }
}






