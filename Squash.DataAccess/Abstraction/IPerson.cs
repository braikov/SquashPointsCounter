namespace Squash.DataAccess.Abstraction
{
    public interface IPerson
    {
        string Name { get; }
        string Email { get; }

        string? FirstName => Name?.Split(' ', StringSplitOptions.RemoveEmptyEntries)?.FirstOrDefault() ?? Name;
        string? FirstNameOrEmail => FirstName ?? Email;
    }
}