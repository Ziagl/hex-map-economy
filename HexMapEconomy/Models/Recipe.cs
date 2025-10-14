using System.Text.Json;

namespace com.hexagonsimulations.HexMapEconomy.Models;

// a description for a factory how to behave in the simulation
public class Recipe
{
    public List<RecipeIngredient> Inputs { get; init; } // list of input assets as asset type and amount
    public List<RecipeIngredient> Outputs { get; init; } // list of output assets, each as asset type and amount
    public int Duration { get; init; } // duration of the recipe in time units (e.g. turns, seconds)

    public Recipe(List<RecipeIngredient>? inputs, List<RecipeIngredient>? outputs, int duration = 1)
    {
        Inputs = inputs ?? new List<RecipeIngredient>();
        Outputs = outputs ?? new List<RecipeIngredient>();
        Duration = duration;
    }

    // --------------- Serialization ----------------

    private sealed class RecipeState
    {
        public List<RecipeIngredient>? Inputs { get; set; }
        public List<RecipeIngredient>? Outputs { get; set; }
        public int Duration { get; set; }
    }

    /// <summary>
    /// Serialize this Recipe to JSON.
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = new RecipeState
        {
            Inputs = Inputs,
            Outputs = Outputs,
            Duration = Duration
        };
        return JsonSerializer.Serialize(state, options);
    }

    /// <summary>
    /// Deserialize JSON into a new Recipe instance.
    /// </summary>
    public static Recipe FromJson(string json, JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = JsonSerializer.Deserialize<RecipeState>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize Recipe.");

        return new Recipe(
            state.Inputs ?? new List<RecipeIngredient>(),
            state.Outputs ?? new List<RecipeIngredient>(),
            state.Duration);
    }
}
