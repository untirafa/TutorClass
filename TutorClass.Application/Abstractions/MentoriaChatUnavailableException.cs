namespace TutorClass.Application.Abstractions;

public sealed class MentoriaChatUnavailableException : Exception
{
    public MentoriaChatUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
