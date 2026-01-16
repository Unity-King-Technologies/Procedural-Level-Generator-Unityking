using System.Collections.Generic;
using UnityEngine;

public static class VisualizationTools
{
    /// <summary>
    /// Creates a debug visualization for the WFC grid
    /// </summary>
    public static void VisualizeWFCGrid(Dictionary<Vector3Int, WFCCore.Cell> grid, Transform parent = null, float cellSize = 1f)
    {
        GameObject visContainer = new GameObject("WFC_Visualization");
        if (parent != null)
        {
            visContainer.transform.SetParent(parent);
        }

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            Vector3 worldPos = new Vector3(position.x * cellSize, position.y * cellSize, position.z * cellSize);

            // Create cell visualization
            GameObject cellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cellObj.transform.SetParent(visContainer.transform);
            cellObj.transform.position = worldPos;
            cellObj.transform.localScale = Vector3.one * (cellSize * 0.8f);

            // Color based on cell state
            Renderer renderer = cellObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (cell.isCollapsed)
                {
                    // Collapsed - color based on quantum phase
                    float hue = (cell.quantumPhase / (2 * Mathf.PI) + 1) % 1;
                    renderer.material.color = Color.HSVToRGB(hue, 0.8f, 1f);
                }
                else
                {
                    // Not collapsed - color based on possibilities and superposition
                    float intensity = Mathf.Clamp01(cell.possibleModules.Count / 10f);
                    float alpha = 0.3f + cell.superpositionAmplitude * 0.7f;
                    renderer.material.color = new Color(1f, 1f, 1f, alpha);
                    renderer.material.color *= new Color(intensity, intensity, 1f);
                }
            }

            // Add entropy display
            if (!cell.isCollapsed && cell.entropy > 0.1f)
            {
                GameObject textObj = new GameObject($"Entropy_{position.x}_{position.y}_{position.z}");
                textObj.transform.SetParent(cellObj.transform);
                textObj.transform.localPosition = Vector3.up * (cellSize * 0.6f);

                // Note: In a real implementation, you'd use TextMeshPro or similar
                // For now, we'll just use the object name for debugging
                textObj.name = $"Entropy: {cell.entropy:F2}";
            }
        }
    }

    /// <summary>
    /// Visualizes constraint propagation waves
    /// </summary>
    public static void VisualizeConstraintPropagation(Vector3Int startPosition, Dictionary<Vector3Int, WFCCore.Cell> grid,
        Transform parent = null, float cellSize = 1f)
    {
        GameObject waveContainer = new GameObject("Constraint_Waves");
        if (parent != null)
        {
            waveContainer.transform.SetParent(parent);
        }

        // Create wave visualization from start position
        Queue<Vector3Int> toProcess = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, int> waveDistance = new Dictionary<Vector3Int, int>();

        toProcess.Enqueue(startPosition);
        visited.Add(startPosition);
        waveDistance[startPosition] = 0;

        Vector3Int[] directions = {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.up, Vector3Int.down
        };

        while (toProcess.Count > 0)
        {
            Vector3Int current = toProcess.Dequeue();
            int distance = waveDistance[current];

            // Create wave indicator
            Vector3 worldPos = new Vector3(current.x * cellSize, current.y * cellSize, current.z * cellSize);
            GameObject waveObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            waveObj.transform.SetParent(waveContainer.transform);
            waveObj.transform.position = worldPos;
            waveObj.transform.localScale = Vector3.one * (cellSize * 0.3f);

            // Color based on wave distance
            Renderer renderer = waveObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                float hue = (distance * 0.1f) % 1f;
                renderer.material.color = Color.HSVToRGB(hue, 1f, 1f);
            }

            // Process neighbors
            foreach (var dir in directions)
            {
                Vector3Int neighbor = current + dir;
                if (grid.ContainsKey(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    waveDistance[neighbor] = distance + 1;
                    toProcess.Enqueue(neighbor);
                }
            }
        }
    }

    /// <summary>
    /// Creates a heatmap visualization of entropy values
    /// </summary>
    public static void VisualizeEntropyHeatmap(Dictionary<Vector3Int, WFCCore.Cell> grid, Transform parent = null, float cellSize = 1f)
    {
        GameObject heatmapContainer = new GameObject("Entropy_Heatmap");
        if (parent != null)
        {
            heatmapContainer.transform.SetParent(parent);
        }

        // Find entropy range
        float minEntropy = float.MaxValue;
        float maxEntropy = float.MinValue;

        foreach (var cell in grid.Values)
        {
            if (!cell.isCollapsed)
            {
                minEntropy = Mathf.Min(minEntropy, cell.entropy);
                maxEntropy = Mathf.Max(maxEntropy, cell.entropy);
            }
        }

        float entropyRange = maxEntropy - minEntropy;
        if (entropyRange == 0) entropyRange = 1;

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (cell.isCollapsed)
                continue;

            Vector3 worldPos = new Vector3(position.x * cellSize, position.y * cellSize, position.z * cellSize);

            // Create heatmap quad
            GameObject quadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObj.transform.SetParent(heatmapContainer.transform);
            quadObj.transform.position = worldPos + Vector3.up * (cellSize * 0.6f);
            quadObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            quadObj.transform.localScale = Vector3.one * (cellSize * 0.8f);

            // Color based on entropy (red = high entropy, blue = low entropy)
            Renderer renderer = quadObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                float normalizedEntropy = (cell.entropy - minEntropy) / entropyRange;
                Color entropyColor = Color.Lerp(Color.blue, Color.red, normalizedEntropy);
                entropyColor.a = 0.7f;
                renderer.material.color = entropyColor;
            }
        }
    }

    /// <summary>
    /// Visualizes the pattern matching process
    /// </summary>
    public static void VisualizePatternMatching(List<Pattern> patterns, Vector3Int targetPosition,
        Dictionary<Vector3Int, WFCCore.Cell> grid, Transform parent = null, float cellSize = 1f)
    {
        GameObject patternContainer = new GameObject("Pattern_Matching");
        if (parent != null)
        {
            patternContainer.transform.SetParent(parent);
        }

        Vector3 worldPos = new Vector3(targetPosition.x * cellSize, targetPosition.y * cellSize, targetPosition.z * cellSize);

        // Create target indicator
        GameObject targetObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        targetObj.transform.SetParent(patternContainer.transform);
        targetObj.transform.position = worldPos;
        targetObj.transform.localScale = new Vector3(cellSize * 0.5f, cellSize * 2f, cellSize * 0.5f);

        Renderer targetRenderer = targetObj.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetRenderer.material.color = Color.yellow;
        }

        // Visualize compatible patterns
        for (int i = 0; i < patterns.Count; i++)
        {
            var pattern = patterns[i];
            Vector3 patternPos = worldPos + new Vector3(i * cellSize * 2, cellSize * 2, 0);

            // Create pattern preview (simplified)
            GameObject patternObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            patternObj.transform.SetParent(patternContainer.transform);
            patternObj.transform.position = patternPos;
            patternObj.transform.localScale = Vector3.one * (cellSize * 0.8f);

            Renderer patternRenderer = patternObj.GetComponent<Renderer>();
            if (patternRenderer != null)
            {
                // Color based on pattern weight
                float intensity = Mathf.Clamp01(pattern.GetFrequencyWeight());
                patternRenderer.material.color = new Color(intensity, 1f - intensity, 0.5f);
            }
        }
    }

    /// <summary>
    /// Creates a real-time performance monitor
    /// </summary>
    public static void CreatePerformanceMonitor(Transform parent = null)
    {
        GameObject monitorObj = new GameObject("Performance_Monitor");
        if (parent != null)
        {
            monitorObj.transform.SetParent(parent);
        }

        var monitor = monitorObj.AddComponent<PerformanceMonitor>();
        monitor.Initialize();
    }

    /// <summary>
    /// Visualizes chunk loading and unloading
    /// </summary>
    public static void VisualizeChunkLoading(Dictionary<Vector3Int, ChunkManager.ChunkData> loadedChunks,
        Dictionary<Vector3Int, ChunkManager.ChunkData> unloadedChunks, Transform parent = null)
    {
        GameObject chunkVisContainer = new GameObject("Chunk_Visualization");
        if (parent != null)
        {
            chunkVisContainer.transform.SetParent(parent);
        }

        // Visualize loaded chunks
        foreach (var kvp in loadedChunks)
        {
            Vector3Int chunkCoord = kvp.Key;
            Vector3 chunkWorldPos = ChunkManager.ChunkToWorldPosition(chunkCoord);

            GameObject chunkObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunkObj.transform.SetParent(chunkVisContainer.transform);
            chunkObj.transform.position = chunkWorldPos;
            chunkObj.transform.localScale = Vector3.one * 0.5f;

            Renderer renderer = chunkObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green; // Loaded
            }

            chunkObj.name = $"Loaded_Chunk_{chunkCoord.x}_{chunkCoord.z}";
        }

        // Visualize unloaded chunks
        foreach (var kvp in unloadedChunks)
        {
            Vector3Int chunkCoord = kvp.Key;
            Vector3 chunkWorldPos = ChunkManager.ChunkToWorldPosition(chunkCoord);

            GameObject chunkObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chunkObj.transform.SetParent(chunkVisContainer.transform);
            chunkObj.transform.position = chunkWorldPos;
            chunkObj.transform.localScale = Vector3.one * 0.3f;

            Renderer renderer = chunkObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1f, 1f, 0f, 0.5f); // Unloaded (transparent yellow)
            }

            chunkObj.name = $"Unloaded_Chunk_{chunkCoord.x}_{chunkCoord.z}";
        }
    }

    /// <summary>
    /// Creates a navigation path visualization
    /// </summary>
    public static void VisualizeNavigationPath(List<Vector3Int> path, Transform parent = null, float cellSize = 1f)
    {
        if (path.Count < 2)
            return;

        GameObject pathContainer = new GameObject("Navigation_Path");
        if (parent != null)
        {
            pathContainer.transform.SetParent(parent);
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 start = new Vector3(path[i].x * cellSize, path[i].y * cellSize, path[i].z * cellSize);
            Vector3 end = new Vector3(path[i + 1].x * cellSize, path[i + 1].y * cellSize, path[i + 1].z * cellSize);

            // Create line renderer for path segment
            GameObject lineObj = new GameObject($"Path_Segment_{i}");
            lineObj.transform.SetParent(pathContainer.transform);

            var lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start + Vector3.up * cellSize * 0.5f);
            lineRenderer.SetPosition(1, end + Vector3.up * cellSize * 0.5f);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.cyan;
            lineRenderer.endColor = Color.cyan;
        }

        // Add path points
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 point = new Vector3(path[i].x * cellSize, path[i].y * cellSize, path[i].z * cellSize);

            GameObject pointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointObj.transform.SetParent(pathContainer.transform);
            pointObj.transform.position = point + Vector3.up * cellSize * 0.5f;
            pointObj.transform.localScale = Vector3.one * 0.2f;

            Renderer renderer = pointObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = i == 0 ? Color.green : (i == path.Count - 1 ? Color.red : Color.blue);
            }

            pointObj.name = $"Path_Point_{i}";
        }
    }

    /// <summary>
    /// Visualizes quantum interference patterns
    /// </summary>
    public static void VisualizeQuantumInterference(Dictionary<Vector3Int, WFCCore.Cell> grid, Transform parent = null, float cellSize = 1f)
    {
        GameObject interferenceContainer = new GameObject("Quantum_Interference");
        if (parent != null)
        {
            interferenceContainer.transform.SetParent(parent);
        }

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (cell.isCollapsed)
                continue;

            Vector3 worldPos = new Vector3(position.x * cellSize, position.y * cellSize, position.z * cellSize);

            // Create interference visualization
            GameObject interferenceObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            interferenceObj.transform.SetParent(interferenceContainer.transform);
            interferenceObj.transform.position = worldPos + Vector3.up * cellSize * 0.8f;
            interferenceObj.transform.localScale = Vector3.one * (cell.superpositionAmplitude * cellSize);

            Renderer renderer = interferenceObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Color based on quantum phase
                float hue = (cell.quantumPhase / (2 * Mathf.PI) + 1) % 1;
                Color interferenceColor = Color.HSVToRGB(hue, 0.6f, 1f);
                interferenceColor.a = 0.4f;
                renderer.material.color = interferenceColor;
            }

            interferenceObj.name = $"Interference_{position.x}_{position.y}_{position.z}";
        }
    }
}

/// <summary>
/// Performance monitoring component
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    private float updateInterval = 1f;
    private float lastUpdateTime;
    private Dictionary<string, float> metrics;

    public void Initialize()
    {
        metrics = new Dictionary<string, float>();
        lastUpdateTime = Time.time;
    }

    private void Update()
    {
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateMetrics();
            DisplayMetrics();
            lastUpdateTime = Time.time;
        }
    }

    private void UpdateMetrics()
    {
        // Update performance metrics
        metrics["FPS"] = 1f / Time.deltaTime;
        metrics["FrameTime"] = Time.deltaTime * 1000f; // ms
        metrics["TotalAllocatedMemory"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f); // MB
        metrics["UsedHeapSize"] = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024f * 1024f); // MB

        // Add WFC-specific metrics if available
        var asyncStats = AsyncProcessor.GetStatistics();
        metrics["QueuedTasks"] = asyncStats.queuedTasks;
        metrics["RunningTasks"] = asyncStats.runningTasks;
    }

    private void DisplayMetrics()
    {
        // Display metrics on screen (simplified - in a real implementation, use OnGUI or UI)
        string metricsText = "Performance Monitor:\n";
        foreach (var kvp in metrics)
        {
            metricsText += $"{kvp.Key}: {kvp.Value:F2}\n";
        }

        Debug.Log(metricsText);
    }

    public Dictionary<string, float> GetMetrics()
    {
        return new Dictionary<string, float>(metrics);
    }
}
