using InitialAspireProject.ApiIdentity.Resources;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace InitialAspireProject.ApiIdentity.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly Func<ISmtpClient> _clientFactory;
        private readonly IStringLocalizer<AuthMessages> _localizer;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger, IStringLocalizer<AuthMessages> localizer)
            : this(configuration, logger, localizer, () => new MailKitSmtpClientWrapper()) { }

        internal SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger, IStringLocalizer<AuthMessages> localizer, Func<ISmtpClient> clientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _localizer = localizer;
            _clientFactory = clientFactory;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default)
        {
            var (host, port, useSsl) = ResolveSmtpSettings();
            var fromAddress = _configuration["Smtp:FromAddress"] ?? "noreply@aspire.local";
            var fromName = _configuration["Smtp:FromName"] ?? "Initial Aspire Project";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = _localizer["EmailSubjectPasswordReset"].Value;
            message.Body = new TextPart("html")
            {
                Text = string.Format(_localizer["EmailBodyPasswordReset"].Value, resetLink)
            };

            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];

            try
            {
                using var client = _clientFactory();
                await client.ConnectAsync(host, port, useSsl, ct);
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    await client.AuthenticateAsync(username, password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
                throw new InvalidOperationException("Failed to send email.", ex);
            }
        }

        public async Task SendActivationEmailAsync(string toEmail, string activationLink, CancellationToken ct = default)
        {
            var (host, port, useSsl) = ResolveSmtpSettings();
            var fromAddress = _configuration["Smtp:FromAddress"] ?? "noreply@aspire.local";
            var fromName = _configuration["Smtp:FromName"] ?? "Initial Aspire Project";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddress));
            message.To.Add(new MailboxAddress(string.Empty, toEmail));
            message.Subject = _localizer["EmailSubjectActivation"].Value;
            message.Body = new TextPart("html")
            {
                Text = string.Format(_localizer["EmailBodyActivation"].Value, activationLink)
            };

            var username = _configuration["Smtp:Username"];
            var password = _configuration["Smtp:Password"];

            try
            {
                using var client = _clientFactory();
                await client.ConnectAsync(host, port, useSsl, ct);
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    await client.AuthenticateAsync(username, password, ct);
                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException and not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to send activation email to {Email}", toEmail);
                throw new InvalidOperationException("Failed to send email.", ex);
            }
        }

        private (string host, int port, bool useSsl) ResolveSmtpSettings()
        {
            var mailpitCs = _configuration.GetConnectionString("mailpit");
            if (!string.IsNullOrEmpty(mailpitCs))
            {
                var uriString = mailpitCs.Contains("Endpoint=", StringComparison.OrdinalIgnoreCase)
                    ? mailpitCs.Split("Endpoint=", StringSplitOptions.None)[1].Split(';')[0].Trim()
                    : mailpitCs;
                var uri = new Uri(uriString);
                return (uri.Host, uri.Port, uri.Scheme.Equals("smtps", StringComparison.OrdinalIgnoreCase));
            }

            var host = _configuration["Smtp:Host"] is { Length: > 0 } h ? h : "localhost";
            var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 1025;
            var useSsl = bool.TryParse(_configuration["Smtp:UseSsl"], out var s) && s;
            return (host, port, useSsl);
        }
    }

    internal sealed class MailKitSmtpClientWrapper : ISmtpClient
    {
        private readonly SmtpClient _client = new();

        public Task ConnectAsync(string host, int port, bool useSsl, CancellationToken ct)
            => _client.ConnectAsync(host, port, useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.Auto, ct);

        public Task AuthenticateAsync(string username, string password, CancellationToken ct)
            => _client.AuthenticateAsync(username, password, ct);

        public Task SendAsync(MimeMessage message, CancellationToken ct)
            => _client.SendAsync(message, ct);

        public Task DisconnectAsync(bool quit, CancellationToken ct)
            => _client.DisconnectAsync(quit, ct);

        public void Dispose() => _client.Dispose();
    }
}
