using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface IRegisterService
    {
        Task<ServiceResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default);
    }

    public class RegisterService(HttpClient httpClient, ILogger<RegisterService> logger)
        : BaseHttpService(httpClient, logger), IRegisterService
    {
        public Task<ServiceResult> RegisterAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            return PostWithValidationAsync("/auth/Register", new RegisterModel { Email = email, Password = password }, cancellationToken);
        }
    }
}
