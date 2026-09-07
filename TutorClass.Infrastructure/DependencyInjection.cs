using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TutorClass.Application.Abstractions;
using TutorClass.Infrastructure.Chat;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TutorClassDb");

        services.AddDbContext<TutorClassDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<TutorClassDbContext>());

        var mongoOptions = configuration.GetSection(MongoDbOptions.SectionName).Get<MongoDbOptions>() ?? new MongoDbOptions();
        if (!string.IsNullOrWhiteSpace(mongoOptions.ConnectionString) &&
            !string.IsNullOrWhiteSpace(mongoOptions.DatabaseName))
        {
            services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoOptions.ConnectionString));
            services.AddSingleton(provider => provider.GetRequiredService<IMongoClient>().GetDatabase(mongoOptions.DatabaseName));
            services.AddSingleton<IMentoriaChatStore, MongoMentoriaChatStore>();
        }

        return services;
    }
}
