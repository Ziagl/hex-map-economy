using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Models;

namespace HexMapEconomy;

public class FactoryManager
{
    private Dictionary<Guid, Factory> _factoryStore = new();
    private Dictionary<int, Recipe> _recipeStore = new();

    public FactoryManager(Dictionary<int, Recipe> definition)
    {
        _recipeStore = definition;
    }

    public bool CreateFactory(CubeCoordinates position, int type, int ownerId)
    {
        // early exit
        if(!_recipeStore.ContainsKey(type))
        {
            return false;   // this factory type is unknown
        }
        var factory = new Factory(_recipeStore[type], position, type, ownerId);
        _factoryStore[factory.Id] = factory;
        return true;
    }

    public bool RemoveFactory(Guid factoryId)
    {
        return _factoryStore.Remove(factoryId);
    }

    public bool ChangeFactoryOwner(Guid factoryId, int newOwnerId)
    {
        if (_factoryStore.TryGetValue(factoryId, out var factory))
        {
            factory.ChangeOwner(newOwnerId);
            return true;
        }
        return false;   // factory not found
    }

    public void ProcessFactories()
    {
        foreach (var factory in _factoryStore.Values)
        {
            factory.Process();
        }
    }
}
