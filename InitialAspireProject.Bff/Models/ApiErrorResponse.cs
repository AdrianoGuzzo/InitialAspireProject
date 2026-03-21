namespace InitialAspireProject.Bff.Models;

public record ApiErrorResponse
{
    public string? Code { get; init; }
    public string? Message { get; init; }
    public object? Details { get; init; }
}
