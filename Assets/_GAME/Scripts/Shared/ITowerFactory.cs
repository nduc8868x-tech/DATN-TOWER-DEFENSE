using UnityEngine;

public interface ITowerFactory
{
    bool TryCreate(string towerId, int level, Vector3 position, out TowerHandle handle);
}
