using System.Net.Http.Json;
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

    public class PermissionService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, ILogger<PermissionService> logger)
        : AuthenticatedHttpService(httpClient, httpContextAccessor, logger), IPermissionService
    {
        public async Task<string[]> GetAllPermissionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/permissions");
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<string[]>(cancellationToken: cancellationToken) ?? [];
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error fetching permissions");
                return [];
            }
        }

        public async Task<List<RolePermissionsDto>> GetAllRolePermissionsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Get, "/permissions/roles");
                using var response = await HttpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<RolePermissionsDto>>(cancellationToken: cancellationToken) ?? [];
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Error fetching role permissions");
                return [];
            }
        }

        public async Task<bool> AssignPermissionAsync(string roleName, string permission, CancellationToken cancellationToken)
        {
            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/permissions/roles/{Uri.EscapeDataString(roleName)}");
                request.Content = JsonContent.Create(new AssignPermissionModel { Permission = permission });
                using var response = await HttpClient.SendAsync(request, cancellationToken);
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
            try
            {
                using var request = CreateAuthenticatedRequest(HttpMethod.Delete, $"/permissions/roles/{Uri.EscapeDataString(roleName)}/{Uri.EscapeDataString(permission)}");
                using var response = await HttpClient.SendAsync(request, cancellationToken);
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
