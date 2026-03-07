namespace InitialAspireProject.ApiIdentity.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken ct = default);
    }
}
