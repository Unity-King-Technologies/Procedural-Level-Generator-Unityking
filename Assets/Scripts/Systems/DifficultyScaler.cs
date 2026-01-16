using UnityEngine;
using System.Collections.Generic;

public class DifficultyScaler : MonoBehaviour
{
    [System.Serializable]
    public class DifficultyPreset
    {
        public string name;
        public int minDifficulty;
        public int maxDifficulty;
        public AnimationCurve roomDistribution;
        public string[] requiredRooms;
        public string[] forbiddenRooms;
    }

    public DifficultyPreset[] presets;
    private int currentDifficulty = 1;

    public DifficultyScaler(AnimationCurve difficultyCurve)
    {
        // Initialize with default preset if none provided
        if (presets == null || presets.Length == 0)
        {
            presets = new DifficultyPreset[]
            {
                new DifficultyPreset
                {
                    name = "Default",
                    minDifficulty = 1,
                    maxDifficulty = 10,
                    roomDistribution = difficultyCurve ?? AnimationCurve.Linear(0, 0, 1, 1),
                    requiredRooms = new string[0],
                    forbiddenRooms = new string[0]
                }
            };
        }
    }

    public List<RoomModule> FilterModules(List<RoomModule> allModules, int playerLevel, Vector3Int position)
    {
        List<RoomModule> filtered = new List<RoomModule>();
        DifficultyPreset preset = GetCurrentPreset(playerLevel);

        foreach (var module in allModules)
        {
            if (IsModuleAllowed(module, preset, position))
            {
                filtered.Add(module);
            }
        }

        return filtered;
    }

    private bool IsModuleAllowed(RoomModule module, DifficultyPreset preset, Vector3Int position)
    {
        // Check difficulty range
        if (module.difficultyRange[0] > preset.maxDifficulty ||
            module.difficultyRange[1] < preset.minDifficulty)
            return false;

        // Check forbidden rooms
        foreach (string forbidden in preset.forbiddenRooms)
        {
            if (System.Array.Exists(module.tags, tag => tag == forbidden))
                return false;
        }

        // Position-based difficulty scaling
        float distanceFromCenter = Vector3.Distance(position, Vector3.zero);
        float scaledDifficulty = currentDifficulty * (1 + distanceFromCenter * 0.1f);

        return module.difficultyRange[0] <= scaledDifficulty;
    }

    private DifficultyPreset GetCurrentPreset(int playerLevel)
    {
        // Find preset that matches current difficulty level
        foreach (var preset in presets)
        {
            if (playerLevel >= preset.minDifficulty && playerLevel <= preset.maxDifficulty)
            {
                return preset;
            }
        }

        // Return first preset as fallback
        return presets.Length > 0 ? presets[0] : null;
    }

    public void SetCurrentDifficulty(int difficulty)
    {
        currentDifficulty = Mathf.Max(1, difficulty);
    }

    public int GetCurrentDifficulty()
    {
        return currentDifficulty;
    }

    public float GetDifficultyMultiplier(Vector3Int position)
    {
        float distanceFromCenter = Vector3.Distance(position, Vector3.zero);
        return 1 + (distanceFromCenter * 0.1f);
    }

    public DifficultyPreset GetPresetByName(string name)
    {
        foreach (var preset in presets)
        {
            if (preset.name == name)
                return preset;
        }
        return null;
    }

    public void AddPreset(DifficultyPreset preset)
    {
        var newPresets = new DifficultyPreset[presets.Length + 1];
        presets.CopyTo(newPresets, 0);
        newPresets[presets.Length] = preset;
        presets = newPresets;
    }

    public bool RemovePreset(string name)
    {
        var newPresets = new System.Collections.Generic.List<DifficultyPreset>(presets);
        var presetToRemove = newPresets.Find(p => p.name == name);

        if (presetToRemove != null)
        {
            newPresets.Remove(presetToRemove);
            presets = newPresets.ToArray();
            return true;
        }

        return false;
    }

    // Advanced difficulty scaling methods
    public float CalculateAdaptiveDifficulty(int playerLevel, float completionRate, float averageTime)
    {
        // Adaptive difficulty based on player performance
        float baseDifficulty = playerLevel;

        // Adjust based on completion rate (0-1)
        float completionModifier = Mathf.Lerp(-0.5f, 0.5f, completionRate);

        // Adjust based on time taken (normalized)
        float timeModifier = Mathf.Lerp(0.3f, -0.3f, averageTime / 300f); // Assuming 5 min average

        return baseDifficulty + completionModifier + timeModifier;
    }

    public void UpdateDifficultyFromPlayerStats(float completionRate, float averageTime, int playerLevel)
    {
        float adaptiveDifficulty = CalculateAdaptiveDifficulty(playerLevel, completionRate, averageTime);
        SetCurrentDifficulty(Mathf.RoundToInt(adaptiveDifficulty));
    }

    // Quantum-inspired difficulty modulation
    public float GetQuantumDifficultyModulation(Vector3Int position, float time)
    {
        // Use quantum phase-like modulation for dynamic difficulty
        float phase = (position.x + position.y + position.z) * 0.1f + time * 0.5f;
        return 1 + Mathf.Sin(phase) * 0.2f; // ±20% modulation
    }
}
