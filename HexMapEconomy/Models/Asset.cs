using com.hexagonsimulations.HexMapBase.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleTo("HexMapEconomy.Tests")]

namespace com.hexagonsimulations.HexMapEconomy.Models;

// an object that takes part in the economy
public class Asset : EconomyBase
{
    public CubeCoordinates Position => _position;
    private CubeCoordinates _position;

    public int TurnsUntilAvailable => _turnsUntilAvailable;
    private int _turnsUntilAvailable;

    public bool IsAvailable => _isAvailable;
    private bool _isAvailable = false;

    internal Asset(CubeCoordinates position, int type, int ownerId, bool rawMaterial = false)
        : base(type, ownerId)
    {
        _position = position;
        _turnsUntilAvailable = int.MaxValue;
        _isAvailable = rawMaterial;
    }

    [JsonConstructor]
    internal Asset(Guid id,
                 CubeCoordinates position,
                 int type,
                 int ownerId,
                 int turnsUntilAvailable,
                 bool isAvailable)
        : base(id, type, ownerId)
    {
        _position = position;
        _turnsUntilAvailable = turnsUntilAvailable;
        _isAvailable = isAvailable;
    }

    public void InitializeTransport(CubeCoordinates newPosition, int distanceInTurns)
    {
        _position = newPosition;
        _turnsUntilAvailable = distanceInTurns;
        _isAvailable = false;
    }

    public void Process()
    {
        if (_turnsUntilAvailable > 0)
        {
            _turnsUntilAvailable--;
            if (_turnsUntilAvailable == 0)
            {
                _isAvailable = true;
            }
        }
    }

    // ---------------- Serialization ----------------
    private sealed class AssetState
    {
        public Guid Id { get; set; }
        public CubeCoordinates Position { get; set; }
        public int Type { get; set; }
        public int OwnerId { get; set; }
        public int TurnsUntilAvailable { get; set; }
        public bool IsAvailable { get; set; }
    }

    public string ToJson(JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = new AssetState
        {
            Id = Id,
            Position = _position,
            Type = Type,
            OwnerId = OwnerId,
            TurnsUntilAvailable = _turnsUntilAvailable,
            IsAvailable = _isAvailable
        };
        return JsonSerializer.Serialize(state, options);
    }

    public static Asset FromJson(string json, JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = JsonSerializer.Deserialize<AssetState>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize Asset.");

        return new Asset(
            state.Id,
            state.Position,
            state.Type,
            state.OwnerId,
            state.TurnsUntilAvailable,
            state.IsAvailable);
    }
}
