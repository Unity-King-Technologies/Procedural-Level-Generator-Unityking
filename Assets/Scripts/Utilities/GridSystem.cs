using UnityEngine;

public class GridSystem
{
    private Vector3Int gridSize;
    private Vector3 cellSize = Vector3.one;
    private Vector3 origin = Vector3.zero;

    public GridSystem(Vector3Int size)
    {
        gridSize = size;
    }

    public GridSystem(Vector3Int size, Vector3 cellSize, Vector3 origin)
    {
        gridSize = size;
        this.cellSize = cellSize;
        this.origin = origin;
    }

    public Vector3 GridToWorld(Vector3Int gridPosition)
    {
        return origin + new Vector3(
            gridPosition.x * cellSize.x,
            gridPosition.y * cellSize.y,
            gridPosition.z * cellSize.z
        );
    }

    public Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - origin;
        return new Vector3Int(
            Mathf.RoundToInt(localPos.x / cellSize.x),
            Mathf.RoundToInt(localPos.y / cellSize.y),
            Mathf.RoundToInt(localPos.z / cellSize.z)
        );
    }

    public bool IsValidGridPosition(Vector3Int position)
    {
        return position.x >= 0 && position.x < gridSize.x &&
               position.y >= 0 && position.y < gridSize.y &&
               position.z >= 0 && position.z < gridSize.z;
    }

    public Vector3Int[] GetNeighbors(Vector3Int position, bool includeDiagonals = false)
    {
        var neighbors = new System.Collections.Generic.List<Vector3Int>();

        // Cardinal directions
        neighbors.Add(position + Vector3Int.forward);
        neighbors.Add(position + Vector3Int.back);
        neighbors.Add(position + Vector3Int.left);
        neighbors.Add(position + Vector3Int.right);
        neighbors.Add(position + Vector3Int.up);
        neighbors.Add(position + Vector3Int.down);

        if (includeDiagonals)
        {
            // Edge diagonals (12 total for 3D)
            neighbors.Add(position + new Vector3Int(1, 1, 0));
            neighbors.Add(position + new Vector3Int(1, -1, 0));
            neighbors.Add(position + new Vector3Int(-1, 1, 0));
            neighbors.Add(position + new Vector3Int(-1, -1, 0));
            neighbors.Add(position + new Vector3Int(1, 0, 1));
            neighbors.Add(position + new Vector3Int(1, 0, -1));
            neighbors.Add(position + new Vector3Int(-1, 0, 1));
            neighbors.Add(position + new Vector3Int(-1, 0, -1));
            neighbors.Add(position + new Vector3Int(0, 1, 1));
            neighbors.Add(position + new Vector3Int(0, 1, -1));
            neighbors.Add(position + new Vector3Int(0, -1, 1));
            neighbors.Add(position + new Vector3Int(0, -1, -1));

            // Corner diagonals (8 total for 3D)
            neighbors.Add(position + new Vector3Int(1, 1, 1));
            neighbors.Add(position + new Vector3Int(1, 1, -1));
            neighbors.Add(position + new Vector3Int(1, -1, 1));
            neighbors.Add(position + new Vector3Int(1, -1, -1));
            neighbors.Add(position + new Vector3Int(-1, 1, 1));
            neighbors.Add(position + new Vector3Int(-1, 1, -1));
            neighbors.Add(position + new Vector3Int(-1, -1, 1));
            neighbors.Add(position + new Vector3Int(-1, -1, -1));
        }

        // Filter valid positions
        return neighbors.FindAll(IsValidGridPosition).ToArray();
    }

    public float GetDistance(Vector3Int a, Vector3Int b)
    {
        return Vector3Int.Distance(a, b);
    }

    public Vector3Int GetGridSize()
    {
        return gridSize;
    }

    public Vector3 GetCellSize()
    {
        return cellSize;
    }

    public void SetCellSize(Vector3 newSize)
    {
        cellSize = newSize;
    }

    public Vector3 GetOrigin()
    {
        return origin;
    }

    public void SetOrigin(Vector3 newOrigin)
    {
        origin = newOrigin;
    }

    public Bounds GetGridBounds()
    {
        Vector3 min = GridToWorld(Vector3Int.zero);
        Vector3 max = GridToWorld(gridSize - Vector3Int.one) + cellSize;
        return new Bounds((min + max) * 0.5f, max - min);
    }

    public int GetTotalCellCount()
    {
        return gridSize.x * gridSize.y * gridSize.z;
    }

    public Vector3Int[] GetAllPositions()
    {
        var positions = new System.Collections.Generic.List<Vector3Int>();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    positions.Add(new Vector3Int(x, y, z));
                }
            }
        }
        return positions.ToArray();
    }
}
