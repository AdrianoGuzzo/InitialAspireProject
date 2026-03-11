using InitialAspireProject.Shared.Models;

namespace InitialAspireProject.Web.Services
{
    public interface IRegisterService
    {
        Task<RegisterResult> RegisterAsync(string fullName, string email, string password, CancellationToken cancellationToken = default);
    }

    public class RegisterService(HttpClient httpClient, ILogger<RegisterService> logger) : IRegisterService
    {

        public async Task<RegisterResult> RegisterAsync(string fullName, string email, string password, CancellationToken cancellationToken = default)
        {
            ErrorValidation[]? errorValidations = null;
            try
            {

                var response = await httpClient.PostAsJsonAsync("/auth/Register", new RegisterModel
                {
                    FullName = fullName,
                    Email = email,
                    Password = password
                }, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    errorValidations = await response.Content.ReadFromJsonAsync<ErrorValidation[]>(cancellationToken: cancellationToken);
                    throw new Exception("Validation Error");
                }

                return new RegisterResult
                {
                    Success = true,
                    Message = null
                };
            }
            catch (Exception ex)
            {

                if (errorValidations is not null)
                {
                    string validationError = string.Join("\n", errorValidations.Select(x => x.Description).ToArray());
                    logger.LogError(ex, "Registration validation failed: {Errors}", validationError);
                    return new RegisterResult
                    {
                        Success = false,
                        Message = validationError
                    };
                }
                logger.LogError(ex, "Unexpected registration error: {Message}", ex.Message);
                return new RegisterResult
                {
                    Success = false,
                    Message = "Erro interno do servidor. Tente novamente mais tarde."
                };
            }
        }
    }

}