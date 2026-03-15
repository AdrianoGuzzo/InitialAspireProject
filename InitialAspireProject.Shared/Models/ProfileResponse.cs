namespace InitialAspireProject.Shared.Models;

public record ProfileResponse
{
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = [];
}
