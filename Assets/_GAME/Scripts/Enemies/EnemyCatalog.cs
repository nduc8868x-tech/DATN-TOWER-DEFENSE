using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyCatalog",
    menuName = "Tower Defense/Enemy Catalog"
)]
public class EnemyCatalog : ScriptableObject
{
    [SerializeField] private EnemyData[] entries = Array.Empty<EnemyData>();

    public bool TryValidate(out string error)
    {
        if (entries == null || entries.Length == 0)
        {
            error = $"{name}: Danh sách Entries đang trống.";
            return false;
        }

        var usedIds = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < entries.Length; i++)
        {
            EnemyData data = entries[i];

            if (data == null)
            {
                error = $"{name}: Element {i} chưa được gán EnemyData.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.enemyId))
            {
                error = $"{data.name}: Enemy Id đang trống.";
                return false;
            }

            if (data.enemyId != data.enemyId.Trim())
            {
                error = $"{data.name}: Enemy Id có khoảng trắng ở đầu hoặc cuối.";
                return false;
            }

            if (!usedIds.Add(data.enemyId))
            {
                error = $"{name}: Enemy Id '{data.enemyId}' bị trùng ở Element {i}.";
                return false;
            }

            if (data.prefab == null)
            {
                error = $"{data.name}: Chưa gán Prefab.";
                return false;
            }

            GameObject prefab = data.prefab;

            if (prefab.GetComponent<Enemy>() == null ||
                prefab.GetComponent<EnemyHealth>() == null ||
                prefab.GetComponent<EnemyMovement>() == null)
            {
                error = $"{prefab.name}: Object gốc cần có Enemy, " +
                        "EnemyHealth và EnemyMovement.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public bool TryGet(string enemyId, out EnemyData data)
    {
        data = null;

        if (entries == null || string.IsNullOrWhiteSpace(enemyId))
            return false;

        foreach (EnemyData entry in entries)
        {
            if (entry != null &&
                string.Equals(entry.enemyId, enemyId, StringComparison.Ordinal))
            {
                data = entry;
                return true;
            }
        }

        return false;
    }

    [ContextMenu("Debug/Validate Catalog")]
    private void DebugValidateCatalog()
    {
        if (!TryValidate(out string error))
        {
            Debug.LogError(error, this);
            return;
        }

        Debug.Log($"{name}: Catalog hợp lệ, có {entries.Length} loại quái.", this);
    }
}