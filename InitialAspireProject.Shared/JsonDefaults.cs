using System.Text.Json;

namespace InitialAspireProject.Shared;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
