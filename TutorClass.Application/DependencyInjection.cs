using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using TutorClass.Application.Abstractions;
using TutorClass.Application.Services;
using TutorClass.Domain.Users;

namespace TutorClass.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<MateriaService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ProfessorService>();
        services.AddScoped<DisponibilidadeProfessorService>();
        services.AddScoped<AulaService>();
        services.AddScoped<GravacaoAulaService>();
        services.AddScoped<AcompanhamentoAulaService>();
        services.AddScoped<AvaliacaoProfessorService>();
        services.AddScoped<MaterialPedagogicoService>();
        services.AddScoped<PlanoAlunoService>();
        services.AddScoped<MentoriaService>();
        services.AddScoped<FinanceiroService>();
        services.AddScoped<TutorIaService>();
        services.AddSingleton<TutorIaMemoria>();
        services.AddScoped<IContextoIaService, ContextoIaService>();
        services.AddScoped<ITutorIaProvider, TutorIaSimuladaProvider>();
        services.AddScoped<MentoriaChatService>();
        services.AddScoped<ResponsavelAlunoService>();
        services.AddScoped<FaqService>();
        services.AddScoped<NotificacaoService>();
        services.AddScoped<AnalyticsService>();
        services.AddSingleton<IMentoriaChatStore, UnconfiguredMentoriaChatStore>();
        services.AddScoped<CandidaturaProfessorService>();
        services.AddScoped<QualificacaoProfessorService>();
        services.AddScoped<DevelopmentDataService>();
        services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

        return services;
    }
}
