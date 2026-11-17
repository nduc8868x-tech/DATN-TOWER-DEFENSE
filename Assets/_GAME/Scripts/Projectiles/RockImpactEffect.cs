using UnityEngine;

// Session-owned, renderer-only effect. It has no colliders or combat requests.
public sealed class RockImpactEffect : MonoBehaviour
{
    private const float Duration = 0.55f;
    private struct Fragment
    {
        public SpriteRenderer Renderer;
        public Vector2 Velocity;
        public float Spin;
    }

    private CombatResolver combat;
    private int sessionId;
    private SpriteRenderer dust;
    private SpriteRenderer breakup;
    private Sprite[] frames;
    private float frameDuration;
    private float duration;
    private Fragment[] fragments;
    private float elapsed;
    internal int SessionId => sessionId;

    internal static void Spawn(CombatResolver resolver, Vector3 position, SpriteRenderer source,
        Sprite dustSprite, Sprite[] fragmentSprites, Sprite[] impactFrames, float impactFrameDuration)
    {
        var root = new GameObject("Rock Impact");
        root.transform.position = position;
        var effect = root.AddComponent<RockImpactEffect>();
        effect.combat = resolver;
        effect.sessionId = resolver.SessionId;
        resolver.RegisterRockImpact(effect);
        effect.frames = impactFrames;
        effect.frameDuration = Mathf.Max(0.01f, impactFrameDuration);
        effect.duration = Mathf.Max(Duration, impactFrames != null ? impactFrames.Length * effect.frameDuration : 0f);
        if (impactFrames != null && impactFrames.Length > 0)
            effect.breakup = effect.CreateSprite("Breakup Frames", impactFrames[0], source, 2);

        if (dustSprite != null)
        {
            effect.dust = effect.CreateSprite("Dust", dustSprite, source, 0);
            effect.dust.transform.localScale = Vector3.one * 0.45f;
        }

        int count = fragmentSprites != null && fragmentSprites.Length > 0 ? Random.Range(6, 11) : 0;
        effect.fragments = new Fragment[count];
        float startAngle = Random.Range(0f, Mathf.PI * 2f);
        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + i * Mathf.PI * 2f / count;
            var sprite = fragmentSprites[i % fragmentSprites.Length];
            if (sprite == null) sprite = source.sprite;
            var renderer = effect.CreateSprite("Stone Fragment", sprite, source, 1);
            renderer.transform.localScale = Vector3.one * Random.Range(0.65f, 1.1f);
            effect.fragments[i] = new Fragment
            {
                Renderer = renderer,
                Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(0.65f, 1.6f)
                    + Vector2.up * 0.55f,
                Spin = Random.Range(200f, 540f) * (i % 2 == 0 ? 1f : -1f)
            };
        }
    }

    private SpriteRenderer CreateSprite(string objectName, Sprite sprite, SpriteRenderer source, int orderOffset)
    {
        var child = new GameObject(objectName);
        child.transform.SetParent(transform, false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder + orderOffset;
        return renderer;
    }

    private void Update()
    {
        if (combat == null || combat.SessionId != sessionId) { Cancel(); return; }
        float dt = Time.deltaTime;
        if (dt <= 0f || Time.timeScale <= 0f) return;
        elapsed += dt;
        if (elapsed >= duration) { Cancel(); return; }

        float progress = elapsed / duration;
        if (breakup != null)
        {
            int frame = Mathf.FloorToInt(elapsed / frameDuration);
            breakup.enabled = frame < frames.Length;
            if (breakup.enabled) breakup.sprite = frames[frame];
        }
        if (dust != null)
        {
            dust.color = new Color(1f, 1f, 1f, 0.85f * (1f - progress));
            dust.transform.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.4f, progress);
        }
        foreach (var fragment in fragments)
        {
            Vector2 offset = fragment.Velocity * elapsed + Vector2.down * (1.3f * elapsed * elapsed);
            fragment.Renderer.transform.localPosition = offset;
            fragment.Renderer.transform.localRotation = Quaternion.Euler(0f, 0f, fragment.Spin * elapsed);
            fragment.Renderer.color = new Color(1f, 1f, 1f, 1f - progress);
        }
    }

    internal void Cancel()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (combat != null) combat.UnregisterRockImpact(this);
    }
}
