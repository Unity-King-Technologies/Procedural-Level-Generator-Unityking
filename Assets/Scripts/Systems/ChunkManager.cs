using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk Configuration")]
    public Vector3Int chunkSize = new Vector3Int(10, 1, 10);
    public int maxLoadedChunks = 9; // 3x3 grid
    public int chunkLoadDistance = 2;
    public int chunkUnloadDistance = 3;

    [Header("Generation Settings")]
    public WFCGenerator wfcGenerator;
    public RoomBank roomBank;
    public float generationPriority = 0.5f; // 0 = distance priority, 1 = visibility priority

    // Runtime data
    private Dictionary<Vector3Int, ChunkData> loadedChunks;
    private Dictionary<Vector3Int, ChunkData> unloadedChunks;
    private Queue<ChunkGenerationRequest> generationQueue;
    private Vector3Int currentPlayerChunk;

    private Transform playerTransform;

    [System.Serializable]
    public class ChunkData
    {
        public Vector3Int chunkCoord;
        public Vector3Int worldPosition;
        public GameObject chunkObject;
        public Dictionary<Vector3Int, RoomInstance> rooms;
        public bool isGenerated;
        public bool isVisible;
        public float lastAccessTime;
        public int generationAttempt;

        public ChunkData(Vector3Int coord, Vector3Int worldPos)
        {
            chunkCoord = coord;
            worldPosition = worldPos;
            rooms = new Dictionary<Vector3Int, RoomInstance>();
            isGenerated = false;
            isVisible = false;
            lastAccessTime = Time.time;
            generationAttempt = 0;
        }
    }

    [System.Serializable]
    public class RoomInstance
    {
        public Vector3Int localPosition;
        public RoomModule module;
        public GameObject gameObject;
        public bool isActive;
    }

    private struct ChunkGenerationRequest
    {
        public Vector3Int chunkCoord;
        public float priority;

        public ChunkGenerationRequest(Vector3Int coord, float prio)
        {
            chunkCoord = coord;
            priority = prio;
        }
    }

    private void Awake()
    {
        InitializeManager();
    }

    private void InitializeManager()
    {
        loadedChunks = new Dictionary<Vector3Int, ChunkData>();
        unloadedChunks = new Dictionary<Vector3Int, ChunkData>();
        generationQueue = new Queue<ChunkGenerationRequest>();

        // Find player transform
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            playerTransform = Camera.main?.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
            return;

        UpdatePlayerChunk();
        UpdateChunkLoading();
        ProcessGenerationQueue();
    }

    /// <summary>
    /// Updates the current player chunk position
    /// </summary>
    private void UpdatePlayerChunk()
    {
        Vector3Int newPlayerChunk = WorldToChunkCoord(playerTransform.position);

        if (newPlayerChunk != currentPlayerChunk)
        {
            currentPlayerChunk = newPlayerChunk;
            OnPlayerChunkChanged();
        }
    }

    /// <summary>
    /// Called when player moves to a new chunk
    /// </summary>
    private void OnPlayerChunkChanged()
    {
        // Request loading of nearby chunks
        for (int x = -chunkLoadDistance; x <= chunkLoadDistance; x++)
        {
            for (int z = -chunkLoadDistance; z <= chunkLoadDistance; z++)
            {
                Vector3Int chunkCoord = currentPlayerChunk + new Vector3Int(x, 0, z);
                RequestChunkLoad(chunkCoord);
            }
        }

        // Unload distant chunks
        UnloadDistantChunks();
    }

    /// <summary>
    /// Requests loading of a chunk
    /// </summary>
    public void RequestChunkLoad(Vector3Int chunkCoord)
    {
        // Check if already loaded
        if (loadedChunks.ContainsKey(chunkCoord))
        {
            loadedChunks[chunkCoord].lastAccessTime = Time.time;
            return;
        }

        // Check if in unloaded cache
        if (unloadedChunks.ContainsKey(chunkCoord))
        {
            // Move from unloaded to loaded
            ChunkData chunkData = unloadedChunks[chunkCoord];
            unloadedChunks.Remove(chunkCoord);
            loadedChunks[chunkCoord] = chunkData;
            chunkData.lastAccessTime = Time.time;
            SetChunkVisibility(chunkData, true);
            return;
        }

        // Need to generate new chunk
        float priority = CalculateGenerationPriority(chunkCoord);
        generationQueue.Enqueue(new ChunkGenerationRequest(chunkCoord, priority));
    }

    /// <summary>
    /// Calculates generation priority for a chunk
    /// </summary>
    private float CalculateGenerationPriority(Vector3Int chunkCoord)
    {
        float distanceToPlayer = Vector3Int.Distance(chunkCoord, currentPlayerChunk);
        float maxDistance = chunkLoadDistance;

        // Distance priority (0 = closest, 1 = farthest)
        float distancePriority = Mathf.Clamp01(distanceToPlayer / maxDistance);

        // Visibility priority (simple distance-based for now)
        float visibilityPriority = distancePriority;

        // Combine priorities
        return Mathf.Lerp(distancePriority, visibilityPriority, generationPriority);
    }

    /// <summary>
    /// Processes the generation queue
    /// </summary>
    private async void ProcessGenerationQueue()
    {
        if (generationQueue.Count == 0)
            return;

        // Sort queue by priority (lower priority number = higher priority)
        var sortedQueue = new List<ChunkGenerationRequest>(generationQueue);
        sortedQueue.Sort((a, b) => a.priority.CompareTo(b.priority));
        generationQueue = new Queue<ChunkGenerationRequest>(sortedQueue);

        // Process highest priority chunk
        ChunkGenerationRequest request = generationQueue.Dequeue();

        // Check if chunk is still needed
        if (!ShouldLoadChunk(request.chunkCoord))
            return;

        // Generate chunk
        await GenerateChunk(request.chunkCoord);
    }

    /// <summary>
    /// Checks if a chunk should still be loaded
    /// </summary>
    private bool ShouldLoadChunk(Vector3Int chunkCoord)
    {
        if (playerTransform == null)
            return false;

        Vector3Int playerChunk = WorldToChunkCoord(playerTransform.position);
        int distance = Mathf.Max(
            Mathf.Abs(chunkCoord.x - playerChunk.x),
            Mathf.Abs(chunkCoord.z - playerChunk.z)
        );

        return distance <= chunkLoadDistance;
    }

    /// <summary>
    /// Generates a new chunk
    /// </summary>
    private async Task GenerateChunk(Vector3Int chunkCoord)
    {
        // Create chunk data
        Vector3Int worldPos = ChunkToWorldPosition(chunkCoord);
        ChunkData chunkData = new ChunkData(chunkCoord, worldPos);

        // Configure WFC generator for this chunk
        if (wfcGenerator != null)
        {
            // Set chunk-specific seed
            int chunkSeed = GetChunkSeed(chunkCoord);
            wfcGenerator.seed = chunkSeed.ToString();
            wfcGenerator.gridSize = chunkSize;
            wfcGenerator.roomBank = roomBank;

            // Generate chunk content
            bool success = await wfcGenerator.GenerateLevel();

            if (success)
            {
                // Extract generated rooms
                ExtractChunkRooms(chunkData);

                // Create chunk game object
                CreateChunkObject(chunkData);

                chunkData.isGenerated = true;
                loadedChunks[chunkCoord] = chunkData;

                Debug.Log($"Generated chunk at {chunkCoord}");
            }
            else
            {
                chunkData.generationAttempt++;
                if (chunkData.generationAttempt < 3)
                {
                    // Retry with different seed
                    generationQueue.Enqueue(new ChunkGenerationRequest(chunkCoord, CalculateGenerationPriority(chunkCoord)));
                }
                else
                {
                    Debug.LogError($"Failed to generate chunk at {chunkCoord} after 3 attempts");
                }
            }
        }
    }

    /// <summary>
    /// Extracts room data from generated WFC grid
    /// </summary>
    private void ExtractChunkRooms(ChunkData chunkData)
    {
        var grid = wfcGenerator.GetGrid();

        foreach (var kvp in grid)
        {
            Vector3Int localPos = kvp.Key;
            var cell = kvp.Value;

            if (cell.isCollapsed && cell.collapsedModule != null)
            {
                RoomInstance roomInstance = new RoomInstance
                {
                    localPosition = localPos,
                    module = cell.collapsedModule,
                    isActive = true
                };

                chunkData.rooms[localPos] = roomInstance;
            }
        }
    }

    /// <summary>
    /// Creates the game object for a chunk
    /// </summary>
    private void CreateChunkObject(ChunkData chunkData)
    {
        // Create chunk container
        chunkData.chunkObject = new GameObject($"Chunk_{chunkData.chunkCoord.x}_{chunkData.chunkCoord.z}");
        chunkData.chunkObject.transform.position = chunkData.worldPosition;
        chunkData.chunkObject.transform.SetParent(transform);

        // Instantiate rooms
        foreach (var room in chunkData.rooms.Values)
        {
            if (room.module != null && room.module.prefab != null)
            {
                Vector3 worldPos = chunkData.worldPosition + room.localPosition;
                room.gameObject = Instantiate(room.module.prefab, worldPos, Quaternion.identity, chunkData.chunkObject.transform);
                room.gameObject.name = $"{room.module.name}_{room.localPosition.x}_{room.localPosition.z}";
            }
        }
    }

    /// <summary>
    /// Unloads chunks that are too far from the player
    /// </summary>
    private void UnloadDistantChunks()
    {
        List<Vector3Int> chunksToUnload = new List<Vector3Int>();

        foreach (var kvp in loadedChunks)
        {
            Vector3Int chunkCoord = kvp.Key;
            ChunkData chunkData = kvp.Value;

            int distance = Mathf.Max(
                Mathf.Abs(chunkCoord.x - currentPlayerChunk.x),
                Mathf.Abs(chunkCoord.z - currentPlayerChunk.z)
            );

            if (distance > chunkUnloadDistance)
            {
                chunksToUnload.Add(chunkCoord);
            }
        }

        foreach (Vector3Int chunkCoord in chunksToUnload)
        {
            UnloadChunk(chunkCoord);
        }
    }

    /// <summary>
    /// Unloads a specific chunk
    /// </summary>
    private void UnloadChunk(Vector3Int chunkCoord)
    {
        if (!loadedChunks.ContainsKey(chunkCoord))
            return;

        ChunkData chunkData = loadedChunks[chunkCoord];
        loadedChunks.Remove(chunkCoord);

        // Move to unloaded cache if within reasonable distance
        int distance = Mathf.Max(
            Mathf.Abs(chunkCoord.x - currentPlayerChunk.x),
            Mathf.Abs(chunkCoord.z - currentPlayerChunk.z)
        );

        if (distance <= chunkUnloadDistance + 2) // Keep in cache for quick reloading
        {
            SetChunkVisibility(chunkData, false);
            unloadedChunks[chunkCoord] = chunkData;
        }
        else
        {
            // Destroy completely
            if (chunkData.chunkObject != null)
            {
                Destroy(chunkData.chunkObject);
            }
        }
    }

    /// <summary>
    /// Sets chunk visibility
    /// </summary>
    private void SetChunkVisibility(ChunkData chunkData, bool visible)
    {
        chunkData.isVisible = visible;

        if (chunkData.chunkObject != null)
        {
            chunkData.chunkObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Updates chunk loading based on player movement
    /// </summary>
    private void UpdateChunkLoading()
    {
        // This is handled in OnPlayerChunkChanged and ProcessGenerationQueue
    }

    /// <summary>
    /// Converts world position to chunk coordinates
    /// </summary>
    public Vector3Int WorldToChunkCoord(Vector3 worldPosition)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize.x),
            0, // Assuming 2D chunks
            Mathf.FloorToInt(worldPosition.z / chunkSize.z)
        );
    }

    /// <summary>
    /// Converts chunk coordinates to world position
    /// </summary>
    public Vector3Int ChunkToWorldPosition(Vector3Int chunkCoord)
    {
        return new Vector3Int(
            chunkCoord.x * chunkSize.x,
            0,
            chunkCoord.z * chunkSize.z
        );
    }

    /// <summary>
    /// Gets a deterministic seed for a chunk
    /// </summary>
    private int GetChunkSeed(Vector3Int chunkCoord)
    {
        // Create deterministic seed based on chunk coordinates
        // Using a simple hash function
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + chunkCoord.x;
            hash = hash * 31 + chunkCoord.y;
            hash = hash * 31 + chunkCoord.z;
            return hash;
        }
    }

    /// <summary>
    /// Gets chunk data for a specific coordinate
    /// </summary>
    public ChunkData GetChunkData(Vector3Int chunkCoord)
    {
        if (loadedChunks.ContainsKey(chunkCoord))
        {
            return loadedChunks[chunkCoord];
        }

        if (unloadedChunks.ContainsKey(chunkCoord))
        {
            return unloadedChunks[chunkCoord];
        }

        return null;
    }

    /// <summary>
    /// Gets all loaded chunks
    /// </summary>
    public Dictionary<Vector3Int, ChunkData> GetLoadedChunks()
    {
        return loadedChunks;
    }

    /// <summary>
    /// Forces unloading of all chunks
    /// </summary>
    public void UnloadAllChunks()
    {
        foreach (var chunkData in loadedChunks.Values)
        {
            if (chunkData.chunkObject != null)
            {
                Destroy(chunkData.chunkObject);
            }
        }

        foreach (var chunkData in unloadedChunks.Values)
        {
            if (chunkData.chunkObject != null)
            {
                Destroy(chunkData.chunkObject);
            }
        }

        loadedChunks.Clear();
        unloadedChunks.Clear();
        generationQueue.Clear();
    }

    /// <summary>
    /// Gets statistics about chunk loading
    /// </summary>
    public ChunkStatistics GetStatistics()
    {
        return new ChunkStatistics
        {
            loadedChunks = loadedChunks.Count,
            unloadedChunks = unloadedChunks.Count,
            pendingGenerations = generationQueue.Count,
            totalChunks = loadedChunks.Count + unloadedChunks.Count
        };
    }

    [System.Serializable]
    public class ChunkStatistics
    {
        public int loadedChunks;
        public int unloadedChunks;
        public int pendingGenerations;
        public int totalChunks;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (playerTransform == null)
            return;

        // Draw current chunk
        Vector3Int currentChunk = WorldToChunkCoord(playerTransform.position);
        Vector3 chunkWorldPos = ChunkToWorldPosition(currentChunk);

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(chunkWorldPos + new Vector3(chunkSize.x * 0.5f, 0, chunkSize.z * 0.5f),
                           new Vector3(chunkSize.x, 0.1f, chunkSize.z));

        // Draw load distance
        Gizmos.color = Color.yellow;
        for (int x = -chunkLoadDistance; x <= chunkLoadDistance; x++)
        {
            for (int z = -chunkLoadDistance; z <= chunkLoadDistance; z++)
            {
                if (x == 0 && z == 0)
                    continue;

                Vector3Int chunkCoord = currentChunk + new Vector3Int(x, 0, z);
                Vector3 pos = ChunkToWorldPosition(chunkCoord);
                Gizmos.DrawWireCube(pos + new Vector3(chunkSize.x * 0.5f, 0, chunkSize.z * 0.5f),
                                   new Vector3(chunkSize.x, 0.05f, chunkSize.z));
            }
        }
    }
#endif
}
