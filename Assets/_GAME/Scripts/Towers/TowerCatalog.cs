using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Tower Defense/Tower Catalog")]
public sealed class TowerCatalog : ScriptableObject
{
    [SerializeField] private TowerData[] entries;

    public bool TryGet(string towerId, out TowerData data)
    {
        data = null;
        if (string.IsNullOrWhiteSpace(towerId) || entries == null) return false;
        var ids = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.towerId) ||
                entry.towerId != entry.towerId.Trim() || !ids.Add(entry.towerId)) return false;
            if (entry.towerId == towerId) data = entry;
        }
        return data != null;
    }
}
