using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WFCDebugger : EditorWindow
{
    [MenuItem("Tools/WFC/WFC Debugger")]
    public static void ShowWindow()
    {
        GetWindow<WFCDebugger>("WFC Debugger");
    }

    private WFCGenerator selectedGenerator;
    private Vector2 scrollPosition;
    private bool showGridVisualization = true;
    private bool showEntropyHeatmap = false;
    private bool showConstraintWaves = false;
    private bool showQuantumInterference = true;
    private bool autoRefresh = true;
    private float refreshInterval = 1f;
    private float lastRefreshTime;

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Clear visualizations when entering/exiting play mode
        if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitedEditMode)
        {
            ClearVisualizations();
        }
    }

    private void Update()
    {
        if (autoRefresh && EditorApplication.isPlaying && Time.time - lastRefreshTime >= refreshInterval)
        {
            Repaint();
            lastRefreshTime = Time.time;
        }
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawGeneratorSelection();
        DrawVisualizationControls();
        DrawDebugInformation();
        DrawActionButtons();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Quantum-Inspired WFC Debugger", EditorStyles.boldLabel);
        EditorGUILayout.Space();
    }

    private void DrawGeneratorSelection()
    {
        EditorGUILayout.LabelField("Generator Selection", EditorStyles.boldLabel);

        selectedGenerator = (WFCGenerator)EditorGUILayout.ObjectField(
            "WFC Generator",
            selectedGenerator,
            typeof(WFCGenerator),
            true
        );

        if (selectedGenerator == null)
        {
            EditorGUILayout.HelpBox("Select a WFC Generator to debug", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Grid Size: {selectedGenerator.gridSize}");
        EditorGUILayout.LabelField($"Seed: {selectedGenerator.seed}");
        EditorGUILayout.Toggle("Use Random Seed", selectedGenerator.useRandomSeed);
        EditorGUILayout.IntField("Max Attempts", selectedGenerator.maxAttempts);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quantum Settings", EditorStyles.boldLabel);
        EditorGUILayout.Slider("Coherence", selectedGenerator.quantumCoherence, 0f, 1f);
        EditorGUILayout.Slider("Tunneling", selectedGenerator.tunnelingProbability, 0f, 1f);

        EditorGUILayout.Space();
    }

    private void DrawVisualizationControls()
    {
        if (selectedGenerator == null)
            return;

        EditorGUILayout.LabelField("Visualization Controls", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        showGridVisualization = EditorGUILayout.Toggle("Grid Visualization", showGridVisualization);
        showEntropyHeatmap = EditorGUILayout.Toggle("Entropy Heatmap", showEntropyHeatmap);
        showConstraintWaves = EditorGUILayout.Toggle("Constraint Waves", showConstraintWaves);
        showQuantumInterference = EditorGUILayout.Toggle("Quantum Interference", showQuantumInterference);

        EditorGUILayout.Space();
        autoRefresh = EditorGUILayout.Toggle("Auto Refresh", autoRefresh);
        if (autoRefresh)
        {
            refreshInterval = EditorGUILayout.Slider("Refresh Interval", refreshInterval, 0.1f, 5f);
        }

        if (EditorGUI.EndChangeCheck())
        {
            UpdateVisualizations();
        }

        EditorGUILayout.Space();
    }

    private void DrawDebugInformation()
    {
        if (selectedGenerator == null || !EditorApplication.isPlaying)
            return;

        EditorGUILayout.LabelField("Debug Information", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        var grid = selectedGenerator.GetGrid();
        if (grid != null)
        {
            EditorGUILayout.LabelField($"Grid Cells: {grid.Count}");

            int collapsedCount = 0;
            int contradictionCount = 0;
            float averageEntropy = 0f;

            foreach (var cell in grid.Values)
            {
                if (cell.isCollapsed)
                    collapsedCount++;
                else if (cell.possibleModules.Count == 0)
                    contradictionCount++;
                else
                    averageEntropy += cell.entropy;
            }

            int uncollapsedCount = grid.Count - collapsedCount - contradictionCount;
            if (uncollapsedCount > 0)
                averageEntropy /= uncollapsedCount;

            EditorGUILayout.LabelField($"Collapsed: {collapsedCount}");
            EditorGUILayout.LabelField($"Uncollapsed: {uncollapsedCount}");
            EditorGUILayout.LabelField($"Contradictions: {contradictionCount}");
            EditorGUILayout.LabelField($"Avg Entropy: {averageEntropy:F3}");

            // Generation progress
            float progress = (float)collapsedCount / grid.Count;
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), progress, $"{progress:P1} Complete");

            // Constraint satisfaction
            float satisfactionRate = ConstraintSolver.CalculateConstraintSatisfaction(grid);
            EditorGUILayout.LabelField($"Constraint Satisfaction: {satisfactionRate:P1}");

            EditorGUILayout.Space();

            // Entropy statistics
            var entropyGradient = EntropyCalculator.CalculateEntropyGradient(grid);
            if (entropyGradient.Count > 0)
            {
                float minGradient = float.MaxValue;
                float maxGradient = float.MinValue;
                float avgGradient = 0f;

                foreach (var gradient in entropyGradient.Values)
                {
                    minGradient = Mathf.Min(minGradient, gradient);
                    maxGradient = Mathf.Max(maxGradient, gradient);
                    avgGradient += gradient;
                }
                avgGradient /= entropyGradient.Count;

                EditorGUILayout.LabelField($"Entropy Gradient - Min: {minGradient:F3}, Avg: {avgGradient:F3}, Max: {maxGradient:F3}");
            }
        }
        else
        {
            EditorGUILayout.LabelField("No grid data available");
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();
    }

    private void DrawActionButtons()
    {
        if (selectedGenerator == null)
            return;

        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Generate Level"))
        {
            GenerateLevel();
        }

        if (GUILayout.Button("Regenerate"))
        {
            RegenerateLevel();
        }

        if (GUILayout.Button("Clear Visualizations"))
        {
            ClearVisualizations();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Step Generation"))
        {
            // This would require modifying WFCGenerator to support stepping
            Debug.Log("Step generation not implemented in this version");
        }

        if (GUILayout.Button("Export Debug Data"))
        {
            ExportDebugData();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private async void GenerateLevel()
    {
        if (selectedGenerator == null)
            return;

        Debug.Log("Starting level generation from debugger...");
        bool success = await selectedGenerator.GenerateLevel();

        if (success)
        {
            Debug.Log("Level generation completed successfully!");
            UpdateVisualizations();
        }
        else
        {
            Debug.LogError("Level generation failed!");
        }
    }

    private void RegenerateLevel()
    {
        if (selectedGenerator == null)
            return;

        selectedGenerator.RegenerateLevel();
        UpdateVisualizations();
    }

    private void UpdateVisualizations()
    {
        if (selectedGenerator == null || !EditorApplication.isPlaying)
            return;

        ClearVisualizations();

        var grid = selectedGenerator.GetGrid();
        if (grid == null)
            return;

        if (showGridVisualization)
        {
            VisualizationTools.VisualizeWFCGrid(grid, selectedGenerator.transform);
        }

        if (showEntropyHeatmap)
        {
            VisualizationTools.VisualizeEntropyHeatmap(grid, selectedGenerator.transform);
        }

        if (showQuantumInterference)
        {
            VisualizationTools.VisualizeQuantumInterference(grid, selectedGenerator.transform);
        }

        // Constraint waves would need a specific position to visualize from
        if (showConstraintWaves && grid.Count > 0)
        {
            // Find a recently collapsed cell as the wave origin
            WFCCore.Cell waveOrigin = null;
            foreach (var cell in grid.Values)
            {
                if (cell.isCollapsed)
                {
                    waveOrigin = cell;
                    break;
                }
            }

            if (waveOrigin != null)
            {
                VisualizationTools.VisualizeConstraintPropagation(waveOrigin.position, grid, selectedGenerator.transform);
            }
        }
    }

    private void ClearVisualizations()
    {
        if (selectedGenerator == null)
            return;

        // Find and destroy all visualization objects
        Transform debugContainer = selectedGenerator.transform.Find("WFC_Debug_Visualizations");
        if (debugContainer != null)
        {
            DestroyImmediate(debugContainer.gameObject);
        }

        // Also clean up other visualization containers
        string[] containerNames = {
            "WFC_Visualization",
            "Entropy_Heatmap",
            "Constraint_Waves",
            "Quantum_Interference"
        };

        foreach (string containerName in containerNames)
        {
            GameObject container = GameObject.Find(containerName);
            if (container != null)
            {
                DestroyImmediate(container);
            }
        }
    }

    private void ExportDebugData()
    {
        if (selectedGenerator == null)
            return;

        string fileName = $"WFC_Debug_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt";
        string path = EditorUtility.SaveFilePanel("Export Debug Data", "", fileName, "txt");

        if (string.IsNullOrEmpty(path))
            return;

        var grid = selectedGenerator.GetGrid();
        if (grid == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine("WFC Debug Data Export");
        sb.AppendLine($"Timestamp: {System.DateTime.Now}");
        sb.AppendLine($"Generator: {selectedGenerator.name}");
        sb.AppendLine($"Grid Size: {selectedGenerator.gridSize}");
        sb.AppendLine($"Seed: {selectedGenerator.seed}");
        sb.AppendLine($"Quantum Coherence: {selectedGenerator.quantumCoherence}");
        sb.AppendLine($"Tunneling Probability: {selectedGenerator.tunnelingProbability}");
        sb.AppendLine();

        sb.AppendLine("Grid Data:");
        foreach (var kvp in grid)
        {
            var cell = kvp.Value;
            sb.AppendLine($"Position {kvp.Key}: Collapsed={cell.isCollapsed}, Entropy={cell.entropy:F3}, " +
                        $"Possibilities={cell.possibleModules.Count}, Phase={cell.quantumPhase:F2}, " +
                        $"Amplitude={cell.superpositionAmplitude:F2}");
        }

        sb.AppendLine();
        sb.AppendLine("Constraint Analysis:");
        var issues = RoomConnector.ValidateConnections(grid);
        sb.AppendLine($"Found {issues.Count} connection issues:");

        foreach (var issue in issues)
        {
            sb.AppendLine($"  Position {issue.Key}: {issue.Value.Count} issues");
            foreach (var connectionIssue in issue.Value)
            {
                sb.AppendLine($"    - {connectionIssue.description}");
            }
        }

        System.IO.File.WriteAllText(path, sb.ToString());
        Debug.Log($"Debug data exported to: {path}");
    }

    [MenuItem("Tools/WFC/Create Debug Generator")]
    private static void CreateDebugGenerator()
    {
        GameObject debugObj = new GameObject("WFC_Debug_Generator");
        debugObj.AddComponent<WFCGenerator>();

        // Set some debug-friendly defaults
        WFCGenerator generator = debugObj.GetComponent<WFCGenerator>();
        generator.gridSize = new Vector3Int(5, 1, 5);
        generator.showDebugVisualization = true;

        Selection.activeGameObject = debugObj;
        Debug.Log("Created debug WFC Generator. Add a RoomBank to get started.");
    }

    [MenuItem("Tools/WFC/Open Documentation")]
    private static void OpenDocumentation()
    {
        string docPath = "Documentation/WFC_Implementation.md";
        if (System.IO.File.Exists(docPath))
        {
            System.Diagnostics.Process.Start(docPath);
        }
        else
        {
            Debug.Log("Documentation file not found. Make sure WFC_Implementation.md exists in the Documentation folder.");
        }
    }
}
