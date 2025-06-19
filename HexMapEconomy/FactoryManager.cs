using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Models;

namespace HexMapEconomy;

/// <summary>
/// The FactoryManager handles factories on game map and their production processes.
/// </summary>
public class FactoryManager
{
    private Dictionary<Guid, Factory> _factoryStore = new();
    private Dictionary<int, Recipe> _recipeStore = new();

    public FactoryManager(Dictionary<int, Recipe> definition)
    {
        _recipeStore = definition;
    }

    /// <summary>
    /// Creates a new <see cref="Factory"/> at the given position with the specified type and owner.
    /// </summary>
    /// <param name="position">Coordinates where Factory is created.</param>
    /// <param name="type">Type of created Factory.</param>
    /// <param name="ownerId">The owner of this Factory.</param>
    /// <returns>true if Factory was created successfully, otherwise false.</returns>
    public bool CreateFactory(CubeCoordinates position, int type, int ownerId, int stockLimit = 0)
    {
        // early exit
        if(!_recipeStore.ContainsKey(type))
        {
            return false;   // this factory type is unknown
        }
        var factory = new Factory(_recipeStore[type], position, type, ownerId, stockLimit);
        _factoryStore[factory.Id] = factory;
        return true;
    }

    /// <summary>
    /// Retrieves a list of factories located at the specified position.
    /// </summary>
    /// <param name="position">The coordinates of the position to search for factories.</param>
    /// <returns>A list of <see cref="Factory"/> objects located at the specified position. 
    /// Returns an empty list if no factories are found at the given position.</returns>
    public List<Factory> GetFactoriesByPosition(CubeCoordinates position)
    {
        return _factoryStore.Values
            .Where(factory => factory.Position.Equals(position))
            .ToList();
    }

    /// <summary>
    /// Removes the factory with the specified identifier from the store.
    /// </summary>
    /// <param name="factoryId">The unique identifier of the factory to remove.</param>
    /// <returns>true if the factory was successfully removed, otherwise false.</returns>
    public bool RemoveFactory(Guid factoryId)
    {
        return _factoryStore.Remove(factoryId);
    }

    /// <summary>
    /// Gets the total number of factories currently stored.
    /// </summary>
    /// <returns>The total count of factories.</returns>
    public int CountFactories()
    {
        return _factoryStore.Count;
    }

    /// <summary>
    /// Changes the owner of the specified factory to a new owner.
    /// </summary>
    /// <remarks>This method attempts to locate the factory by its <paramref name="factoryId"/>. If the
    /// factory is found, its ownership is updated to the specified <paramref name="newOwnerId"/>.
    /// If the factory is not found, the method returns false without making any changes.</remarks>
    /// <param name="factoryId">The unique identifier of the factory whose ownership is to be changed.</param>
    /// <param name="newOwnerId">The identifier of the new owner to assign to the factory.</param>
    /// <returns>true if the factory was found and its ownership was successfully changed, otherwise false.</returns>
    public bool ChangeFactoryOwner(Guid factoryId, int newOwnerId)
    {
        if (_factoryStore.TryGetValue(factoryId, out var factory))
        {
            factory.ChangeOwner(newOwnerId);
            return true;
        }
        return false;   // factory not found
    }

    /// <summary>
    /// Processes all factories stored in the factory collection.
    /// </summary>
    /// <remarks>This method iterates through all factories in the internal collection and invokes their  <see
    /// cref="IFactory.Process"/> method.</remarks>
    public void ProcessFactories()
    {
        foreach (var factory in _factoryStore.Values)
        {
            factory.Process();
        }
    }
}
