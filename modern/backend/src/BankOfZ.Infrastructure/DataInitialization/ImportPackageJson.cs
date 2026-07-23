using System.Text.Json;
using System.Text.Json.Serialization;

namespace BankOfZ.Infrastructure.DataInitialization;

public static class ImportPackageJson
{
    public const string CurrentSchemaVersion = "bank-of-z-import/v1";
    public const int MaximumPackageBytes = 10 * 1024 * 1024;

    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = false,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
