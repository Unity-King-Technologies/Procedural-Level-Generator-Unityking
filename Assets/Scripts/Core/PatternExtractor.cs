using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PatternExtractor
{
    /// <summary>
    /// Extracts patterns from a completed level grid
    /// </summary>
    public static Dictionary<Vector3Int, Pattern> ExtractPatterns(Dictionary<Vector3Int, WFCCore.Cell> grid, int patternSize = 2)
    {
        Dictionary<Vector3Int, Pattern> patterns = new Dictionary<Vector3Int, Pattern>();

        // Find all valid pattern positions
        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            if (CanExtractPatternAt(grid, position, patternSize))
            {
                Pattern pattern = ExtractPatternAt(grid, position, patternSize);
                if (pattern != null)
                {
                    patterns[position] = pattern;
                }
            }
        }

        return patterns;
    }

    /// <summary>
    /// Checks if a pattern can be extracted at the given position
    /// </summary>
    private static bool CanExtractPatternAt(Dictionary<Vector3Int, WFCCore.Cell> grid, Vector3Int position, int patternSize)
    {
        for (int x = 0; x < patternSize; x++)
        {
            for (int y = 0; y < patternSize; y++)
            {
                for (int z = 0; z < patternSize; z++)
                {
                    Vector3Int checkPos = position + new Vector3Int(x, y, z);
                    if (!grid.ContainsKey(checkPos) || !grid[checkPos].isCollapsed)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Extracts a single pattern at the given position
    /// </summary>
    private static Pattern ExtractPatternAt(Dictionary<Vector3Int, WFCCore.Cell> grid, Vector3Int position, int patternSize)
    {
        Pattern pattern = new Pattern(patternSize);

        for (int x = 0; x < patternSize; x++)
        {
            for (int y = 0; y < patternSize; y++)
            {
                for (int z = 0; z < patternSize; z++)
                {
                    Vector3Int patternPos = position + new Vector3Int(x, y, z);
                    WFCCore.Cell cell = grid[patternPos];

                    pattern.SetModule(x, y, z, cell.collapsedModule);
                    pattern.SetPhase(x, y, z, cell.quantumPhase);
                }
            }
        }

        pattern.CalculateFrequencyWeight();
        return pattern;
    }

    /// <summary>
    /// Learns patterns from multiple example levels
    /// </summary>
    public static PatternDictionary LearnFromExamples(List<Dictionary<Vector3Int, WFCCore.Cell>> exampleLevels, int patternSize = 2)
    {
        PatternDictionary learnedPatterns = new PatternDictionary();

        foreach (var level in exampleLevels)
        {
            var levelPatterns = ExtractPatterns(level, patternSize);

            foreach (var pattern in levelPatterns.Values)
            {
                learnedPatterns.AddOrUpdatePattern(pattern);
            }
        }

        learnedPatterns.NormalizeWeights();
        return learnedPatterns;
    }

    /// <summary>
    /// Finds the most compatible pattern for a given position
    /// </summary>
    public static Pattern FindCompatiblePattern(Vector3Int position, Dictionary<Vector3Int, WFCCore.Cell> grid, PatternDictionary learnedPatterns)
    {
        // This would implement pattern matching logic
        // For now, return a random pattern
        return learnedPatterns.GetRandomPattern();
    }

    /// <summary>
    /// Validates pattern compatibility with neighboring patterns
    /// </summary>
    public static bool ValidatePatternCompatibility(Pattern pattern, Vector3Int position, Dictionary<Vector3Int, Pattern> placedPatterns, int patternSize)
    {
        // Check overlap with neighboring patterns
        Vector3Int[] directions = {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.up, Vector3Int.down
        };

        foreach (var direction in directions)
        {
            Vector3Int neighborPos = position + direction * patternSize;
            if (placedPatterns.ContainsKey(neighborPos))
            {
                Pattern neighborPattern = placedPatterns[neighborPos];
                if (!PatternsAreCompatible(pattern, neighborPattern, direction, patternSize))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if two patterns are compatible along their shared edge
    /// </summary>
    private static bool PatternsAreCompatible(Pattern pattern1, Pattern pattern2, Vector3Int direction, int patternSize)
    {
        // This would implement detailed edge compatibility checking
        // For now, assume patterns are compatible
        return true;
    }
}

/// <summary>
/// Represents a single pattern extracted from the grid
/// </summary>
public class Pattern
{
    private RoomModule[,,] modules;
    private float[,,] phases;
    private int size;
    private float frequencyWeight;
    private string hash;

    public Pattern(int patternSize)
    {
        size = patternSize;
        modules = new RoomModule[size, size, size];
        phases = new float[size, size, size];
        frequencyWeight = 1f;
        CalculateHash();
    }

    public void SetModule(int x, int y, int z, RoomModule module)
    {
        modules[x, y, z] = module;
        CalculateHash();
    }

    public void SetPhase(int x, int y, int z, float phase)
    {
        phases[x, y, z] = phase;
    }

    public RoomModule GetModule(int x, int y, int z)
    {
        return modules[x, y, z];
    }

    public float GetPhase(int x, int y, int z)
    {
        return phases[x, y, z];
    }

    public float GetFrequencyWeight()
    {
        return frequencyWeight;
    }

    public void SetFrequencyWeight(float weight)
    {
        frequencyWeight = weight;
    }

    public void CalculateFrequencyWeight()
    {
        // Weight based on pattern complexity and module variety
        int uniqueModules = 0;
        HashSet<RoomModule> uniqueSet = new HashSet<RoomModule>();

        foreach (var module in modules)
        {
            if (module != null && uniqueSet.Add(module))
            {
                uniqueModules++;
            }
        }

        frequencyWeight = uniqueModules > 0 ? uniqueModules * 0.1f : 0.1f;
    }

    public string GetHash()
    {
        return hash;
    }

    private void CalculateHash()
    {
        string hashString = "";
        foreach (var module in modules)
        {
            hashString += module != null ? module.GetInstanceID().ToString() : "null";
            hashString += ",";
        }
        hash = hashString.GetHashCode().ToString();
    }

    public override bool Equals(object obj)
    {
        if (obj is Pattern other)
        {
            return hash == other.hash;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return hash.GetHashCode();
    }
}

/// <summary>
/// Dictionary for storing and managing learned patterns
/// </summary>
public class PatternDictionary
{
    private Dictionary<string, Pattern> patterns;
    private Dictionary<string, float> weights;

    public PatternDictionary()
    {
        patterns = new Dictionary<string, Pattern>();
        weights = new Dictionary<string, float>();
    }

    public void AddOrUpdatePattern(Pattern pattern)
    {
        string hash = pattern.GetHash();

        if (patterns.ContainsKey(hash))
        {
            // Update frequency weight
            weights[hash] += pattern.GetFrequencyWeight();
        }
        else
        {
            patterns[hash] = pattern;
            weights[hash] = pattern.GetFrequencyWeight();
        }
    }

    public void NormalizeWeights()
    {
        float totalWeight = weights.Values.Sum();
        if (totalWeight > 0)
        {
            foreach (var key in weights.Keys.ToList())
            {
                weights[key] /= totalWeight;
            }
        }
    }

    public Pattern GetRandomPattern()
    {
        if (patterns.Count == 0)
            return null;

        float randomValue = UnityEngine.Random.value;
        float cumulative = 0f;

        foreach (var kvp in weights)
        {
            cumulative += kvp.Value;
            if (randomValue <= cumulative)
            {
                return patterns[kvp.Key];
            }
        }

        // Fallback
        return patterns.Values.First();
    }

    public Pattern GetPatternByHash(string hash)
    {
        return patterns.ContainsKey(hash) ? patterns[hash] : null;
    }

    public int GetPatternCount()
    {
        return patterns.Count;
    }

    public Dictionary<string, float> GetWeights()
    {
        return new Dictionary<string, float>(weights);
    }
}
