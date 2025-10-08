# Copilot Instructions for Gamejam Unity Project

## Project Overview
This is a Unity-based game project with a focus on player interaction, item collection, and vehicle management. The codebase is organized by gameplay domains (Player, Items, Car) under `Assets/Scripts/Game/`. Key systems include player spawning, inventory, item pickup/deposit, and car fuel mechanics.

## Architecture & Major Components
- **Player System**: Handles spawning (`PlayerSpawner`), inventory (`PlayerInventory`), and input via Unity's Input System.
- **Item System**: Items are defined by `CollectibleData` (ScriptableObject). World items use `WorldCollectible` for pickup logic and effects.
- **Car System**: The car's movement and fuel are managed by `MovCarro` and `CarFuelSystem`. Players deposit collected items to refuel the car.
- **UI Integration**: Uses TextMeshPro for inventory and item UI feedback.

## Data Flow & Interactions
- Players collect items (`WorldCollectible`) if within range and inventory space allows. Items are visually stacked on the player.
- Items are deposited into the car (`CarFuelSystem`) at a deposit point, increasing fuel.
- Car movement speed changes based on fuel percentage (slow when full, fast when low).
- Debug logs are used extensively for state changes and player feedback.

## Developer Workflows
- **Build/Run**: Use Unity Editor for play mode and builds. No custom build scripts detected.
- **Testing**: Manual playtesting in Unity. No automated test scripts found.
- **Debugging**: Enable `showDebugLogs` in components for verbose runtime logs.
- **ScriptableObjects**: Create new item types via `CollectibleData` in the Unity Editor (right-click > Create > Game > Collectible Data).

## Project-Specific Conventions
- Spanish is used for variable names, comments, and prompts (e.g., `diesel`, `depositPrompt`, `velocidad`).
- Item pickup uses `E` key, deposit uses `F` key (hardcoded in scripts).
- Inventory is limited by `maxCarryCapacity` (default 3).
- All item and car logic is component-based, following Unity best practices.
- UI feedback is provided via TextMeshProUGUI and Image components.

## Integration Points & Dependencies
- **Unity Input System**: Used for player input and joining.
- **TextMeshPro**: Required for UI text.
- **Audio**: Item pickup/deposit can trigger sounds via `AudioClip` fields in `CollectibleData`.

## Key Files & Directories
- `Assets/Scripts/Game/Player/PlayerSpawner.cs` — Player spawn logic
- `Assets/Scripts/Game/Player/PlayerInventory.cs` — Inventory and item management
- `Assets/Scripts/Game/Items/WorldCollectible.cs` — Item pickup logic
- `Assets/Scripts/Game/Items/CollectibleData.cs` — Item definitions
- `Assets/Scripts/Game/Car/CarFuelSystem.cs` — Car fuel and deposit system
- `Assets/Scripts/Game/Car/MovCarro.cs` — Car movement and fuel consumption

## Example Patterns
- To add a new collectible item:
  1. Create a new `CollectibleData` asset in Unity Editor.
  2. Assign prefab, icon, and values.
  3. Place a `WorldCollectible` in the scene and assign the new data.
- To change inventory capacity, edit `maxCarryCapacity` in `PlayerInventory`.
- To adjust car speed/fuel, modify values in `MovCarro` and `CarFuelSystem` components.

---

If any section is unclear or missing, please provide feedback or specify which workflows or conventions need more detail.