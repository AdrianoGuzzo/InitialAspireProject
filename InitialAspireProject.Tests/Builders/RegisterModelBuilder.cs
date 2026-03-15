using Bogus;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Tests.Builders;

public class RegisterModelBuilder
{
    private readonly Faker _faker = new("pt_BR");

    private string? _email;
    private string? _password;
    private string? _fullName;

    public RegisterModelBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public RegisterModelBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public RegisterModelBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public RegisterModel Build() => new()
    {
        Email    = _email    ?? _faker.Internet.Email(),
        Password = _password ?? $"Pass{_faker.Random.AlphaNumeric(6)}1$",
        FullName = _fullName
    };

    public static RegisterModel Default() => new RegisterModelBuilder().Build();
}
