using com.hexagonsimulations.HexMapBase.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;

[assembly: InternalsVisibleTo("HexMapEconomy.Tests")]

namespace HexMapEconomy.Models;

// an object that takes part in the economy
public class Asset : EconomyBase
{
    // to transport an Asset to another position
    public CubeCoordinates Position { get { return _position; } }
    private CubeCoordinates _position;
    public int TurnsUntilAvailable { get { return _turnsUntilAvailable; } }
    private int _turnsUntilAvailable;
    public bool IsAvailable { get { return _isAvailable; } }
    private bool _isAvailable = false;

    internal Asset(CubeCoordinates position, int type, int ownerId, bool rawMaterial = false)
        : base(type, ownerId)
    {
        _position = position;
        _turnsUntilAvailable = int.MaxValue;
        _isAvailable = rawMaterial;
    }

    internal Asset(Guid id, CubeCoordinates position, int type, int ownerId,
                   int turnsUntilAvailable, bool isAvailable)
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

    // DTO used for (de)serialization
    private sealed class AssetState
    {
        public Guid Id { get; set; }
        public CubeCoordinates Position { get; set; }
        public int Type { get; set; }
        public int OwnerId { get; set; }
        public int TurnsUntilAvailable { get; set; }
        public bool IsAvailable { get; set; }
    }

    /// <summary>
    /// Serialize this Asset to JSON.
    /// </summary>
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

    /// <summary>
    /// Deserialize JSON into a new Asset instance (preserves original Id).
    /// </summary>
    public static Asset FromJson(string json, JsonSerializerOptions? options = null)
    {
        options ??= Utils.CreateDefaultJsonOptions();
        var state = JsonSerializer.Deserialize<AssetState>(json, options)
            ?? throw new InvalidOperationException("Failed to deserialize Asset.");

        // rawMaterial logic no longer needed separately: pass state directly
        return new Asset(
            state.Id,
            state.Position,
            state.Type,
            state.OwnerId,
            state.TurnsUntilAvailable,
            state.IsAvailable);
    }
}
