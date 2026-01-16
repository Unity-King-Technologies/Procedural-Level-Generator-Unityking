using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class LevelDirector : MonoBehaviour
{
    [Header("Level Configuration")]
    public WFCGenerator wfcGenerator;
    public RoomBank roomBank;
    public RoomTemplates roomTemplates;
    public SeedManager seedManager;

    [Header("Level Structure")]
    public int targetRoomCount = 50;
    public int minRoomCount = 25;
    public int maxRoomCount = 100;
    public float branchingFactor = 0.3f; // How often rooms branch

    [Header("Special Rooms")]
    public bool includeStartRoom = true;
    public bool includeBossRoom = true;
    public bool includeTreasureRooms = true;
    public int treasureRoomCount = 3;

    [Header("Generation Parameters")]
    public int maxGenerationAttempts = 5;
    public float generationTimeout = 30f; // seconds

    // Runtime data
    private LevelStructure currentLevelStructure;
    private Dictionary<Vector3Int, RoomInstance> roomInstances;
    private List<SpecialRoomPlacement> specialRoomPlacements;

    [System.Serializable]
    public class RoomInstance
    {
        public Vector3Int position;
        public RoomModule module;
        public GameObject gameObject;
        public RoomType roomType;
        public float difficulty;
        public List<string> tags;
    }

    [System.Serializable]
    public class SpecialRoomPlacement
    {
        public string roomId;
        public RoomTemplateType templateType;
        public Vector3Int position;
        public bool isPlaced;
    }

    public enum RoomType
    {
        Basic,
        Start,
        End,
        Boss,
        Treasure,
        Special,
        Connector
    }

    private void Awake()
    {
        InitializeDirector();
    }

    private void InitializeDirector()
    {
        roomInstances = new Dictionary<Vector3Int, RoomInstance>();
        specialRoomPlacements = new List<SpecialRoomPlacement>();
        currentLevelStructure = new LevelStructure();
    }

    /// <summary>
    /// Generates a complete level with structure and special rooms
    /// </summary>
    public async Task<bool> GenerateCompleteLevel()
    {
        Debug.Log("Starting complete level generation...");

        // Initialize level structure
        currentLevelStructure = new LevelStructure
        {
            targetRoomCount = targetRoomCount,
            branchingFactor = branchingFactor,
            includeStartRoom = includeStartRoom,
            includeBossRoom = includeBossRoom,
            includeTreasureRooms = includeTreasureRooms
        };

        // Plan level structure
        if (!PlanLevelStructure())
        {
            Debug.LogError("Failed to plan level structure");
            return false;
        }

        // Generate base layout using WFC
        bool generationSuccess = await GenerateBaseLayout();
        if (!generationSuccess)
        {
            Debug.LogError("Failed to generate base layout");
            return false;
        }

        // Apply special room templates
        if (!ApplySpecialRooms())
        {
            Debug.LogWarning("Failed to apply some special rooms");
        }

        // Post-process level
        PostProcessLevel();

        Debug.Log("Complete level generation finished successfully!");
        return true;
    }

    /// <summary>
    /// Plans the overall level structure
    /// </summary>
    private bool PlanLevelStructure()
    {
        // Initialize special room placements
        specialRoomPlacements.Clear();

        if (includeStartRoom)
        {
            specialRoomPlacements.Add(new SpecialRoomPlacement
            {
                roomId = "start_room",
                templateType = RoomTemplateType.StartArea,
                position = Vector3Int.zero,
                isPlaced = false
            });
        }

        if (includeBossRoom)
        {
            specialRoomPlacements.Add(new SpecialRoomPlacement
            {
                roomId = "boss_room",
                templateType = RoomTemplateType.BossArena,
                position = Vector3Int.zero, // Will be determined during generation
                isPlaced = false
            });
        }

        if (includeTreasureRooms)
        {
            for (int i = 0; i < treasureRoomCount; i++)
            {
                specialRoomPlacements.Add(new SpecialRoomPlacement
                {
                    roomId = $"treasure_room_{i}",
                    templateType = RoomTemplateType.TreasureRoom,
                    position = Vector3Int.zero,
                    isPlaced = false
                });
            }
        }

        return true;
    }

    /// <summary>
    /// Generates the base WFC layout
    /// </summary>
    private async Task<bool> GenerateBaseLayout()
    {
        if (wfcGenerator == null)
        {
            Debug.LogError("WFC Generator not assigned!");
            return false;
        }

        // Configure generator for this level
        wfcGenerator.gridSize = CalculateOptimalGridSize();
        wfcGenerator.roomBank = roomBank;

        // Generate with timeout
        var generationTask = wfcGenerator.GenerateLevel();
        var timeoutTask = Task.Delay((int)(generationTimeout * 1000));

        var completedTask = await Task.WhenAny(generationTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            Debug.LogError("Level generation timed out");
            return false;
        }

        bool success = await generationTask;
        if (success)
        {
            // Extract room instances from generated level
            ExtractRoomInstances();
        }

        return success;
    }

    /// <summary>
    /// Calculates optimal grid size based on target room count
    /// </summary>
    private Vector3Int CalculateOptimalGridSize()
    {
        // Estimate grid size needed for target room count
        // Using a simple cubic approximation
        int sideLength = Mathf.CeilToInt(Mathf.Pow(targetRoomCount, 1f / 3f));
        return new Vector3Int(sideLength, 1, sideLength); // 2D layout for now
    }

    /// <summary>
    /// Extracts room instances from the generated WFC grid
    /// </summary>
    private void ExtractRoomInstances()
    {
        roomInstances.Clear();
        var grid = wfcGenerator.GetGrid();

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            var cell = kvp.Value;

            if (cell.isCollapsed && cell.collapsedModule != null)
            {
                RoomInstance instance = new RoomInstance
                {
                    position = position,
                    module = cell.collapsedModule,
                    roomType = DetermineRoomType(cell.collapsedModule),
                    difficulty = CalculateRoomDifficulty(position),
                    tags = new List<string>(cell.collapsedModule.tags)
                };

                roomInstances[position] = instance;
            }
        }
    }

    /// <summary>
    /// Determines room type from module tags
    /// </summary>
    private RoomType DetermineRoomType(RoomModule module)
    {
        if (module.tags.Contains("start")) return RoomType.Start;
        if (module.tags.Contains("boss")) return RoomType.Boss;
        if (module.tags.Contains("treasure")) return RoomType.Treasure;
        if (module.tags.Contains("special")) return RoomType.Special;
        if (module.tags.Contains("connector")) return RoomType.Connector;
        return RoomType.Basic;
    }

    /// <summary>
    /// Applies special room templates to the generated layout
    /// </summary>
    private bool ApplySpecialRooms()
    {
        if (roomTemplates == null)
            return true; // Not an error, just no templates to apply

        bool allPlaced = true;

        foreach (var placement in specialRoomPlacements)
        {
            if (placement.isPlaced)
                continue;

            Vector3Int bestPosition = FindBestPositionForSpecialRoom(placement.templateType);
            if (bestPosition != Vector3Int.one * -1)
            {
                var template = roomTemplates.GetTemplatesByType(placement.templateType)
                    .Find(t => true); // Get first available template

                if (template != null)
                {
                    bool applied = roomTemplates.ApplyTemplate(template, bestPosition, wfcGenerator.GetGrid(), roomBank);
                    if (applied)
                    {
                        placement.position = bestPosition;
                        placement.isPlaced = true;
                    }
                    else
                    {
                        allPlaced = false;
                    }
                }
            }
            else
            {
                allPlaced = false;
            }
        }

        return allPlaced;
    }

    /// <summary>
    /// Finds the best position for a special room
    /// </summary>
    private Vector3Int FindBestPositionForSpecialRoom(RoomTemplateType templateType)
    {
        // Simple position finding - can be made more sophisticated
        switch (templateType)
        {
            case RoomTemplateType.StartArea:
                return Vector3Int.zero; // Center for start

            case RoomTemplateType.BossArena:
                // Find position farthest from start
                return FindFarthestPositionFrom(Vector3Int.zero);

            case RoomTemplateType.TreasureRoom:
                // Find isolated positions
                return FindIsolatedPosition();

            default:
                return FindRandomValidPosition();
        }
    }

    /// <summary>
    /// Finds position farthest from given point
    /// </summary>
    private Vector3Int FindFarthestPositionFrom(Vector3Int fromPosition)
    {
        Vector3Int farthestPos = fromPosition;
        float maxDistance = 0f;

        foreach (var position in roomInstances.Keys)
        {
            float distance = Vector3Int.Distance(position, fromPosition);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                farthestPos = position;
            }
        }

        return farthestPos;
    }

    /// <summary>
    /// Finds an isolated position
    /// </summary>
    private Vector3Int FindIsolatedPosition()
    {
        // Find position with fewest neighbors
        Vector3Int bestPos = roomInstances.Keys.First();
        int minNeighbors = int.MaxValue;

        foreach (var kvp in roomInstances)
        {
            Vector3Int pos = kvp.Key;
            int neighborCount = CountNeighbors(pos);

            if (neighborCount < minNeighbors)
            {
                minNeighbors = neighborCount;
                bestPos = pos;
            }
        }

        return bestPos;
    }

    /// <summary>
    /// Finds a random valid position
    /// </summary>
    private Vector3Int FindRandomValidPosition()
    {
        var positions = new List<Vector3Int>(roomInstances.Keys);
        if (positions.Count == 0)
            return Vector3Int.zero;

        return positions[UnityEngine.Random.Range(0, positions.Count)];
    }

    /// <summary>
    /// Counts neighboring rooms
    /// </summary>
    private int CountNeighbors(Vector3Int position)
    {
        int count = 0;
        Vector3Int[] directions = {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.up, Vector3Int.down
        };

        foreach (var dir in directions)
        {
            if (roomInstances.ContainsKey(position + dir))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Post-processes the generated level
    /// </summary>
    private void PostProcessLevel()
    {
        // Add navigation connections
        GenerateNavigationGraph();

        // Calculate difficulty progression
        CalculateDifficultyProgression();

        // Validate level connectivity
        ValidateLevelConnectivity();

        // Apply final lighting and effects
        ApplyLevelEffects();
    }

    /// <summary>
    /// Generates navigation graph for pathfinding
    /// </summary>
    private void GenerateNavigationGraph()
    {
        // This would integrate with Unity's NavMesh or a custom pathfinding system
        Debug.Log("Generating navigation graph...");
    }

    /// <summary>
    /// Calculates difficulty progression through the level
    /// </summary>
    private void CalculateDifficultyProgression()
    {
        // Calculate difficulty based on distance from start and room types
        Vector3Int startPos = Vector3Int.zero;

        foreach (var instance in roomInstances.Values)
        {
            float distanceFromStart = Vector3Int.Distance(instance.position, startPos);
            float typeMultiplier = GetRoomTypeDifficultyMultiplier(instance.roomType);

            instance.difficulty = distanceFromStart * 0.1f * typeMultiplier;
        }
    }

    /// <summary>
    /// Gets difficulty multiplier for room type
    /// </summary>
    private float GetRoomTypeDifficultyMultiplier(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.Start: return 0.5f;
            case RoomType.Basic: return 1.0f;
            case RoomType.Treasure: return 1.2f;
            case RoomType.Special: return 1.5f;
            case RoomType.Boss: return 2.0f;
            case RoomType.End: return 1.8f;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// Validates that the level is fully connected
    /// </summary>
    private void ValidateLevelConnectivity()
    {
        // Check if all rooms are reachable from start
        var reachableRooms = FindReachableRooms(Vector3Int.zero);

        if (reachableRooms.Count != roomInstances.Count)
        {
            Debug.LogWarning($"Level has {roomInstances.Count - reachableRooms.Count} unreachable rooms");
        }
    }

    /// <summary>
    /// Finds all rooms reachable from a starting position
    /// </summary>
    private HashSet<Vector3Int> FindReachableRooms(Vector3Int startPosition)
    {
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> toVisit = new Queue<Vector3Int>();

        toVisit.Enqueue(startPosition);
        visited.Add(startPosition);

        while (toVisit.Count > 0)
        {
            Vector3Int current = toVisit.Dequeue();

            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back,
                Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (var dir in directions)
            {
                Vector3Int neighbor = current + dir;
                if (roomInstances.ContainsKey(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    toVisit.Enqueue(neighbor);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// Applies final level effects and lighting
    /// </summary>
    private void ApplyLevelEffects()
    {
        // This would apply lighting, particle effects, etc.
        Debug.Log("Applying level effects...");
    }

    /// <summary>
    /// Calculates difficulty for a room at given position
    /// </summary>
    private float CalculateRoomDifficulty(Vector3Int position)
    {
        float distanceFromCenter = Vector3Int.Distance(position, Vector3Int.zero);
        return Mathf.Max(1f, distanceFromCenter * 0.2f);
    }

    /// <summary>
    /// Gets the current level structure
    /// </summary>
    public LevelStructure GetLevelStructure()
    {
        return currentLevelStructure;
    }

    /// <summary>
    /// Gets all room instances
    /// </summary>
    public Dictionary<Vector3Int, RoomInstance> GetRoomInstances()
    {
        return roomInstances;
    }

    /// <summary>
    /// Regenerates the current level
    /// </summary>
    public async Task<bool> RegenerateLevel()
    {
        ClearLevel();
        return await GenerateCompleteLevel();
    }

    /// <summary>
    /// Clears the current level
    /// </summary>
    public void ClearLevel()
    {
        if (wfcGenerator != null)
        {
            wfcGenerator.ClearLevel();
        }

        roomInstances.Clear();
        specialRoomPlacements.Clear();
    }

    /// <summary>
    /// Level structure data
    /// </summary>
    [System.Serializable]
    public class LevelStructure
    {
        public int targetRoomCount;
        public float branchingFactor;
        public bool includeStartRoom;
        public bool includeBossRoom;
        public bool includeTreasureRooms;
        public int actualRoomCount;
        public float averageDifficulty;
        public bool isConnected;
    }
}
