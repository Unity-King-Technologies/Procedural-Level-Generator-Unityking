# 📚 Quantum-Inspired WFC API Reference

## Table of Contents
- [Core Classes](#core-classes)
  - [WFCGenerator](#wfcgenerator)
  - [WFCCore](#wfccore)
  - [EntropyCalculator](#entropycalculator)
  - [ConstraintSolver](#constraintsolver)
  - [PatternExtractor](#patternextractor)
- [Room Management](#room-management)
  - [RoomModule](#roommodule)
  - [RoomBank](#roombank)
  - [RoomData](#roomdata)
  - [RoomConnector](#roomconnector)
  - [RoomTemplates](#roomtemplates)
- [System Classes](#system-classes)
  - [LevelDirector](#leveldirector)
  - [ChunkManager](#chunkmanager)
  - [InfiniteGenerator](#infinitegenerator)
  - [SeedManager](#seedmanager)
  - [DifficultyScaler](#difficultyscaler)
- [Utility Classes](#utility-classes)
  - [GridSystem](#gridsystem)
  - [MathUtilities](#mathutilities)
  - [AsyncProcessor](#asyncprocessor)
  - [VisualizationTools](#visualizationtools)
- [Data Types](#data-types)
- [Editor Classes](#editor-classes)

---

## Core Classes

### WFCGenerator

**Namespace:** Default  
**Inherits from:** MonoBehaviour  
**Description:** Main quantum-enhanced Wave Function Collapse generator component.

#### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `gridSize` | Vector3Int | Dimensions of the generation grid |
| `seed` | string | Seed string for deterministic generation |
| `useRandomSeed` | bool | Whether to generate random seeds |
| `maxAttempts` | int | Maximum generation attempts before failure |
| `quantumCoherence` | float | Quantum coherence factor (0.0-1.0) |
| `tunnelingProbability` | float | Probability of quantum tunneling events |
| `roomBank` | RoomBank | Reference to room bank ScriptableObject |
| `difficultyCurve` | AnimationCurve | Difficulty scaling curve |
| `showDebugVisualization` | bool | Enable debug visualization |
| `debugMaterial` | Material | Material for debug visualization |

#### Public Methods

```csharp
Task<bool> GenerateLevel()
```
Generates a complete level using quantum WFC algorithm.
- **Returns:** `Task<bool>` - True if generation succeeded

```csharp
Task RegenerateLevel()
```
Regenerates the current level with new parameters.

```csharp
Dictionary<Vector3Int, WFCCore.Cell> GetGrid()
```
Gets the current generation grid state.
- **Returns:** `Dictionary<Vector3Int, WFCCore.Cell>` - Current grid

```csharp
Dictionary<Vector3Int, GameObject> GetInstantiatedRooms()
```
Gets all instantiated room GameObjects.
- **Returns:** `Dictionary<Vector3Int, GameObject>` - Room instances

```csharp
void ClearLevel()
```
Clears all generated content.

---

### WFCCore

**Namespace:** Default  
**Description:** Core quantum WFC algorithm implementation.

#### Public Methods

```csharp
WFCCore(Vector3Int size)
```
Constructor for WFC core.
- **Parameters:**
  - `size`: Grid dimensions

```csharp
void InitializeSuperpositions(List<RoomModule> allModules)
```
Initializes quantum superposition states.
- **Parameters:**
  - `allModules`: Available room modules

```csharp
Vector3Int GetLowestEntropyCell()
```
Finds cell with minimum entropy.
- **Returns:** `Vector3Int` - Cell position (-1,-1,-1 if none found)

```csharp
RoomModule CollapseCell(Vector3Int position)
```
Collapses a cell to a specific module.
- **Parameters:**
  - `position`: Cell position
- **Returns:** `RoomModule` - Selected module (null if failed)

```csharp
bool IsFullyCollapsed()
```
Checks if generation is complete.
- **Returns:** `bool` - True if all cells collapsed

```csharp
void PropagateConstraints(Vector3Int position, RoomModule collapsedModule)
```
Propagates constraints from collapsed cell.
- **Parameters:**
  - `position`: Cell position
  - `collapsedModule`: Collapsed module

```csharp
Dictionary<Vector3Int, Cell> GetGrid()
```
Gets the generation grid.
- **Returns:** `Dictionary<Vector3Int, Cell>` - Grid data

#### Nested Classes

##### Cell
Represents a single grid cell with quantum properties.

**Properties:**
- `position`: Vector3Int - Cell position
- `possibleModules`: List<RoomModule> - Available modules
- `isCollapsed`: bool - Collapse state
- `collapsedModule`: RoomModule - Selected module
- `entropy`: float - Current entropy
- `quantumPhase`: float - Quantum phase (0-2π)
- `superpositionAmplitude`: float - Quantum amplitude

---

### EntropyCalculator

**Namespace:** Default  
**Description:** Static utility class for entropy calculations.

#### Static Methods

```csharp
static float CalculateShannonEntropy(List<RoomModule> modules)
```
Calculates Shannon entropy for module set.
- **Parameters:**
  - `modules`: Module list
- **Returns:** `float` - Entropy value

```csharp
static float CalculateQuantumEntropy(List<RoomModule> modules, float superpositionAmplitude, float quantumPhase)
```
Calculates quantum-enhanced entropy.
- **Parameters:**
  - `modules`: Module list
  - `superpositionAmplitude`: Quantum amplitude
  - `quantumPhase`: Quantum phase
- **Returns:** `float` - Quantum entropy

```csharp
static WFCCore.Cell FindMinimumEntropyCell(Dictionary<Vector3Int, WFCCore.Cell> grid)
```
Finds cell with minimum entropy.
- **Parameters:**
  - `grid`: Generation grid
- **Returns:** `WFCCore.Cell` - Minimum entropy cell

```csharp
static Dictionary<Vector3Int, float> CalculateEntropyGradient(Dictionary<Vector3Int, WFCCore.Cell> grid)
```
Calculates entropy gradient across grid.
- **Parameters:**
  - `grid`: Generation grid
- **Returns:** `Dictionary<Vector3Int, float>` - Gradient map

---

### ConstraintSolver

**Namespace:** Default  
**Description:** Static utility class for constraint solving.

#### Static Methods

```csharp
static bool PropagateConstraints(Dictionary<Vector3Int, WFCCore.Cell> grid, Vector3Int position, RoomModule collapsedModule)
```
Propagates constraints from collapsed cell.
- **Parameters:**
  - `grid`: Generation grid
  - `position`: Cell position
  - `collapsedModule`: Collapsed module
- **Returns:** `bool` - True if constraints changed

```csharp
static bool ValidateAllConstraints(Dictionary<Vector3Int, WFCCore.Cell> grid)
```
Validates all grid constraints.
- **Parameters:**
  - `grid`: Generation grid
- **Returns:** `bool` - True if all constraints satisfied

```csharp
static float CalculateConstraintSatisfaction(Dictionary<Vector3Int, WFCCore.Cell> grid)
```
Calculates constraint satisfaction ratio.
- **Parameters:**
  - `grid`: Generation grid
- **Returns:** `float` - Satisfaction ratio (0.0-1.0)

---

### PatternExtractor

**Namespace:** Default  
**Description:** Static utility class for pattern extraction and learning.

#### Static Methods

```csharp
static Dictionary<Vector3Int, Pattern> ExtractPatterns(Dictionary<Vector3Int, WFCCore.Cell> grid, int patternSize = 2)
```
Extracts patterns from completed grid.
- **Parameters:**
  - `grid`: Generation grid
  - `patternSize`: Pattern dimensions
- **Returns:** `Dictionary<Vector3Int, Pattern>` - Extracted patterns

```csharp
static PatternDictionary LearnFromExamples(List<Dictionary<Vector3Int, WFCCore.Cell>> exampleLevels, int patternSize = 2)
```
Learns patterns from multiple examples.
- **Parameters:**
  - `exampleLevels`: List of example grids
  - `patternSize`: Pattern dimensions
- **Returns:** `PatternDictionary` - Learned patterns

---

## Room Management

### RoomModule

**Namespace:** Default  
**Inherits from:** ScriptableObject  
**Description:** ScriptableObject defining a room module with sockets and properties.

#### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `prefab` | GameObject | Room prefab |
| `dimensions` | Vector3Int | Room dimensions |
| `sockets` | Socket[] | Connection sockets |
| `baseWeight` | float | Selection weight |
| `difficultyRange` | int[] | Difficulty range [min, max] |
| `tags` | string[] | Room tags |

#### Public Methods

```csharp
bool ConnectsTo(RoomModule other, Direction dir)
```
Checks if this module can connect to another.
- **Parameters:**
  - `other`: Other room module
  - `dir`: Connection direction
- **Returns:** `bool` - True if compatible

#### Nested Classes

##### Socket
Represents a connection socket.

**Properties:**
- `id`: string - Socket identifier
- `direction`: Direction - Socket direction
- `type`: SocketType - Socket type
- `weight`: int - Connection weight

##### Direction Enum
- `North`, `East`, `South`, `West`, `Up`, `Down`

##### SocketType Enum
- `Entrance`, `Exit`, `Connector`, `Special`

---

### RoomBank

**Namespace:** Default  
**Inherits from:** ScriptableObject  
**Description:** Manages collections of room modules.

#### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `categories` | RoomCategory[] | Room categories |
| `standaloneRooms` | RoomModule[] | Standalone rooms |

#### Public Methods

```csharp
List<RoomModule> GetAllModules()
```
Gets all available modules.
- **Returns:** `List<RoomModule>` - All modules

```csharp
List<RoomModule> GetModulesByCategory(string categoryName)
```
Gets modules in specific category.
- **Parameters:**
  - `categoryName`: Category name
- **Returns:** `List<RoomModule>` - Category modules

```csharp
List<RoomModule> GetModulesByTag(string tag)
```
Gets modules with specific tag.
- **Parameters:**
  - `tag`: Tag string
- **Returns:** `List<RoomModule>` - Tagged modules

```csharp
void AddRoom(RoomModule room, string categoryName = null)
```
Adds a room to the bank.
- **Parameters:**
  - `room`: Room module
  - `categoryName`: Optional category

```csharp
RoomModule GetRandomRoom(System.Random random = null)
```
Gets a random room.
- **Parameters:**
  - `random`: Optional random generator
- **Returns:** `RoomModule` - Random room

---

### RoomData

**Namespace:** Default  
**Inherits from:** ScriptableObject  
**Description:** Extended room statistics and data management.

#### Public Methods

```csharp
RoomStats GetRoomStats(string roomName)
```
Gets statistics for a room.
- **Parameters:**
  - `roomName`: Room name
- **Returns:** `RoomStats` - Room statistics

```csharp
List<RoomStats> GetRoomsByType(RoomType roomType)
```
Gets rooms of specific type.
- **Parameters:**
  - `roomType`: Room type
- **Returns:** `List<RoomStats>` - Rooms of type

```csharp
List<string> ValidateRoomData()
```
Validates room data integrity.
- **Returns:** `List<string>` - Validation errors

---

### RoomConnector

**Namespace:** Default  
**Description:** Static utility class for room connections.

#### Static Methods

```csharp
static bool CanConnect(RoomModule module1, RoomModule module2, RoomModule.Direction direction)
```
Checks if modules can connect.
- **Parameters:**
  - `module1`: First module
  - `module2`: Second module
  - `direction`: Connection direction
- **Returns:** `bool` - True if connectable

```csharp
static List<RoomModule.Direction> GetConnectionPoints(RoomModule module)
```
Gets module connection points.
- **Parameters:**
  - `module`: Room module
- **Returns:** `List<Direction>` - Connection directions

```csharp
static Dictionary<Vector3Int, List<ConnectionIssue>> ValidateConnections(Dictionary<Vector3Int, WFCCore.Cell> grid)
```
Validates grid connections.
- **Parameters:**
  - `grid`: Generation grid
- **Returns:** `Dictionary<Vector3Int, List<ConnectionIssue>>` - Connection issues

---

### RoomTemplates

**Namespace:** Default  
**Inherits from:** ScriptableObject  
**Description:** Manages predefined room templates.

#### Public Methods

```csharp
RoomTemplate GetTemplate(string templateName)
```
Gets template by name.
- **Parameters:**
  - `templateName`: Template name
- **Returns:** `RoomTemplate` - Room template

```csharp
bool ApplyTemplate(RoomTemplate template, Vector3Int position, Dictionary<Vector3Int, WFCCore.Cell> grid, RoomBank roomBank)
```
Applies template to grid.
- **Parameters:**
  - `template`: Room template
  - `position`: Application position
  - `grid`: Generation grid
  - `roomBank`: Room bank reference
- **Returns:** `bool` - True if applied successfully

---

## System Classes

### LevelDirector

**Namespace:** Default  
**Inherits from:** MonoBehaviour  
**Description:** High-level level orchestration system.

#### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `wfcGenerator` | WFCGenerator | WFC generator reference |
| `roomBank` | RoomBank | Room bank reference |
| `roomTemplates` | RoomTemplates | Room templates reference |
| `seedManager` | SeedManager | Seed manager reference |
| `targetRoomCount` | int | Target number of rooms |
| `branchingFactor` | float | Level branching factor |
| `includeStartRoom` | bool | Include start room |
| `includeBossRoom` | bool | Include boss room |
| `includeTreasureRooms` | bool | Include treasure rooms |
| `maxGenerationAttempts` | int | Maximum generation attempts |
| `generationTimeout` | float | Generation timeout (seconds) |

#### Public Methods

```csharp
Task<bool> GenerateCompleteLevel()
```
Generates a complete level with all features.
- **Returns:** `Task<bool>` - True if successful

```csharp
Dictionary<Vector3Int, RoomInstance> GetRoomInstances()
```
Gets all room instances.
- **Returns:** `Dictionary<Vector3Int, RoomInstance>` - Room instances

---

### ChunkManager

**Namespace:** Default  
**Inherits from:** MonoBehaviour  
**Description:** Manages chunk loading and unloading for infinite worlds.

#### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `chunkSize` | Vector3Int | Size of each chunk |
| `maxLoadedChunks` | int | Maximum loaded chunks |
| `chunkLoadDistance` | int | Distance to load chunks |
| `chunkUnloadDistance` | int | Distance to unload chunks |
| `wfcGenerator` | WFCGenerator | WFC generator reference |
| `roomBank` | RoomBank | Room bank reference |
| `generationPriority` | float | Generation priority factor |

#### Public Methods

```csharp
void RequestChunkLoad(Vector3Int chunkCoord)
```
Requests chunk loading.
- **Parameters:**
  - `chunkCoord`: Chunk coordinates

```csharp
Vector3Int WorldToChunkCoord(Vector3 worldPosition)
```
Converts world position to chunk coordinates.
- **Parameters:**
  - `worldPosition`: World position
- **Returns:** `Vector3Int` - Chunk coordinates

```csharp
Vector3Int ChunkToWorldPosition(Vector3Int chunkCoord)
```
Converts chunk coordinates to world position.
- **Parameters:**
  - `chunkCoord`: Chunk coordinates
- **Returns:** `Vector3Int` - World position

```csharp
ChunkStatistics GetStatistics()
```
Gets chunk loading statistics.
- **Returns:** `ChunkStatistics` - Loading statistics

---

### InfiniteGenerator

**Namespace:** Default  
**Inherits from:** MonoBehaviour  
**Description:** Manages infinite world generation with biomes and features.

#### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `chunkManager` | ChunkManager | Chunk manager reference |
| `seed` | int | World seed |
| `useRandomSeed` | bool | Use random seed |
| `enableBiomes` | bool | Enable biome generation |
| `enableHeightVariation` | bool | Enable height variation |
| `enableSpecialEvents` | bool | Enable special events |

#### Public Methods

```csharp
WorldChunk GetWorldChunk(Vector3Int chunkCoord)
```
Gets world chunk data.
- **Parameters:**
  - `chunkCoord`: Chunk coordinates
- **Returns:** `WorldChunk` - World chunk data

```csharp
BiomeType GetBiomeAtPosition(Vector3 worldPosition)
```
Gets biome at world position.
- **Parameters:**
  - `worldPosition`: World position
- **Returns:** `BiomeType` - Biome type

```csharp
WorldStatistics GetStatistics()
```
Gets world generation statistics.
- **Returns:** `WorldStatistics` - World statistics

---

### SeedManager

**Namespace:** Default  
**Inherits from:** MonoBehaviour  
**Description:** Manages deterministic seeds for reproducible generation.

#### Public Properties

| Property | Type | Description |
|----------|------|-------------|
| `globalSeed` | string | Global seed string |
| `useGlobalSeed` | bool | Use global seed |
| `levelSeed` | int | Level seed |
| `layoutSeed` | int | Layout seed |
| `difficultySeed` | int | Difficulty seed |

#### Public Methods

```csharp
void InitializeSeeds()
```
Initializes all seeds.

```csharp
Random.State GetRandomStateForCategory(SeedCategory category)
```
Gets random state for category.
- **Parameters:**
  - `category`: Seed category
- **Returns:** `Random.State` - Random state

```csharp
string GenerateDeterministicSeed(int playerLevel, string playerId)
```
Generates deterministic seed.
- **Parameters:**
  - `playerLevel`: Player level
  - `playerId`: Player identifier
- **Returns:** `string` - Deterministic seed

```csharp
int GenerateQuantumSeed(Vector3Int position, float time)
```
Generates quantum seed.
- **Parameters:**
  - `position`: Position vector
  - `time`: Time value
- **Returns:** `int` - Quantum seed

---

### DifficultyScaler

**Namespace:** Default  
**Inherits from:** MonoBehaviour  
**Description:** Manages adaptive difficulty scaling.

#### Public Methods

```csharp
List<RoomModule> FilterModules(List<RoomModule> allModules, int playerLevel, Vector3Int position)
```
Filters modules by difficulty.
- **Parameters:**
  - `allModules`: All available modules
  - `playerLevel`: Player level
  - `position`: Position vector
- **Returns:** `List<RoomModule>` - Filtered modules

```csharp
void SetCurrentDifficulty(int difficulty)
```
Sets current difficulty level.
- **Parameters:**
  - `difficulty`: Difficulty level

```csharp
float GetDifficultyMultiplier(Vector3Int position)
```
Gets difficulty multiplier for position.
- **Parameters:**
  - `position`: Position vector
- **Returns:** `float` - Difficulty multiplier

---

## Utility Classes

### GridSystem

**Namespace:** Default  
**Description:** 3D grid coordinate system management.

#### Public Methods

```csharp
GridSystem(Vector3Int size, Vector3 cellSize = default, Vector3 origin = default)
```
Constructor for grid system.
- **Parameters:**
  - `size`: Grid size
  - `cellSize`: Cell size (default Vector3.one)
  - `origin`: Grid origin (default Vector3.zero)

```csharp
Vector3 GridToWorld(Vector3Int gridPosition)
```
Converts grid coordinates to world position.
- **Parameters:**
  - `gridPosition`: Grid position
- **Returns:** `Vector3` - World position

```csharp
Vector3Int WorldToGrid(Vector3 worldPosition)
```
Converts world position to grid coordinates.
- **Parameters:**
  - `worldPosition`: World position
- **Returns:** `Vector3Int` - Grid position

```csharp
bool IsValidGridPosition(Vector3Int position)
```
Checks if grid position is valid.
- **Parameters:**
  - `position`: Grid position
- **Returns:** `bool` - True if valid

```csharp
Vector3Int[] GetNeighbors(Vector3Int position, bool includeDiagonals = false)
```
Gets neighboring positions.
- **Parameters:**
  - `position`: Center position
  - `includeDiagonals`: Include diagonal neighbors
- **Returns:** `Vector3Int[]` - Neighbor positions

---

### MathUtilities

**Namespace:** Default  
**Description:** Static mathematical utility functions.

#### Static Methods

```csharp
static float DistanceToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
```
Calculates distance to line segment.
- **Parameters:**
  - `point`: Test point
  - `lineStart`: Line start
  - `lineEnd`: Line end
- **Returns:** `float` - Distance

```csharp
static Vector2 RandomPointInCircle(float radius, System.Random random = null)
```
Generates random point in circle.
- **Parameters:**
  - `radius`: Circle radius
  - `random`: Optional random generator
- **Returns:** `Vector2` - Random point

```csharp
static Vector3 RandomPointInSphere(float radius, System.Random random = null)
```
Generates random point in sphere.
- **Parameters:**
  - `radius`: Sphere radius
  - `random`: Optional random generator
- **Returns:** `Vector3` - Random point

```csharp
static List<Vector2> GeneratePoissonDiskDistribution(float width, float height, float minDistance, int maxAttempts = 30, System.Random random = null)
```
Generates Poisson disk distribution.
- **Parameters:**
  - `width`: Distribution width
  - `height`: Distribution height
  - `minDistance`: Minimum point distance
  - `maxAttempts`: Maximum placement attempts
  - `random`: Optional random generator
- **Returns:** `List<Vector2>` - Point distribution

---

### AsyncProcessor

**Namespace:** Default  
**Inherits from:** MonoBehaviour  
**Description:** Manages asynchronous task processing.

#### Static Methods

```csharp
static void EnqueueTask(string taskId, Task task, System.Action onComplete = null, System.Action<AsyncTask> onProgress = null)
```
Enqueues a task for processing.
- **Parameters:**
  - `taskId`: Task identifier
  - `task`: Task to process
  - `onComplete`: Completion callback
  - `onProgress`: Progress callback

```csharp
static void EnqueueGenerationTask(string taskId, System.Func<Task> generationFunction, System.Action onComplete = null)
```
Enqueues a generation task.
- **Parameters:**
  - `taskId`: Task identifier
  - `generationFunction`: Generation function
  - `onComplete`: Completion callback

```csharp
static int GetQueuedTaskCount()
```
Gets number of queued tasks.
- **Returns:** `int` - Queue count

```csharp
static int GetRunningTaskCount()
```
Gets number of running tasks.
- **Returns:** `int` - Running count

---

### VisualizationTools

**Namespace:** Default  
**Description:** Static visualization utility functions.

#### Static Methods

```csharp
static void VisualizeWFCGrid(Dictionary<Vector3Int, WFCCore.Cell> grid, Transform parent = null, float cellSize = 1f)
```
Visualizes WFC grid state.
- **Parameters:**
  - `grid`: Generation grid
  - `parent`: Parent transform
  - `cellSize`: Cell visualization size

```csharp
static void VisualizeEntropyHeatmap(Dictionary<Vector3Int, WFCCore.Cell> grid, Transform parent = null, float cellSize = 1f)
```
Visualizes entropy heatmap.
- **Parameters:**
  - `grid`: Generation grid
  - `parent`: Parent transform
  - `cellSize`: Cell visualization size

```csharp
static void CreatePerformanceMonitor(Transform parent = null)
```
Creates performance monitor.
- **Parameters:**
  - `parent`: Parent transform

---

## Data Types

### Enums

#### RoomTemplateType
- `StartArea`, `BossArena`, `TreasureRoom`, `PuzzleArea`, `Corridor`, `Junction`, `DeadEnd`, `Custom`

#### BiomeType
- `Plains`, `Forest`, `Desert`, `Mountains`, `Swamp`, `Tundra`, `Volcano`

#### FeatureType
- `None`, `Treasure`, `EnemyCamp`, `Ruins`, `MagicalSite`, `DungeonEntrance`, `ResourceNode`

### Structs

#### ChunkStatistics
- `loadedChunks`: int
- `unloadedChunks`: int
- `pendingGenerations`: int
- `totalChunks`: int

#### TaskStatistics
- `queuedTasks`: int
- `runningTasks`: int
- `maxConcurrentTasks`: int

---

## Editor Classes

### RoomModuleEditor

**Namespace:** UnityEditor  
**Inherits from:** Editor  
**Description:** Custom Unity inspector for RoomModule.

#### Features
- Custom property drawers for sockets
- Tag management interface
- Real-time validation
- Quick setup buttons

### WFCDebugger

**Namespace:** UnityEditor  
**Inherits from:** EditorWindow  
**Description:** Real-time WFC debugging window.

#### Features
- Live generation monitoring
- Entropy visualization
- Quantum state inspection
- Debug data export
- Performance metrics

### PatternBaker

**Namespace:** UnityEditor  
**Inherits from:** EditorWindow  
**Description:** Machine learning pattern extraction tool.

#### Features
- Multi-example pattern learning
- Baked pattern export
- Pattern statistics
- Scene setup automation

---

## Usage Examples

### Basic Generation
```csharp
// Attach WFCGenerator to GameObject
WFCGenerator generator = GetComponent<WFCGenerator>();
generator.gridSize = new Vector3Int(10, 1, 10);
generator.quantumCoherence = 0.8f;

// Generate level
await generator.GenerateLevel();
```

### Advanced Configuration
```csharp
// Configure quantum parameters
generator.quantumCoherence = 0.6f;        // More creative
generator.tunnelingProbability = 0.1f;    // Allow emergence

// Use custom room bank
generator.roomBank = myCustomRoomBank;

// Generate with seed
generator.seed = "my_custom_level";
await generator.GenerateLevel();
```

### Infinite World Setup
```csharp
// Set up infinite generation
InfiniteGenerator infiniteGen = gameObject.AddComponent<InfiniteGenerator>();
infiniteGen.enableBiomes = true;
infiniteGen.enableSpecialEvents = true;

// Configure chunk manager
ChunkManager chunkManager = GetComponent<ChunkManager>();
chunkManager.chunkSize = new Vector3Int(16, 1, 16);
chunkManager.chunkLoadDistance = 3;
```

---

*This API reference covers the complete Quantum-Inspired WFC system. For implementation details, see the WFC_Implementation.md guide.*
