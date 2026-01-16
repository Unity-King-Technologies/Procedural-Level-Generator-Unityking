using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class InfiniteGenerator : MonoBehaviour
{
    [Header("Infinite Generation Settings")]
    public ChunkManager chunkManager;
    public WFCGenerator baseGenerator;
    public RoomBank globalRoomBank;

    [Header("World Configuration")]
    public int seed = 0;
    public bool useRandomSeed = true;
    public float worldScale = 1f;
    public bool enableCulling = true;

    [Header("Performance Settings")]
    public int maxGenerationPerFrame = 1;
    public float generationCooldown = 0.1f;
    public bool useMultithreading = true;

    [Header("World Features")]
    public bool enableBiomes = false;
    public bool enableHeightVariation = false;
    public bool enableSpecialEvents = false;

    // Runtime data
    private System.Random worldRandom;
    private Dictionary<Vector3Int, WorldChunk> worldChunks;
    private Queue<ChunkGenerationTask> generationTasks;
    private HashSet<Vector3Int> activeChunks;

    private Transform playerTransform;
    private Vector3Int lastPlayerChunk;
    private float lastGenerationTime;

    [System.Serializable]
    public class WorldChunk
    {
        public Vector3Int chunkCoord;
        public ChunkManager.ChunkData chunkData;
        public BiomeType biome;
        public float heightOffset;
        public bool hasSpecialEvent;
        public List<WorldFeature> features;

        public WorldChunk(Vector3Int coord)
        {
            chunkCoord = coord;
            biome = BiomeType.Plains;
            heightOffset = 0f;
            hasSpecialEvent = false;
            features = new List<WorldFeature>();
        }
    }

    [System.Serializable]
    public class WorldFeature
    {
        public string featureId;
        public Vector3Int localPosition;
        public FeatureType featureType;
        public float intensity;
    }

    public enum BiomeType
    {
        Plains,
        Forest,
        Desert,
        Mountains,
        Swamp,
        Tundra,
        Volcano
    }

    public enum FeatureType
    {
        None,
        Treasure,
        EnemyCamp,
        Ruins,
        MagicalSite,
        DungeonEntrance,
        ResourceNode
    }

    private struct ChunkGenerationTask
    {
        public Vector3Int chunkCoord;
        public Task<ChunkManager.ChunkData> generationTask;

        public ChunkGenerationTask(Vector3Int coord, Task<ChunkManager.ChunkData> task)
        {
            chunkCoord = coord;
            generationTask = task;
        }
    }

    private void Awake()
    {
        InitializeInfiniteGenerator();
    }

    private void InitializeInfiniteGenerator()
    {
        // Initialize random number generator
        int worldSeed = useRandomSeed ? Random.Range(0, int.MaxValue) : seed;
        worldRandom = new System.Random(worldSeed);

        // Initialize data structures
        worldChunks = new Dictionary<Vector3Int, WorldChunk>();
        generationTasks = new Queue<ChunkGenerationTask>();
        activeChunks = new HashSet<Vector3Int>();

        // Find player
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            playerTransform = Camera.main?.transform;
        }

        // Initialize chunk manager
        if (chunkManager == null)
        {
            chunkManager = gameObject.AddComponent<ChunkManager>();
            chunkManager.wfcGenerator = baseGenerator;
            chunkManager.roomBank = globalRoomBank;
        }

        lastGenerationTime = Time.time;
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        UpdatePlayerPosition();
        UpdateChunkGeneration();
        UpdateWorldFeatures();

        if (enableCulling)
        {
            PerformFrustumCulling();
        }
    }

    /// <summary>
    /// Updates player position and handles chunk transitions
    /// </summary>
    private void UpdatePlayerPosition()
    {
        if (chunkManager == null)
            return;

        Vector3Int currentPlayerChunk = chunkManager.WorldToChunkCoord(playerTransform.position);

        if (currentPlayerChunk != lastPlayerChunk)
        {
            OnPlayerChunkChanged(currentPlayerChunk);
            lastPlayerChunk = currentPlayerChunk;
        }
    }

    /// <summary>
    /// Called when player moves to a new chunk
    /// </summary>
    private void OnPlayerChunkChanged(Vector3Int newChunk)
    {
        // Update active chunks
        UpdateActiveChunks(newChunk);

        // Generate world features for new chunks
        GenerateWorldFeatures(newChunk);

        Debug.Log($"Player moved to chunk {newChunk}");
    }

    /// <summary>
    /// Updates the set of active chunks around the player
    /// </summary>
    private void UpdateActiveChunks(Vector3Int playerChunk)
    {
        HashSet<Vector3Int> newActiveChunks = new HashSet<Vector3Int>();

        // Define active area (can be customized)
        int activeRadius = chunkManager.chunkLoadDistance;

        for (int x = -activeRadius; x <= activeRadius; x++)
        {
            for (int z = -activeRadius; z <= activeRadius; z++)
            {
                Vector3Int chunkCoord = playerChunk + new Vector3Int(x, 0, z);
                newActiveChunks.Add(chunkCoord);
            }
        }

        // Request loading of new active chunks
        foreach (Vector3Int chunkCoord in newActiveChunks)
        {
            if (!activeChunks.Contains(chunkCoord))
            {
                RequestChunkGeneration(chunkCoord);
            }
        }

        activeChunks = newActiveChunks;
    }

    /// <summary>
    /// Requests generation of a chunk
    /// </summary>
    private void RequestChunkGeneration(Vector3Int chunkCoord)
    {
        if (worldChunks.ContainsKey(chunkCoord))
            return; // Already exists

        // Create world chunk data
        WorldChunk worldChunk = new WorldChunk(chunkCoord);

        // Generate world features
        GenerateChunkBiome(worldChunk);
        GenerateChunkFeatures(worldChunk);

        worldChunks[chunkCoord] = worldChunk;

        // Request chunk generation from chunk manager
        chunkManager.RequestChunkLoad(chunkCoord);
    }

    /// <summary>
    /// Generates biome for a chunk
    /// </summary>
    private void GenerateChunkBiome(WorldChunk chunk)
    {
        if (!enableBiomes)
        {
            chunk.biome = BiomeType.Plains;
            return;
        }

        // Simple biome generation based on world seed and position
        float noiseX = chunk.chunkCoord.x * 0.1f;
        float noiseZ = chunk.chunkCoord.z * 0.1f;

        float biomeNoise = Mathf.PerlinNoise(noiseX + worldRandom.Next(1000), noiseZ + worldRandom.Next(1000));

        if (biomeNoise < 0.2f)
            chunk.biome = BiomeType.Desert;
        else if (biomeNoise < 0.4f)
            chunk.biome = BiomeType.Forest;
        else if (biomeNoise < 0.6f)
            chunk.biome = BiomeType.Mountains;
        else if (biomeNoise < 0.8f)
            chunk.biome = BiomeType.Swamp;
        else
            chunk.biome = BiomeType.Plains;
    }

    /// <summary>
    /// Generates special features for a chunk
    /// </summary>
    private void GenerateChunkFeatures(WorldChunk chunk)
    {
        if (!enableSpecialEvents)
            return;

        // Generate height variation
        if (enableHeightVariation)
        {
            float heightNoise = Mathf.PerlinNoise(
                chunk.chunkCoord.x * 0.05f + 1000,
                chunk.chunkCoord.z * 0.05f + 1000
            );
            chunk.heightOffset = (heightNoise - 0.5f) * 10f; // ±5 units variation
        }

        // Generate special features with low probability
        float featureChance = 0.05f; // 5% chance per chunk

        if (worldRandom.NextDouble() < featureChance)
        {
            FeatureType featureType = (FeatureType)worldRandom.Next(1, System.Enum.GetValues(typeof(FeatureType)).Length);

            WorldFeature feature = new WorldFeature
            {
                featureId = $"{featureType}_{chunk.chunkCoord.x}_{chunk.chunkCoord.z}",
                localPosition = new Vector3Int(
                    worldRandom.Next(0, chunkManager.chunkSize.x),
                    0,
                    worldRandom.Next(0, chunkManager.chunkSize.z)
                ),
                featureType = featureType,
                intensity = (float)worldRandom.NextDouble()
            };

            chunk.features.Add(feature);
            chunk.hasSpecialEvent = true;
        }
    }

    /// <summary>
    /// Updates chunk generation tasks
    /// </summary>
    private void UpdateChunkGeneration()
    {
        if (Time.time - lastGenerationTime < generationCooldown)
            return;

        int generatedThisFrame = 0;

        while (generationTasks.Count > 0 && generatedThisFrame < maxGenerationPerFrame)
        {
            ChunkGenerationTask task = generationTasks.Dequeue();

            if (task.generationTask.IsCompleted)
            {
                // Apply generated chunk
                ApplyGeneratedChunk(task.chunkCoord);
                generatedThisFrame++;
            }
            else
            {
                // Re-queue for next frame
                generationTasks.Enqueue(task);
            }
        }

        if (generatedThisFrame > 0)
        {
            lastGenerationTime = Time.time;
        }
    }

    /// <summary>
    /// Applies a generated chunk to the world
    /// </summary>
    private void ApplyGeneratedChunk(Vector3Int chunkCoord)
    {
        if (!worldChunks.ContainsKey(chunkCoord))
            return;

        WorldChunk worldChunk = worldChunks[chunkCoord];
        ChunkManager.ChunkData chunkData = chunkManager.GetChunkData(chunkCoord);

        if (chunkData != null && chunkData.isGenerated)
        {
            // Apply biome-specific modifications
            ApplyBiomeModifications(worldChunk, chunkData);

            // Apply world features
            ApplyWorldFeatures(worldChunk, chunkData);

            Debug.Log($"Applied world chunk at {chunkCoord} with biome {worldChunk.biome}");
        }
    }

    /// <summary>
    /// Applies biome-specific modifications to a chunk
    /// </summary>
    private void ApplyBiomeModifications(WorldChunk worldChunk, ChunkManager.ChunkData chunkData)
    {
        if (!enableBiomes)
            return;

        // Modify room selection based on biome
        switch (worldChunk.biome)
        {
            case BiomeType.Forest:
                // Add forest-specific rooms
                break;
            case BiomeType.Desert:
                // Add desert-specific rooms
                break;
            case BiomeType.Mountains:
                // Add mountain-specific rooms
                break;
                // Add more biome modifications as needed
        }
    }

    /// <summary>
    /// Applies world features to a chunk
    /// </summary>
    private void ApplyWorldFeatures(WorldChunk worldChunk, ChunkManager.ChunkData chunkData)
    {
        foreach (WorldFeature feature in worldChunk.features)
        {
            // Apply feature to chunk (this would instantiate special objects, modify rooms, etc.)
            ApplyFeatureToChunk(feature, chunkData);
        }
    }

    /// <summary>
    /// Applies a specific feature to a chunk
    /// </summary>
    private void ApplyFeatureToChunk(WorldFeature feature, ChunkManager.ChunkData chunkData)
    {
        // Find the closest room to the feature position
        Vector3Int closestRoomPos = feature.localPosition;
        float minDistance = float.MaxValue;

        foreach (var roomPos in chunkData.rooms.Keys)
        {
            float distance = Vector3Int.Distance(roomPos, feature.localPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestRoomPos = roomPos;
            }
        }

        // Modify the room based on feature type
        if (chunkData.rooms.ContainsKey(closestRoomPos))
        {
            var room = chunkData.rooms[closestRoomPos];
            ModifyRoomForFeature(room, feature);
        }
    }

    /// <summary>
    /// Modifies a room to include a special feature
    /// </summary>
    private void ModifyRoomForFeature(ChunkManager.RoomInstance room, WorldFeature feature)
    {
        // Add feature-specific modifications
        switch (feature.featureType)
        {
            case FeatureType.Treasure:
                // Add treasure chest or valuable items
                AddTreasureToRoom(room, feature.intensity);
                break;

            case FeatureType.EnemyCamp:
                // Add enemy spawners
                AddEnemiesToRoom(room, feature.intensity);
                break;

            case FeatureType.DungeonEntrance:
                // Add dungeon entrance mechanics
                AddDungeonEntrance(room);
                break;

                // Add more feature types as needed
        }
    }

    /// <summary>
    /// Updates world features (animations, effects, etc.)
    /// </summary>
    private void UpdateWorldFeatures()
    {
        // Update feature animations, particle effects, etc.
        // This would be called every frame for dynamic world features
    }

    /// <summary>
    /// Performs frustum culling for performance
    /// </summary>
    private void PerformFrustumCulling()
    {
        if (Camera.main == null)
            return;

        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        foreach (var chunk in chunkManager.GetLoadedChunks())
        {
            Vector3 chunkWorldPos = chunkManager.ChunkToWorldPosition(chunk.Key);
            Vector3 chunkCenter = chunkWorldPos + new Vector3(
                chunkManager.chunkSize.x * 0.5f,
                0,
                chunkManager.chunkSize.z * 0.5f
            );

            // Simple bounding box check
            Bounds chunkBounds = new Bounds(chunkCenter, new Vector3(
                chunkManager.chunkSize.x,
                1f,
                chunkManager.chunkSize.z
            ));

            bool isVisible = GeometryUtility.TestPlanesAABB(frustumPlanes, chunkBounds);

            // Update chunk visibility
            if (chunk.Value.isVisible != isVisible)
            {
                chunkManager.GetChunkData(chunk.Key).chunkObject?.SetActive(isVisible);
                chunk.Value.isVisible = isVisible;
            }
        }
    }

    /// <summary>
    /// Generates world features around the player
    /// </summary>
    private void GenerateWorldFeatures(Vector3Int playerChunk)
    {
        // Generate features in a larger radius than loaded chunks
        int featureRadius = chunkManager.chunkLoadDistance + 2;

        for (int x = -featureRadius; x <= featureRadius; x++)
        {
            for (int z = -featureRadius; z <= featureRadius; z++)
            {
                Vector3Int chunkCoord = playerChunk + new Vector3Int(x, 0, z);

                if (!worldChunks.ContainsKey(chunkCoord))
                {
                    WorldChunk worldChunk = new WorldChunk(chunkCoord);
                    GenerateChunkBiome(worldChunk);
                    GenerateChunkFeatures(worldChunk);
                    worldChunks[chunkCoord] = worldChunk;
                }
            }
        }
    }

    // Feature application methods (placeholders for actual implementation)
    private void AddTreasureToRoom(ChunkManager.RoomInstance room, float intensity)
    {
        // Implementation would add treasure objects to the room
        Debug.Log($"Added treasure (intensity: {intensity}) to room at {room.localPosition}");
    }

    private void AddEnemiesToRoom(ChunkManager.RoomInstance room, float intensity)
    {
        // Implementation would add enemy spawners to the room
        Debug.Log($"Added enemies (intensity: {intensity}) to room at {room.localPosition}");
    }

    private void AddDungeonEntrance(ChunkManager.RoomInstance room)
    {
        // Implementation would add dungeon entrance mechanics
        Debug.Log($"Added dungeon entrance to room at {room.localPosition}");
    }

    /// <summary>
    /// Gets world chunk data
    /// </summary>
    public WorldChunk GetWorldChunk(Vector3Int chunkCoord)
    {
        return worldChunks.ContainsKey(chunkCoord) ? worldChunks[chunkCoord] : null;
    }

    /// <summary>
    /// Gets biome at world position
    /// </summary>
    public BiomeType GetBiomeAtPosition(Vector3 worldPosition)
    {
        Vector3Int chunkCoord = chunkManager.WorldToChunkCoord(worldPosition);
        WorldChunk worldChunk = GetWorldChunk(chunkCoord);

        return worldChunk != null ? worldChunk.biome : BiomeType.Plains;
    }

    /// <summary>
    /// Gets world statistics
    /// </summary>
    public WorldStatistics GetStatistics()
    {
        var chunkStats = chunkManager.GetStatistics();

        return new WorldStatistics
        {
            totalWorldChunks = worldChunks.Count,
            loadedChunks = chunkStats.loadedChunks,
            activeChunks = activeChunks.Count,
            biomes = GetBiomeCounts(),
            features = GetFeatureCounts()
        };
    }

    private Dictionary<BiomeType, int> GetBiomeCounts()
    {
        Dictionary<BiomeType, int> counts = new Dictionary<BiomeType, int>();
        foreach (var worldChunk in worldChunks.Values)
        {
            if (!counts.ContainsKey(worldChunk.biome))
            {
                counts[worldChunk.biome] = 0;
            }
            counts[worldChunk.biome]++;
        }
        return counts;
    }

    private Dictionary<FeatureType, int> GetFeatureCounts()
    {
        Dictionary<FeatureType, int> counts = new Dictionary<FeatureType, int>();
        foreach (var worldChunk in worldChunks.Values)
        {
            foreach (var feature in worldChunk.features)
            {
                if (!counts.ContainsKey(feature.featureType))
                {
                    counts[feature.featureType] = 0;
                }
                counts[feature.featureType]++;
            }
        }
        return counts;
    }

    [System.Serializable]
    public class WorldStatistics
    {
        public int totalWorldChunks;
        public int loadedChunks;
        public int activeChunks;
        public Dictionary<BiomeType, int> biomes;
        public Dictionary<FeatureType, int> features;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerTransform == null || chunkManager == null)
            return;

        // Draw biome visualization
        foreach (var kvp in worldChunks)
        {
            Vector3Int chunkCoord = kvp.Key;
            WorldChunk worldChunk = kvp.Value;

            Vector3 chunkWorldPos = chunkManager.ChunkToWorldPosition(chunkCoord);
            Vector3 chunkCenter = chunkWorldPos + new Vector3(
                chunkManager.chunkSize.x * 0.5f,
                0,
                chunkManager.chunkSize.z * 0.5f
            );

            // Color based on biome
            Gizmos.color = GetBiomeColor(worldChunk.biome);
            Gizmos.DrawWireCube(chunkCenter, new Vector3(
                chunkManager.chunkSize.x * 0.9f,
                0.1f,
                chunkManager.chunkSize.z * 0.9f
            ));

            // Draw features
            foreach (var feature in worldChunk.features)
            {
                Gizmos.color = Color.red;
                Vector3 featurePos = chunkWorldPos + feature.localPosition;
                Gizmos.DrawSphere(featurePos, 0.5f);
            }
        }
    }

    private Color GetBiomeColor(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Plains: return Color.green;
            case BiomeType.Forest: return Color.green * 0.7f;
            case BiomeType.Desert: return Color.yellow;
            case BiomeType.Mountains: return Color.gray;
            case BiomeType.Swamp: return Color.magenta;
            case BiomeType.Tundra: return Color.white;
            case BiomeType.Volcano: return Color.red;
            default: return Color.white;
        }
    }
#endif
}
