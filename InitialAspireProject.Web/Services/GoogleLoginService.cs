namespace InitialAspireProject.Web.Services
{
    public interface IGoogleLoginService
    {
        Task<ResponseToken?> GetJwtAsync(string email, string name);
    }

    public class GoogleLoginService(HttpClient httpClient, IConfiguration configuration) : IGoogleLoginService
    {
        public async Task<ResponseToken?> GetJwtAsync(string email, string name)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/auth/google-login");
            request.Headers.Add("X-Internal-Key", configuration["InternalApiKey"]);
            request.Content = JsonContent.Create(new { email, name });

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<ResponseToken>();
        }
    }
}
