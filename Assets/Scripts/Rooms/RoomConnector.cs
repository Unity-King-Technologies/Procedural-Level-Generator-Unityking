using System.Collections.Generic;
using UnityEngine;

public static class RoomConnector
{
    /// <summary>
    /// Attempts to connect two room modules at specific positions
    /// </summary>
    public static bool CanConnect(RoomModule module1, RoomModule module2, RoomModule.Direction direction)
    {
        if (module1 == null || module2 == null)
            return false;

        return module1.ConnectsTo(module2, direction);
    }

    /// <summary>
    /// Finds all possible connection points for a room module
    /// </summary>
    public static List<RoomModule.Direction> GetConnectionPoints(RoomModule module)
    {
        List<RoomModule.Direction> connections = new List<RoomModule.Direction>();

        if (module == null || module.sockets == null)
            return connections;

        foreach (var socket in module.sockets)
        {
            if (socket.type == RoomModule.SocketType.Entrance ||
                socket.type == RoomModule.SocketType.Exit ||
                socket.type == RoomModule.SocketType.Connector)
            {
                connections.Add(socket.direction);
            }
        }

        return connections;
    }

    /// <summary>
    /// Gets compatible modules for a specific connection direction
    /// </summary>
    public static List<RoomModule> GetCompatibleModules(RoomModule sourceModule, List<RoomModule> allModules, RoomModule.Direction direction)
    {
        List<RoomModule> compatible = new List<RoomModule>();

        foreach (var module in allModules)
        {
            if (sourceModule.ConnectsTo(module, direction))
            {
                compatible.Add(module);
            }
        }

        return compatible;
    }

    /// <summary>
    /// Validates all connections in a generated level
    /// </summary>
    public static Dictionary<Vector3Int, List<ConnectionIssue>> ValidateConnections(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        Dictionary<Vector3Int, List<ConnectionIssue>> issues = new Dictionary<Vector3Int, List<ConnectionIssue>>();

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (!cell.isCollapsed)
                continue;

            var cellIssues = ValidateCellConnections(position, cell, grid);
            if (cellIssues.Count > 0)
            {
                issues[position] = cellIssues;
            }
        }

        return issues;
    }

    /// <summary>
    /// Validates connections for a single cell
    /// </summary>
    private static List<ConnectionIssue> ValidateCellConnections(Vector3Int position, WFCCore.Cell cell, Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        List<ConnectionIssue> issues = new List<ConnectionIssue>();

        Vector3Int[] directions = {
            Vector3Int.forward, Vector3Int.back,
            Vector3Int.left, Vector3Int.right,
            Vector3Int.up, Vector3Int.down
        };

        foreach (Vector3Int direction in directions)
        {
            Vector3Int neighborPos = position + direction;
            RoomModule.Direction connectionDir = VectorToDirection(direction);

            if (!grid.ContainsKey(neighborPos))
            {
                // Check if this direction should have a connection
                if (HasRequiredSocket(cell.collapsedModule, connectionDir))
                {
                    issues.Add(new ConnectionIssue
                    {
                        type = ConnectionIssueType.MissingNeighbor,
                        direction = connectionDir,
                        description = $"Required connection to {connectionDir} has no neighboring cell"
                    });
                }
                continue;
            }

            WFCCore.Cell neighbor = grid[neighborPos];

            if (!neighbor.isCollapsed)
            {
                issues.Add(new ConnectionIssue
                {
                    type = ConnectionIssueType.UncollapsedNeighbor,
                    direction = connectionDir,
                    description = $"Neighbor at {connectionDir} is not collapsed"
                });
                continue;
            }

            // Check socket compatibility
            if (!cell.collapsedModule.ConnectsTo(neighbor.collapsedModule, connectionDir))
            {
                issues.Add(new ConnectionIssue
                {
                    type = ConnectionIssueType.IncompatibleSockets,
                    direction = connectionDir,
                    description = $"Incompatible connection between {cell.collapsedModule.name} and {neighbor.collapsedModule.name} at {connectionDir}"
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Converts Vector3Int direction to RoomModule.Direction
    /// </summary>
    private static RoomModule.Direction VectorToDirection(Vector3Int dir)
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
    /// Checks if a module has a required socket in the given direction
    /// </summary>
    private static bool HasRequiredSocket(RoomModule module, RoomModule.Direction direction)
    {
        if (module == null || module.sockets == null)
            return false;

        foreach (var socket in module.sockets)
        {
            if (socket.direction == direction)
            {
                return socket.type == RoomModule.SocketType.Entrance ||
                       socket.type == RoomModule.SocketType.Exit ||
                       socket.type == RoomModule.SocketType.Connector;
            }
        }

        return false;
    }

    /// <summary>
    /// Calculates connection strength between two modules
    /// </summary>
    public static float CalculateConnectionStrength(RoomModule module1, RoomModule module2, RoomModule.Direction direction)
    {
        if (!module1.ConnectsTo(module2, direction))
            return 0f;

        // Find matching sockets
        var socket1 = GetSocket(module1, direction);
        var socket2 = GetSocket(module2, GetOppositeDirection(direction));

        if (socket1 == null || socket2 == null)
            return 0f;

        // Calculate compatibility score based on socket properties
        float typeCompatibility = (socket1.type == socket2.type) ? 1f : 0.5f;
        float idCompatibility = (socket1.id == socket2.id) ? 1f : 0.8f;
        float weightCompatibility = Mathf.Min(socket1.weight, socket2.weight) / Mathf.Max(socket1.weight, socket2.weight);

        return (typeCompatibility + idCompatibility + weightCompatibility) / 3f;
    }

    /// <summary>
    /// Gets a socket from a module in the specified direction
    /// </summary>
    private static RoomModule.Socket GetSocket(RoomModule module, RoomModule.Direction direction)
    {
        if (module == null || module.sockets == null)
            return null;

        foreach (var socket in module.sockets)
        {
            if (socket.direction == direction)
            {
                return socket;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the opposite direction
    /// </summary>
    private static RoomModule.Direction GetOppositeDirection(RoomModule.Direction direction)
    {
        switch (direction)
        {
            case RoomModule.Direction.North: return RoomModule.Direction.South;
            case RoomModule.Direction.South: return RoomModule.Direction.North;
            case RoomModule.Direction.East: return RoomModule.Direction.West;
            case RoomModule.Direction.West: return RoomModule.Direction.East;
            case RoomModule.Direction.Up: return RoomModule.Direction.Down;
            case RoomModule.Direction.Down: return RoomModule.Direction.Up;
            default: return direction;
        }
    }

    /// <summary>
    /// Finds connection paths between two points in the grid
    /// </summary>
    public static List<List<Vector3Int>> FindConnectionPaths(Vector3Int start, Vector3Int end, Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        List<List<Vector3Int>> paths = new List<List<Vector3Int>>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        List<Vector3Int> currentPath = new List<Vector3Int>();

        FindPathsRecursive(start, end, grid, visited, currentPath, paths, maxDepth: 20);

        return paths;
    }

    /// <summary>
    /// Recursive path finding
    /// </summary>
    private static void FindPathsRecursive(Vector3Int current, Vector3Int end, Dictionary<Vector3Int, WFCCore.Cell> grid,
        HashSet<Vector3Int> visited, List<Vector3Int> currentPath, List<List<Vector3Int>> allPaths, int maxDepth)
    {
        if (currentPath.Count > maxDepth)
            return;

        currentPath.Add(current);
        visited.Add(current);

        if (current == end)
        {
            // Found a path
            allPaths.Add(new List<Vector3Int>(currentPath));
        }
        else
        {
            // Explore neighbors
            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back,
                Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighbor = current + direction;

                if (grid.ContainsKey(neighbor) && !visited.Contains(neighbor))
                {
                    var neighborCell = grid[neighbor];
                    if (neighborCell.isCollapsed)
                    {
                        FindPathsRecursive(neighbor, end, grid, visited, currentPath, allPaths, maxDepth);
                    }
                }
            }
        }

        currentPath.RemoveAt(currentPath.Count - 1);
        visited.Remove(current);
    }

    /// <summary>
    /// Creates connection metadata for procedural content generation
    /// </summary>
    public static Dictionary<Vector3Int, ConnectionMetadata> GenerateConnectionMetadata(Dictionary<Vector3Int, WFCCore.Cell> grid)
    {
        Dictionary<Vector3Int, ConnectionMetadata> metadata = new Dictionary<Vector3Int, ConnectionMetadata>();

        foreach (var kvp in grid)
        {
            Vector3Int position = kvp.Key;
            WFCCore.Cell cell = kvp.Value;

            if (!cell.isCollapsed)
                continue;

            ConnectionMetadata cellMetadata = new ConnectionMetadata
            {
                position = position,
                module = cell.collapsedModule,
                connections = new List<ConnectionInfo>()
            };

            // Analyze connections
            Vector3Int[] directions = {
                Vector3Int.forward, Vector3Int.back,
                Vector3Int.left, Vector3Int.right,
                Vector3Int.up, Vector3Int.down
            };

            foreach (Vector3Int direction in directions)
            {
                Vector3Int neighborPos = position + direction;
                RoomModule.Direction connectionDir = VectorToDirection(direction);

                ConnectionInfo connectionInfo = new ConnectionInfo
                {
                    direction = connectionDir,
                    hasNeighbor = grid.ContainsKey(neighborPos),
                    isConnected = false,
                    connectionStrength = 0f
                };

                if (connectionInfo.hasNeighbor)
                {
                    var neighbor = grid[neighborPos];
                    if (neighbor.isCollapsed)
                    {
                        connectionInfo.isConnected = cell.collapsedModule.ConnectsTo(neighbor.collapsedModule, connectionDir);
                        connectionInfo.connectionStrength = CalculateConnectionStrength(cell.collapsedModule, neighbor.collapsedModule, connectionDir);
                        connectionInfo.neighborModule = neighbor.collapsedModule;
                    }
                }

                cellMetadata.connections.Add(connectionInfo);
            }

            // Calculate connectivity metrics
            cellMetadata.connectivityScore = CalculateConnectivityScore(cellMetadata);
            cellMetadata.isDeadEnd = cellMetadata.connections.Count(c => c.isConnected) <= 1;

            metadata[position] = cellMetadata;
        }

        return metadata;
    }

    /// <summary>
    /// Calculates a connectivity score for a cell
    /// </summary>
    private static float CalculateConnectivityScore(ConnectionMetadata metadata)
    {
        int connectedNeighbors = metadata.connections.Count(c => c.isConnected);
        float totalStrength = metadata.connections.Sum(c => c.connectionStrength);

        return connectedNeighbors * 0.5f + totalStrength * 0.5f;
    }
}

/// <summary>
/// Represents a connection issue found during validation
/// </summary>
public class ConnectionIssue
{
    public ConnectionIssueType type;
    public RoomModule.Direction direction;
    public string description;
}

public enum ConnectionIssueType
{
    MissingNeighbor,
    UncollapsedNeighbor,
    IncompatibleSockets,
    InvalidConnection
}

/// <summary>
/// Metadata about connections for a cell
/// </summary>
public class ConnectionMetadata
{
    public Vector3Int position;
    public RoomModule module;
    public List<ConnectionInfo> connections;
    public float connectivityScore;
    public bool isDeadEnd;
}

/// <summary>
/// Information about a single connection
/// </summary>
public class ConnectionInfo
{
    public RoomModule.Direction direction;
    public bool hasNeighbor;
    public bool isConnected;
    public float connectionStrength;
    public RoomModule neighborModule;
}
