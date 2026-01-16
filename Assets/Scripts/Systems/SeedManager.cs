using UnityEngine;

public class SeedManager : MonoBehaviour
{
    [Header("Seed Settings")]
    public string globalSeed = "default";
    public bool useGlobalSeed = true;

    [Header("Seed Components")]
    public int levelSeed;
    public int layoutSeed;
    public int difficultySeed;

    public void InitializeSeeds()
    {
        int baseSeed = useGlobalSeed ? globalSeed.GetHashCode() : Random.Range(0, int.MaxValue);

        Random.InitState(baseSeed);

        levelSeed = Random.Range(0, int.MaxValue);
        layoutSeed = Random.Range(0, int.MaxValue);
        difficultySeed = Random.Range(0, int.MaxValue);

        // Store seeds for regeneration
        PlayerPrefs.SetString("LastGlobalSeed", globalSeed);
        PlayerPrefs.SetInt("LastLevelSeed", levelSeed);
        PlayerPrefs.SetInt("LastLayoutSeed", layoutSeed);
        PlayerPrefs.SetInt("LastDifficultySeed", difficultySeed);
    }

    public Random.State GetRandomStateForCategory(SeedCategory category)
    {
        int seed = 0;
        switch (category)
        {
            case SeedCategory.Level: seed = levelSeed; break;
            case SeedCategory.Layout: seed = layoutSeed; break;
            case SeedCategory.Difficulty: seed = difficultySeed; break;
        }

        Random.State originalState = Random.state;
        Random.InitState(seed);
        Random.State categoryState = Random.state;
        Random.state = originalState;

        return categoryState;
    }

    public enum SeedCategory { Level, Layout, Difficulty }

    // Advanced seed management
    public void LoadSeedsFromPrefs()
    {
        if (PlayerPrefs.HasKey("LastGlobalSeed"))
        {
            globalSeed = PlayerPrefs.GetString("LastGlobalSeed");
            levelSeed = PlayerPrefs.GetInt("LastLevelSeed");
            layoutSeed = PlayerPrefs.GetInt("LastLayoutSeed");
            difficultySeed = PlayerPrefs.GetInt("LastDifficultySeed");
        }
    }

    public string GenerateDeterministicSeed(int playerLevel, string playerId)
    {
        // Create deterministic seed based on player data
        string combined = $"{playerId}_{playerLevel}_{System.DateTime.Now.Date.ToString("yyyyMMdd")}";
        return combined.GetHashCode().ToString();
    }

    public void SetGlobalSeed(string newSeed)
    {
        globalSeed = newSeed;
        InitializeSeeds();
    }

    public string GetCurrentSeedString()
    {
        return $"{globalSeed}_{levelSeed}_{layoutSeed}_{difficultySeed}";
    }

    public void RestoreFromSeedString(string seedString)
    {
        var parts = seedString.Split('_');
        if (parts.Length >= 4)
        {
            globalSeed = parts[0];
            int.TryParse(parts[1], out levelSeed);
            int.TryParse(parts[2], out layoutSeed);
            int.TryParse(parts[3], out difficultySeed);
        }
    }

    // Quantum seed generation for more "random" but reproducible results
    public int GenerateQuantumSeed(Vector3Int position, float time)
    {
        // Use quantum-inspired pseudo-random generation
        float phase = (position.x * 0.1f + position.y * 0.2f + position.z * 0.3f) + time * 0.5f;
        float quantumNoise = Mathf.PerlinNoise(phase, phase * 1.618f); // Golden ratio for better distribution

        // Combine with current seed
        int baseHash = globalSeed.GetHashCode();
        int positionHash = position.GetHashCode();
        int timeHash = time.GetHashCode();

        return baseHash ^ positionHash ^ timeHash ^ Mathf.RoundToInt(quantumNoise * int.MaxValue);
    }

    // Seed validation for competitive/tournament play
    public bool ValidateSeedIntegrity(string seedString, string expectedHash)
    {
        // Create hash of seed for integrity checking
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(seedString);
            var hash = sha256.ComputeHash(bytes);
            var hashString = System.BitConverter.ToString(hash).Replace("-", "").ToLower();

            return hashString == expectedHash;
        }
    }

    public string GenerateSeedHash(string seedString)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(seedString);
            var hash = sha256.ComputeHash(bytes);
            return System.BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
    }
}
