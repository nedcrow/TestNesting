# Hanok Construction System

A Unity 3D project implementing a **Manor Lords–style construction simulation system** for traditional Korean architecture (Hanok). Players define polygonal plots via vertex placement and automatically populate them with multiple buildings respecting size constraints and placement rules.

Built with **Unity 6000.2.2f1**.

## 🏗️ Core Concept

The system follows the Manor Lords gameplay pattern:
1. **Define Plot Area**: Click to place vertices forming a polygonal construction area
2. **Select House Type**: Choose from available traditional Korean house styles
3. **Auto-Generate Buildings**: System divides the plot and places multiple houses with appropriate buildings
4. **Manage Construction**: Track progress, materials, and building states

## 🎮 Controls

### Plot Definition Mode
- **Left Click**: Add vertex at cursor position
- **Right Click**: Remove last placed vertex
- **Enter**: Finalize current plot and generate houses
- **Escape**: Cancel current plot
- **Note**: All plot commands ignored when modifier keys (Alt/Ctrl/Shift) are pressed

### Construction Management
- **Tab**: Cycle through available house types (when implemented)
- **B**: Toggle construction mode on/off (when implemented)

## 🏛️ System Architecture

### Core Components

| Component | Responsibility |
|-----------|---------------|
| **HanokConstructionSystem** | Top-level controller handling all input. Only class using Update() loop. |
| **PlotDefinitionController** | Manages plot preview with lines, fill mesh, and vertex validation. |
| **Building** | Individual structure with construction states, materials, and placement logic. |
| **BuildingCatalog** | Registry of all available building prefabs organized by type and priority. |
| **House** | Container for buildings with 4-vertex outline and requirement validation. |
| **HouseMaker** | Divides plots into houses and populates them with buildings. |
| **HouseCatalog** | Registry of available house types with size constraints. |
| **PoolingComponent** | Performance optimization through object pooling. |
| **StructureManager** | World-state manager tracking all constructed houses and buildings. |

### Building States
- **Waiting**: Ready for construction
- **Preview**: Visual preview mode
- **Under Construction**: Active construction with progress tracking
- **Completed**: Finished building
- **Decayed**: Abandoned/deteriorated state

### House States  
- **Planning**: Initial design phase
- **Preview**: Visual preview before commitment
- **Under Construction**: Active building phase
- **Completed**: All buildings finished
- **Abandoned**: Unused/deteriorated state

## 🚀 Quick Start

### 1. Scene Setup

Create a new scene with the following hierarchy:

```
Hanok Construction Scene
├── Construction System (HanokConstructionSystem)
│   ├── Plot Controller (PlotDefinitionController)
│   ├── House Maker (HouseMaker)  
│   ├── Structure Manager (StructureManager)
│   └── Pooling (PoolingComponent)
├── Ground (with collider on ground layer)
└── Main Camera
```

### 2. Component Configuration

**HanokConstructionSystem**:
- Assign PlotDefinitionController reference
- Assign HouseMaker reference  
- Assign StructureManager reference
- Create and assign HouseCatalog asset

**PlotDefinitionController**:
- Set Player Camera reference
- Configure Ground Layer Mask
- Create line and surface materials for plot preview
- Set min/max vertices (3-8 recommended)

### 3. Create Asset Catalogs

**Building Catalog** (`Assets/Create/Hanok/Building Catalog`):
- Add building prefabs with priorities
- Configure material requirements
- Set building types (Residential, Commercial, etc.)

**House Catalog** (`Assets/Create/Hanok/HouseCatalog`):
- Add house prefabs with size constraints
- Define required building types per house
- Set min/max dimensions

### 4. Building Prefab Setup

Each building prefab should have:
- `Building` component with configured properties
- Collider for bounds calculation
- Preview and completed visual variants
- Optional `IPooledObject` implementation

### 5. House Prefab Setup

Each house prefab should have:
- `House` component with building requirements
- Size constraints (min/max length/width)
- Configured building type requirements

## 📐 Plot Validation Rules

The system enforces geometric constraints:
- **Minimum vertices**: 3 (triangle)
- **Maximum vertices**: 8 (configurable)
- **Interior angles**: Must be between 30° and 150°
- **No self-intersections**: Plot edges cannot cross
- **Valid placement**: Must raycast hit ground layer

## 🏠 House Generation Logic

1. **Plot Division**: System divides the first plot edge into equal house segments
2. **Size Validation**: Each house division must meet min/max size constraints  
3. **Building Placement**: Houses populated with required buildings from catalog
4. **Collision Avoidance**: Buildings placed without overlaps using spacing rules
5. **Priority System**: Higher priority buildings placed first

## 🔧 Customization

### Custom Building Types

```csharp
namespace Hanok
{
    public class CustomBuilding : Building
    {
        [SerializeField] private float customProperty;
        
        protected override void UpdateVisualState()
        {
            base.UpdateVisualState();
            // Custom visual logic
        }
    }
}
```

### Custom House Logic

```csharp
namespace Hanok  
{
    public class CustomHouse : House
    {
        protected override bool ValidateCustomConstraints()
        {
            // Custom validation logic
            return base.VerifyRequiredBuildings();
        }
    }
}
```

### Material System Integration

Buildings define material requirements:

```csharp
[SerializeField] private MaterialRequirement[] requiredMaterials = {
    new MaterialRequirement("Wood", 20),
    new MaterialRequirement("Stone", 10),
    new MaterialRequirement("Roof Tiles", 5)
};
```

## 🎯 Development Stages

### Stage 1 - Architecture ✅
- ✅ Core class structure implemented  
- ✅ Clear component responsibilities defined
- ✅ Class diagram and data flow documented
- ✅ Namespace organization (`namespace Hanok`)

### Stage 2 - Core Logic (In Progress)
- ⏳ Plot creation and mesh generation
- ⏳ Building and House data definitions
- ⏳ Basic placement algorithms

### Stage 3 - Input Integration (Planned)
- 📋 Complete input handling through HanokConstructionSystem
- 📋 Modifier key detection and filtering
- 📋 Event-driven architecture

### Stage 4 - Visual Feedback (Planned) 
- 📋 Semi-transparent plot previews
- 📋 Real-time building placement previews
- 📋 Building type selection UI
- 📋 Construction state indicators

### Stage 5 - Validation & Extended Rules ✅
- ✅ PlotValidator with angle/geometry checks (30°-150°)
- ✅ Self-intersection detection
- ✅ Building placement feasibility checks
- ✅ Resource cost checking hooks
- ✅ Construction time simulation framework
- 📋 Modding support for external definitions

## 🛠️ Technical Details

### Performance Optimizations
- **Object Pooling**: All buildings and houses use pooling system
- **Batch Operations**: House generation processes multiple buildings efficiently
- **Spatial Partitioning**: StructureManager tracks objects by regions
- **LOD Support**: Ready for Level-of-Detail integration

### Code Conventions
- **Namespace**: All classes in `namespace Hanok`
- **Naming**: PascalCase classes/methods, camelCase fields
- **Serialization**: `[SerializeField]` for Unity inspector fields
- **Documentation**: XML comments for public APIs

## 📊 System Metrics

The StructureManager provides comprehensive statistics:
- Total houses and buildings
- Construction states breakdown
- Building type distribution  
- Resource consumption tracking
- Performance metrics

## 🔍 Debug Features

Enable detailed logging in components:
```csharp
[SerializeField] private bool debugMode = true;
```

Provides information about:
- Plot validation results
- Building placement attempts  
- Collision detection results
- Resource calculation details

## 📚 Class Reference

See [CLASS_DIAGRAM.md](CLASS_DIAGRAM.md) for detailed:
- System architecture overview
- Component relationships
- Data flow patterns
- Design pattern implementations
- Responsibility matrix

## 🎮 Gameplay Example

1. Player enters construction mode
2. Clicks to define a rectangular plot area
3. Presses Enter to finalize plot
4. System analyzes plot dimensions
5. Divides plot edge into 3 house segments
6. Places traditional Korean houses in each segment
7. Populates each house with required buildings:
   - Main residence (priority 10)
   - Storage building (priority 5) 
   - Workshop (priority 3)
8. All buildings enter construction state
9. Player can monitor progress and manage resources

## 🚧 Current Limitations

- UI system not yet implemented
- Resource management placeholder
- Advanced validation rules pending
- Construction time simulation basic
- No save/load system yet

## 📜 License

This project is provided for educational and development purposes. Modify and distribute freely while respecting traditional Korean architectural heritage.