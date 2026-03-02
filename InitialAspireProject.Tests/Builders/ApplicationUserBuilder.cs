using Bogus;
using InitialAspireProject.ApiIdentity.Repository;

namespace InitialAspireProject.Tests.Builders;

public class ApplicationUserBuilder
{
    private readonly Faker _faker = new("pt_BR");

    private string _id = Guid.NewGuid().ToString();
    private string? _email;
    private string? _fullName;

    public ApplicationUserBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public ApplicationUserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public ApplicationUserBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public ApplicationUser Build()
    {
        var email = _email ?? _faker.Internet.Email();
        return new ApplicationUser
        {
            Id       = _id,
            Email    = email,
            UserName = email,
            FullName = _fullName ?? _faker.Name.FullName()
        };
    }

    public static ApplicationUser Default() => new ApplicationUserBuilder().Build();
}
