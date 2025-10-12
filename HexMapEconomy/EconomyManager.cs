using com.hexagonsimulations.HexMapBase.Models;
using HexMapEconomy.Models;

namespace HexMapEconomy;

/// <summary>
/// The FactoryManager handles factories on game map and their production processes.
/// </summary>
public class EconomyManager
{
    private Dictionary<Guid, Factory> _factoryStore = new();
    private Dictionary<Guid, Warehouse> _warehouseStore = new();
    private Dictionary<int, Recipe> _recipeStore = new();
    // how many tiles an Asset can be transported per turn
    private readonly int _transportationPerTurn = 5;   // TODO: make this configurable 

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
    /// Retrieves a list of warehouses owned by the specified owner id.
    /// </summary>
    /// <param name="ownerId">The unique identifier of the owner whose warehouses are to be retrieved.</param>
    /// <returns>A list of <see cref="Warehouse"/> objects owned by the specified owner. Returns an empty list if the owner
    /// owns no warehouses.</returns>
    public List<Warehouse> GetWarehousesByOwner(int ownerId)
        => _warehouseStore.Values.Where(warehouse => warehouse.OwnerId == ownerId).ToList();

    /// <summary>
    /// Retrieves a warehouse by its unique identifier or null if there is none.
    /// </summary>
    /// <param name="warehouseId">Guid of searched warehouse.</param>
    /// <exception cref="Exception">Thrown when no warehouse was found with the given GUID.</exception>
    /// <returns>Warehouse object or null if no warehouse exists with given GUID.</returns>
    public Warehouse GetWarehouseById(Guid warehouseId)
        => _warehouseStore.TryGetValue(warehouseId, out var warehouse) ? warehouse : throw new Exception("No Warehouse with this guid found.");

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
                            _warehouseStore[f.WarehouseId].Stock.StockLimit > 0)
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
            factory.Process(_warehouseStore[factory.WarehouseId]);
        }
        // 2. generators
        // so that Assets lasts at least one turn in stock
        foreach (var factory in generators)
        {
            factory.Process(_warehouseStore[factory.WarehouseId]);
        }

        foreach (var ownerFactories in factoriesByOwner.Values)
        {
            // generate demands for all factories and add it to the warehouse
            CreateFactoryDemands(ownerFactories);

            // process the outputs to be transported further, once per owner
            FulfillFactoryDemands(warehousesByOwner[ownerFactories.First().OwnerId]);
        }
    }

    /// <summary>
    /// Estimates the estimated delivery time for a set of required ingredients to a specified position.
    /// </summary>
    /// <param name="requredIngredients">A list of ingredients required for the recipe. Cannot be null or empty.</param>
    /// <param name="position">The target position represented by cube coordinates where the ingredients need to be delivered.</param>
    /// <returns>The estimated delivery time in turns for every type of ingredient and a max turn. -1 if it is not possible to deliver.</returns>
    public RecipeIngredientsAvailability EstimateDeliveryTime(List<RecipeIngredient> requredIngredients, int ownerId, CubeCoordinates position)
    {
        int maxTurns = 0;
        var warehouses = GetWarehousesByOwner(ownerId);
        RecipeIngredientsAvailability availability = new();

        // sort warehouses by distance to the target position
        var sortedWarehouses = warehouses
            .OrderBy(w => w.Position.DistanceTo(position))
            .ToList();

        foreach (var ingredient in requredIngredients)
        {
            int needed = ingredient.Amount;
            int farthestDistance = 0;
            foreach (var warehouse in sortedWarehouses)
            {
                int available = warehouse.Stock.GetCount(ingredient.Type);
                if (available > 0)
                {
                    int take = Math.Min(needed, available);
                    int distance = warehouse.Position.DistanceTo(position);
                    if (distance > farthestDistance)
                    {
                        farthestDistance = distance;
                    }  
                    needed -= take;
                    if (needed == 0)
                    {
                        break;
                    }
                }
            }
            if (needed == 0)
            {
                int turns = CalculateTurnDistance(farthestDistance);
                if (turns > maxTurns && maxTurns != -1)
                {
                    maxTurns = turns;
                }
                availability.AvailabilityDetails.Add(new IngredientAvailability() { Type = ingredient.Type, Turns = turns });
            }
            else
            {
                maxTurns = -1;
                availability.AvailabilityDetails.Add(new IngredientAvailability() { Type = ingredient.Type, Turns = maxTurns });
            }
        }

        availability.Turns = maxTurns;

        return availability;
    }

    private void CreateFactoryDemands(List<Factory> factories)
    {
        // producers ( factory with inputs like a sawmill)
        var producers = factories
                .Where(f => f.Recipe.Inputs != null &&
                            f.Recipe.Inputs.Count > 0 &&
                            _warehouseStore[f.WarehouseId].Stock.StockLimit > 0)
                .ToList();

        // generate a list of needed ingredients
        foreach (var producer in producers)
        {
            Dictionary<int, int> calculatedMaxAmount = new();

            // storage needed to produce one output
            int totalInputPerCycle = producer.Recipe.Inputs.Sum(x => x.Amount);
            // max possible cycles that fit in stock
            int maxCycles = _warehouseStore[producer.WarehouseId].Stock.StockLimit / totalInputPerCycle;
            // remainder space for this ingredient
            int remainder = _warehouseStore[producer.WarehouseId].Stock.StockLimit % totalInputPerCycle;
            foreach (var input in producer.Recipe.Inputs)
            {
                // max amount for this ingredient: cycles * amount per cycle + min(remainder, amount per cycle)
                calculatedMaxAmount[input.Type] = (maxCycles * input.Amount) + Math.Min(remainder, input.Amount);
            }

            // create maximum demand to fill the stock
            foreach (var input in producer.Recipe.Inputs)
            {
                int currentAmount = _warehouseStore[producer.WarehouseId].Stock.GetCount(input.Type);
                int missingAmount = calculatedMaxAmount[input.Type] - currentAmount;
                if (missingAmount > 0)
                {
                    // Add a demand for the missing amount of this ingredient
                    _warehouseStore[producer.WarehouseId].AddDemand(new Demand(
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

    private void FulfillFactoryDemands(List<Warehouse> warehouses)
    {
        foreach(var warehouse in warehouses)
        {
            foreach(var demand in warehouse.Demands)
            {
                // check demand against all other warehouses
                var otherWarehouses = warehouses
                    .Where(w => w.Id != warehouse.Id)
                    .OrderBy(w => w.Position.DistanceTo(warehouse.Position))
                    .ToList();
                foreach (var otherWarehouse in otherWarehouses)
                {
                    // get the maximum amount of this ingredient that can be taken from this warehouse
                    int possibleAmount = Math.Min(
                        otherWarehouse.Stock.GetCount(demand.Ingredient.Type),
                        demand.Ingredient.Amount);
                    if (possibleAmount == 0)
                    {
                        continue;   // this warehouse has no assets of the required type
                    }
                    
                    // check if demand factory can handle this input
                    if (warehouse.Stock.GetCount(demand.Ingredient.Type) + possibleAmount <= warehouse.Stock.StockLimit)
                    {
                        int distance = otherWarehouse.Position.DistanceTo(warehouse.Position);
                        // get assets from warehouse
                        var assets = otherWarehouse.Stock.Take(demand.Ingredient.Type, possibleAmount);

                        // adjust assets
                        foreach (var asset in assets)
                        {
                            asset.InitializeTransport(
                                warehouse.Position,
                               CalculateTurnDistance(distance));
                        }
                        // add assets to warehouse with demand
                        var addedAssets = warehouse.Stock.AddRange(assets);
                        if (addedAssets != assets.Count)
                        {
                            throw new Exception($"Warehouse {warehouse.Id} could not add all assets to input stock.");
                        }
                        // adjust this demand and check if it is fulfilled
                        demand.Ingredient.Amount -= possibleAmount;
                        if (demand.Ingredient.Amount == 0)
                        {
                            break;  // demand is fulfilled, no need to check further warehouses
                        }
                    }
                    else
                    {
                        throw new Exception($"Warehouse {warehouse.Id} can not handle demand.");
                    }
                }
            }
            // clear demands after processing
            // TODO: check if it is a good idea to keep demands for next turn
            warehouse.Demands.Clear();
        }
    }

    private int CalculateTurnDistance(int distance)
    {
        return (int)Math.Ceiling((float)distance / _transportationPerTurn);
    }
}
