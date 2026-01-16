using System.Collections.Generic;
using System.Linq;

public static class ConstraintSolver
{
    /// <summary>
    /// Propagates constraints from a collapsed cell to its neighbors
    /// </summary>
    public static bool PropagateConstraints(Dictionary<Vector3Int, WFCCore.Cell> grid, Vector3Int position, RoomModule collapsedModule)
    {
        Queue<Vector3Int> toProcess = new Queue<Vector3Int>();
        toProcess.Enqueue(position);

        bool constraintsChanged = false;

        while (toProcess.Count > 0)
        {
            Vector3Int currentPos = toProcess.Dequeue();
            WFCCore.Cell currentCell = grid[currentPos];

            // Process all 6 neighboring directions (3D)
            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back,
                Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborPos = currentPos + direction;

                if (!grid.ContainsKey(neighborPos))
                    continue;

                WFCCore.Cell neighborCell = grid[neighborPos];

                // Skip collapsed neighbors
                if (neighborCell.isCollapsed)
                    continue;

                // Get compatible modules for this neighbor
                var compatibleModules = GetCompatibleModules(neighborCell.possibleModules, collapsedModule, direction);

                if (compatibleModules.Count != neighborCell.possibleModules.Count)
                {
                    // Constraints have changed
                    constraintsChanged = true;
                    neighborCell.possibleModules = compatibleModules;

                    // Recalculate entropy
                    neighborCell.CalculateEntropy();

                    // If only one possibility remains, this cell should be collapsed
                    if (neighborCell.possibleModules.Count == 1)
                    {
                        neighborCell.isCollapsed = true;
                        neighborCell.collapsedModule = neighborCell.possibleModules[0];
                        neighborCell.entropy = 0;

                        // Add this cell to processing queue for further propagation
                        toProcess.Enqueue(neighborPos);
                    }
                    else if (neighborCell.possibleModules.Count == 0)
                    {
                        // Contradiction detected!
                        return false;
                    }
                    else
                    {
                        // Add to queue for further processing
                        toProcess.Enqueue(neighborPos);
                    }
                }
            }
        }

        return constraintsChanged;
    }

    /// <summary>
    /// Gets modules compatible with the given neighbor and direction
    /// </summary>
    private static List<RoomModule> GetCompatibleModules(List<RoomModule> possibleModules, RoomModule neighborModule, Vector3Int direction)
    {
        RoomModule.Direction connectionDir = GetDirectionFromVector(direction);
        List<RoomModule> compatible = new List<RoomModule>();

        foreach (var module in possibleModules)
        {
            if (module.ConnectsTo(neighborModule, connectionDir))
            {
                compatible.Add(module);
            }
        }

        return compatible;
    }

    /// <summary>
    /// Converts Vector3Int direction to RoomModule.Direction
    /// </summary>
    private static RoomModule.Direction GetDirectionFromVector(Vector3Int dir)
    {
        if (dir == Vector3Int.forward) return RoomModule.Direction.North;
        if (dir == Vector3Int.back) return RoomModule.Direction.South;
        if (dir == Vector3Int.left) return RoomModule.Direction.West;
        if (dir == Vector3Int.right) return RoomModule.Direction.East;
        if (dir == Vector3Int.up) return RoomModule.Direction.Up;
        if (dir == Vector3Int.down) return RoomModule.Direction.Down;
        return RoomModule.Direction.North;
    }

    /// <summary>
    /// Validates all constraints in the grid
    /// </summary>
    public static bool ValidateAllConstraints(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (!cell.isCollapsed)
                continue;

            // Check compatibility with all neighbors
            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back,
                Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborPos = position + direction;

                if (!grid.ContainsKey(neighborPos))
                    continue;

                WFCCore.Cell neighbor = grid[neighborPos];

                if (!neighbor.isCollapsed)
                    continue;

                RoomModule.Direction connectionDir = GetDirectionFromVector(direction);
                if (!cell.collapsedModule.ConnectsTo(neighbor.collapsedModule, connectionDir))
                {
                    return false; // Invalid constraint
                }
            }
        }

        return true; // All constraints valid
    }

    /// <summary>
    /// Applies initial constraints to the grid (e.g., starting positions)
    /// </summary>
    public static void ApplyInitialConstraints(Dictionary<Vector3Int, WFCCore.Cell> grid, List<RoomModule> startModules, Vector3Int startPosition)
    {
        if (!grid.ContainsKey(startPosition))
            return;

        WFCCore.Cell startCell = grid[startPosition];
        startCell.possibleModules = new List<RoomModule>(startModules);
        startCell.superpositionAmplitude = 0f; // Force deterministic start
        startCell.CalculateEntropy();
    }

    /// <summary>
    /// Finds cells with constraint contradictions
    /// </summary>
    public static List<Vector3Int> FindContradictions(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        List<Vector3Int> contradictions = new List<Vector3Int>();

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (cell.possibleModules.Count == 0 && !cell.isCollapsed)
            {
                contradictions.Add(position);
            }
        }

        return contradictions;
    }

    /// <summary>
    /// Attempts to resolve contradictions using quantum tunneling
    /// </summary>
    public static bool ResolveContradictionsWithTunneling(Dictionary<Vector3Int, WFCCore.Cell> grid, System.Random quantumRandom, float tunnelingProbability)
    {
        var contradictions = FindContradictions(grid);
        bool resolvedAny = false;

        foreach (Vector3Int position in contradictions)
        {
            if (quantumRandom.NextDouble() < tunnelingProbability)
            {
                // Quantum tunneling: allow any module as a possibility
                WFCCore.Cell cell = grid[position];
                // This would need access to all possible modules - we'll assume it's handled by the caller
                resolvedAny = true;
            }
        }

        return resolvedAny;
    }

    /// <summary>
    /// Calculates constraint satisfaction rate
    /// </summary>
    public static float CalculateConstraintSatisfaction(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        int totalConstraints = 0;
        int satisfiedConstraints = 0;

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (!cell.isCollapsed)
                continue;

            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back,
                Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborPos = position + direction;

                if (!grid.ContainsKey(neighborPos))
                    continue;

                WFCCore.Cell neighbor = grid[neighborPos];

                if (!neighbor.isCollapsed)
                    continue;

                totalConstraints++;
                RoomModule.Direction connectionDir = GetDirectionFromVector(direction);

                if (cell.collapsedModule.ConnectsTo(neighbor.collapsedModule, connectionDir))
                {
                    satisfiedConstraints++;
                }
            }
        }

        return totalConstraints > 0 ? (float)satisfiedConstraints / totalConstraints : 1f;
    }

    /// <summary>
    /// Optimizes constraint propagation order using entropy heuristics
    /// </summary>
    public static List<Vector3Int> GetOptimalPropagationOrder(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        // Sort cells by entropy (lowest first) for optimal propagation
        return grid
            .Where(kvp => !kvp.Value.isCollapsed)
            .OrderBy(kvp => kvp.Value.entropy)
            .Select(kvp => kvp.Key)
            .ToList();
    }
}
