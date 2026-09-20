using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using TutorClass.Application.Abstractions;
using TutorClass.Infrastructure.Chat;
using TutorClass.Infrastructure.Persistence;

namespace TutorClass.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool useLocalDatabase = false)
    {
        var connectionString = configuration.GetConnectionString("TutorClassDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Configure ConnectionStrings:TutorClassDb para PostgreSQL ou execute com o perfil http em Development para usar o banco local.");
        }

        services.AddDbContext<TutorClassDbContext>(options =>
        {
            if (useLocalDatabase)
                options.UseSqlite(connectionString);
            else
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
        else if (useLocalDatabase)
        {
            var localDatabase = new SqliteConnectionStringBuilder(connectionString).DataSource;
            var chatPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(localDatabase))!, "mentoria-chat.db");
            services.AddSingleton<IMentoriaChatStore>(_ => new SqliteMentoriaChatStore(chatPath));
        }

        return services;
    }
}
