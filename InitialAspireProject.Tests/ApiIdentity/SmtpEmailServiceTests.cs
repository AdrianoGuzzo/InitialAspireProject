using InitialAspireProject.ApiIdentity.Resources;
using InitialAspireProject.ApiIdentity.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;
using Moq;
using System.Net.Sockets;

namespace InitialAspireProject.Tests.ApiIdentity;

public class SmtpEmailServiceTests
{
    private static (Mock<ISmtpClient>, SmtpEmailService) CreateService(Dictionary<string, string?>? configOverrides = null)
    {
        var configValues = new Dictionary<string, string?>
        {
            ["Smtp:Host"] = "mailpit.test",
            ["Smtp:Port"] = "1025",
            ["Smtp:UseSsl"] = "false",
            ["Smtp:FromAddress"] = "noreply@aspire.local",
            ["Smtp:FromName"] = "Test Project"
        };

        if (configOverrides is not null)
            foreach (var kv in configOverrides)
                configValues[kv.Key] = kv.Value;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        var smtpClientMock = new Mock<ISmtpClient>();
        smtpClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        smtpClientMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        smtpClientMock.Setup(x => x.DisconnectAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var localizer = new Mock<IStringLocalizer<AuthMessages>>();
        localizer.Setup(l => l["EmailBodyPasswordReset"])
                 .Returns(new LocalizedString("EmailBodyPasswordReset", "<a href='{0}'>{0}</a>"));
        localizer.Setup(l => l["EmailBodyActivation"])
                 .Returns(new LocalizedString("EmailBodyActivation", "<a href='{0}'>Activate</a>"));
        localizer.Setup(l => l[It.Is<string>(k => k != "EmailBodyPasswordReset" && k != "EmailBodyActivation")])
                 .Returns<string>(key => new LocalizedString(key, key));

        var service = new SmtpEmailService(config, NullLogger<SmtpEmailService>.Instance, localizer.Object, () => smtpClientMock.Object);
        return (smtpClientMock, service);
    }

    [Fact]
    public async Task SendEmail_CallsConnectWithConfiguredHost()
    {
        var (smtpMock, service) = CreateService();

        await service.SendPasswordResetEmailAsync("user@test.com", "https://example.com/reset", TestContext.Current.CancellationToken);

        smtpMock.Verify(x => x.ConnectAsync("mailpit.test", 1025, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmail_SendsToCorrectRecipient()
    {
        var (smtpMock, service) = CreateService();
        MimeMessage? captured = null;
        smtpMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await service.SendPasswordResetEmailAsync("recipient@test.com", "https://example.com/reset", TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Contains(captured.To.Mailboxes, m => m.Address == "recipient@test.com");
    }

    [Fact]
    public async Task SendEmail_BodyContainsResetLink()
    {
        var (smtpMock, service) = CreateService();
        MimeMessage? captured = null;
        smtpMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        const string resetLink = "https://example.com/reset-password?email=user%40test.com&token=abc123";
        await service.SendPasswordResetEmailAsync("user@test.com", resetLink, TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Contains(resetLink, captured.HtmlBody ?? captured.TextBody);
    }

    [Fact]
    public async Task SendEmail_FromAddressMatchesConfig()
    {
        var (smtpMock, service) = CreateService();
        MimeMessage? captured = null;
        smtpMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await service.SendPasswordResetEmailAsync("user@test.com", "https://example.com/reset", TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Contains(captured.From.Mailboxes, m => m.Address == "noreply@aspire.local");
    }

    [Fact]
    public async Task SendEmail_CallsDisconnectAfterSend()
    {
        var (smtpMock, service) = CreateService();

        await service.SendPasswordResetEmailAsync("user@test.com", "https://example.com/reset", TestContext.Current.CancellationToken);

        smtpMock.Verify(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendEmail_SmtpException_ThrowsInvalidOperationException()
    {
        var (smtpMock, service) = CreateService();
        smtpMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP connection refused"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendPasswordResetEmailAsync("user@test.com", "https://example.com/reset", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendEmail_SocketException_ThrowsInvalidOperationException()
    {
        var (smtpMock, service) = CreateService();
        smtpMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SocketException());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendPasswordResetEmailAsync("user@test.com", "https://example.com/reset", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task SendEmail_AspireMailpitConnectionString_ParsesHostAndPort()
    {
        var (smtpMock, service) = CreateService(new Dictionary<string, string?>
        {
            ["ConnectionStrings:mailpit"] = "Endpoint=smtp://localhost:58796"
        });

        await service.SendPasswordResetEmailAsync("user@test.com", "https://example.com/reset", TestContext.Current.CancellationToken);

        smtpMock.Verify(x => x.ConnectAsync("localhost", 58796, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Activation Email ---

    [Fact]
    public async Task SendActivationEmail_SendsToCorrectRecipient()
    {
        var (smtpMock, service) = CreateService();
        MimeMessage? captured = null;
        smtpMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await service.SendActivationEmailAsync("newuser@test.com", "https://example.com/confirm", TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Contains(captured.To.Mailboxes, m => m.Address == "newuser@test.com");
    }

    [Fact]
    public async Task SendActivationEmail_BodyContainsActivationLink()
    {
        var (smtpMock, service) = CreateService();
        MimeMessage? captured = null;
        smtpMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        const string activationLink = "https://example.com/confirm-email?email=user%40test.com&token=abc123";
        await service.SendActivationEmailAsync("user@test.com", activationLink, TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Contains(activationLink, captured.HtmlBody ?? captured.TextBody);
    }

    [Fact]
    public async Task SendActivationEmail_SubjectIsActivation()
    {
        var (smtpMock, service) = CreateService();
        MimeMessage? captured = null;
        smtpMock.Setup(x => x.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>()))
            .Callback<MimeMessage, CancellationToken>((msg, _) => captured = msg)
            .Returns(Task.CompletedTask);

        await service.SendActivationEmailAsync("user@test.com", "https://example.com/confirm", TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal("EmailSubjectActivation", captured.Subject);
    }

    [Fact]
    public async Task SendActivationEmail_SmtpException_ThrowsInvalidOperationException()
    {
        var (smtpMock, service) = CreateService();
        smtpMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP connection refused"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SendActivationEmailAsync("user@test.com", "https://example.com/confirm", TestContext.Current.CancellationToken));
    }
}
