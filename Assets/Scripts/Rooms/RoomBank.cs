using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "RoomBank", menuName = "WFC/Room Bank")]
public class RoomBank : ScriptableObject
{
    [System.Serializable]
    public class RoomCategory
    {
        public string categoryName;
        public List<RoomModule> rooms = new List<RoomModule>();
        public float categoryWeight = 1f;
    }

    public List<RoomCategory> categories = new List<RoomCategory>();
    public List<RoomModule> standaloneRooms = new List<RoomModule>();

    // Cached list for performance
    private List<RoomModule> allModulesCache;
    private bool isCacheDirty = true;

    public List<RoomModule> GetAllModules()
    {
        if (isCacheDirty || allModulesCache == null)
        {
            RebuildCache();
        }
        return allModulesCache;
    }

    private void RebuildCache()
    {
        allModulesCache = new List<RoomModule>();
        allModulesCache.AddRange(standaloneRooms);

        foreach (var category in categories)
        {
            allModulesCache.AddRange(category.rooms);
        }

        isCacheDirty = false;
    }

    public List<RoomModule> GetModulesByCategory(string categoryName)
    {
        var category = categories.Find(c => c.categoryName == categoryName);
        return category != null ? category.rooms : new List<RoomModule>();
    }

    public List<RoomModule> GetModulesByTag(string tag)
    {
        return GetAllModules().FindAll(room => System.Array.Exists(room.tags, t => t == tag));
    }

    public List<RoomModule> GetModulesInDifficultyRange(int minDifficulty, int maxDifficulty)
    {
        return GetAllModules().FindAll(room =>
            room.difficultyRange[0] >= minDifficulty && room.difficultyRange[1] <= maxDifficulty);
    }

    public void AddRoom(RoomModule room, string categoryName = null)
    {
        if (string.IsNullOrEmpty(categoryName))
        {
            if (!standaloneRooms.Contains(room))
            {
                standaloneRooms.Add(room);
                isCacheDirty = true;
            }
        }
        else
        {
            var category = categories.Find(c => c.categoryName == categoryName);
            if (category == null)
            {
                category = new RoomCategory { categoryName = categoryName };
                categories.Add(category);
            }

            if (!category.rooms.Contains(room))
            {
                category.rooms.Add(room);
                isCacheDirty = true;
            }
        }
    }

    public void RemoveRoom(RoomModule room)
    {
        if (standaloneRooms.Remove(room))
        {
            isCacheDirty = true;
            return;
        }

        foreach (var category in categories)
        {
            if (category.rooms.Remove(room))
            {
                isCacheDirty = true;
                break;
            }
        }
    }

    public RoomModule GetRandomRoom(System.Random random = null)
    {
        var allRooms = GetAllModules();
        if (allRooms.Count == 0) return null;

        if (random != null)
        {
            return allRooms[random.Next(allRooms.Count)];
        }
        else
        {
            return allRooms[UnityEngine.Random.Range(0, allRooms.Count)];
        }
    }

    public RoomModule GetRandomRoomByCategory(string categoryName, System.Random random = null)
    {
        var categoryRooms = GetModulesByCategory(categoryName);
        if (categoryRooms.Count == 0) return null;

        if (random != null)
        {
            return categoryRooms[random.Next(categoryRooms.Count)];
        }
        else
        {
            return categoryRooms[UnityEngine.Random.Range(0, categoryRooms.Count)];
        }
    }

    // Validation methods
    public bool ValidateConnections()
    {
        var allRooms = GetAllModules();
        bool allValid = true;

        foreach (var room in allRooms)
        {
            if (room.sockets.Length == 0)
            {
                Debug.LogWarning($"Room {room.name} has no sockets defined!");
                allValid = false;
            }
        }

        return allValid;
    }

    public void InvalidateCache()
    {
        isCacheDirty = true;
    }

#if UNITY_EDITOR
    [ContextMenu("Validate All Rooms")]
    private void EditorValidateRooms()
    {
        if (ValidateConnections())
        {
            Debug.Log("All rooms validated successfully!");
        }
    }

    [ContextMenu("Rebuild Cache")]
    private void EditorRebuildCache()
    {
        RebuildCache();
        Debug.Log("Room cache rebuilt!");
    }
#endif
}
