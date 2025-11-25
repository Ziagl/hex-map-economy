# AGENTS.md - HexMapEconomy Library Reference

## Project Overview

**HexMapEconomy** is a .NET library (C#) for managing economy systems (resources, goods, and production) in tile-based games using hexagonal maps. It provides an abstract economy system with resource production, transformation, and distribution.

- **Repository**: https://github.com/Ziagl/hex-map-economy
- **Owner**: Werner Ziegelwanger (Ziagl)
- **Company**: Hexagon Simulations
- **License**: GNU Lesser General Public License v3
- **Current Version**: 0.5.0
- **Target Frameworks**: .NET 10.0
- **Dependencies**: HexMapBase v0.5.0 (for CubeCoordinates)
- **NuGet Package**: Available with tags: hexagon, geometry, map, economy, resources, library

## Architecture Overview

### Core Component
- **EconomyManager**: The main entry point that orchestrates the entire economy system. It manages factories, warehouses, recipes, and processes production/transportation cycles.

### Key Economic Entities

1. **Asset** (`Models/Asset.cs`)
   - Represents any economic good with type, position, owner
   - Tracks availability and transportation state
   - Has `TurnsUntilAvailable` property for transportation simulation

2. **Factory** (`Models/Factory.cs`)
   - Buildings that produce or transform assets using recipes
   - Two types:
     - **Generators**: Produce raw materials (no inputs) - e.g., lumberjack, mine
     - **Producers**: Transform inputs to outputs - e.g., sawmill
   - Tracks productivity statistics over last 10 turns
   - Must be owned by same owner as connected warehouse

3. **Warehouse** (`Models/Warehouse.cs`)
   - Stores assets with a stock limit
   - Manages demands from connected factories
   - Has a position and serves adjacent factories
   - Handles asset distribution between warehouses

4. **Recipe** (`Models/Recipe.cs`)
   - Defines production process for factories
   - Contains input ingredients, output ingredients, and duration
   - Used as factory behavior template

5. **Stock** (`Models/Stock.cs`)
   - Storage container with capacity limits
   - Manages asset collections with Add/Take operations

6. **Demand** (`Models/Demand.cs`)
   - Represents a factory's need for specific ingredients
   - Generated automatically during production cycles

7. **RecipeIngredient** (`Models/RecipeIngredient.cs`)
   - Type and amount specification for inputs/outputs

### Economic Flow

```
Turn Processing (ProcessFactories):
1. Assets in transport → Reduce TurnsUntilAvailable
2. Producers → Try to produce (if inputs available)
3. Generators → Produce raw materials
4. Create demands → Calculate missing ingredients
5. Fulfill demands → Transfer assets between warehouses
```

### Transportation System
- Assets have distance-based delivery time
- `TransportationPerTurn` property controls speed (default: 5 tiles/turn)
- `EstimateDeliveryTime()` calculates delivery estimates for recipe ingredients
- Assets become available after transportation completes

## Technical Details

### Namespace Structure
- Root: `com.hexagonsimulations.HexMapEconomy`
- Models: `com.hexagonsimulations.HexMapEconomy.Models`

### Serialization Support
- **JSON**: Full support via System.Text.Json with custom converters
- **Binary**: Efficient binary serialization for game saves
- Both formats preserve complete state including:
  - All entities (factories, warehouses, assets)
  - Transportation states
  - Demands
  - Productivity statistics

### Key Design Patterns
- Internal constructors for controlled instantiation
- Factory pattern for entity creation through EconomyManager
- Repository pattern (EconomyManager stores entities by Guid)
- Immutable properties with init-only setters where appropriate

## Testing

### Test Structure
- Location: `HexMapEconomy.Tests/`
- Framework: MSTest
- Key test files:
  - `EconomyManagerTests.cs`: Core functionality tests
  - `EconomyManagerSerializationTests.cs`: Serialization/deserialization tests
  - `TestUtils.cs`: Helper methods for test data generation

### Test Patterns
- TestUtils provides factory type definitions:
  - `LUMBERJACK` (type 1): Generator producing wood
  - `SAWMILL` (type 2): Producer transforming wood → planks
- Common test scenarios include multi-warehouse transportation

## Common Operations

### Creating Economy Components
```csharp
// 1. Initialize with recipe definitions
var manager = new EconomyManager(recipeStore);

// 2. Create warehouse
manager.CreateWarehouse(position, ownerId, stockLimit);

// 3. Create factory
var warehouse = manager.GetWarehouseByPosition(position);
manager.CreateFactory(position, recipeType, ownerId, warehouse);

// 4. Process economy turn
manager.ProcessFactories();
```

### Querying System
- `GetWarehouseByPosition()`, `GetWarehousesByOwner()`, `GetWarehouseById()`
- `GetFactoriesByPosition()`
- `EstimateDeliveryTime()` for ingredient availability
- Warehouse.Stock methods: `GetCount()`, `Has()`, `Take()`, `Add()`

### Owner Management
- All entities have `OwnerId` (int)
- Factories must share owner with their warehouse
- Ownership can be changed via `ChangeFactoryOwner()`

## Code Style & Conventions

### Naming
- PascalCase for public members
- _camelCase with underscore prefix for private fields
- Descriptive names (e.g., `_lastTenTurnsOutput`, `_turnsUntilAvailable`)

### Access Modifiers
- `internal` for test-visible members (with `InternalsVisibleTo` attribute)
- Public API minimal and well-defined
- Properties use init-only setters where immutability desired

### Documentation
- XML documentation comments on public methods
- Clear parameter descriptions and return value docs
- Exception documentation where applicable

## Important Constraints & Rules

1. **Warehouse Ownership**: Factory owner must match warehouse owner
2. **Stock Limits**: Warehouses enforce stock capacity limits
3. **Recipe Validation**: Factory types must exist in recipe store
4. **Unique Positions**: Only one warehouse per position
5. **Generator Logic**: Factories with empty Recipe.Inputs are generators
6. **Asset Availability**: Assets must be available (not in transport) for use
7. **Demand Clearing**: Demands are cleared each turn after processing

## Future Work Considerations

The codebase has several TODOs and areas marked for enhancement:
- Transportation speed is currently hardcoded (marked as TODO)
- Demand persistence across turns (currently cleared each turn)
- Recipe duration is defined but not actively used in processing

## Project Files

### Main Project
- `HexMapEconomy.csproj`: Target .NET 10.0
- All model classes in `Models/` subfolder
- `EconomyManager.cs` at project root

### Test Project
- `HexMapEconomy.Tests.csproj`
- References main project
- Uses MSTest and Microsoft.NET.Test.Sdk

### Documentation
- `README.md`: User-facing overview
- `LICENSE`: LGPL v3
- `icon.png`: Package icon

## When Working on This Project

### For Bug Fixes:
1. Check relevant tests in `EconomyManagerTests.cs`
2. Understand the turn-based processing flow
3. Consider serialization impact (both JSON and binary)
4. Verify owner/warehouse relationships

### For New Features:
1. Add model classes to `Models/` folder
2. Extend `EconomyManager` with management methods
3. Implement both JSON and binary serialization
4. Add comprehensive tests following existing patterns
5. Update this AGENTS.md if adding significant concepts

### For Refactoring:
1. Maintain backward compatibility for serialization
2. Keep test coverage high
3. Preserve internal visibility for test access
4. Consider impact on dependent HexMapBase types

### For Testing:
1. Use `TestUtils.GenerateFactoryTypes()` for consistent test data
2. Test multi-warehouse scenarios
3. Verify transportation mechanics
4. Check serialization round-trips
5. Test edge cases (empty stock, missing ingredients, etc.)

---

*This document should be updated when significant architectural changes are made to the project.*
