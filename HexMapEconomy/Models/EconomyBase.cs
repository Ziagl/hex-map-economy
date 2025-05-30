namespace HexMapEconomy.Models;

public abstract class EconomyBase
{
    public Guid Id { get; init; }
    public int Type { get; init; } // type of the asset, e.g. resource type, product type, etc.
    public int OwnerId { get => _ownerId; } // ID of the owner, e.g. player ID

    private int _ownerId { get; set; } 

    public EconomyBase(int type, int ownerId)
    {
        Id = Guid.NewGuid();
        Type = type;
        _ownerId = ownerId;
    }

    public void ChangeOwner(int newOwnerId)
    {
        _ownerId = newOwnerId;
    }
}
