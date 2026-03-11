namespace InitialAspireProject.ApiIdentity.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default);
        Task SendActivationEmailAsync(string toEmail, string activationLink, CancellationToken ct = default);
    }
}
