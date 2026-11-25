using UnityEngine;

// Lives beneath the tower: selling/restarting removes the effect with its owner.
public sealed class TowerUpgradeEffect : MonoBehaviour
{
    private LineRenderer ring;
    private readonly LineRenderer[] sparks = new LineRenderer[12];
    private Material material;
    private float elapsed;
    public static void Play(Tower tower)
    {
        var go = new GameObject("UpgradeBurst");
        go.transform.SetParent(tower.transform, false);
        go.AddComponent<TowerUpgradeEffect>();
    }
    private LineRenderer CreateLine(string label, int count, float width)
    {
        var go = new GameObject(label); go.transform.SetParent(transform, false);
        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material; line.useWorldSpace = true;
        line.positionCount = count; line.widthMultiplier = width;
        line.sortingOrder = 25; line.numCapVertices = 3;
        return line;
    }
    private void Awake()
    {
        material = new Material(Shader.Find("Sprites/Default"));
        ring = CreateLine("Ring", 64, .025f); ring.loop = true;
        for (int i = 0; i < sparks.Length; i++) sparks[i] = CreateLine("Spark", 2, .024f);
    }
    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / .9f);
        var color = new Color(1, .82f, .2f, 1 - t);
        ring.startColor = ring.endColor = color;
        for (int i = 0; i < 64; i++)
        {
            float a = i * Mathf.PI * 2 / 64;
            ring.SetPosition(i, transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * Mathf.Lerp(.12f, .65f, t));
        }
        for (int i = 0; i < sparks.Length; i++)
        {
            float a = i * Mathf.PI * 2 / sparks.Length;
            var offset = new Vector3(Mathf.Cos(a) * .25f, Mathf.Sin(a) * .15f, 0);
            var position = transform.position + offset * (1 + t) + Vector3.up * t * .65f;
            sparks[i].startColor = sparks[i].endColor = color;
            sparks[i].SetPosition(0, position); sparks[i].SetPosition(1, position + Vector3.up * .08f);
        }
        if (elapsed >= .9f) Destroy(gameObject);
    }
    private void OnDestroy() { if (material != null) Destroy(material); }
}
