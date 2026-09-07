namespace TutorClass.Infrastructure.Chat;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string? ConnectionString { get; init; }
    public string? DatabaseName { get; init; }
}
