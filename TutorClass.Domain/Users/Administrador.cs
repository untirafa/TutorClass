namespace TutorClass.Domain.Users;

public sealed class Administrador : Usuario
{
    private Administrador()
    {
    }

    public Administrador(string nome, string email, string passwordHash)
        : base(nome, email, passwordHash)
    {
    }
}
