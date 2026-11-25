using UnityEngine;
using UnityEngine.UI;

// UI mesh: a clockwise countdown border beginning at twelve o'clock.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class WaveCountdownRing : MaskableGraphic
{
    [SerializeField, Range(0, 1)] private float progress;
    [SerializeField, Min(1)] private float thickness = 8;
    [SerializeField] private Color trackColor = new Color(.24f, .21f, .13f, .7f);
    public float Progress
    {
        get => progress;
        set { value = Mathf.Clamp01(value); if (Mathf.Approximately(progress, value)) return; progress = value; SetVerticesDirty(); }
    }
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = GetPixelAdjustedRect();
        float outer = Mathf.Min(rect.width, rect.height) * .5f;
        float inner = Mathf.Max(0, outer - thickness - 3);
        DrawArc(mesh, rect.center, outer, inner, 1, new Color(.06f, .05f, .02f, .95f));
        DrawArc(mesh, rect.center, outer - 1.5f, inner + 1.5f, 1, trackColor);
        DrawArc(mesh, rect.center, outer - 1.5f, inner + 1.5f, progress, color);
    }
    private static void DrawArc(VertexHelper mesh, Vector2 center, float outer, float inner, float amount, Color tint)
    {
        if (amount <= 0 || outer <= inner) return;
        int segments = Mathf.Max(1, Mathf.CeilToInt(128 * amount));
        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.PI * .5f - 2 * Mathf.PI * amount * i / segments;
            float b = Mathf.PI * .5f - 2 * Mathf.PI * amount * (i + 1) / segments;
            Vector2 start = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Vector2 end = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
            int index = mesh.currentVertCount;
            mesh.AddVert(center + start * inner, tint, Vector2.zero);
            mesh.AddVert(center + start * outer, tint, Vector2.zero);
            mesh.AddVert(center + end * outer, tint, Vector2.zero);
            mesh.AddVert(center + end * inner, tint, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
