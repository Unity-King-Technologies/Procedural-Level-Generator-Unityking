using System.Collections.Generic;
using System.Linq;

public static class EntropyCalculator
{
    /// <summary>
    /// Calculates Shannon entropy for a set of possibilities
    /// </summary>
    public static float CalculateShannonEntropy(List<RoomModule> modules)
    {
        if (modules == null || modules.Count == 0)
            return 0f;

        if (modules.Count == 1)
            return 0f; // No uncertainty with single possibility

        float totalWeight = modules.Sum(m => m.baseWeight);
        if (totalWeight == 0)
            return 0f;

        float entropy = 0f;

        foreach (var module in modules)
        {
            float probability = module.baseWeight / totalWeight;
            if (probability > 0)
            {
                entropy -= probability * Mathf.Log(probability, 2f); // Log base 2 for bits
            }
        }

        return entropy;
    }

    /// <summary>
    /// Calculates quantum-enhanced entropy with superposition effects
    /// </summary>
    public static float CalculateQuantumEntropy(List<RoomModule> modules, float superpositionAmplitude, float quantumPhase)
    {
        float baseEntropy = CalculateShannonEntropy(modules);

        // Quantum enhancement: superposition states contribute additional entropy
        float quantumUncertainty = superpositionAmplitude * Mathf.PI * 0.5f;

        // Phase-based entropy modulation
        float phaseModulation = Mathf.Abs(Mathf.Sin(quantumPhase)) * 0.3f;

        return baseEntropy * (1 + quantumUncertainty + phaseModulation);
    }

    /// <summary>
    /// Finds the cell with minimum entropy (most constrained)
    /// </summary>
    public static WFCCore.Cell FindMinimumEntropyCell(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        WFCCore.Cell minEntropyCell = null;
        float minEntropy = float.MaxValue;

        foreach (var cell in grid.Values)
        {
            if (!cell.isCollapsed && cell.entropy < minEntropy)
            {
                minEntropy = cell.entropy;
                minEntropyCell = cell;
            }
        }

        return minEntropyCell;
    }

    /// <summary>
    /// Calculates entropy reduction after constraint propagation
    /// </summary>
    public static float CalculateEntropyReduction(WFCCore.Cell before, WFCCore.Cell after)
    {
        return before.entropy - after.entropy;
    }

    /// <summary>
    /// Checks if entropy is effectively zero (cell is practically collapsed)
    /// </summary>
    public static bool IsEntropyEffectivelyZero(float entropy, float threshold = 0.01f)
    {
        return entropy < threshold;
    }

    /// <summary>
    /// Calculates the entropy gradient across the grid
    /// </summary>
    public static Dictionary<Vector3Int, float> CalculateEntropyGradient(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        var gradient = new Dictionary<Vector3Int, float>();

        foreach (var kvp in grid)
        {
            Vector3Int pos = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (cell.isCollapsed)
            {
                gradient[pos] = 0f;
                continue;
            }

            // Calculate local entropy gradient
            float localGradient = 0f;
            int neighborCount = 0;

            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (var dir in directions)
            {
                Vector3Int neighborPos = pos + dir;
                if (grid.ContainsKey(neighborPos))
                {
                    var neighbor = grid[neighborPos];
                    if (!neighbor.isCollapsed)
                    {
                        localGradient += Mathf.Abs(cell.entropy - neighbor.entropy);
                        neighborCount++;
                    }
                }
            }

            gradient[pos] = neighborCount > 0 ? localGradient / neighborCount : 0f;
        }

        return gradient;
    }
}
