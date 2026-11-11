using UnityEngine;

[DisallowMultipleComponent]
public class FakeTowerFactory : MonoBehaviour, ITowerFactory
{
    [Header("Debug Controls")]
    [SerializeField] private bool failCreate;
    [SerializeField] private bool failUpgrade;
    [SerializeField] private bool oneShotFailFlags = true;

    public bool FailCreate
    {
        get => failCreate;
        set => failCreate = value;
    }

    public bool FailUpgrade
    {
        get => failUpgrade;
        set => failUpgrade = value;
    }

    public bool TryCreate(string towerId, int level, Vector3 position, out TowerHandle handle)
    {
        if (failCreate)
        {
            if (oneShotFailFlags)
                failCreate = false;

            handle = null;
            Debug.LogWarning("[FakeTowerFactory] FailCreate đang bật -> Trả về false, không tạo rác.");
            return false;
        }

        if (string.IsNullOrEmpty(towerId) || level < 1 || level > 2)
        {
            handle = null;
            Debug.LogError($"[FakeTowerFactory] Tham số tạo tháp không hợp lệ: towerId='{towerId}', level={level}.");
            return false;
        }

        if (!TryGetStats(towerId, level, out int damage, out float interval, out float radius))
        {
            handle = null;
            Debug.LogError($"[FakeTowerFactory] Không tìm thấy cấu hình tháp '{towerId}' cấp {level}.");
            return false;
        }

        GameObject visual = new GameObject($"FakeTower_{towerId}_Lv{level}");
        visual.transform.SetParent(transform, false);
        visual.transform.position = position;

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = GetOrCreateTowerSprite();
        sr.sortingOrder = 5;

        handle = new FakeTowerHandle(this, visual, sr, towerId, level, damage, interval, radius);
        return true;
    }

    private static Sprite cachedTowerSprite;

    private static Sprite GetOrCreateTowerSprite()
    {
        if (cachedTowerSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            cachedTowerSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width
            );
        }

        return cachedTowerSprite;
    }

    internal bool ShouldFailUpgrade()
    {
        if (!failUpgrade)
            return false;

        if (oneShotFailFlags)
            failUpgrade = false;

        return true;
    }

    internal static bool TryGetStats(string towerId, int level, out int damage, out float interval, out float radius)
    {
        damage = 0;
        interval = 1f;
        radius = 0f;

        switch (towerId)
        {
            case "arrow":
            case "archer":
                if (level == 1) { damage = 16; interval = 0.8f; return true; }
                if (level == 2) { damage = 28; interval = 0.65f; return true; }
                return false;

            case "rock":
                if (level == 1) { damage = 30; interval = 2.0f; radius = 1.0f; return true; }
                if (level == 2) { damage = 50; interval = 1.8f; radius = 1.25f; return true; }
                return false;

            case "magic":
                if (level == 1) { damage = 6; interval = 1.0f; return true; }
                if (level == 2) { damage = 10; interval = 0.9f; return true; }
                return false;

            default:
                return false;
        }
    }

    private sealed class FakeTowerHandle : TowerHandle
    {
        private readonly FakeTowerFactory owner;
        private readonly GameObject visual;
        private readonly SpriteRenderer spriteRenderer;
        private readonly string towerId;
        private int level;
        private bool isActive;
        private bool isDisposed;
        private int damage;
        private float interval;
        private float radius;
        public override bool IsValid => !isDisposed && visual != null;

        public FakeTowerHandle(
            FakeTowerFactory owner,
            GameObject visual,
            SpriteRenderer spriteRenderer,
            string towerId,
            int level,
            int damage,
            float interval,
            float radius)
        {
            this.owner = owner;
            this.visual = visual;
            this.spriteRenderer = spriteRenderer;
            this.towerId = towerId;
            this.level = level;
            this.damage = damage;
            this.interval = interval;
            this.radius = radius;
            isActive = false;
            isDisposed = false;
            RefreshVisual();
        }

        public override TowerSnapshot ReadSnapshot()
        {
            return new TowerSnapshot(towerId, level, 2, isActive, isDisposed, damage, interval, radius);
        }

        public override bool Activate()
        {
            if (isDisposed)
                return false;

            isActive = true;
            RefreshVisual();
            return true;
        }

        public override bool Suspend()
        {
            if (isDisposed)
                return false;

            isActive = false;
            RefreshVisual();
            return true;
        }

        public override bool TryApplyLevel(int targetLevel)
        {
            if (isDisposed || targetLevel != level + 1)
                return false;

            if (owner != null && owner.ShouldFailUpgrade())
            {
                Debug.LogWarning("[FakeTowerFactory] FailUpgrade đang bật -> Từ chối nâng cấp.");
                return false;
            }

            if (!TryGetStats(towerId, targetLevel, out int newDamage, out float newInterval, out float newRadius))
                return false;

            level = targetLevel;
            damage = newDamage;
            interval = newInterval;
            radius = newRadius;

            if (visual != null)
                visual.name = $"FakeTower_{towerId}_Lv{level}";

            RefreshVisual();
            return true;
        }

        public override void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            isActive = false;
            if (visual != null)
                Destroy(visual);
        }

        private void RefreshVisual()
        {
            if (visual == null)
                return;

            visual.transform.localScale = new Vector3(0.6f, 0.55f + 0.3f * (level - 1), 1f);

            if (spriteRenderer != null)
            {
                if (!isActive)
                    spriteRenderer.color = Color.gray;
                else if (towerId == "arrow" || towerId == "archer")
                    spriteRenderer.color = new Color(0.2f, 0.85f, 0.35f);
                else if (towerId == "rock")
                    spriteRenderer.color = new Color(0.85f, 0.55f, 0.2f);
                else
                    spriteRenderer.color = new Color(0.3f, 0.6f, 1.0f);
            }
        }
    }
}

