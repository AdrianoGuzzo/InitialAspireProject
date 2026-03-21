using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Bff.Services;

public interface IIdentityProxyService
{
    Task<HttpResponseMessage> LoginAsync(LoginModel model);
    Task<HttpResponseMessage> RegisterAsync(RegisterModel model);
    Task<HttpResponseMessage> RefreshAsync(RefreshTokenRequest model);
    Task<HttpResponseMessage> RevokeAsync(RevokeTokenRequest model, string bearerToken);
    Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordModel model);
    Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordModel model);
    Task<HttpResponseMessage> ConfirmEmailAsync(ConfirmEmailModel model);
    Task<HttpResponseMessage> ResendActivationAsync(ForgotPasswordModel model);
    Task<HttpResponseMessage> GetProfileAsync(string bearerToken);
    Task<HttpResponseMessage> UpdateProfileAsync(UpdateProfileModel model, string bearerToken);
    Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordModel model, string bearerToken);
}

public class IdentityProxyService(HttpClient httpClient, ILogger<IdentityProxyService> logger)
    : BackendProxyService(httpClient, logger), IIdentityProxyService
{
    public Task<HttpResponseMessage> LoginAsync(LoginModel model)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/login", model));

    public Task<HttpResponseMessage> RegisterAsync(RegisterModel model)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/register", model));

    public Task<HttpResponseMessage> RefreshAsync(RefreshTokenRequest model)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/refresh", model));

    public Task<HttpResponseMessage> RevokeAsync(RevokeTokenRequest model, string bearerToken)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/revoke", model, bearerToken));

    public Task<HttpResponseMessage> ForgotPasswordAsync(ForgotPasswordModel model)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/forgot-password", model));

    public Task<HttpResponseMessage> ResetPasswordAsync(ResetPasswordModel model)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/reset-password", model));

    public Task<HttpResponseMessage> ConfirmEmailAsync(ConfirmEmailModel model)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/confirm-email", model));

    public Task<HttpResponseMessage> ResendActivationAsync(ForgotPasswordModel model)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/resend-activation", model));

    public Task<HttpResponseMessage> GetProfileAsync(string bearerToken)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Get, "/auth/profile", bearerToken));

    public Task<HttpResponseMessage> UpdateProfileAsync(UpdateProfileModel model, string bearerToken)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Put, "/auth/profile", model, bearerToken));

    public Task<HttpResponseMessage> ChangePasswordAsync(ChangePasswordModel model, string bearerToken)
        => HttpClient.SendAsync(CreateForwardRequest(HttpMethod.Post, "/auth/change-password", model, bearerToken));
}
