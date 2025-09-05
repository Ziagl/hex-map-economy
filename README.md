# hex-map-economy
A library that can handle economy (resources, goods and production) for tiled maps 

## Economic objects

This library includes following assets that maybe take part in an economical system:
* Asset
Any good of a type that take part in economic process. It has a location and can be available or not (is transported to this location)
* Demand
A demand is the lack of an RecipeIngredient for a specific Factory.
* Factory
An entity that is connected to a Warehouse, has its own location and has a Recipe that it tries to fulfill.
* Recipe
A plan a Factory entity should fulfill. It has input and output ingredients. A factory takes input ingredients from warehouse store and adds ouput ingredients in exchange to this store.
* RecipeIngredient
Any type of asset in given amount.
* Stock
A store of any maximal number of assets in given amount.
* Warehouse
A Stock with location and it takes care of demand for all adjacent Factories.

In theory this objects should represent an abstract economy system with never ending resources that are combined to higher products endlessly.
