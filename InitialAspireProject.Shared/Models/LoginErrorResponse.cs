namespace InitialAspireProject.Shared.Models
{
    public record LoginErrorResponse
    {
        public string? Code { get; init; }
        public string? Message { get; init; }
    }
}
