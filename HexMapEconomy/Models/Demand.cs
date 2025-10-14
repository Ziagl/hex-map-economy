using System.Text.Json;

namespace com.hexagonsimulations.HexMapEconomy.Models;

internal class Demand
{
    internal Factory Factory { get; init; }
    internal RecipeIngredient Ingredient { get; init; }

    internal Demand(Factory factory, RecipeIngredient value)
    {
        Factory = factory;
        Ingredient = value;
    }

    // -------- Serialization --------
    private sealed class DemandState
    {
        public Guid FactoryId { get; set; }
        public int IngredientType { get; set; }
        public int IngredientAmount { get; set; }
    }

    /// <summary>
    /// Serialize this Demand to JSON (stores only FactoryId and ingredient data).
    /// </summary>
    internal string ToJson(JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = new DemandState
        {
            FactoryId = Factory.Id,
            IngredientType = Ingredient.Type,
            IngredientAmount = Ingredient.Amount
        };
        return JsonSerializer.Serialize(state, options);
    }

    /// <summary>
    /// Deserialize a Demand from JSON.
    /// </summary>
    /// <param name="json">JSON created by ToJson.</param>
    /// <param name="factoryResolver">Resolver to obtain Factory by its Id.</param>
    internal static Demand FromJson(string json, Func<Guid, Factory?> factoryResolver, JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = JsonSerializer.Deserialize<DemandState>(json, options)
                    ?? throw new InvalidOperationException("Invalid Demand JSON.");
        var factory = factoryResolver(state.FactoryId)
                      ?? throw new InvalidOperationException($"Factory {state.FactoryId} not found for Demand.");
        var ingredient = new RecipeIngredient { Type = state.IngredientType, Amount = state.IngredientAmount };
        return new Demand(factory, ingredient);
    }

    // -------- Binary --------
    internal void Write(BinaryWriter writer)
    {
        writer.Write(Factory.Id.ToByteArray());
        writer.Write(Ingredient.Type);
        writer.Write(Ingredient.Amount);
    }

    internal static Demand Read(BinaryReader reader, Func<Guid, Factory?> factoryResolver)
    {
        var factoryId = new Guid(reader.ReadBytes(16));
        int type = reader.ReadInt32();
        int amount = reader.ReadInt32();
        var factory = factoryResolver(factoryId)
            ?? throw new InvalidOperationException($"Factory {factoryId} missing while reading Demand.");
        return new Demand(factory, new RecipeIngredient { Type = type, Amount = amount });
    }
}
