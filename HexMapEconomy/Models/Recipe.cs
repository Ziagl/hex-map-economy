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
            ?? throw new InvalidOperationException("Invalid Recipe JSON.");
        return new Recipe(state.Inputs, state.Outputs, state.Duration);
    }

    // -------- Binary --------
    internal void Write(BinaryWriter writer)
    {
        writer.Write(Duration);
        writer.Write(Inputs.Count);
        foreach (var i in Inputs)
        {
            writer.Write(i.Type);
            writer.Write(i.Amount);
        }
        writer.Write(Outputs.Count);
        foreach (var o in Outputs)
        {
            writer.Write(o.Type);
            writer.Write(o.Amount);
        }
    }

    internal static Recipe Read(BinaryReader reader)
    {
        int duration = reader.ReadInt32();
        int inCount = reader.ReadInt32();
        var inputs = new List<RecipeIngredient>(inCount);
        for (int i = 0; i < inCount; i++)
        {
            int t = reader.ReadInt32();
            int a = reader.ReadInt32();
            inputs.Add(new RecipeIngredient { Type = t, Amount = a });
        }
        int outCount = reader.ReadInt32();
        var outputs = new List<RecipeIngredient>(outCount);
        for (int i = 0; i < outCount; i++)
        {
            int t = reader.ReadInt32();
            int a = reader.ReadInt32();
            outputs.Add(new RecipeIngredient { Type = t, Amount = a });
        }
        return new Recipe(inputs, outputs, duration);
    }
}
