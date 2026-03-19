namespace InitialAspireProject.Shared.Models
{
    public record LoginResponse
    {
        public required string Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}
