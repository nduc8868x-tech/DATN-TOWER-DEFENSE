using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class GuiNavigation : MonoBehaviour
{
    private static GuiNavigation instance;
    public static string PendingLevel="Level01";
    public static string ReturnScene="MainMenu";
    public static bool Loading=>instance!=null;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){instance=null;PendingLevel="Level01";ReturnScene="MainMenu";}
    public static bool Go(string scene,TdGuiTheme theme=null)
    {
        if(theme==null)theme=Resources.Load<TdGuiTheme>("TdGuiTheme");
        if(Loading||string.IsNullOrWhiteSpace(scene)||!Application.CanStreamedLevelBeLoaded(scene))return false;
        var go=new GameObject("Scene Loading");DontDestroyOnLoad(go);instance=go.AddComponent<GuiNavigation>();instance.StartCoroutine(instance.Load(scene,theme));return true;
    }
    private IEnumerator Load(string scene,TdGuiTheme theme)
    {
        var root=new GameObject("Loading Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
        var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
        var shade=TdGuiScreen.Picture(root.transform,"Loading Shade",theme!=null?theme.Get("load_bar/dark"):null,new Vector2(1920,1080),Vector2.zero);shade.color=new Color(0,0,0,.9f);shade.raycastTarget=true;
        var frame=TdGuiScreen.Picture(root.transform,"Loading Frame",theme!=null?theme.Get("load_bar/load_bar_bg"):null,new Vector2(900,52),new Vector2(0,-20));
        var fill=TdGuiScreen.Picture(root.transform,"Loading Progress",theme!=null?theme.Get("load_bar/load_bar_1"):null,new Vector2(880,36),new Vector2(0,-20));
        fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=0;fill.raycastTarget=false;
        TdGuiScreen.Label(root.transform,"Loading Label","LOADING...",new Vector2(800,60),new Vector2(0,50),34);
        yield return null;
        var operation=SceneManager.LoadSceneAsync(scene);operation.allowSceneActivation=false;
        while(operation.progress<.9f){fill.fillAmount=Mathf.Clamp01(operation.progress/.9f);yield return null;}
        fill.fillAmount=1;yield return null;operation.allowSceneActivation=true;
        while(!operation.isDone)yield return null;
        instance=null;Destroy(gameObject);
    }
    private void OnDestroy(){if(instance==this)instance=null;}
}
