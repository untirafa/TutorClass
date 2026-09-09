using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TutorClass.API.Hubs;
using TutorClass.API.Middleware;
using TutorClass.API.Security;
using TutorClass.Application.Abstractions;
using TutorClass.Application;
using TutorClass.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSignalR();
var configuredCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("TutorClassFrontend", policy =>
    {
        var origins = configuredCorsOrigins.Length > 0
            ? configuredCorsOrigins
            : builder.Environment.IsDevelopment()
                ?
                [
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "http://127.0.0.1:3000",
                    "http://127.0.0.1:5173"
                ]
                : [];

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

});
builder.Services.AddApplication();
TutorClass.Infrastructure.AI.TutorIaRegistration.AddTutorIaProvider(builder.Services, builder.Configuration);
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddInfrastructure(builder.Configuration);
}
builder.Services.AddScoped<TutorClass.Application.Abstractions.IJwtTokenService, JwtTokenService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
{
    throw new InvalidOperationException("Jwt:Secret deve ser configurado por User Secrets ou variavel de ambiente.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
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

app.UseCors("TutorClassFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", async (IApplicationDbContext db, IMentoriaChatStore chatStore, CancellationToken cancellationToken) =>
{
    var postgresHealthy = true;
    try
    {
        _ = db.Materias.Take(1).ToArray();
    }
    catch
    {
        postgresHealthy = false;
    }

    var mongoStatus = chatStore.Configurado
        ? await chatStore.IsHealthyAsync(cancellationToken) ? "Healthy" : "Unhealthy"
        : "NotConfigured";

    return Results.Ok(new
    {
        status = postgresHealthy && mongoStatus is "Healthy" or "NotConfigured" ? "Healthy" : "Degraded",
        service = "TutorClass.API",
        timestampUtc = DateTime.UtcNow,
        postgresql = postgresHealthy ? "Healthy" : "Unhealthy",
        mongodb = mongoStatus
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
