using System.Text.Json;

namespace HexMapEconomy.Models;

public class Stock
{
    public List<Asset> Assets { get; } = new();
    public int StockLimit { get; init; }

    public Stock(int stockLimit = 0)
    {
        StockLimit = stockLimit;
    }

    /// <summary>
    /// Removes all assets from the stock.
    /// </summary>
    public void Clear()
    {
        Assets.Clear();
    }

    /// <summary>
    /// Gets the number of assets of a certain type.
    /// </summary>
    public int GetCount(int type)
    {
        return Assets.Count(a => a.Type == type);
    }

    /// <summary>
    /// Adds an asset to the stock.
    /// </summary>
    /// <param name="asset">The asset to add.</param>
    /// <returns>true if added, false if stock is full.</returns>
    public bool Add(Asset asset)
    {
        if (Assets.Count >= StockLimit && StockLimit > 0)
        {
            return false;
        }

        Assets.Add(asset);
        return true;
    }

    /// <summary>
    /// Adds a list of assets to the stock, only if all can be added.
    /// </summary>
    /// <param name="assets">The assets to add.</param>
    /// <returns>Number of assets actually added (all or none).</returns>
    public int AddRange(IEnumerable<Asset> assets)
    {
        var assetList = assets.ToList();
        if (assetList.Count > StockLimit - Assets.Count)
        {
            return 0;
        }

        Assets.AddRange(assetList);
        return assetList.Count;
    }

    /// <summary>
    /// Takes (removes and returns) a specified number of assets of a given type.
    /// </summary>
    /// <param name="type">The type of asset to take.</param>
    /// <param name="amount">The number of assets to take.</param>
    /// <returns>List of taken assets, or empty list if not enough assets available.</returns>
    public List<Asset> Take(int type, int amount)
    {
        var assetsOfType = Assets.Where(a => a.Type == type && a.IsAvailable)
                                 .Take(amount).ToList();
        if (assetsOfType.Count < amount)
        {
            return new List<Asset>();
        }

        foreach (var asset in assetsOfType)
        {
            Assets.Remove(asset);
        }

        return assetsOfType;
    }

    /// <summary>
    /// Checks if given dictionary of assets is available in the stock.
    /// </summary>
    /// <param name="assets">A dictionary of assets types and amounts needed.</param>
    /// <returns>true if all assets are available in given amount, otherwise false.</returns>
    public bool Has(Dictionary<int, int> assets)
    {
        foreach (var asset in assets)
        {
            if (GetCount(asset.Key) < asset.Value)
                return false;
        }
        return true;
    }

    // ------------------ Serialization ------------------

    private sealed class StockState
    {
        public int StockLimit { get; set; }
        public List<Asset> Assets { get; set; } = new();
    }

    /// <summary>
    /// Serialize this stock (including contained assets) to JSON.
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = new StockState
        {
            StockLimit = StockLimit,
            Assets = Assets.Select(a => new Asset(
                a.Id,
                a.Position,
                a.Type,
                a.OwnerId,
                a.TurnsUntilAvailable,
                a.IsAvailable)).ToList()
        };
        return JsonSerializer.Serialize(state, options);
    }

    /// <summary>
    /// Deserialize JSON into a new Stock instance (recreates asset objects).
    /// </summary>
    public static Stock FromJson(string json, JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = JsonSerializer.Deserialize<StockState>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize Stock.");

        var stock = new Stock(state.StockLimit);

        foreach (var a in state.Assets)
        {
            // Uses the internal Asset(Guid, ...) constructor (present per provided signatures)
            var asset = new Asset(a.Id, a.Position, a.Type, a.OwnerId, a.TurnsUntilAvailable, a.IsAvailable);
            stock.Assets.Add(asset);
        }

        return stock;
    }
}
