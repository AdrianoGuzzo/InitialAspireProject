using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Bff.Services;

public interface IIdentityProxyService
{
    Task<HttpResponseMessage> LoginAsync(LoginModel model, string? acceptLanguage = null);
    Task<HttpResponseMessage> RegisterAsync(RegisterModel model, string? acceptLanguage = null);
    Task<HttpResponseMessage> RefreshAsync(RefreshTokenRequest model, string? acceptLanguage = null);
    Task<HttpResponseMessage> RevokeAsync(RevokeTokenRequest model, string bearerToken, string? acceptLanguage = null);
    Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordModel model, string? acceptLanguage = null);
    Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordModel model, string? acceptLanguage = null);
    Task<HttpResponseMessage> ConfirmEmailAsync(ConfirmEmailModel model, string? acceptLanguage = null);
    Task<HttpResponseMessage> ResendActivationAsync(ForgotPasswordModel model, string? acceptLanguage = null);
    Task<HttpResponseMessage> GetProfileAsync(string bearerToken, string? acceptLanguage = null);
    Task<HttpResponseMessage> UpdateProfileAsync(UpdateProfileModel model, string bearerToken, string? acceptLanguage = null);
    Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordModel model, string bearerToken, string? acceptLanguage = null);
}

public class IdentityProxyService(HttpClient httpClient, ILogger<IdentityProxyService> logger)
    : BackendProxyService(httpClient, logger), IIdentityProxyService
{
    public Task<HttpResponseMessage> LoginAsync(LoginModel model, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/login", model, acceptLanguage: acceptLanguage));

    public Task<HttpResponseMessage> RegisterAsync(RegisterModel model, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/register", model, acceptLanguage: acceptLanguage));

    public Task<HttpResponseMessage> RefreshAsync(RefreshTokenRequest model, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/refresh", model, acceptLanguage: acceptLanguage));

    public Task<HttpResponseMessage> RevokeAsync(RevokeTokenRequest model, string bearerToken, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/revoke", model, bearerToken, acceptLanguage));

    public Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordModel model, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/forgot-password", model, acceptLanguage: acceptLanguage));

    public Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordModel model, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/reset-password", model, acceptLanguage: acceptLanguage));

    public Task<HttpResponseMessage> ConfirmEmailAsync(ConfirmEmailModel model, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/confirm-email", model, acceptLanguage: acceptLanguage));

    public Task<HttpResponseMessage> ResendActivationAsync(ForgotPasswordModel model, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/resend-activation", model, acceptLanguage: acceptLanguage));

    public Task<HttpResponseMessage> GetProfileAsync(string bearerToken, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Get, "/auth/profile", bearerToken, acceptLanguage));

    public Task<HttpResponseMessage> UpdateProfileAsync(UpdateProfileModel model, string bearerToken, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Put, "/auth/profile", model, bearerToken, acceptLanguage));

    public Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordModel model, string bearerToken, string? acceptLanguage = null)
        => SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/change-password", model, bearerToken, acceptLanguage));
}
