namespace InitialAspireProject.Web
{
    public class SessionTokenStore
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string TokenKey = "AuthToken";

        public SessionTokenStore(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Save(string token)
        {
            _httpContextAccessor.HttpContext?.Session.SetString(TokenKey, token);
        }

        public string? Get()
        {
            return _httpContextAccessor.HttpContext?.Session.GetString(TokenKey);
        }

        public void Remove()
        {
            _httpContextAccessor.HttpContext?.Session.Remove(TokenKey);
        }
    }
}
