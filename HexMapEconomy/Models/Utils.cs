using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.hexagonsimulations.HexMapEconomy.Models;

internal class Utils
{
    internal static JsonSerializerOptions CreateDefaultJsonOptions() =>
        new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
}
