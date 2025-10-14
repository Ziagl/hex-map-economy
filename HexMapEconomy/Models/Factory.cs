using System.Text.Json.Serialization;
using com.hexagonsimulations.HexMapBase.Models;

namespace com.hexagonsimulations.HexMapEconomy.Models;

// a building that generates Assets or combines them to other Assets
public class Factory : EconomyBase
{
    public CubeCoordinates Position { get; init; }          // map position of this factory
    public Recipe Recipe { get; init; }                     // recipe defines how this factory works
    public Guid WarehouseId { get; init; }

    // statistic information about the factory
    public float Productivity => _lastTenTurnsOutput.Count == 0
        ? 0f
        : (float)_lastTenTurnsOutput.Sum() / _lastTenTurnsOutput.Count;

    private readonly Queue<int> _lastTenTurnsOutput = new(10);

    // Runtime ctor (contains Warehouse – not usable for JSON deserialization)
    public Factory(Recipe recipe, CubeCoordinates position, int type, int ownerId, Warehouse warehouse)
        : base(type, ownerId)
    {
        Position = position;
        Recipe = recipe;
        WarehouseId = warehouse.Id;
    }

    // Deserialization ctor: every parameter must map to a property (case-insensitive).
    // This avoids the serializer picking the public ctor with the unmatched 'warehouse' parameter.
    [JsonConstructor]
    internal Factory(Guid id, CubeCoordinates position, Recipe recipe, int type, int ownerId, Guid warehouseId, List<int>? lastTenOutputs = null)
        : base(id, type, ownerId)
    {
        Position = position;
        Recipe = recipe;
        WarehouseId = warehouseId;

        if (lastTenOutputs != null)
        {
            foreach (var v in lastTenOutputs.Take(10))
                _lastTenTurnsOutput.Enqueue(v);
        }
    }

    // Exposed for serialization & test equality (test reflects "LastTenOutputs")
    [JsonInclude]
    public List<int> LastTenOutputs
    {
        get => _lastTenTurnsOutput.ToList();
        private set
        {
            _lastTenTurnsOutput.Clear();
            if (value == null) return;
            foreach (var v in value.Take(10))
                _lastTenTurnsOutput.Enqueue(v);
        }
    }

    internal void Process(Warehouse warehouse)
    {
        bool success = false;
        bool generator = Recipe.Inputs.Count == 0;

        // if a receipe has no inputs, the factury is a generator, like a mine or a lumberjack
        if (generator)
        {
            // generator logic, e.g. mine or lumberjack
            success = true;
        }
        // otherwise this factory does manufacturing and needs inputs to generate outputs
        else
        {
            // only if all inputs are available
            success = Recipe.Inputs.All(input =>
            {
                var assets = warehouse.Stock.Assets.Where(s => s.Type == input.Type && s.IsAvailable).ToList();
                return assets.Count >= input.Amount;
            });

            if (success)
            {
                List<Asset> takenAssets = new();
                foreach (var input in Recipe.Inputs)
                {
                    var takenEntries = warehouse.Stock.Take(input.Type, input.Amount);
                    if (takenEntries.Count == 0)
                    {
                        success = false;
                        warehouse.Demands.Add(new Demand(this, input));
                    }
                    takenAssets.AddRange(takenEntries);
                }
                if (!success)
                {
                    warehouse.Stock.AddRange(takenAssets);
                }
            }
        }

        if (success)
        {
            foreach (var output in Recipe.Outputs)
            {
                for (int i = 0; i < output.Amount; i++)
                {
                    var asset = new Asset(Position, output.Type, OwnerId, generator);
                    if (!warehouse.Stock.Add(asset))
                    {
                        success = false;
                        break;
                    }
                }
                if (!success) break;
            }
        }

        AddOutput(success ? 1 : 0);
    }

    private void AddOutput(int value)
    {
        if (_lastTenTurnsOutput.Count == 10)
            _lastTenTurnsOutput.Dequeue();
        _lastTenTurnsOutput.Enqueue(value);
    }

    // -------- Binary --------
    internal void Write(BinaryWriter writer)
    {
        writer.Write(Id.ToByteArray());
        Position.Write(writer);
        writer.Write(Type);
        writer.Write(OwnerId);
        writer.Write(WarehouseId.ToByteArray());
        // last 10 outputs
        var list = _lastTenTurnsOutput.ToList();
        writer.Write(list.Count);
        foreach (var v in list) writer.Write(v);
    }

    internal static Factory Read(BinaryReader reader, Dictionary<int, Recipe> recipeStore)
    {
        var id = new Guid(reader.ReadBytes(16));
        var position = CubeCoordinates.Read(reader);
        int type = reader.ReadInt32();
        int owner = reader.ReadInt32();
        var warehouseId = new Guid(reader.ReadBytes(16));
        int outputCount = reader.ReadInt32();
        var outputs = new List<int>(outputCount);
        for (int i = 0; i < outputCount; i++) outputs.Add(reader.ReadInt32());
        var factory = new Factory(id, position, recipe: recipeStore[type], type, owner, warehouseId, outputs);
        return factory;
    }
}
