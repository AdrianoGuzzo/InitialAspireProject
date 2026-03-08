using InitialAspireProject.Web.Resources;

namespace InitialAspireProject.Tests.Web;

public class WebMessagesTests
{
    [Fact]
    public void UsernameRequired_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.UsernameRequired);

    [Fact]
    public void PasswordRequired_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.PasswordRequired);

    [Fact]
    public void NameRequired_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.NameRequired);

    [Fact]
    public void EmailRequired_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.EmailRequired);

    [Fact]
    public void InvalidEmail_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.InvalidEmail);

    [Fact]
    public void PasswordMinLength_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.PasswordMinLength);

    [Fact]
    public void PasswordMin8_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.PasswordMin8);

    [Fact]
    public void ConfirmPasswordRequired_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.ConfirmPasswordRequired);

    [Fact]
    public void PasswordMismatch_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.PasswordMismatch);

    [Fact]
    public void MustAcceptTerms_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.MustAcceptTerms);

    [Fact]
    public void NewPasswordRequired_ReturnsNonEmpty() => Assert.NotEmpty(WebMessages.NewPasswordRequired);
}
