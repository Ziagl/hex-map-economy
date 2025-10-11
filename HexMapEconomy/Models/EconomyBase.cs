namespace HexMapEconomy.Models;

public abstract class EconomyBase
{
    public Guid Id { get; init; }
    public int Type { get; }
    public int OwnerId { get; private set; }

    protected EconomyBase(int type, int ownerId) : this(Guid.NewGuid(), type, ownerId) { }

    protected EconomyBase(Guid id, int type, int ownerId)
    {
        Id = id;
        Type = type;
        OwnerId = ownerId;
    }

    public void ChangeOwner(int newOwnerId) => OwnerId = newOwnerId;
}
