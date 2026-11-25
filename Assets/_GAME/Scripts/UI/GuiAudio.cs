using UnityEngine;
using UnityEngine.UI;

public sealed class GuiAudio : MonoBehaviour
{
    private static GuiAudio instance;
    public AudioClip musicClip;
    private AudioSource music,sfx;
    private AudioClip click;
    private static float lastClick;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){instance=null;lastClick=-1;}
    public static void Ensure()
    {
        if(instance!=null)return;
        var go=new GameObject("GUI Audio");DontDestroyOnLoad(go);instance=go.AddComponent<GuiAudio>();
    }
    private void Awake()
    {
        if(instance!=null&&instance!=this){Destroy(gameObject);return;}instance=this;DontDestroyOnLoad(gameObject);
        var theme=Resources.Load<TdGuiTheme>("TdGuiTheme");if(musicClip==null&&theme!=null)musicClip=theme.music;
        music=gameObject.AddComponent<AudioSource>();sfx=gameObject.AddComponent<AudioSource>();music.loop=true;music.ignoreListenerPause=sfx.ignoreListenerPause=true;
        int n=2205;var samples=new float[n];for(int i=0;i<n;i++)samples[i]=Mathf.Sin(i*2*Mathf.PI*850/44100)*.12f*(1-i/(float)n);
        click=AudioClip.Create("UI Click",n,1,44100,false);click.SetData(samples,0);
        PlayerProgress.Changed+=Apply;Apply();
    }
    private void Update(){if(musicClip!=null&&music.clip!=musicClip){music.clip=musicClip;music.Play();}}
    private void Apply(){music.volume=PlayerProgress.Data.music;sfx.volume=PlayerProgress.Data.sound;}
    public static void Click(){Ensure();if(Time.unscaledTime-lastClick<.04f)return;lastClick=Time.unscaledTime;instance.sfx.PlayOneShot(instance.click);}
    private void OnDestroy(){PlayerProgress.Changed-=Apply;if(click!=null)Destroy(click);if(instance==this)instance=null;}
}
