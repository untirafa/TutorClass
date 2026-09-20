using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TutorClass.API.Configuration;

namespace TutorClass.Tests;

public sealed class LocalDevelopmentTests
{
    [Fact]
    public void Development_sem_configuracao_reutiliza_banco_e_chave_locais_ao_reiniciar()
    {
        using var temporaryRoot = new TemporaryContentRoot();
        var environment = new TestEnvironment(temporaryRoot.Path, "Development");
        var firstConfiguration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        Assert.True(LocalDevelopment.Configure(firstConfiguration, environment));

        var firstConnection = new SqliteConnectionStringBuilder(firstConfiguration.GetConnectionString("TutorClassDb"));
        Assert.Equal(System.IO.Path.Combine(temporaryRoot.Path, ".local", "tutorclass.db"), firstConnection.DataSource);
        Assert.True(firstConnection.ForeignKeys);
        var firstSecret = firstConfiguration["Jwt:Secret"];
        Assert.False(string.IsNullOrWhiteSpace(firstSecret));
        Assert.Equal(64, Convert.FromBase64String(firstSecret!).Length);
        Assert.True(File.Exists(System.IO.Path.Combine(temporaryRoot.Path, ".local", "jwt-secret.key")));

        var secondConfiguration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        Assert.True(LocalDevelopment.Configure(secondConfiguration, environment));
        Assert.Equal(firstConfiguration.GetConnectionString("TutorClassDb"), secondConfiguration.GetConnectionString("TutorClassDb"));
        // Boolean assertions avoid including signing keys in failure output.
        Assert.True(CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(firstSecret!), Encoding.UTF8.GetBytes(secondConfiguration["Jwt:Secret"]!)));
    }

    [Fact]
    public void Development_preserva_PostgreSQL_e_chave_configurados_explicitamente()
    {
        using var temporaryRoot = new TemporaryContentRoot();
        const string connection = "Host=localhost;Database=tutorclass_test;Username=test_user;Password=test-only";
        const string secret = "configured-test-signing-key-with-at-least-thirty-two-bytes";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:TutorClassDb"] = connection,
            ["Jwt:Secret"] = secret
        }).Build();

        var useLocalDatabase = LocalDevelopment.Configure(configuration, new TestEnvironment(temporaryRoot.Path, "Development"));

        Assert.False(useLocalDatabase);
        Assert.Equal(connection, configuration.GetConnectionString("TutorClassDb"));
        Assert.True(CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)));
        Assert.False(Directory.Exists(System.IO.Path.Combine(temporaryRoot.Path, ".local")));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Testing")]
    public void Fora_de_Development_nao_cria_configuracao_local(string environmentName)
    {
        using var temporaryRoot = new TemporaryContentRoot();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var useLocalDatabase = LocalDevelopment.Configure(configuration, new TestEnvironment(temporaryRoot.Path, environmentName));

        Assert.False(useLocalDatabase);
        Assert.Null(configuration.GetConnectionString("TutorClassDb"));
        Assert.Null(configuration["Jwt:Secret"]);
        Assert.False(Directory.Exists(System.IO.Path.Combine(temporaryRoot.Path, ".local")));
    }

    private sealed class TestEnvironment(string rootPath, string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "TutorClass.Tests";
        public string ContentRootPath { get; set; } = rootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TemporaryContentRoot : IDisposable
    {
        private const string Prefix = "TutorClass.LocalDevelopmentTests.";
        private readonly string _temporaryDirectory = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath())
            .TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

        public string Path { get; } = Directory.CreateTempSubdirectory(Prefix).FullName;

        public void Dispose()
        {
            var resolvedPath = System.IO.Path.GetFullPath(Path);
            if (!string.Equals(System.IO.Path.GetDirectoryName(resolvedPath), _temporaryDirectory, StringComparison.OrdinalIgnoreCase)
                || !System.IO.Path.GetFileName(resolvedPath).StartsWith(Prefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("O diretorio de teste deve permanecer dentro da pasta temporaria.");
            }

            if (Directory.Exists(resolvedPath))
                Directory.Delete(resolvedPath, recursive: true);
        }
    }
}
