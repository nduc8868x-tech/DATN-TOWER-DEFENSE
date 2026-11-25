using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GameGuiCoordinator : MonoBehaviour
{
    public TdGuiTheme theme;
    public LevelHUD hud;
    private GameSession session;
    private GameObject result;
    private TdGuiScreen pauseScreen;
    private int sessionId,startingLives=20,awardedSession;
    private bool suppressPause;
    private void Start()
    {
        if(theme==null)theme=Resources.Load<TdGuiTheme>("TdGuiTheme");GuiAudio.Ensure();
        if(hud==null)hud=GetComponent<LevelHUD>();
        if(transform.Find("Gameplay GUI Links")==null)
        {
            var links=TdGuiScreen.Rect(transform,"Gameplay GUI Links",new Vector2(1920,1080),Vector2.zero);
            var settings=TdGuiScreen.Button(links,"Settings",theme.Get("menu/button_settings"),new Vector2(70,70),new Vector2(-840,-470));settings.onClick.AddListener(()=>Utility(TdGuiScreen.Kind.Settings));
            var help=TdGuiScreen.Button(links,"Help",theme.Get("levels/btton_empty"),new Vector2(70,70),new Vector2(-745,-470),"?");help.onClick.AddListener(()=>Utility(TdGuiScreen.Kind.Help));
        }
        Bind();
    }
    private void LateUpdate(){Bind();}
    private void Bind()
    {
        var next=hud!=null&&hud.flow!=null?hud.flow.Session:null;if(session==next)return;
        Unbind();session=next;if(session==null)return;
        session.StateChanged+=StateChanged;session.SnapshotChanged+=Snapshot;Snapshot(session.GetSnapshot());StateChanged(session.State);
    }
    private void Snapshot(SessionSnapshot snapshot)
    {
        if(session!=null&&session.SessionId!=sessionId){sessionId=session.SessionId;startingLives=Mathf.Max(1,snapshot.Lives);ClearResult();ClearOverlays();}
    }
    private void StateChanged(GameState state)
    {
        if(state==GameState.Victory||state==GameState.Defeat){ClearOverlays();ShowResult(state==GameState.Victory);return;}
        if(state==GameState.Paused&&!suppressPause&&pauseScreen==null)
            pauseScreen=TdGuiScreen.OpenOverlay(TdGuiScreen.Kind.Pause,theme,transform,()=>{if(session!=null&&session.State==GameState.Paused)session.Resume();pauseScreen=null;});
        if(state==GameState.Preparing||state==GameState.Running){ClearOverlays();ClearResult();}
    }
    private void Utility(TdGuiScreen.Kind kind)
    {
        if(session==null||!session.BuildAllowed)return;suppressPause=true;bool resume=session.Pause();suppressPause=false;
        TdGuiScreen.OpenOverlay(kind,theme,transform,()=>{if(resume&&session!=null&&session.State==GameState.Paused)session.Resume();});
    }
    private void ShowResult(bool won)
    {
        if(result!=null)return;var canvas=TdGuiScreen.Rect(transform,won?"Victory GUI":"Defeat GUI",new Vector2(1920,1080),Vector2.zero);result=canvas.gameObject;
        var shade=TdGuiScreen.Picture(canvas,"Shade",theme.Get("win/dark"),new Vector2(4000,3000),Vector2.zero);shade.raycastTarget=true;
        string group=won?"win":"failed";
        TdGuiScreen.Picture(canvas,"Result Board",theme.Get(group+"/table"),new Vector2(510,640),Vector2.zero);
        TdGuiScreen.Picture(canvas,"Result Header",theme.Get(group+"/header_"+(won?"win":"failed")),new Vector2(335,115),new Vector2(0,285));
        TdGuiScreen.Picture(canvas,"Inner Window",theme.Get(won?"win/window":"failed/wondow"),new Vector2(365,430),new Vector2(0,-5));
        int stars=0;
        if(won)
        {
            string scene=gameObject.scene.name;if(awardedSession!=session.SessionId){stars=PlayerProgress.Award(scene,session.Lives,startingLives);awardedSession=session.SessionId;}else stars=PlayerProgress.Stars(scene);
        }
        TdGuiScreen.Picture(canvas,"Stars",theme.Get("win/star_"+(won?stars+1:1)),new Vector2(285,160),new Vector2(0,100));
        TdGuiScreen.Label(canvas,"Message",won?"LEVEL COMPLETE\n"+session.Lives+" LIVES REMAIN\nBEST: "+PlayerProgress.Stars(gameObject.scene.name)+" STARS":"BASE LOST\nTry another tower placement\nor change difficulty.",new Vector2(365,200),new Vector2(0,-70),30);
        var levels=TdGuiScreen.Button(canvas,"Level Select",theme.Get(won?"win/button_menu":"failed/button_left"),new Vector2(105,105),new Vector2(-110,-265));levels.onClick.AddListener(()=>{GuiAudio.Click();GuiNavigation.Go("Levels",theme);});
        var retry=TdGuiScreen.Button(canvas,won?"Next":"Retry",theme.Get(won?"win/button_right":"failed/button_restart"),new Vector2(105,105),new Vector2(110,-265));
        if(won)
        {
            int level=1;int.TryParse(gameObject.scene.name.Replace("Level",""),out level);string next="Level"+(level+1).ToString("00");retry.interactable=Application.CanStreamedLevelBeLoaded(next);
            retry.onClick.AddListener(()=>{GuiNavigation.PendingLevel=next;GuiNavigation.ReturnScene="Levels";GuiNavigation.Go("Difficulty",theme);});
            if(!retry.interactable)TdGuiScreen.Label(canvas,"More Levels","More levels coming soon",new Vector2(400,40),new Vector2(0,-355),23);
        }
        else retry.onClick.AddListener(()=>{GuiAudio.Click();hud.flow.StartNewSession();});
    }
    private void ClearResult(){if(result==null)return;result.SetActive(false);Destroy(result);result=null;}
    private void ClearOverlays(){foreach(var gui in GetComponentsInChildren<TdGuiScreen>(true))if(gui.overlay){gui.gameObject.SetActive(false);Destroy(gui.gameObject);}pauseScreen=null;}
    private void ClearPause(){if(pauseScreen==null)return;pauseScreen.gameObject.SetActive(false);Destroy(pauseScreen.gameObject);pauseScreen=null;}
    private void Unbind(){if(session!=null){session.StateChanged-=StateChanged;session.SnapshotChanged-=Snapshot;}session=null;}
    private void OnDisable(){Unbind();ClearOverlays();ClearResult();}
}
