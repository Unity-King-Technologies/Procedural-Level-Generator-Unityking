using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RoomData", menuName = "WFC/Room Data")]
public class RoomData : ScriptableObject
{
    [System.Serializable]
    public class RoomStats
    {
        public string roomName;
        public RoomType roomType;
        public Vector3Int dimensions = Vector3Int.one;
        public float baseDifficulty = 1f;
        public List<string> tags = new List<string>();
        public List<SocketData> sockets = new List<SocketData>();
        public List<PropPlacement> propPlacements = new List<PropPlacement>();
        public AudioClip ambientSound;
        public float lightIntensity = 1f;
        public Color lightColor = Color.white;
    }

    [System.Serializable]
    public class SocketData
    {
        public RoomModule.Direction direction;
        public RoomModule.SocketType socketType;
        public string socketId;
        public int weight = 1;
        public bool isRequired = true;
    }

    [System.Serializable]
    public class PropPlacement
    {
        public string propId;
        public Vector3 localPosition;
        public Vector3 localRotation;
        public Vector3 localScale = Vector3.one;
        public float spawnProbability = 1f;
    }

    public enum RoomType
    {
        Basic,
        Special,
        Boss,
        Connector,
        DeadEnd,
        Start,
        End
    }

    public List<RoomStats> roomStats = new List<RoomStats>();

    // Runtime data
    private Dictionary<string, RoomStats> roomStatsLookup;

    private void OnEnable()
    {
        BuildLookupTable();
    }

    private void BuildLookupTable()
    {
        roomStatsLookup = new Dictionary<string, RoomStats>();
        foreach (var stats in roomStats)
        {
            if (!roomStatsLookup.ContainsKey(stats.roomName))
            {
                roomStatsLookup[stats.roomName] = stats;
            }
        }
    }

    public RoomStats GetRoomStats(string roomName)
    {
        if (roomStatsLookup == null)
        {
            BuildLookupTable();
        }

        return roomStatsLookup.ContainsKey(roomName) ? roomStatsLookup[roomName] : null;
    }

    public List<RoomStats> GetRoomsByType(RoomType roomType)
    {
        return roomStats.FindAll(stats => stats.roomType == roomType);
    }

    public List<RoomStats> GetRoomsByTag(string tag)
    {
        return roomStats.FindAll(stats => stats.tags.Contains(tag));
    }

    public List<RoomStats> GetRoomsInDifficultyRange(float minDifficulty, float maxDifficulty)
    {
        return roomStats.FindAll(stats =>
            stats.baseDifficulty >= minDifficulty && stats.baseDifficulty <= maxDifficulty);
    }

    public void AddRoomStats(RoomStats stats)
    {
        if (!roomStats.Exists(s => s.roomName == stats.roomName))
        {
            roomStats.Add(stats);
            if (roomStatsLookup != null)
            {
                roomStatsLookup[stats.roomName] = stats;
            }
        }
    }

    public void RemoveRoomStats(string roomName)
    {
        roomStats.RemoveAll(stats => stats.roomName == roomName);
        if (roomStatsLookup != null)
        {
            roomStatsLookup.Remove(roomName);
        }
    }

    public void UpdateRoomStats(RoomStats updatedStats)
    {
        int index = roomStats.FindIndex(stats => stats.roomName == updatedStats.roomName);
        if (index >= 0)
        {
            roomStats[index] = updatedStats;
            if (roomStatsLookup != null)
            {
                roomStatsLookup[updatedStats.roomName] = updatedStats;
            }
        }
    }

    // Validation methods
    public List<string> ValidateRoomData()
    {
        List<string> errors = new List<string>();

        foreach (var stats in roomStats)
        {
            if (string.IsNullOrEmpty(stats.roomName))
            {
                errors.Add("Room with empty name found");
            }

            if (stats.dimensions.x <= 0 || stats.dimensions.y <= 0 || stats.dimensions.z <= 0)
            {
                errors.Add($"Room '{stats.roomName}' has invalid dimensions");
            }

            // Check for duplicate socket directions
            HashSet<RoomModule.Direction> directions = new HashSet<RoomModule.Direction>();
            foreach (var socket in stats.sockets)
            {
                if (!directions.Add(socket.direction))
                {
                    errors.Add($"Room '{stats.roomName}' has duplicate socket direction: {socket.direction}");
                }
            }
        }

        return errors;
    }

    // Utility methods
    public RoomStats CreateDefaultRoomStats(string roomName, RoomType roomType = RoomType.Basic)
    {
        return new RoomStats
        {
            roomName = roomName,
            roomType = roomType,
            dimensions = Vector3Int.one,
            baseDifficulty = 1f,
            tags = new List<string>(),
            sockets = new List<SocketData>(),
            propPlacements = new List<PropPlacement>(),
            lightIntensity = 1f,
            lightColor = Color.white
        };
    }

    public SocketData CreateDefaultSocket(RoomModule.Direction direction, RoomModule.SocketType socketType)
    {
        return new SocketData
        {
            direction = direction,
            socketType = socketType,
            socketId = $"{direction}_{socketType}",
            weight = 1,
            isRequired = true
        };
    }

    public PropPlacement CreateDefaultPropPlacement(string propId, Vector3 position)
    {
        return new PropPlacement
        {
            propId = propId,
            localPosition = position,
            localRotation = Vector3.zero,
            localScale = Vector3.one,
            spawnProbability = 1f
        };
    }

#if UNITY_EDITOR
    [ContextMenu("Validate Room Data")]
    private void EditorValidateRoomData()
    {
        var errors = ValidateRoomData();
        if (errors.Count == 0)
        {
            Debug.Log("All room data validation passed!");
        }
        else
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }
        }
    }

    [ContextMenu("Rebuild Lookup Table")]
    private void EditorRebuildLookupTable()
    {
        BuildLookupTable();
        Debug.Log("Room data lookup table rebuilt!");
    }
#endif
}
