using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WFCCore
{
    private class Cell
    {
        public Vector3Int position;
        public List<RoomModule> possibleModules;
        public bool isCollapsed = false;
        public RoomModule collapsedModule;
        public float entropy;
        public float quantumPhase = 0f; // Quantum-inspired phase for interference patterns
        public float superpositionAmplitude = 1f; // Quantum superposition strength

        public void CalculateEntropy()
        {
            if (isCollapsed)
            {
                entropy = 0;
                return;
            }

            float totalWeight = possibleModules.Sum(m => m.baseWeight);
            float entropySum = 0;

            foreach (var module in possibleModules)
            {
                float probability = module.baseWeight / totalWeight;
                entropySum -= probability * Mathf.Log(probability);
            }

            // Quantum entropy modification - superposition states have higher effective entropy
            entropy = entropySum * (1 + superpositionAmplitude * 0.5f);
        }
    }

    private Dictionary<Vector3Int, Cell> grid;
    private Vector3Int gridSize;
    private float quantumCoherence = 0.8f; // How long superposition states are maintained
    private System.Random quantumRandom; // Separate random for quantum effects

    public WFCCore(Vector3Int size)
    {
        gridSize = size;
        quantumRandom = new System.Random();
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        grid = new Dictionary<Vector3Int, Cell>();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    Cell cell = new Cell
                    {
                        position = pos,
                        possibleModules = new List<RoomModule>(),
                        quantumPhase = (float)quantumRandom.NextDouble() * 2 * Mathf.PI
                    };
                    grid[pos] = cell;
                }
            }
        }
    }

    public void InitializeSuperpositions(List<RoomModule> allModules)
    {
        foreach (var cell in grid.Values)
        {
            cell.possibleModules = new List<RoomModule>(allModules);
            // Quantum initialization - some cells maintain higher superposition
            cell.superpositionAmplitude = quantumCoherence * (0.5f + 0.5f * Mathf.Sin(cell.quantumPhase));
            cell.CalculateEntropy();
        }
    }

    public Vector3Int GetLowestEntropyCell()
    {
        float minEntropy = float.MaxValue;
        Vector3Int result = Vector3Int.one * -1;

        foreach (var cell in grid.Values)
        {
            if (!cell.isCollapsed && cell.entropy < minEntropy)
            {
                minEntropy = cell.entropy;
                result = cell.position;
            }
        }

        // Quantum tunneling - occasionally allow "impossible" collapses for emergent behavior
        if (result == Vector3Int.one * -1 && quantumRandom.NextDouble() < 0.1f)
        {
            // Find a cell with quantum tunneling potential
            var uncollapsedCells = grid.Values.Where(c => !c.isCollapsed).ToList();
            if (uncollapsedCells.Count > 0)
            {
                result = uncollapsedCells[quantumRandom.Next(uncollapsedCells.Count)].position;
            }
        }

        return result;
    }

    public RoomModule CollapseCell(Vector3Int position)
    {
        var cell = grid[position];

        if (cell.possibleModules.Count == 0)
            return null;

        // Quantum-weighted random selection with interference patterns
        float totalWeight = cell.possibleModules.Sum(m =>
        {
            // Apply quantum interference based on neighboring cells
            float interference = CalculateQuantumInterference(position, m);
            return m.baseWeight * (1 + interference);
        });

        float randomValue = (float)quantumRandom.NextDouble() * totalWeight;
        float cumulative = 0;

        foreach (var module in cell.possibleModules)
        {
            float interference = CalculateQuantumInterference(position, module);
            float effectiveWeight = module.baseWeight * (1 + interference);
            cumulative += effectiveWeight;

            if (randomValue <= cumulative)
            {
                cell.isCollapsed = true;
                cell.collapsedModule = module;
                cell.possibleModules = new List<RoomModule> { module };
                cell.superpositionAmplitude = 0; // Collapse superposition
                cell.entropy = 0;
                return module;
            }
        }

        return null;
    }

    private float CalculateQuantumInterference(Vector3Int position, RoomModule module)
    {
        float interference = 0f;
        Vector3Int[] directions = {
            Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right,
            Vector3Int.up, Vector3Int.down
        };

        foreach (var dir in directions)
        {
            Vector3Int neighborPos = position + dir;
            if (grid.ContainsKey(neighborPos))
            {
                var neighbor = grid[neighborPos];
                if (neighbor.isCollapsed)
                {
                    // Quantum interference based on compatibility and phase difference
                    bool compatible = module.ConnectsTo(neighbor.collapsedModule,
                        GetDirectionFromVector(dir));
                    float phaseDiff = Mathf.Abs(cell.quantumPhase - neighbor.quantumPhase);
                    float phaseFactor = Mathf.Cos(phaseDiff);

                    interference += compatible ? phaseFactor * 0.1f : -phaseFactor * 0.1f;
                }
            }
        }

        return interference;
    }

    private RoomModule.Direction GetDirectionFromVector(Vector3Int dir)
    {
        if (dir == Vector3Int.forward) return RoomModule.Direction.North;
        if (dir == Vector3Int.back) return RoomModule.Direction.South;
        if (dir == Vector3Int.left) return RoomModule.Direction.West;
        if (dir == Vector3Int.right) return RoomModule.Direction.East;
        if (dir == Vector3Int.up) return RoomModule.Direction.Up;
        if (dir == Vector3Int.down) return RoomModule.Direction.Down;
        return RoomModule.Direction.North;
    }

    public bool IsFullyCollapsed()
    {
        return grid.Values.All(cell => cell.isCollapsed);
    }

    public void PropagateConstraints(Vector3Int position, RoomModule collapsedModule)
    {
        Queue<Vector3Int> toUpdate = new Queue<Vector3Int>();
        toUpdate.Enqueue(position);

        while (toUpdate.Count > 0)
        {
            Vector3Int current = toUpdate.Dequeue();
            var currentCell = grid[current];

            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (var dir in directions)
            {
                Vector3Int neighborPos = current + dir;
                if (grid.ContainsKey(neighborPos))
                {
                    var neighbor = grid[neighborPos];
                    if (!neighbor.isCollapsed)
                    {
                        // Quantum constraint propagation with decoherence
                        List<RoomModule> compatibleModules = new List<RoomModule>();

                        foreach (var module in neighbor.possibleModules)
                        {
                            RoomModule.Direction connectionDir = GetDirectionFromVector(dir);
                            if (collapsedModule.ConnectsTo(module, GetOppositeDirection(connectionDir)))
                            {
                                compatibleModules.Add(module);
                            }
                        }

                        if (compatibleModules.Count != neighbor.possibleModules.Count)
                        {
                            neighbor.possibleModules = compatibleModules;
                            neighbor.superpositionAmplitude *= quantumCoherence; // Decoherence effect
                            neighbor.CalculateEntropy();

                            if (neighbor.possibleModules.Count == 0)
                            {
                                // Quantum tunneling - allow impossible states with low probability
                                if (quantumRandom.NextDouble() < 0.05f)
                                {
                                    neighbor.possibleModules = new List<RoomModule> { collapsedModule };
                                }
                            }

                            if (neighbor.possibleModules.Count > 0)
                            {
                                toUpdate.Enqueue(neighborPos);
                            }
                        }
                    }
                }
            }
        }
    }

    private RoomModule.Direction GetOppositeDirection(RoomModule.Direction dir)
    {
        switch (dir)
        {
            case RoomModule.Direction.North: return RoomModule.Direction.South;
            case RoomModule.Direction.South: return RoomModule.Direction.North;
            case RoomModule.Direction.East: return RoomModule.Direction.West;
            case RoomModule.Direction.West: return RoomModule.Direction.East;
            case RoomModule.Direction.Up: return RoomModule.Direction.Down;
            case RoomModule.Direction.Down: return RoomModule.Direction.Up;
            default: return dir;
        }
    }

    public Dictionary<Vector3Int, Cell> GetGrid() => grid;
}
