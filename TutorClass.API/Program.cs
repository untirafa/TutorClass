using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.SignalR;
using TutorClass.API.Hubs;
using TutorClass.API.Configuration;
using TutorClass.API.Middleware;
using TutorClass.API.Security;
using TutorClass.Application.Abstractions;
using TutorClass.Application;
using TutorClass.Infrastructure;
using TutorClass.Infrastructure.Persistence;
using TutorClass.Infrastructure.Chat;

var builder = WebApplication.CreateBuilder(args);
var useLocalDatabase = LocalDevelopment.Configure(builder.Configuration, builder.Environment);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ControleAbuso>();
builder.Services.AddSingleton<LimiteChatHubFilter>();
builder.Services.AddSignalR(options => options.AddFilter<LimiteChatHubFilter>());
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("TutorClassFrontend", policy =>
    {
        // O site publicado usa a mesma origem; CORS adicional é só de desenvolvimento.
        var origins = builder.Environment.IsDevelopment() ? configuredCorsOrigins : [];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe apenas o token JWT. O Swagger enviara como Bearer TOKEN."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.OperationFilter<SwaggerAuthorizationOperationFilter>();
});
builder.Services.AddApplication();
TutorClass.Infrastructure.AI.TutorIaRegistration.AddTutorIaProvider(builder.Services, builder.Configuration);
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddInfrastructure(builder.Configuration, useLocalDatabase);
}
builder.Services.AddScoped<TutorClass.Application.Abstractions.IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException("Jwt:Secret deve ser configurado por User Secrets ou variavel de ambiente.");
}
if (Encoding.UTF8.GetByteCount(jwtOptions.Secret) < 32)
{
    throw new InvalidOperationException("Jwt:Secret deve ter pelo menos 32 bytes para assinar tokens HS256.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            // Conta suspensa pela moderacao perde o acesso imediatamente, sem esperar o token expirar.
            OnTokenValidated = context =>
            {
                var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();
                    var ativo = db.Usuarios.Where(u => u.Id == userId).Select(u => (bool?)u.Ativo).FirstOrDefault();
                    if (ativo == false)
                    {
                        context.Fail("Conta suspensa.");
                    }
                }

                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/mentoria-chat"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Aluno", policy => policy.RequireRole("Aluno"));
    options.AddPolicy("Responsavel", policy => policy.RequireRole("Responsavel"));
    options.AddPolicy("Professor", policy => policy.RequireRole("Professor"));
    options.AddPolicy("Administrador", policy => policy.RequireRole("Administrador"));
});

var app = builder.Build();
if (useLocalDatabase)
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<TutorClassDbContext>();
    // PostgreSQL migrations remain exclusive to PostgreSQL. This is a separate local database.
    await database.Database.EnsureCreatedAsync();
    // EnsureCreated nao altera um banco local que ja existe: cria a tabela de denuncias se ela ainda nao estiver la.
    await database.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS denuncias (
            id TEXT NOT NULL CONSTRAINT pk_denuncias PRIMARY KEY,
            denunciante_id TEXT NOT NULL, alvo_usuario_id TEXT NOT NULL, tipo_alvo TEXT NOT NULL, alvo_id TEXT NOT NULL,
            motivo TEXT NOT NULL, descricao TEXT NULL, resumo_alvo TEXT NOT NULL, vinculo_mentoria_id TEXT NULL,
            status TEXT NOT NULL, sancao TEXT NOT NULL, observacao_moderacao TEXT NULL, analisada_por_id TEXT NULL,
            analisada_em_utc TEXT NULL, conta_reativada_em_utc TEXT NULL, created_at_utc TEXT NOT NULL, updated_at_utc TEXT NULL);
        CREATE INDEX IF NOT EXISTS ix_denuncias_alvo_usuario_id ON denuncias (alvo_usuario_id);
        CREATE INDEX IF NOT EXISTS ix_denuncias_denunciante_id_tipo_alvo_alvo_id ON denuncias (denunciante_id, tipo_alvo, alvo_id);
        CREATE INDEX IF NOT EXISTS ix_denuncias_status_created_at_utc ON denuncias (status, created_at_utc);
        """);
    app.Logger.LogInformation("Desenvolvimento: banco SQLite local em .local/tutorclass.db. As contas persistem ao reiniciar a API.");
}

app.UseMiddleware<TutorIaPerformanceMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapOpenApi();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TutorClass API v1");
    options.RoutePrefix = "swagger";
});

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Somente wwwroot é público. Rotas do frontend usam # e não precisam de fallback.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("TutorClassFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", async (IApplicationDbContext db, IMentoriaChatStore chatStore, CancellationToken cancellationToken) =>
{
    var databaseHealthy = true;
    try
    {
        _ = db.Materias.Take(1).ToArray();
    }
    catch
    {
        databaseHealthy = false;
    }

    var chatStatus = chatStore.Configurado
        ? await chatStore.IsHealthyAsync(cancellationToken) ? "Healthy" : "Unhealthy"
        : "NotConfigured";

    return Results.Ok(new
    {
        status = databaseHealthy && chatStatus is "Healthy" or "NotConfigured" ? "Healthy" : "Degraded",
        service = "TutorClass.API",
        timestampUtc = DateTime.UtcNow,
        database = new { provider = useLocalDatabase ? "SQLite" : "PostgreSQL", status = databaseHealthy ? "Healthy" : "Unhealthy" },
        postgresql = useLocalDatabase ? "NotConfigured" : databaseHealthy ? "Healthy" : "Unhealthy",
        chat = new { provider = chatStore is SqliteMentoriaChatStore ? "SQLite" : chatStore is MongoMentoriaChatStore ? "MongoDB" : "NotConfigured", status = chatStatus },
        mongodb = chatStore is MongoMentoriaChatStore ? chatStatus : "NotConfigured"
    });
})
.WithName("HealthCheck")
.WithTags("Health");
app.MapControllers();
app.MapHub<MentoriaChatHub>("/hubs/mentoria-chat");

app.Run();

public partial class Program
{
}
