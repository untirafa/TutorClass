using TutorClass.Mobile.Services;
using TutorClass.Mobile.ViewModels;

namespace TutorClass.Mobile.Tests;

// Opt-in: credentials come from the process environment, never source or test output.
public sealed class RenderTests
{
    [RenderFact]
    public async Task LoginRealEConsultaAutenticada()
    {
        var email = Environment.GetEnvironmentVariable("TUTORCLASS_TEST_EMAIL");
        var password = Environment.GetEnvironmentVariable("TUTORCLASS_TEST_PASSWORD");
        Assert.False(string.IsNullOrWhiteSpace(email), "Set TUTORCLASS_TEST_EMAIL to run the Render test.");
        Assert.False(string.IsNullOrWhiteSpace(password), "Set TUTORCLASS_TEST_PASSWORD to run the Render test.");
        using var client = new HttpClient { BaseAddress = new("https://tutorclass.onrender.com/"), Timeout = TimeSpan.FromSeconds(90) };
        var store = new SessionTests.Store();
        var session = new SessionService(client, store);
        var nav = new SessionTests.Navigation();
        var vm = new LoginViewModel(session, nav) { Email = email!, Senha = password! };
        await vm.LoginAsync();
        Assert.True(nav.Page == "Home", vm.Erro);
        Assert.False(string.IsNullOrWhiteSpace(session.Usuario?.Nome));
        Assert.False(string.IsNullOrWhiteSpace(store.Token));
        Assert.True(await session.RestoreAsync());
        session.Logout();
        Assert.Null(store.Token);
    }
}

public sealed class RenderFactAttribute : FactAttribute
{
    public RenderFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TUTORCLASS_TEST_EMAIL")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TUTORCLASS_TEST_PASSWORD")))
            Skip = "Opt-in Render test: set TUTORCLASS_TEST_EMAIL and TUTORCLASS_TEST_PASSWORD.";
    }
}
