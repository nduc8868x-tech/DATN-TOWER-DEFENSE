using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TdGuiScreen : MonoBehaviour
{
    public enum Kind { Settings = 0, Difficulty = 1, Help = 3, Pause = 4 }
    public TdGuiTheme theme;
    public Kind page;
    public bool overlay;
    [NonSerialized] public Action closed;
    private readonly List<Action> unsubscribe=new List<Action>();
    private Transform panel;
    private bool busy;
    public static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
    {
        var go=new GameObject(name,typeof(RectTransform));var r=(RectTransform)go.transform;r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    public static Image Picture(Transform parent,string name,Sprite sprite,Vector2 size,Vector2 position)
    {
        var r=Rect(parent,name,size,position);var im=r.gameObject.AddComponent<Image>();im.sprite=sprite;im.raycastTarget=false;return im;
    }
    public static Text Label(Transform parent,string name,string text,Vector2 size,Vector2 position,int fontSize=28)
    {
        var r=Rect(parent,name,size,position);var t=r.gameObject.AddComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=text;t.fontSize=fontSize;t.color=new Color(1,.88f,.55f);t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;
        var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.12f,.05f,0,.8f);outline.effectDistance=new Vector2(1,-1);return t;
    }
    public static Button Button(Transform parent,string name,Sprite sprite,Vector2 size,Vector2 position,string caption="")
    {
        var im=Picture(parent,name,sprite,size,position);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;
        var c=b.colors;c.highlightedColor=new Color(1,1,.8f);c.pressedColor=new Color(.7f,.7f,.7f);c.disabledColor=new Color(.4f,.4f,.4f,.8f);b.colors=c;
        var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;
        if(caption.Length>0)Label(b.transform,"Caption",caption,size*.92f,Vector2.zero,26);return b;
    }
    public static TdGuiScreen OpenOverlay(Kind kind,TdGuiTheme art,Transform parent,Action onClose=null)
    {
        var go=new GameObject(kind+" Overlay",typeof(RectTransform));go.transform.SetParent(parent,false);
        go.SetActive(false);var screen=go.AddComponent<TdGuiScreen>();screen.theme=art;screen.page=kind;screen.overlay=true;screen.closed=onClose;screen.Build();go.SetActive(true);return screen;
    }
    private Sprite Art(string key)=>theme!=null?theme.Get(key):null;
    public void Build()
    {
        if(transform.Find("GUI Page")!=null)return;
        if(theme==null)theme=Resources.Load<TdGuiTheme>("TdGuiTheme");
        panel=Rect(transform,"GUI Page",new Vector2(1920,1080),Vector2.zero);
        if(!overlay){var bg=Picture(panel,"Background",Art("menu/bg"),new Vector2(1920,1080),Vector2.zero);var fit=bg.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=16f/9;}
        var shade=Picture(panel,"Shade",Art("menu/dark"),new Vector2(4000,3000),Vector2.zero);shade.color=new Color(1,1,1,.9f);shade.raycastTarget=true;
        string folder=page==Kind.Settings?"settings":page==Kind.Difficulty?"difficulty":"settings";
        Picture(panel,"Board",Art(folder+"/table"),page==Kind.Difficulty?new Vector2(700,820):new Vector2(1050,690),new Vector2(0,-10));
        if(page!=Kind.Difficulty){Picture(panel,"Left Rope",Art("settings/rope_big"),new Vector2(37,310),new Vector2(-360,385));Picture(panel,"Right Rope",Art("settings/rope_big"),new Vector2(37,310),new Vector2(360,385));}
        string header=page==Kind.Settings?"settings/header_settings":page==Kind.Difficulty?"difficulty/header_diff":"";
        if(header.Length>0)Picture(panel,"Header",Art(header),new Vector2(400,130),new Vector2(0,315));
        else Label(panel,"Header",page==Kind.Pause?"PAUSED":"HOW TO PLAY",new Vector2(700,90),new Vector2(0,270),48);
        Button(panel,"Close",Art("settings/button_close"),new Vector2(90,90),page==Kind.Difficulty?new Vector2(330,370):new Vector2(500,310));
        Label(panel,"Status","",new Vector2(850,60),new Vector2(0,-270),25);
        if(page==Kind.Settings)
        {
            Volume("Music",145,"MUSIC");Volume("Sound",10,"SOUND EFFECTS");
            Label(panel,"Quality Label","GRAPHICS",new Vector2(300,50),new Vector2(-260,-125),30);
            Button(panel,"Quality Minus",Art("settings/button_minus"),new Vector2(55,55),new Vector2(-50,-125));
            Label(panel,"Quality",QualitySettings.names[QualitySettings.GetQualityLevel()],new Vector2(300,55),new Vector2(160,-125),26);
            Button(panel,"Quality Plus",Art("settings/button_plus"),new Vector2(55,55),new Vector2(370,-125));
        }
        else if(page==Kind.Difficulty)
        {
            foreach(var pair in new[]{new[]{"Easy","0.8x enemy health"},new[]{"Normal","Original enemy health"},new[]{"Hard","1.3x enemy health"}})
            {
                int i=pair[0]=="Easy"?0:pair[0]=="Normal"?1:2;float y=200-i*180;
                Button(panel,pair[0],Art("difficulty/button_"+pair[0].ToLowerInvariant()),new Vector2(300,100),new Vector2(0,y));
                Label(panel,pair[0]+" Info",pair[1],new Vector2(500,50),new Vector2(0,y-70),27);
            }
        }
        else if(page==Kind.Help)
        {
            Picture(panel,"Guide Bubble",Art("empty_table/table_2"),new Vector2(850,430),new Vector2(0,0));
            var text=Label(panel,"Instructions","1. Select a build point to buy a tower.\n2. Select a tower to upgrade, sell or view its range.\n3. Tap the skull to call the next wave early.\n4. Speed switches 1x / 2x; Pause stops the battle.\n5. Protect the base and finish every wave.\n6. Victory earns 1-3 stars for your level rating.",new Vector2(730,340),new Vector2(0,0),29);text.color=new Color(.22f,.12f,.04f);text.GetComponent<Outline>().enabled=false;
        }
        else
        {
            Button(panel,"Resume",Art("levels/btton_empty"),new Vector2(320,90),new Vector2(0,140),"RESUME");
            Button(panel,"Settings",Art("levels/btton_empty"),new Vector2(320,90),new Vector2(0,20),"SETTINGS");
            Button(panel,"Restart",Art("levels/btton_empty"),new Vector2(320,90),new Vector2(0,-100),"RESTART");
            Button(panel,"Levels",Art("levels/btton_empty"),new Vector2(320,90),new Vector2(0,-220),"LEVEL SELECT");
        }
    }
    private void Volume(string name,float y,string caption)
    {
        Label(panel,name+" Label",caption,new Vector2(320,55),new Vector2(-260,y),30);
        var bg=Picture(panel,name+" Slider",Art("settings/bar_bg"),new Vector2(350,35),new Vector2(130,y));bg.raycastTarget=true;
        var fill=Picture(bg.transform,"Fill",Art("settings/bar"),new Vector2(350,25),Vector2.zero);fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
        var slider=bg.gameObject.AddComponent<Slider>();slider.fillRect=fill.rectTransform;slider.minValue=0;slider.maxValue=1;slider.transition=Selectable.Transition.None;
        Button(panel,name+" Toggle",Art("settings/button_on"),new Vector2(95,45),new Vector2(390,y));
    }
    private Transform Find(string name)=>transform.Find("GUI Page/"+name);
    private void Hook(string name,UnityAction action)
    {
        var t=Find(name);if(t==null)return;var b=t.GetComponent<Button>();UnityAction call=()=>{GuiAudio.Click();action();};b.onClick.AddListener(call);unsubscribe.Add(()=>{if(b!=null)b.onClick.RemoveListener(call);});
    }
    private void OnEnable()
    {
        if(!Application.isPlaying)return;if(theme==null)theme=Resources.Load<TdGuiTheme>("TdGuiTheme");if(theme==null)return;
        Build();GuiAudio.Ensure();Hook("Close",Close);
        if(page==Kind.Settings)
        {
            BindVolume("Music",true);BindVolume("Sound",false);Hook("Quality Minus",()=>Quality(-1));Hook("Quality Plus",()=>Quality(1));
        }
        else if(page==Kind.Difficulty){Hook("Easy",()=>StartLevel(0));Hook("Normal",()=>StartLevel(1));Hook("Hard",()=>StartLevel(2));}
        else if(page==Kind.Pause)
        {
            Hook("Resume",Close);Hook("Settings",()=>{gameObject.SetActive(false);OpenOverlay(Kind.Settings,theme,transform.parent,()=>{if(this!=null)gameObject.SetActive(true);});});
            Hook("Restart",()=>{closed=null;var flow=UnityEngine.Object.FindFirstObjectByType<FlowBootstrapper>();if(flow!=null)flow.StartNewSession();Destroy(gameObject);});
            Hook("Levels",()=>GuiNavigation.Go("Levels",theme));
        }
        PlayerProgress.Changed+=Refresh;unsubscribe.Add(()=>PlayerProgress.Changed-=Refresh);Refresh();
    }
    private void OnDisable(){foreach(var action in unsubscribe)action();unsubscribe.Clear();}
    private void BindVolume(string name,bool music)
    {
        var slider=Find(name+" Slider").GetComponent<Slider>();slider.SetValueWithoutNotify(music?PlayerProgress.Data.music:PlayerProgress.Data.sound);
        UnityAction<float> action=value=>{if(music)PlayerProgress.Data.music=value;else PlayerProgress.Data.sound=value;PlayerProgress.Store();};slider.onValueChanged.AddListener(action);unsubscribe.Add(()=>{if(slider!=null)slider.onValueChanged.RemoveListener(action);});
        Hook(name+" Toggle",()=>{float value=(music?PlayerProgress.Data.music:PlayerProgress.Data.sound)>0?0:.7f;if(music)PlayerProgress.Data.music=value;else PlayerProgress.Data.sound=value;PlayerProgress.Store();});
    }
    private void Refresh()
    {
        if(page==Kind.Settings)
        {
            foreach(string name in new[]{"Music","Sound"}){float value=name=="Music"?PlayerProgress.Data.music:PlayerProgress.Data.sound;Find(name+" Slider").GetComponent<Slider>().SetValueWithoutNotify(value);Find(name+" Toggle").GetComponent<Image>().sprite=Art("settings/button_"+(value>0?"on":"off"));}
        }
    }
    private void Quality(int delta){int value=Mathf.Clamp(QualitySettings.GetQualityLevel()+delta,0,QualitySettings.names.Length-1);QualitySettings.SetQualityLevel(value);PlayerProgress.Data.quality=value;PlayerProgress.Store();Find("Quality").GetComponent<Text>().text=QualitySettings.names[value];}
    private void StartLevel(int difficulty){if(busy)return;PlayerProgress.Data.difficulty=difficulty;PlayerProgress.Store();busy=GuiNavigation.Go(GuiNavigation.PendingLevel,theme);}
    private void Close(){if(overlay){var action=closed;closed=null;Destroy(gameObject);action?.Invoke();}else GuiNavigation.Go(GuiNavigation.ReturnScene==gameObject.scene.name?"MainMenu":GuiNavigation.ReturnScene,theme);}
}
