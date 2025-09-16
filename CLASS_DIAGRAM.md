# Hanok Construction System - Class Diagram

## System Architecture

```mermaid
classDiagram
    class HanokConstructionSystem {
        -PlotDefinitionController plotDefinitionController
        -HouseMaker houseMaker
        -StructureManager structureManager
        -HouseCatalog houseCatalog
        -bool isConstructionMode
        -House selectedHouseType
        +Update()
        +HandleInput()
        +SetConstructionMode(bool)
        +SelectHouseType(House)
    }

    class PlotDefinitionController {
        -Material lineMaterial
        -Material surfaceMaterial
        -LayerMask groundLayerMask
        -Camera playerCamera
        -List~Vector3~ currentVertices
        -LineRenderer lineRenderer
        -MeshRenderer meshRenderer
        +GetWorldPositionFromCursor(out Vector3)
        +AddVertex(Vector3)
        +RemoveLastVertex()
        +CanFinalizePlot()
        +GetCurrentPlotVertices()
    }

    class Building {
        <<enumeration>> BuildingMode
        <<enumeration>> BuildingType
        -string buildingName
        -BuildingType buildingType
        -Vector2 size2D
        -int priority
        -bool allowDuplicates
        -MaterialRequirement[] requiredMaterials
        -BuildingMode currentMode
        +SetMode(BuildingMode)
        +GetBounds()
        +OverlapsWith(Building)
    }

    class Catalog {
        <<abstract>>
        #string catalogName
        #string description
        #Sprite catalogIcon
        #bool isEnabled
        +GetInfo() string
        +ValidateCatalog() bool*
        +GetItem(int)* GameObject
        +FindItemByName(string)* GameObject
    }

    class BuildingCatalog {
        -GameObject[] buildingPrefabs
        +GetBuildingsByType(BuildingType)
        +GetFirstBuildingOfType(BuildingType)
        +GetBuildingsByPriority()
        +GetRandomBuildingOfType(BuildingType)
        +ValidateCatalog() bool
    }

    class House {
        <<enumeration>> HouseMode
        -string houseName
        -BuildingRequirement[] requiredBuildingTypes
        -Vector3[4] outlineVertices
        -float minLength, maxLength
        -float minWidth, maxWidth
        -HouseMode currentMode
        -List~Building~ placedBuildings
        +Initialize(Vector3[], BuildingCatalog)
        +VerifyRequiredBuildings()
        +AddBuilding(Building)
        +IsPointInside(Vector3)
        +GetBounds()
    }

    class HouseMaker {
        -BuildingCatalog buildingCatalog
        -PoolingComponent poolingComponent
        -float housePadding
        -float buildingSpacing
        +CreateHousesInPlot(Vector3[], House)
        +PopulateHouseWithBuildings(House)
        -CalculateHouseDivisions(Vector3[], House)
        -FindValidBuildingPosition(House, Building)
    }

    class HouseCatalog {
        -GameObject[] housePrefabs
        +GetHousesBySizeConstraints(float,float,float,float)
        +GetHousesWithBuildingRequirement(BuildingType)
        +FindBestFitHouse(float,float)
        +GetRandomHouse()
        +ValidateCatalog() bool
    }

    class PoolingComponent {
        <<abstract>>
        #PoolData[] pools
        #Dictionary~GameObject, PoolData~ poolLookup
        +GetPooledObject(GameObject)
        +ReturnToPool(GameObject)
        +ReturnAllToPool()
        +ClearPool(GameObject)
        #HandleUnregisteredPrefab(GameObject)*
    }

    class BuildingPoolingComponent {
        -BuildingCatalog buildingCatalog
        -TypePoolSetting[] typeSettings
        +GetRandomBuildingOfType(BuildingType)
        +GetBuildingWithinSize(Vector2)
        +ReturnAllBuildingsOfType(BuildingType)
        +SetBuildingCatalog(BuildingCatalog)
    }

    class HousePoolingComponent {
        -HouseCatalog houseCatalog
        -SizePoolSetting[] sizeSettings
        +GetBestFitHouse(float,float)
        +GetRandomHouseInSizeRange(float,float,float,float)
        +GetSimplestHouse()
        +SetHouseCatalog(HouseCatalog)
    }

    class StructureManager {
        -List~House~ managedHouses
        -Dictionary~House, List~Building~~ houseBuildings
        -float overlapTolerance
        +AddHouse(House)
        +RemoveHouse(House)
        +HasOverlap(House)
        +GetHousesInRadius(Vector3, float)
        +StartConstruction(House)
        +DemolishHouse(House)
    }

    class PoolData {
        +GameObject prefab
        +int initialSize
        +bool allowGrowth
        +Transform poolParent
        +Queue~GameObject~ pool
        +HashSet~GameObject~ activeObjects
    }

    class BuildingRequirement {
        +BuildingType buildingType
        +int minCount
        +int maxCount
        +bool isRequired
    }

    class MaterialRequirement {
        +string materialName
        +int amount
    }

    class StructureStats {
        +int totalHouses
        +int totalBuildings
        +Dictionary~HouseMode, int~ housesByMode
        +Dictionary~BuildingType, int~ buildingsByType
    }

    interface IPooledObject {
        +OnObjectSpawn()
        +OnObjectReturn()
    }

    %% Relationships
    HanokConstructionSystem --> PlotDefinitionController : controls
    HanokConstructionSystem --> HouseMaker : uses
    HanokConstructionSystem --> StructureManager : manages
    HanokConstructionSystem --> HouseCatalog : selects from
    
    HouseMaker --> BuildingCatalog : reads
    HouseMaker --> PoolingComponent : uses
    HouseMaker --> House : creates
    HouseMaker --> Building : places
    
    House --> Building : contains
    House --> BuildingRequirement : defines
    
    Building --> MaterialRequirement : requires
    Building --> BuildingMode : has state
    
    BuildingCatalog --|> Catalog : extends
    HouseCatalog --|> Catalog : extends
    BuildingCatalog --> Building : stores
    HouseCatalog --> House : stores
    
    BuildingPoolingComponent --|> PoolingComponent : extends
    HousePoolingComponent --|> PoolingComponent : extends
    BuildingPoolingComponent --> BuildingCatalog : uses
    HousePoolingComponent --> HouseCatalog : uses
    
    StructureManager --> House : manages
    StructureManager --> Building : tracks
    StructureManager --> StructureStats : generates
    
    PoolingComponent --> PoolData : manages
    PoolingComponent --> IPooledObject : notifies
    
    Building ..|> IPooledObject : implements
    House ..|> IPooledObject : implements
```

## Data Flow

### 1. Plot Creation Flow
```
User Input → HanokConstructionSystem → PlotDefinitionController
                ↓
PlotDefinitionController creates visual preview
                ↓
User finalizes plot → HanokConstructionSystem validates
```

### 2. House Creation Flow
```
Plot Vertices + House Template → HouseMaker
                ↓
HouseMaker.CreateHousesInPlot()
                ↓
Calculate house divisions along plot edge
                ↓
For each division: Create House + Populate with Buildings
                ↓
StructureManager.AddHouse()
```

### 3. Building Placement Flow
```
House + BuildingRequirement → HouseMaker.PopulateHouseWithBuildings()
                ↓
For each required building type:
                ↓
BuildingCatalog.GetFirstBuildingOfType()
                ↓
FindValidBuildingPosition() within house bounds
                ↓
PoolingComponent.GetPooledObject() or Instantiate
                ↓
House.AddBuilding()
```

## Key Design Patterns

### 1. **Command Pattern**
- `HanokConstructionSystem` handles all input and dispatches commands
- Single point of input control prevents conflicts

### 2. **Factory Pattern**
- `HouseMaker` creates houses and buildings
- `PoolingComponent` manages object creation/recycling

### 3. **Strategy Pattern**
- `BuildingCatalog` and `HouseCategory` provide different selection strategies
- Extensible for different building/house types

### 4. **Observer Pattern**
- Buildings and Houses can implement `IPooledObject` for lifecycle events
- Extensible for construction progress notifications

### 5. **Repository Pattern**
- `StructureManager` centralizes house/building management
- `BuildingCatalog`/`HouseCategory` provide data access

## Responsibilities Summary

| Class | Primary Responsibility |
|-------|----------------------|
| `HanokConstructionSystem` | **Input handling and orchestration** - Only class with Update() loop |
| `PlotDefinitionController` | **Plot visualization and validation** - Handles vertex placement and visual feedback |
| `Building` | **Individual structure state** - Manages construction phases and requirements |
| `BuildingCatalog` | **Building type registry** - Provides building templates by type/priority |
| `House` | **Container for buildings** - Manages building placement within bounds |
| `HouseMaker` | **House generation logic** - Divides plots and populates with buildings |
| `HouseCatalog` | **House type registry** - Provides house templates and selection |
| `PoolingComponent` | **Performance optimization** - Manages object lifecycle for memory efficiency |
| `StructureManager` | **World state management** - Tracks all constructed houses and buildings |