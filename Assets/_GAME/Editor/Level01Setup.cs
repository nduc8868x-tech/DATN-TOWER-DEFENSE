using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using UnityEditor;
using UnityEditor.SceneManagement;

// Wires existing objects only; never creates points or changes geometry.
public static class Level01Setup
{
    [InitializeOnLoadMethod]
    private static void PendingConfiguration()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists("Temp/level01-configure.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try { Configure(); File.Delete("Temp/level01-configure.request"); }
            catch (Exception e) { File.WriteAllText("Temp/level01-configure.error", e.ToString()); Debug.LogException(e); }
        };
    }
    private static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    [MenuItem("DATN/Integration/Configure Level01 Gameplay")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/_GAME/Scenes/Level01.unity") throw new InvalidOperationException("Open existing Level01 first.");
        EditorSceneManager.SaveScene(scene);
        var points = UnityEngine.Object.FindObjectsByType<BuildPoint>(FindObjectsSortMode.None);
        if (points.Length != 5) throw new InvalidOperationException("Expected exactly five active BuildPoints.");
        var containers = UnityEngine.Object.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None)
            .Where(p => p.name.StartsWith("EnemyPath_Spline")).ToArray();
        if (containers.Length != 2 || containers.Any(p => p.Splines.Count != 1))
            throw new InvalidOperationException("Expected two single-spline containers. Geometry preserved.");
        containers = containers.OrderBy(p => p.transform.TransformPoint((Vector3)p.Spline[0].Position).x).ToArray();
        var wave = UnityEngine.Object.FindFirstObjectByType<WaveManager>();
        var so = new SerializedObject(wave);
        var routes = so.FindProperty("routes"); routes.arraySize = 2;
        var report = new System.Text.StringBuilder("Level01 geometry and coverage audit\n");
        for (int i = 0; i < 2; i++)
        {
            var p = containers[i]; var start = p.EvaluatePosition(0); var end = p.EvaluatePosition(1);
            if (end.y <= start.y) throw new InvalidOperationException("Inspect direction before reversing.");
            routes.GetArrayElementAtIndex(i).FindPropertyRelative("routeId").stringValue = i == 0 ? "route_left" : "route_right";
            routes.GetArrayElementAtIndex(i).FindPropertyRelative("container").objectReferenceValue = p;
            report.AppendLine($"{(i == 0 ? "route_left" : "route_right")}: {p.name}; index=0; start={start}; end={end}; length={p.CalculateLength():F3}");
        }
        var waves = so.FindProperty("waves"); waves.arraySize = 6;
        for (int i = 0; i < 6; i++) waves.GetArrayElementAtIndex(i).objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<WaveData>($"Assets/_GAME/Data/Waves/Wave{i + 1:00}.asset");
        so.ApplyModifiedPropertiesWithoutUndo();
        var arrow = AssetDatabase.LoadAssetAtPath<TowerData>("Assets/_GAME/Data/Towers/TowerData_Arrow.asset");
        if (!arrow || !arrow.TryGetLevel(1, out var stats, out _)) throw new InvalidOperationException("Arrow data missing.");
        foreach (var point in points.OrderBy(p => p.name))
        {
            point.SetPointId(point.name);
            Ref(point, "buildAnchor", point.transform); Ref(point, "visualRenderer", point.GetComponent<SpriteRenderer>());
            point.gameObject.layer = LayerMask.NameToLayer("BuildPoint");
            var collider = point.GetComponent<Collider2D>();
            if (!collider) throw new InvalidOperationException(point.name + " has no collider.");
            collider.enabled = true;
            float nearest = float.PositiveInfinity;
            foreach (var path in containers) for (int i = 0; i <= 2000; i++)
            { Vector2 offset = (Vector3)path.EvaluatePosition(i / 2000f) - point.AnchorPosition; nearest = Mathf.Min(nearest, offset.magnitude); }
            report.AppendLine($"{point.PointId}: anchor={point.AnchorPosition}; collider={collider.GetType().Name}; nearest route={nearest:F3}; range={stats.range}; useful={nearest <= stats.range}");
            if (nearest > stats.range) Debug.LogWarning(point.name + " has no route in range; layout preserved.", point);
        }
        var input = UnityEngine.Object.FindFirstObjectByType<BuildInputController>();
        var inputSO = new SerializedObject(input); inputSO.FindProperty("buildPointLayer").intValue = 1 << LayerMask.NameToLayer("BuildPoint"); inputSO.ApplyModifiedPropertiesWithoutUndo();
        var hud = UnityEngine.Object.FindFirstObjectByType<LevelHUD>();
        if (!hud.buyRock)
        {
            hud.buyRock = UnityEngine.Object.Instantiate(hud.buy, hud.buy.transform.parent);
            hud.buyRock.name = "BuyRock";
            ((RectTransform)hud.buyRock.transform).anchoredPosition = new Vector2(520, 24);
            hud.buyRock.GetComponentInChildren<Text>().text = "Đá 140 (sau đợt 3)";
        }
        hud.message.text = "Bảo vệ cửa hầm — chống lại 6 đợt quái";
        var menu = (RectTransform)hud.buy.transform.parent;
        menu.anchorMin = menu.anchorMax = new Vector2(0, 0);
        menu.anchoredPosition = new Vector2(380, 72); menu.sizeDelta = new Vector2(760, 144);
        Place(hud.selection.rectTransform, new Vector2(0, 50), new Vector2(740, 35));
        Place(hud.message.rectTransform, new Vector2(0, -45), new Vector2(740, 40));
        Place((RectTransform)hud.buy.transform, new Vector2(-180, 5), new Vector2(230, 48));
        Place((RectTransform)hud.buyRock.transform, new Vector2(100, 5), new Vector2(290, 48));
        Place((RectTransform)hud.upgrade.transform, new Vector2(70, 5), new Vector2(220, 48));
        Place((RectTransform)hud.sell.transform, new Vector2(280, 5), new Vector2(170, 48));
        EditorUtility.SetDirty(hud);
        File.WriteAllText("Docs/Level01-geometry-audit.txt", report.ToString());
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log(report.ToString());
    }
    public static void ConfigureOnePoint() => Configure();
    private static void Place(RectTransform r, Vector2 p, Vector2 size) { r.anchoredPosition = p; r.sizeDelta = size; }
    [MenuItem("DATN/Integration/Run Level01 Checks (Play Mode)")]
    public static void RunChecks() => Debug.LogWarning("Legacy three-wave/eight-point checks are obsolete. Follow Docs/Level01.md.");
}
