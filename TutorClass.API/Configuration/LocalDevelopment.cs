using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace TutorClass.API.Configuration;

public static class LocalDevelopment
{
    // Defaults only apply to local development. Explicit secrets and PostgreSQL always take priority.
    public static bool Configure(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
            return false;

        var directory = Path.Combine(environment.ContentRootPath, ".local");
        var useLocalDatabase = string.IsNullOrWhiteSpace(configuration.GetConnectionString("TutorClassDb"));
        if (useLocalDatabase)
        {
            Directory.CreateDirectory(directory);
            configuration["ConnectionStrings:TutorClassDb"] = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(directory, "tutorclass.db"),
                ForeignKeys = true
            }.ToString();
        }

        if (string.IsNullOrWhiteSpace(configuration["Jwt:Secret"]))
        {
            Directory.CreateDirectory(directory);
            var secretPath = Path.Combine(directory, "jwt-secret.key");
            if (!File.Exists(secretPath))
            {
                // CreateNew prevents concurrent startup from overwriting an existing signing key.
                try
                {
                    using var stream = new FileStream(secretPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    using var writer = new StreamWriter(stream);
                    writer.Write(Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
                }
                catch (IOException) when (File.Exists(secretPath)) { }
            }

            configuration["Jwt:Secret"] = File.ReadAllText(secretPath).Trim();
        }

        return useLocalDatabase;
    }
}
