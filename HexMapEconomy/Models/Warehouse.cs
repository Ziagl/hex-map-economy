using com.hexagonsimulations.HexMapBase.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace com.hexagonsimulations.HexMapEconomy.Models;

// a warehouse that stores Assets of attached factories
public class Warehouse : EconomyBase
{
    public CubeCoordinates Position { get; init; }          // map position of this warehouse
    public Stock Stock { get; } = new();                    // stock for this warehouse

    // Expose a StockLimit property so JSON ctor parameter 'stockLimit' can bind.
    // (Parameter only needs to match an existing property name; it may be get-only.)
    public int StockLimit => Stock.StockLimit;

    internal List<Demand> Demands { get; } = new();         // list of demands for this warehouse

    /// <summary>
    /// Creates a new Warehouse. A Warehouse has always type 0, because there are not types yet.
    /// </summary>
    public Warehouse(CubeCoordinates position, int ownerId, int stockLimit) : base(0, ownerId)
    {
        Position = position;
        Stock = new Stock(stockLimit);
    }

    /// <summary>
    /// Deserialization constructor for System.Text.Json.
    /// All parameter names must match properties: id, position, ownerId, stockLimit, type.
    /// 'stockLimit' is used only to recreate the Stock instance if no stock object was provided.
    /// </summary>
    [JsonConstructor]
    internal Warehouse(Guid id,
                       CubeCoordinates position,
                       int ownerId,
                       int stockLimit,
                       int type = 0,
                       Stock? stock = null) : base(id, type, ownerId)
    {
        Position = position;
        Stock = stock ?? new Stock(stockLimit);
    }

    internal Warehouse(Guid id,
                       CubeCoordinates position,
                       int ownerId,
                       int stockLimit,
                       Stock stock)
        : this(id, position, ownerId, stockLimit, 0, stock) { }

    internal bool AddDemand(Demand demand)
    {
        if (demand.Factory.WarehouseId != Id) return false;
        Demands.Add(demand);
        return true;
    }

    // ---------------- Serialization ----------------

    private sealed class WarehouseState
    {
        public Guid Id { get; set; }
        public CubeCoordinates Position { get; set; }
        public int OwnerId { get; set; }
        public int StockLimit { get; set; }
        public string StockJson { get; set; } = string.Empty;
        public List<string> Demands { get; set; } = new(); // each entry is Demand.ToJson()
    }

    /// <summary>
    /// Serialize this warehouse (including stock and current demands) to JSON.
    /// Demands are stored by FactoryId and ingredient data only.
    /// </summary>
    public string ToJson(JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = new WarehouseState
        {
            Id = Id,
            Position = Position,
            OwnerId = OwnerId,
            StockLimit = Stock.StockLimit,
            StockJson = Stock.ToJson(options),
            Demands = Demands.Select(d => d.ToJson(options)).ToList()
        };
        return JsonSerializer.Serialize(state, options);
    }

    /// <summary>
    /// Deserialize a warehouse from JSON.
    /// </summary>
    /// <param name="json">JSON produced by ToJson.</param>
    /// <param name="factoryResolver">Resolver used to obtain Factory instances for demand re-linking.</param>
    /// <param name="options">Optional serializer options.</param>
    public static Warehouse FromJson(string json, Func<Guid, Factory?> factoryResolver, JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = JsonSerializer.Deserialize<WarehouseState>(json, options)
                    ?? throw new InvalidOperationException("Invalid Warehouse JSON.");

        var stock = Stock.FromJson(state.StockJson, options);
        var warehouse = new Warehouse(state.Id, state.Position, state.OwnerId, state.StockLimit, stock);

        foreach (var demandJson in state.Demands)
        {
            var demand = Demand.FromJson(demandJson, factoryResolver, options);
            warehouse.Demands.Add(demand);
        }
        return warehouse;
    }

    // -------- Binary --------
    internal void Write(BinaryWriter writer)
    {
        writer.Write(Id.ToByteArray());
        Position.Write(writer);
        writer.Write(OwnerId);
        writer.Write(Stock.StockLimit);
        Stock.Write(writer);
        writer.Write(Demands.Count);
        foreach (var d in Demands)
            d.Write(writer);
    }

    internal static Warehouse Read(BinaryReader reader, Func<Guid, Factory?> factoryResolver)
    {
        var id = new Guid(reader.ReadBytes(16));
        var pos = CubeCoordinates.Read(reader);
        int owner = reader.ReadInt32();
        int stockLimit = reader.ReadInt32();
        var stock = Stock.Read(reader);
        var wh = new Warehouse(id, pos, owner, stockLimit, stock);
        int demandCount = reader.ReadInt32();
        for (int i = 0; i < demandCount; i++)
        {
            wh.Demands.Add(Demand.Read(reader, factoryResolver));
        }
        return wh;
    }
}
