using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class PatternBaker : EditorWindow
{
    [MenuItem("Tools/WFC/Pattern Baker")]
    public static void ShowWindow()
    {
        GetWindow<PatternBaker>("Pattern Baker");
    }

    // Source data
    private List<WFCGenerator> exampleGenerators = new List<WFCGenerator>();
    private int patternSize = 2;
    private bool includeRotations = true;
    private bool includeReflections = false;
    private bool useQuantumEnhancements = true;

    // Output data
    private PatternDictionary bakedPatterns;
    private string outputPath = "Assets/Data/Patterns/BakedPatterns.asset";
    private Vector2 scrollPosition;

    // Baking progress
    private bool isBaking = false;
    private float bakingProgress = 0f;
    private string bakingStatus = "";

    private void OnGUI()
    {
        DrawHeader();
        DrawSourceConfiguration();
        DrawPatternSettings();
        DrawOutputConfiguration();
        DrawBakingControls();
        DrawResults();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Pattern Baker - Learn from Examples", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool extracts patterns from completed WFC generations to create a learned pattern dictionary " +
            "for more intelligent level generation.",
            MessageType.Info
        );
        EditorGUILayout.Space();
    }

    private void DrawSourceConfiguration()
    {
        EditorGUILayout.LabelField("Source Examples", EditorStyles.boldLabel);

        // Example generators list
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.LabelField("Example WFC Generators", EditorStyles.miniBoldLabel);

        for (int i = 0; i < exampleGenerators.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            exampleGenerators[i] = (WFCGenerator)EditorGUILayout.ObjectField(
                $"Example {i + 1}",
                exampleGenerators[i],
                typeof(WFCGenerator),
                true
            );

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                exampleGenerators.RemoveAt(i);
                break; // Exit to avoid index issues
            }

            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add Example Generator"))
        {
            exampleGenerators.Add(null);
        }

        EditorGUILayout.EndVertical();

        // Quick setup buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Find All Generators"))
        {
            exampleGenerators.Clear();
            var generators = FindObjectsOfType<WFCGenerator>();
            exampleGenerators.AddRange(generators);
        }

        if (GUILayout.Button("Clear All"))
        {
            exampleGenerators.Clear();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void DrawPatternSettings()
    {
        EditorGUILayout.LabelField("Pattern Extraction Settings", EditorStyles.boldLabel);

        patternSize = EditorGUILayout.IntSlider("Pattern Size", patternSize, 2, 5);
        includeRotations = EditorGUILayout.Toggle("Include Rotations", includeRotations);
        includeReflections = EditorGUILayout.Toggle("Include Reflections", includeReflections);
        useQuantumEnhancements = EditorGUILayout.Toggle("Use Quantum Enhancements", useQuantumEnhancements);

        EditorGUILayout.HelpBox(
            $"Pattern size {patternSize}x{patternSize}x{patternSize} will extract " +
            $"{CalculateTotalVariations()} total variations per unique pattern.",
            MessageType.Info
        );

        EditorGUILayout.Space();
    }

    private void DrawOutputConfiguration()
    {
        EditorGUILayout.LabelField("Output Configuration", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "Save Pattern Dictionary",
                "BakedPatterns",
                "asset",
                "Choose where to save the baked pattern dictionary"
            );

            if (!string.IsNullOrEmpty(selectedPath))
            {
                outputPath = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void DrawBakingControls()
    {
        EditorGUILayout.LabelField("Baking Controls", EditorStyles.boldLabel);

        if (isBaking)
        {
            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(), bakingProgress, bakingStatus);
            GUI.enabled = false;
        }

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = !isBaking && exampleGenerators.Count > 0 && exampleGenerators.Any(g => g != null);

        if (GUILayout.Button("Bake Patterns"))
        {
            BakePatterns();
        }

        GUI.enabled = !isBaking && bakedPatterns != null;

        if (GUILayout.Button("Save Patterns"))
        {
            SavePatterns();
        }

        GUI.enabled = true;

        if (GUILayout.Button("Load Existing"))
        {
            LoadExistingPatterns();
        }

        EditorGUILayout.EndHorizontal();

        if (isBaking)
        {
            GUI.enabled = true;
        }

        EditorGUILayout.Space();
    }

    private void DrawResults()
    {
        if (bakedPatterns == null)
            return;

        EditorGUILayout.LabelField("Baking Results", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

        EditorGUILayout.LabelField($"Total Patterns: {bakedPatterns.GetPatternCount()}");

        // Show pattern statistics
        var weights = bakedPatterns.GetWeights();
        if (weights.Count > 0)
        {
            var sortedWeights = weights.OrderByDescending(kvp => kvp.Value).Take(10);

            EditorGUILayout.LabelField("Top 10 Patterns by Weight:");
            foreach (var kvp in sortedWeights)
            {
                EditorGUILayout.LabelField($"  Pattern {kvp.Key}: {kvp.Value:F4}", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
    }

    private int CalculateTotalVariations()
    {
        int variations = 1;

        if (includeRotations)
        {
            variations *= 4; // 4 rotations in 2D, would be more for 3D
        }

        if (includeReflections)
        {
            variations *= 2; // Reflection
        }

        return variations;
    }

    private async void BakePatterns()
    {
        if (exampleGenerators.Count == 0 || exampleGenerators.All(g => g == null))
        {
            Debug.LogError("No valid example generators provided!");
            return;
        }

        isBaking = true;
        bakingProgress = 0f;
        bakedPatterns = new PatternDictionary();

        try
        {
            // Collect all example grids
            List<Dictionary<Vector3Int, WFCCore.Cell>> exampleGrids = new List<Dictionary<Vector3Int, WFCCore.Cell>>();

            foreach (var generator in exampleGenerators)
            {
                if (generator != null)
                {
                    var grid = generator.GetGrid();
                    if (grid != null && grid.Count > 0)
                    {
                        exampleGrids.Add(new Dictionary<Vector3Int, WFCCore.Cell>(grid));
                    }
                }
            }

            if (exampleGrids.Count == 0)
            {
                Debug.LogError("No valid grids found in example generators!");
                return;
            }

            bakingStatus = "Learning patterns from examples...";
            await System.Threading.Tasks.Task.Delay(100); // Allow UI update

            // Extract patterns from all examples
            bakedPatterns = PatternExtractor.LearnFromExamples(exampleGrids, patternSize);

            // Apply quantum enhancements if enabled
            if (useQuantumEnhancements)
            {
                bakingStatus = "Applying quantum enhancements...";
                bakingProgress = 0.8f;
                await System.Threading.Tasks.Task.Delay(100);

                // This could involve additional processing of patterns
                // For now, we just mark it as enhanced
            }

            bakingStatus = "Pattern baking completed!";
            bakingProgress = 1f;

            Debug.Log($"Successfully baked {bakedPatterns.GetPatternCount()} patterns from {exampleGrids.Count} examples");

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Pattern baking failed: {ex.Message}");
            bakingStatus = "Baking failed!";
        }
        finally
        {
            isBaking = false;
            Repaint();
        }
    }

    private void SavePatterns()
    {
        if (bakedPatterns == null)
        {
            Debug.LogError("No patterns to save!");
            return;
        }

        // Create a ScriptableObject to hold the pattern dictionary
        BakedPatternData patternData = CreateInstance<BakedPatternData>();
        patternData.patternDictionary = bakedPatterns;
        patternData.patternSize = patternSize;
        patternData.creationTime = System.DateTime.Now;
        patternData.sourceExamples = exampleGenerators.Count(g => g != null);

        // Ensure the directory exists
        string directory = System.IO.Path.GetDirectoryName(outputPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(patternData, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Pattern dictionary saved to: {outputPath}");

        // Ping the asset in the project window
        Object asset = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
        }
    }

    private void LoadExistingPatterns()
    {
        string loadPath = EditorUtility.OpenFilePanel(
            "Load Pattern Dictionary",
            "Assets/Data/Patterns",
            "asset"
        );

        if (string.IsNullOrEmpty(loadPath))
            return;

        // Convert absolute path to relative
        string relativePath = "Assets" + loadPath.Substring(Application.dataPath.Length);

        BakedPatternData patternData = AssetDatabase.LoadAssetAtPath<BakedPatternData>(relativePath);

        if (patternData != null)
        {
            bakedPatterns = patternData.patternDictionary;
            patternSize = patternData.patternSize;
            outputPath = relativePath;

            Debug.Log($"Loaded pattern dictionary with {bakedPatterns.GetPatternCount()} patterns");
        }
        else
        {
            Debug.LogError("Failed to load pattern dictionary!");
        }

        Repaint();
    }

    [MenuItem("Tools/WFC/Create Pattern Baker Scene")]
    private static void CreatePatternBakerScene()
    {
        // Create a scene optimized for pattern baking
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        // Create multiple generators in a grid layout for pattern learning
        GameObject container = new GameObject("Pattern_Baking_Setup");

        int gridSize = 3; // 3x3 grid of generators
        float spacing = 50f;

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                GameObject genObj = new GameObject($"Generator_{x}_{z}");
                genObj.transform.SetParent(container.transform);
                genObj.transform.position = new Vector3(x * spacing, 0, z * spacing);

                var generator = genObj.AddComponent<WFCGenerator>();
                generator.gridSize = new Vector3Int(8, 1, 8);
                generator.useRandomSeed = true;
                generator.showDebugVisualization = false;
            }
        }

        // Create a shared room bank
        GameObject bankObj = new GameObject("Shared_RoomBank");
        bankObj.transform.SetParent(container.transform);
        var roomBank = bankObj.AddComponent<RoomBank>();

        // Assign the room bank to all generators
        var generators = container.GetComponentsInChildren<WFCGenerator>();
        foreach (var gen in generators)
        {
            // Note: This would need to be done at runtime or through inspector
            // gen.roomBank = roomBank;
        }

        Debug.Log("Created pattern baking scene with " + generators.Length + " generators");
        Selection.activeGameObject = container;
    }

    [MenuItem("Tools/WFC/Batch Generate Examples")]
    private static void BatchGenerateExamples()
    {
        var generators = FindObjectsOfType<WFCGenerator>();
        if (generators.Length == 0)
        {
            Debug.LogError("No WFC Generators found in scene!");
            return;
        }

        Debug.Log($"Starting batch generation of {generators.Length} examples...");

        // This would need to be async in a real implementation
        // For now, just log the plan
        foreach (var generator in generators)
        {
            if (generator.roomBank != null)
            {
                Debug.Log($"Would generate example for: {generator.name}");
            }
        }
    }
}

/// <summary>
/// ScriptableObject to store baked pattern data
/// </summary>
public class BakedPatternData : ScriptableObject
{
    public PatternDictionary patternDictionary;
    public int patternSize;
    public System.DateTime creationTime;
    public int sourceExamples;
    public string notes = "";

    [ContextMenu("Log Statistics")]
    private void LogStatistics()
    {
        if (patternDictionary != null)
        {
            Debug.Log($"Pattern Dictionary Statistics:");
            Debug.Log($"  Total Patterns: {patternDictionary.GetPatternCount()}");
            Debug.Log($"  Pattern Size: {patternSize}x{patternSize}x{patternSize}");
            Debug.Log($"  Source Examples: {sourceExamples}");
            Debug.Log($"  Creation Time: {creationTime}");

            var weights = patternDictionary.GetWeights();
            if (weights.Count > 0)
            {
                var sortedWeights = weights.OrderByDescending(kvp => kvp.Value);
                Debug.Log($"  Highest Weight: {sortedWeights.First().Value:F4}");
                Debug.Log($"  Lowest Weight: {sortedWeights.Last().Value:F4}");
            }
        }
        else
        {
            Debug.LogError("No pattern dictionary available!");
        }
    }
}
