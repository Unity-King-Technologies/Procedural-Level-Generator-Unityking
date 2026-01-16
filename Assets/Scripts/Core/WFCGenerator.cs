using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WFCGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public Vector3Int gridSize = new Vector3Int(10, 1, 10);
    public int seed = 0;
    public bool useRandomSeed = true;
    public int maxAttempts = 1000;

    [Header("Quantum Settings")]
    [Range(0f, 1f)]
    public float quantumCoherence = 0.8f;
    [Range(0f, 1f)]
    public float tunnelingProbability = 0.05f;

    [Header("Room Settings")]
    public RoomBank roomBank;
    public AnimationCurve difficultyCurve;

    [Header("Visualization")]
    public bool showDebugVisualization = true;
    public Material debugMaterial;

    private WFCCore wfcCore;
    private GridSystem gridSystem;
    private DifficultyScaler difficultyScaler;
    private Dictionary<Vector3Int, GameObject> instantiatedRooms;

    private void Start()
    {
        InitializeGenerator();
        GenerateLevel();
    }

    private void InitializeGenerator()
    {
        Random.InitState(useRandomSeed ? Random.Range(0, int.MaxValue) : seed);

        wfcCore = new WFCCore(gridSize);
        gridSystem = new GridSystem(gridSize);
        difficultyScaler = new DifficultyScaler(difficultyCurve);
        instantiatedRooms = new Dictionary<Vector3Int, GameObject>();

        // Apply quantum settings to core
        if (wfcCore is WFCCore quantumCore)
        {
            // Note: We'd need to expose these settings in WFCCore for full control
        }
    }

    public async Task<bool> GenerateLevel()
    {
        Debug.Log("Starting Quantum-Inspired WFC Generation...");

        // Step 1: Initialize superpositions
        wfcCore.InitializeSuperpositions(roomBank.GetAllModules());
        Debug.Log("Superposition initialized with quantum coherence");

        // Step 2: Apply initial constraints
        ApplyInitialConstraints();

        // Step 3: Collapse wave function
        int attempts = 0;
        while (!wfcCore.IsFullyCollapsed() && attempts < maxAttempts)
        {
            Vector3Int lowestEntropyCell = wfcCore.GetLowestEntropyCell();
            if (lowestEntropyCell == Vector3Int.one * -1)
            {
                Debug.LogWarning("No valid cell found for collapse - possible contradiction");
                break;
            }

            RoomModule selectedModule = wfcCore.CollapseCell(lowestEntropyCell);
            if (selectedModule == null)
            {
                Debug.LogWarning($"Failed to collapse cell {lowestEntropyCell}");
                attempts++;
                continue;
            }

            Debug.Log($"Collapsed cell {lowestEntropyCell} to {selectedModule.name}");

            // Quantum-inspired constraint propagation
            wfcCore.PropagateConstraints(lowestEntropyCell, selectedModule);

            // Async yield for performance
            await Task.Yield();
            attempts++;
        }

        // Step 4: Instantiate rooms
        if (wfcCore.IsFullyCollapsed())
        {
            InstantiateRooms();
            Debug.Log("Level generation completed successfully!");
            return true;
        }
        else
        {
            Debug.LogWarning($"Generation failed after {maxAttempts} attempts");
            return false;
        }
    }

    private void ApplyInitialConstraints()
    {
        // Set starting point constraints
        Vector3Int startPos = new Vector3Int(gridSize.x / 2, 0, gridSize.z / 2);

        // Force start room at center (could be made configurable)
        var startModules = roomBank.GetAllModules().FindAll(m =>
            m.tags.Contains("start") || m.difficultyRange[0] == 1);

        if (startModules.Count > 0)
        {
            var startCell = wfcCore.GetGrid()[startPos];
            startCell.possibleModules = startModules;
            startCell.superpositionAmplitude = 0f; // Force collapse start
            startCell.CalculateEntropy();
        }
    }

    private void InstantiateRooms()
    {
        var grid = wfcCore.GetGrid();

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            var cell = kvp.Value;

            if (cell.isCollapsed && cell.collapsedModule != null)
            {
                Vector3 worldPos = gridSystem.GridToWorld(position);
                GameObject roomInstance = Instantiate(cell.collapsedModule.prefab, worldPos, Quaternion.identity, transform);
                roomInstance.name = $"{cell.collapsedModule.name}_{position.x}_{position.y}_{position.z}";
                instantiatedRooms[position] = roomInstance;

                // Add debug visualization if enabled
                if (showDebugVisualization)
                {
                    AddDebugVisualization(roomInstance, cell);
                }
            }
        }
    }

    private void AddDebugVisualization(GameObject room, WFCCore.Cell cell)
    {
        // Add a small indicator showing quantum properties
        GameObject debugObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        debugObj.transform.SetParent(room.transform);
        debugObj.transform.localPosition = Vector3.up * 2;
        debugObj.transform.localScale = Vector3.one * 0.2f;

        if (debugMaterial != null)
        {
            debugObj.GetComponent<Renderer>().material = debugMaterial;
        }

        // Color based on quantum phase
        float hue = (cell.quantumPhase / (2 * Mathf.PI) + 1) % 1;
        debugObj.GetComponent<Renderer>().material.color = Color.HSVToRGB(hue, 0.8f, 1f);
    }

    // Public methods for external control
    public void RegenerateLevel()
    {
        ClearLevel();
        InitializeGenerator();
        GenerateLevel();
    }

    private void ClearLevel()
    {
        foreach (var room in instantiatedRooms.Values)
        {
            if (room != null)
            {
                Destroy(room);
            }
        }
        instantiatedRooms.Clear();
    }

    public Dictionary<Vector3Int, GameObject> GetInstantiatedRooms()
    {
        return instantiatedRooms;
    }

    // Editor visualization
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showDebugVisualization) return;

        var grid = wfcCore?.GetGrid();
        if (grid == null) return;

        foreach (var kvp in grid)
        {
            Vector3Int pos = kvp.Key;
            var cell = kvp.Value;

            Vector3 worldPos = gridSystem != null ? gridSystem.GridToWorld(pos) : pos;

            // Draw cell bounds
            Gizmos.color = cell.isCollapsed ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(worldPos + Vector3.one * 0.5f, Vector3.one);

            // Draw quantum phase indicator
            if (!cell.isCollapsed)
            {
                Gizmos.color = Color.HSVToRGB((cell.quantumPhase / (2 * Mathf.PI) + 1) % 1, 0.5f, 1f);
                Gizmos.DrawSphere(worldPos + Vector3.up * 1.2f, cell.superpositionAmplitude * 0.1f);
            }
        }
    }
}
