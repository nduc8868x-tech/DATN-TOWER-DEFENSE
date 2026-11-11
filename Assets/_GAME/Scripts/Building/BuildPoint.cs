using UnityEngine;

[DisallowMultipleComponent]
public class BuildPoint : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private string pointId = "BP_01";
    [SerializeField] private Transform buildAnchor;
    [SerializeField] private SpriteRenderer visualRenderer;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color occupiedColor = new Color(0.7f, 0.9f, 1f, 1f);

    public string PointId => pointId;
    public Vector3 AnchorPosition => buildAnchor != null ? buildAnchor.position : transform.position;
    public TowerHandle CurrentTowerHandle { get; private set; }
    public string CurrentTowerId { get; private set; }
    public int CurrentLevel { get; private set; }
    public int TotalInvested { get; private set; }
    public bool TransactionInProgress { get; private set; }

    public bool IsOccupied =>
        CurrentTowerHandle != null && !CurrentTowerHandle.ReadSnapshot().IsDisposed;

    private bool isSelected;
    private static Sprite fallbackSquareSprite;

    private void Awake()
    {
        if (visualRenderer == null)
            visualRenderer = GetComponent<SpriteRenderer>();

        if (visualRenderer != null && visualRenderer.sprite == null)
            visualRenderer.sprite = GetOrCreateFallbackSprite();

        RefreshColor();
    }

    public void SetPointId(string id)
    {
        pointId = id;
    }

    public void BeginTransaction() => TransactionInProgress = true;
    public void EndTransaction() => TransactionInProgress = false;

    public void SetOccupied(TowerHandle handle, string towerId, int level, int cost)
    {
        CurrentTowerHandle = handle;
        CurrentTowerId = towerId;
        CurrentLevel = level;
        TotalInvested = Mathf.Max(0, cost);
        RefreshColor();
    }

    public void AddInvestment(int upgradeCost, int newLevel)
    {
        TotalInvested += Mathf.Max(0, upgradeCost);
        CurrentLevel = newLevel;
        RefreshColor();
    }

    public void ClearOccupancy()
    {
        CurrentTowerHandle = null;
        CurrentTowerId = null;
        CurrentLevel = 0;
        TotalInvested = 0;
        RefreshColor();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (visualRenderer == null)
            return;

        if (isSelected)
            visualRenderer.color = selectedColor;
        else if (IsOccupied)
            visualRenderer.color = occupiedColor;
        else
            visualRenderer.color = normalColor;
    }

    private static Sprite GetOrCreateFallbackSprite()
    {
        if (fallbackSquareSprite == null)
        {
            Texture2D tex = Texture2D.whiteTexture;
            fallbackSquareSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                tex.width
            );
        }

        return fallbackSquareSprite;
    }
}
