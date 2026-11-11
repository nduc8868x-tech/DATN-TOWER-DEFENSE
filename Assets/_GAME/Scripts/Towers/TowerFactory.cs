using UnityEngine;

[DisallowMultipleComponent]
public sealed class TowerFactory : MonoBehaviour
{
    public TowerCatalog catalog;
    public CombatResolver resolver;

    public bool TryCreate(string towerId, int level, Vector3 position, out TowerHandle handle)
    {
        handle = null;
        if (!Application.isPlaying || catalog == null || resolver == null || !resolver.isActiveAndEnabled ||
            resolver.SessionId <= 0 || !Finite(position.x) || !Finite(position.y) || !Finite(position.z) ||
            !catalog.TryGet(towerId, out var data) || !data.TryValidate(out _) ||
            !data.prefab.GetComponent<Tower>().TryValidateCombatLevel(data, level, out _, out _)) return false;
        var tower = Instantiate(data.prefab, position, Quaternion.identity).GetComponent<Tower>();
        if (!tower.Initialize(data, level, resolver.Registry, resolver, out _) ||
            !tower.TryValidateForActivation(out _))
        {
            tower.gameObject.SetActive(false);
            Destroy(tower.gameObject);
            return false;
        }
        handle = new TowerHandle(tower);
        return true;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
