using UnityEngine;

[CreateAssetMenu(fileName = "TowerData_Arrow", menuName = "Tower Defense/Tower Data")]
public class TowerData : ScriptableObject
{
    public string towerId = "arrow";
    public GameObject prefab;
    public Sprite icon;
    [SerializeField] private TowerLevelStats[] levels = new TowerLevelStats[2];

    public int LevelCount => levels == null ? 0 : levels.Length;

    // Levels are one-based. Prices are read by B; Tower never changes gold.
    public bool TryGetLevel(int level, out TowerLevelStats stats, out string error)
    {
        stats = default;
        if (level < 1 || level > LevelCount)
        {
            error = $"{name}: missing level {level}; configured levels: {LevelCount}.";
            return false;
        }
        stats = levels[level - 1];
        if (!stats.TryValidate(out error))
        {
            error = $"{name}, level {level}: {error}";
            return false;
        }
        return true;
    }

    public bool TryValidate(out string error)
    {
        if (string.IsNullOrWhiteSpace(towerId) || towerId != towerId.Trim())
            error = $"{name}: towerId is empty or contains surrounding whitespace.";
        else if (prefab == null || prefab.GetComponent<Tower>() == null)
            error = $"{name}: prefab must have a Tower on its root.";
        else if (icon == null)
            error = $"{name}: icon is missing.";
        else if (LevelCount < 2)
            error = $"{name}: level II is missing.";
        else
        {
            for (int i = 1; i <= LevelCount; i++)
                if (!TryGetLevel(i, out _, out error))
                    return false;
            error = string.Empty;
        }
        return error.Length == 0;
    }
}
