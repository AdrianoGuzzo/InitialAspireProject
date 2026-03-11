using Bogus;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Tests.Builders;

public class LoginModelBuilder
{
    private readonly Faker _faker = new("pt_BR");

    private string? _email;
    private string? _password;

    public LoginModelBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public LoginModelBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public LoginModel Build() => new()
    {
        Email    = _email    ?? _faker.Internet.Email(),
        Password = _password ?? $"Pass{_faker.Random.AlphaNumeric(6)}1$"
    };

    public static LoginModel Default() => new LoginModelBuilder().Build();
}
