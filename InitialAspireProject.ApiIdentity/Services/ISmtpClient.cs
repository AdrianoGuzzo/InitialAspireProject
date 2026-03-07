using MimeKit;

namespace InitialAspireProject.ApiIdentity.Services
{
    internal interface ISmtpClient : IDisposable
    {
        Task ConnectAsync(string host, int port, bool useSsl, CancellationToken ct);
        Task AuthenticateAsync(string username, string password, CancellationToken ct);
        Task SendAsync(MimeMessage message, CancellationToken ct);
        Task DisconnectAsync(bool quit, CancellationToken ct);
    }
}
