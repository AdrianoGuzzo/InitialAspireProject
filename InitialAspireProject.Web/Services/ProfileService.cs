using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services;

public interface IProfileService
{
    Task<ProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default);
    Task<ProfileResult> UpdateProfileAsync(string fullName, CancellationToken cancellationToken = default);
    Task<ProfileResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

public class ProfileService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<ProfileService> logger) : IProfileService
{
    private const string TokenKey = "AuthToken";

    private void AttachToken()
    {
        var token = httpContextAccessor.HttpContext?.Session.GetString(TokenKey);
        if (!string.IsNullOrEmpty(token))
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ProfileResponse?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        AttachToken();
        try
        {
            return await httpClient.GetFromJsonAsync<ProfileResponse>("/auth/profile", cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching profile");
            return null;
        }
    }

    public async Task<ProfileResult> UpdateProfileAsync(string fullName, CancellationToken cancellationToken = default)
    {
        AttachToken();
        try
        {
            var response = await httpClient.PutAsJsonAsync("/auth/profile", new UpdateProfileModel { FullName = fullName }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errors = await response.Content.ReadFromJsonAsync<ErrorValidation[]>(cancellationToken: cancellationToken);
                var message = errors is not null
                    ? string.Join("\n", errors.Select(x => x.Description))
                    : await response.Content.ReadAsStringAsync(cancellationToken);
                return new ProfileResult { Success = false, Message = message };
            }

            return new ProfileResult { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating profile");
            return new ProfileResult { Success = false, Message = "Erro interno do servidor. Tente novamente mais tarde." };
        }
    }

    public async Task<ProfileResult> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        AttachToken();
        try
        {
            var response = await httpClient.PostAsJsonAsync("/auth/change-password", new ChangePasswordModel
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errors = await response.Content.ReadFromJsonAsync<ErrorValidation[]>(cancellationToken: cancellationToken);
                var message = errors is not null
                    ? string.Join("\n", errors.Select(x => x.Description))
                    : await response.Content.ReadAsStringAsync(cancellationToken);
                return new ProfileResult { Success = false, Message = message };
            }

            return new ProfileResult { Success = true };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing password");
            return new ProfileResult { Success = false, Message = "Erro interno do servidor. Tente novamente mais tarde." };
        }
    }
}
