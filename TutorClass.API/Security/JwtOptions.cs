namespace TutorClass.API.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "TutorClass";
    public string Audience { get; init; } = "TutorClass.Client";
    public string Secret { get; init; } = string.Empty;
    public int ExpirationMinutes { get; init; } = 120;
}
