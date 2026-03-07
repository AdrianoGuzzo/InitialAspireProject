using Bogus;
using InitialAspireProject.ApiIdentity;

namespace InitialAspireProject.Tests.Builders;

public class ForgotPasswordModelBuilder
{
    private readonly Faker _faker = new("pt_BR");
    private string? _email;

    public ForgotPasswordModelBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public ForgotPasswordModel Build() => new()
    {
        Email = _email ?? _faker.Internet.Email()
    };
}
