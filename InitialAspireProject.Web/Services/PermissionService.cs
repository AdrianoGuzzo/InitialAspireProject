using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface IPermissionService
    {
        Task<string[]> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
        Task<List<RolePermissionsDto>> GetAllRolePermissionsAsync(CancellationToken cancellationToken = default);
        Task<bool> AssignPermissionAsync(string roleName, string permission, CancellationToken cancellationToken = default);
        Task<bool> RemovePermissionAsync(string roleName, string permission, CancellationToken cancellationToken = default);
    }

    public class PermissionService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<PermissionService> logger) : IPermissionService
    {
        private void AttachToken()
        {
            var token = httpContextAccessor.HttpContext?.Session.GetString(SessionConstants.TokenKey);
            if (!string.IsNullOrEmpty(token))
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<string[]> GetAllPermissionsAsync(CancellationToken cancellationToken)
        {
            AttachToken();
            try
            {
                return await httpClient.GetFromJsonAsync<string[]>("/permissions", cancellationToken) ?? [];
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error fetching permissions");
                return [];
            }
        }

        public async Task<List<RolePermissionsDto>> GetAllRolePermissionsAsync(CancellationToken cancellationToken)
        {
            AttachToken();
            try
            {
                return await httpClient.GetFromJsonAsync<List<RolePermissionsDto>>("/permissions/roles", cancellationToken) ?? [];
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error fetching role permissions");
                return [];
            }
        }

        public async Task<bool> AssignPermissionAsync(string roleName, string permission, CancellationToken cancellationToken)
        {
            AttachToken();
            try
            {
                var response = await httpClient.PostAsJsonAsync($"/permissions/roles/{Uri.EscapeDataString(roleName)}", new AssignPermissionModel { Permission = permission }, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error assigning permission {Permission} to role {Role}", permission, roleName);
                return false;
            }
        }

        public async Task<bool> RemovePermissionAsync(string roleName, string permission, CancellationToken cancellationToken)
        {
            AttachToken();
            try
            {
                var response = await httpClient.DeleteAsync($"/permissions/roles/{Uri.EscapeDataString(roleName)}/{Uri.EscapeDataString(permission)}", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error removing permission {Permission} from role {Role}", permission, roleName);
                return false;
            }
        }
    }
}
