using com.hexagonsimulations.HexMapBase.Models;
using System.Runtime.CompilerServices;

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

    internal Asset(CubeCoordinates position, int type, int ownerId) : base(type, ownerId)
    {
        _position = position;
        _turnsUntilAvailable = int.MaxValue;
        _isAvailable = false;
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
        }
        else
        {
            _isAvailable = true;
        }
    }
}
