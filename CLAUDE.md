# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 3D project (Unity 6000.2.2f1) focused on building a **Manor Lords–style construction simulation system** for traditional Korean architecture (Hanok).  
The core gameplay loop allows players to define polygonal plots via vertex placement and automatically populate them with multiple buildings of a selected type, respecting min/max size constraints and placement rules.

## Development Goals

### Stage 1 – Architecture & Folder Setup
- Create `Scripts/Hanok/` folder.
- Define base classes **with clear responsibilities and intended purpose**:

  | Class | Purpose |
  |-------|---------|
  | `HanokConstructionSystem` | Top-level controller for the construction system. Only this class may use the `Update()` loop. Handles all input interfaces. Finalizes construction after verifying the placement status of all houses in the current plot preview. |
  | `PlotDefinitionController` | Previews the construction plot as points, lines, and filled mesh. Line and surface materials/colors can be modified by the user. Uses a layer mask to determine valid vertex placement. Supports snap-to-road at a set distance, and automatically curves lines to match road shapes. |
  | `Building` | Represents a structure within a house. Holds name, type, 2D size, priority, duplicate-allowance flag, required construction materials, and an enum mode (Waiting, Preview, UnderConstruction, Completed, Decayed, etc.). |
  | `Catalog` | Scriptable class. Stores prefabs.
  | `BuildingCatalog` | Inherited Catalog. Manages registered prefabs as `Building` type. |
  | `House` | Contains a list of required building types. Defined by four outline vertices. Has an enum mode, minimum/maximum length constraints, and logic to verify that required buildings are present. |
  | `HouseMaker` | Divides the first plot line into as many houses as can fit, based on length. Places houses and populates them with buildings according to each house’s requirements. |
  | `HouseCatalog` | Inherited Catalog. Manages registered prefabs as `House` type. |
  | `PoolingComponent` | Manages pooling of all registered GameObjects for performance optimization. |
  | `BuildingPoolingComponent` | Manages pooling of all registered GameObjects for performance optimization. Inherited PoolingComponent. Use BuildingCatalog. |
  | `HousePoolingComponent` | Manages pooling of all registered GameObjects for performance optimization. Inherited PoolingComponent. Use HouseCatalog. |
  | `StructureManager` | Manages all constructed houses in the game world. |

- Provide a **class diagram** showing relationships and data flow between components.
- Deliver a minimal working scene setup and updated README.

### Stage 2 – Core Logic Prototype
- Implement plot creation from vertices → mesh generation.
- Implement `Building` and `House` data definitions.
- Implement basic house and building placement algorithms, ignoring UI for now.

### Stage 3 – Input Handling
- Integrate all plot-related input handling exclusively through `HanokConstructionSystem`:
  - **Left Click**: Add a vertex at the cursor position.
  - **Right Click**: Remove the last placed vertex.
  - **Enter**: Finalize the current plot.
  - **Escape**: Cancel the current plot.
- Ignore all plot commands when any modifier key (Alt, Ctrl, Shift) is pressed.
- Ensure that `PlotDefinitionController` only responds to input events provided by `HanokConstructionSystem`.

### Stage 4 – UI & Visual Feedback
- Add a semi-transparent fill mesh and dashed lines for plot preview.
- Provide real-time placement previews for houses and buildings before confirmation.
- Implement building type selection UI (dropdown or hotkey-based).
- Display visual indicators for:
  - Valid vs invalid plot geometry
  - Available building types
  - Final placement confirmation status

### Stage 5 – Validation & Extended Rules
- Implement `PlotValidator` to check:
  - Interior angle range for plots (must be between 30° and 150°)
  - Minimum/maximum vertex count
- Add feasibility checks for building placement inside plots.
- Integrate hooks for:
  - Resource cost checking
  - Construction time simulation
- Prepare the resource system to support future modding (data-driven resource definitions loaded from external files).


## Coding Conventions

- Use `namespace Hanok` for all core scripts.
- Follow PascalCase for classes and methods, camelCase for fields.
- Place all public configuration fields above private fields.
- Use `SerializeField` for Unity inspector-exposed private fields.

## Use Case Example

