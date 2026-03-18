using System.Net.Http.Json;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services;

public interface IProfileService
{
    Task<ProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateProfileAsync(string fullName, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

public class ProfileService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<ProfileService> logger)
    : AuthenticatedHttpService(httpClient, httpContextAccessor, logger), IProfileService
{
    public async Task<ProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/auth/profile");
            var response = await HttpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProfileResponse>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching profile");
            return null;
        }
    }

    public Task<ServiceResult> UpdateProfileAsync(string fullName, CancellationToken cancellationToken = default)
    {
        return SendWithValidationAsync(HttpMethod.Put, "/auth/profile", new UpdateProfileModel { FullName = fullName }, cancellationToken, token: GetToken());
    }

    public Task<ServiceResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        return SendWithValidationAsync(HttpMethod.Post, "/auth/change-password", new ChangePasswordModel
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        }, cancellationToken, token: GetToken());
    }
}
