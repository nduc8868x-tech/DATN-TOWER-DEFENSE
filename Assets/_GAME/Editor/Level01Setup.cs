using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

// Explicit, repeatable editor wiring; never changes scene configuration during Play.
public static class Level01Setup
{
    [MenuItem("DATN/Integration/Run Level01 Checks (Play Mode)")]
    public static void RunChecks()
    {
        if (!Application.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Level01")
            throw new System.InvalidOperationException("Open Level01 and enter Play Mode first.");
        if (UnityEngine.Object.FindFirstObjectByType<LevelIntegrationChecks>())
            throw new System.InvalidOperationException("Checks already attached. Stop Play Mode before another run.");
        new GameObject("IntegrationChecks_RuntimeOnly").AddComponent<LevelIntegrationChecks>();
    }
    private static void Ref(Object owner, string field, Object value)
    {
        var so = new SerializedObject(owner); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform; r.anchorMin = r.anchorMax = anchor; r.pivot = new Vector2(.5f,.5f);
        r.anchoredPosition = pos; r.sizeDelta = size; return r;
    }
    private static Text Text(string name, Transform parent, Vector2 pos, Vector2 size, string label, int fontSize = 22)
    {
        var r = Rect(name,parent,new Vector2(.5f,.5f),pos,size); var t=r.gameObject.AddComponent<Text>();
        t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize=fontSize; t.alignment=TextAnchor.MiddleCenter;
        t.color=new Color(1f,.95f,.8f); t.text=label; t.raycastTarget=false; return t;
    }
    private static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var r=Rect(name,parent,anchor,pos,size); var image=r.gameObject.AddComponent<Image>();
        image.color=new Color(.10f,.13f,.13f,.96f); return image;
    }
    private static Button Button(string name, Transform parent, Vector2 pos, Vector2 size, string label)
    {
        var r=Rect(name,parent,new Vector2(.5f,.5f),pos,size); var image=r.gameObject.AddComponent<Image>();
        image.color=new Color(.35f,.45f,.27f,1f);
        var b=r.gameObject.AddComponent<Button>(); b.targetGraphic=image;
        var colors=b.colors; colors.disabledColor=new Color(.3f,.3f,.3f,.65f); b.colors=colors;
        Text("Label",r,Vector2.zero,size,label,21); return b;
    }
    [MenuItem("DATN/Integration/Configure Level01 Gameplay")]
    public static void Configure() => ConfigurePoints(8);
    public static void ConfigureOnePoint() => ConfigurePoints(1);
    private static void ConfigurePoints(int pointCount)
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path!="Assets/_GAME/Scenes/Level01.unity") throw new System.InvalidOperationException("Open existing Level01 first.");
        var systems=GameObject.Find("Systems");
        if(!systems) throw new System.InvalidOperationException("Systems must be wired before configuring UI.");
        var flow=systems.GetComponent<FlowBootstrapper>(); var session=systems.GetComponent<GameSession>();
        var economy=systems.GetComponent<EconomyManager>(); var wave=systems.GetComponent<WaveManager>();
        var build=systems.GetComponent<BuildManager>() ?? systems.AddComponent<BuildManager>();
        var input=systems.GetComponent<BuildInputController>() ?? systems.AddComponent<BuildInputController>();
        var catalog=AssetDatabase.LoadAssetAtPath<TowerCatalog>("Assets/_GAME/Data/Towers/TowerCatalog.asset");
        Ref(flow,"gameSession",session); Ref(flow,"economyManager",economy); Ref(flow,"waveManager",wave);
        Ref(session,"economyManager",economy);
        Ref(build,"gameSession",session); Ref(build,"economyManager",economy); Ref(build,"bootstrapper",flow); Ref(build,"catalog",catalog);
        Ref(input,"gameSession",session); Ref(input,"mainCamera",Camera.main);
        var so=new SerializedObject(input); so.FindProperty("buildPointLayer").intValue=1<<0; so.ApplyModifiedPropertiesWithoutUndo();
        // All points are off the existing path and within arrow range of a useful segment.
        var positions=new[]{new Vector3(-7.5f,-.5f,0),new Vector3(-4.6f,0,0),new Vector3(-4.7f,2.5f,0),
            new Vector3(-2.8f,2.5f,0),new Vector3(-2f,-.6f,0),new Vector3(.1f,1.5f,0),
            new Vector3(2.7f,.4f,0),new Vector3(.9f,3.1f,0)};
        var marker=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_GAME/Art/UI/PNG/interface_game/button_quick.png");
        for(int i=0;i<pointCount;i++)
        {
            string name="BuildPoint_"+(i+1).ToString("00"); if(GameObject.Find(name)) continue;
            var go=new GameObject(name); go.transform.position=positions[i];
            var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=marker; sr.sortingOrder=3;
            if(marker) go.transform.localScale=Vector3.one*(.65f/Mathf.Max(marker.bounds.size.x,marker.bounds.size.y));
            var collider=go.AddComponent<CircleCollider2D>(); collider.radius=marker ? Mathf.Max(marker.bounds.size.x,marker.bounds.size.y)*.65f : .45f;
            var bp=go.AddComponent<BuildPoint>(); bp.SetPointId(name);
            // Keep anchor scale independent from marker size (factory uses world position only).
            Ref(bp,"visualRenderer",sr);
        }
        if(UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0)
            new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        if(GameObject.Find("LevelHUD")) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); return; }
        var canvasGo=new GameObject("LevelHUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=canvasGo.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
        var hud=canvasGo.AddComponent<LevelHUD>(); hud.flow=flow; hud.waves=wave; hud.builds=build; hud.input=input;
        var top=Panel("TopBar",canvasGo.transform,new Vector2(.5f,1),new Vector2(0,-36),new Vector2(1600,72));
        hud.status=Text("Status",top.transform,new Vector2(-310,0),new Vector2(920,60),"GOLD 240   LIVES 20   WAVE 1/3",25);
        hud.startWave=Button("StartWave",top.transform,new Vector2(300,0),new Vector2(190,52),"Start Wave");
        hud.pause=Button("Pause",top.transform,new Vector2(500,0),new Vector2(170,52),"Pause");
        hud.restart=Button("Restart",top.transform,new Vector2(685,0),new Vector2(170,52),"Restart");
        var bottom=Panel("BuildMenu",canvasGo.transform,new Vector2(.5f,0),new Vector2(0,72),new Vector2(1600,144));
        hud.selection=Text("Selection",bottom.transform,new Vector2(-450,26),new Vector2(620,48),"Select a marked build point");
        hud.message=Text("Message",bottom.transform,new Vector2(0,-40),new Vector2(1500,36),"Build arrows, then press Start Wave",18);
        hud.buy=Button("BuyArrow",bottom.transform,new Vector2(260,24),new Vector2(230,54),"Buy Arrow");
        // Never replace Buy with Upgrade under the same pointer during a double click.
        hud.upgrade=Button("Upgrade",bottom.transform,new Vector2(0,24),new Vector2(230,54),"Upgrade");
        hud.sell=Button("Sell",bottom.transform,new Vector2(520,24),new Vector2(230,54),"Sell");
        var overlay=Panel("ResultOverlay",canvasGo.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(4000,3000));
        overlay.color=new Color(0,0,0,.65f); hud.resultPanel=overlay.gameObject;
        var box=Panel("Result",overlay.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(500,270));
        hud.resultTitle=Text("ResultTitle",box.transform,new Vector2(0,65),new Vector2(480,80),"VICTORY",44);
        hud.resultRestart=Button("ResultRestart",box.transform,new Vector2(0,-50),new Vector2(250,65),"Restart");
        hud.resultPanel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("Level01 gameplay configured: real services, 8 build points, Canvas and InputSystem EventSystem.");
    }
}
