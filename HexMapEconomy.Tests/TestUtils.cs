using com.hexagonsimulations.HexMapBase.Models;
using com.hexagonsimulations.HexMapEconomy.Models;

namespace com.hexagonsimulations.HexMapEconomy.Tests;

internal class TestUtils
{
    internal static readonly int LUMBERJACK = 1;
    internal static readonly int SAWMILL = 2;

    internal static List<Asset> CreateAssets(int type, int amount, CubeCoordinates position, int ownerId, int distance = 1)
    {
        var assets = new List<Asset>();
        for (int i = 0; i < amount; i++)
        {
            var asset = new Asset(position, type, ownerId);
            asset.InitializeTransport(position, distance);
            assets.Add(asset);
        }

        return assets;
    }

    internal static Dictionary<int, Recipe> GenerateFactoryTypes()
    {
        return new Dictionary<int, Recipe>
        {
            { LUMBERJACK, new Recipe(
                new List<RecipeIngredient>(),
                new List<RecipeIngredient>(){ new RecipeIngredient() { Type = 1, Amount = 1 } }
                ) },    // lumberjack creates wood (abstract)
            { SAWMILL, new Recipe(
                new List<RecipeIngredient>() { new RecipeIngredient() { Type = 1, Amount = 1 } },
                new List<RecipeIngredient>() { new RecipeIngredient() { Type = 2, Amount = 1 } }
                ) },    // sawmill creates a plank from a wood (abstract)
        };
    }
}

