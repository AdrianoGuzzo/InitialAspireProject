using Bogus;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Tests.Builders;

public class ResetPasswordModelBuilder
{
    private readonly Faker _faker = new("pt_BR");
    private string? _email;
    private string? _token;
    private string? _newPassword;

    public ResetPasswordModelBuilder WithEmail(string email) { _email = email; return this; }
    public ResetPasswordModelBuilder WithToken(string token) { _token = token; return this; }
    public ResetPasswordModelBuilder WithNewPassword(string newPassword) { _newPassword = newPassword; return this; }

    public ResetPasswordModel Build()
    {
        var password = _newPassword ?? $"Pass{_faker.Random.AlphaNumeric(6)}1$";
        return new()
        {
            Email = _email ?? _faker.Internet.Email(),
            Token = _token ?? _faker.Random.AlphaNumeric(32),
            NewPassword = password,
            ConfirmPassword = password
        };
    }
}
