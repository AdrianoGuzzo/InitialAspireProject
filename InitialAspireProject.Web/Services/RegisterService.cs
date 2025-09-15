using Microsoft.AspNetCore.Identity.Data;
using System.Net.Http;

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

                var response = await httpClient.PostAsJsonAsync("/auth/Register", new
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
                    Message = "Conta criada com sucesso!"
                };
            }
            catch (Exception ex)
            {

                if (errorValidations is not null)
                {
                    string validationError = string.Join("/n", errorValidations.Select(x => x.Description).ToArray());
                    logger.LogError(ex, $"{ex.Message}, {validationError}");
                    return new RegisterResult
                    {
                        Success = false,
                        Message = validationError
                    };
                }
                logger.LogError(ex, $"{ex.Message}");
                return new RegisterResult
                {
                    Success = false,
                    Message = "Erro interno do servidor. Tente novamente mais tarde."
                };
            }
        }
    }

    public class RegisterResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
    public class ErrorValidation
    {
        public string Code { get; set; }
        public string? Description { get; set; }
    }
}