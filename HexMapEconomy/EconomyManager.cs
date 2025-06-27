using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Models;
using System.Security.AccessControl;

namespace HexMapEconomy;

/// <summary>
/// The FactoryManager handles factories on game map and their production processes.
/// </summary>
public class EconomyManager
{
    private Dictionary<Guid, Factory> _factoryStore = new();
    private Dictionary<Guid, Warehouse> _warehouseStore = new();
    private Dictionary<int, Recipe> _recipeStore = new();

    public EconomyManager(Dictionary<int, Recipe> definition)
    {
        _recipeStore = definition;
    }

    /// <summary>
    /// Creates a new Warehouse at a given position.
    /// </summary>
    /// <param name="position">Position in CubeCoordinates.</param>
    /// <returns>true if the warehouse was created, false if there is already a warehouse on this position.</returns>
    public bool CreateWarehouse(CubeCoordinates position, int ownerId, int stockLimit)
    {   
        // early exit
        if (_warehouseStore.Values.Any(w => w.Position.Equals(position)))
        {
            return false;   // this warehouse already exists
        }
        
        var warehouse = new Warehouse(position, ownerId, stockLimit);
        _warehouseStore[warehouse.Id] = warehouse;
        return true;
    }

    /// <summary>
    /// Retrieves a warehouse that is located at the specified position or null.
    /// </summary>
    /// <param name="position">The coordinates of the position to search for warehouses.</param>
    /// <returns>A warehouse object or null if there is none at this position.</returns>
    public Warehouse? GetWarehouseByPosition(CubeCoordinates position)
        => _warehouseStore.Values.FirstOrDefault(warehouse => warehouse.Position.Equals(position));

    /// <summary>
    /// Creates a new <see cref="Factory"/> at the given position with the specified type and owner.
    /// </summary>
    /// <param name="position">Coordinates where Factory is created.</param>
    /// <param name="type">Type of created Factory.</param>
    /// <param name="ownerId">The owner of this Factory.</param>
    /// <returns>true if Factory was created successfully, otherwise false.</returns>
    public bool CreateFactory(CubeCoordinates position, int type, int ownerId, Warehouse warehouse)
    {
        // early exit
        if (!_recipeStore.ContainsKey(type))
        {
            return false;   // this factory type is unknown
        }
        if (ownerId != warehouse.OwnerId)
        {
            return false;   // owner of factory must be the same as owner of warehouse
        }
        var factory = new Factory(_recipeStore[type], position, type, ownerId, warehouse);
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
        => _factoryStore.Values
            .Where(factory => factory.Position.Equals(position))
            .ToList();

    /// <summary>
    /// Removes the factory with the specified identifier from the store.
    /// </summary>
    /// <param name="factoryId">The unique identifier of the factory to remove.</param>
    /// <returns>true if the factory was successfully removed, otherwise false.</returns>
    public bool RemoveFactory(Guid factoryId)
        => _factoryStore.Remove(factoryId);

    /// <summary>
    /// Gets the total number of factories currently stored.
    /// </summary>
    /// <returns>The total count of factories.</returns>
    public int CountFactories()
        => _factoryStore.Count;

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
    /// Processes all factories by handling their input assets, executing their production processes,
    /// and managing the outputs for further transportation.
    /// </summary>
    public void ProcessFactories()
    {
        // cache factories to avoid multiple enumerations
        var factories = _factoryStore.Values.ToList();

        // group factories by owner once
        var factoriesByOwner = factories
            .GroupBy(factory => factory.OwnerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // producers (factory with inputs like a sawmill)
        var producers = factories
                .Where(f => f.Recipe.Inputs != null &&
                            f.Recipe.Inputs.Count > 0 &&
                            f.Warehouse.Stock.StockLimit > 0)
                .ToList();

        // generators (factory with no inputs, like a lumberjack or mine)
        var generators = factories
                .Where(f => f.Recipe.Inputs == null ||
                       f.Recipe.Inputs.Count == 0)
                .ToList();

        // group warehouses to avoid multiple enumerations
        var warehouses = _warehouseStore.Values.ToList();

        // group warehouses by owner
        var warehousesByOwner = warehouses
            .GroupBy(warehouse => warehouse.OwnerId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Process each asset that is currently in transportation
        foreach (var warehouse in warehouses)
        {
            foreach (var asset in warehouse.Stock.Assets)
            {
                asset.Process();
            }
        }

        // Process the production of each factory
        // 1. producers
        foreach (var factory in producers)
        {
            factory.Process();
        }
        // 2. generators
        // so that Assets lasts at least one turn in stock
        foreach (var factory in generators)
        {
            factory.Process();
        }

        foreach (var ownerFactories in factoriesByOwner.Values)
        {
            // generate demands for all factories and add it to the warehouse
            CreateFactoryDemands(ownerFactories);

            // Process the outputs to be transported further, once per owner
            //ProcessFactoryOutputs(ownerFactories);
        }
    }

    private void CreateFactoryDemands(List<Factory> factories)
    {
        // producers ( factory with inputs like a sawmill)
        var producers = factories
                .Where(f => f.Recipe.Inputs != null &&
                            f.Recipe.Inputs.Count > 0 &&
                            f.Warehouse.Stock.StockLimit > 0)
                .ToList();

        // generate a list of needed ingredients
        foreach (var producer in producers)
        {
            Dictionary<int, int> calculatedMaxAmount = new();

            // storage needed to produce one output
            int totalInputPerCycle = producer.Recipe.Inputs.Sum(x => x.Amount);
            // max possible cycles that fit in stock
            int maxCycles = producer.Warehouse.Stock.StockLimit / totalInputPerCycle;
            // remainder space for this ingredient
            int remainder = producer.Warehouse.Stock.StockLimit % totalInputPerCycle;
            foreach (var input in producer.Recipe.Inputs)
            {
                // max amount for this ingredient: cycles * amount per cycle + min(remainder, amount per cycle)
                calculatedMaxAmount[input.Type] = (maxCycles * input.Amount) + Math.Min(remainder, input.Amount);
            }

            // create maximum demand to fill the stock
            foreach (var input in producer.Recipe.Inputs)
            {
                int currentAmount = producer.Warehouse.Stock.GetCount(input.Type);
                int missingAmount = calculatedMaxAmount[input.Type] - currentAmount;
                if (missingAmount > 0)
                {
                    // Add a demand for the missing amount of this ingredient
                    producer.Warehouse.AddDemand(new Demand(
                        producer,
                        new RecipeIngredient()
                        {
                            Type = input.Type,
                            Amount = missingAmount
                        }
                    ));
                }
            }
        }
    }

    // all outputstock assets should be transported to next factory inputstock
    /*private void ProcessFactoryOutputs(List<Factory> factories)
    {
        // try to fulfill demands
        foreach (var demand in demands)
        {
            // get a list of all factories that are not the one of demand and sort them by distance from demand.Factory to the actual factory from small to large
            var sortedFactories = factories
                .Where(f => f != demand.Factory)
                .OrderBy(f => demand.Factory.Position.DistanceTo(f.Position))
                .ToList();

            foreach(var factory in sortedFactories)
            {
                // get the maximum amount of demand that this factory can handle
                int possibleAmount = Math.Min(
                    factory.Warehouse.Stock.GetCount(demand.Ingredient.Type),
                    demand.Ingredient.Amount);

                if (possibleAmount == 0)
                {
                    continue;   // this factory has no assets of the required type
                }

                // get assets from factory
                var assets = factory.Warehouse.Stock.Take(demand.Ingredient.Type, possibleAmount);

                // check if demand factory can handle this input
                if(demand.Factory.Warehouse.Stock.GetCount(demand.Ingredient.Type) + assets.Count > demand.Factory.Warehouse.Stock.StockLimit)
                {
                    throw new Exception($"Factory {demand.Factory.Id} can not handle demand.");
                }

                // adjust assets
                foreach(var asset in assets)
                {
                    int distance = factory.Position.DistanceTo(demand.Factory.Position);
                    int area = Math.Max(1, demand.Factory.AreaOfInfluence);
                    float turns = (float)distance / area;
                    int distanceInTurns = (int)Math.Ceiling(turns);

                    asset.InitializeTransport(
                        demand.Factory.Position,
                        distanceInTurns);
                }

                // add assets to factory with demand
                var addedAssets = demand.Factory.Warehouse.Stock.AddRange(assets);

                if(addedAssets != assets.Count)
                {
                    throw new Exception($"Factory {demand.Factory.Id} could not add all assets to input stock.");
                }

                // adjust this demand and check if it is fulfilled
                demand.Ingredient.Amount -= possibleAmount;
                if (demand.Ingredient.Amount == 0)
                {
                    break;
                }
            }
        }
    }*/
}
