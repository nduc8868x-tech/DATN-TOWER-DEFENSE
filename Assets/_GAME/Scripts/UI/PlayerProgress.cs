using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerProgress
{
    public const string SaveKey="DATN.PlayerProgress.v1";
    [Serializable] public sealed class Result { public string scene; public int stars; }
    [Serializable] public sealed class Save
    {
        public int version=1;
        public string firstName="",lastName="",email="";
        public float music=.6f,sound=.8f;
        public int difficulty=1,quality=-1;
        public List<Result> results=new List<Result>();
    }
    private static Save data;
    public static Save Data { get { if(data==null)Reload();return data; } }
    public static event Action Changed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){data=null;Changed=null;}
    public static void Reload()
    {
        try { data=JsonUtility.FromJson<Save>(PlayerPrefs.GetString(SaveKey,"")); } catch { data=null; }
        if(data==null)data=new Save();if(data.results==null)data.results=new List<Result>();
        data.difficulty=Mathf.Clamp(data.difficulty,0,2);
        data.music=Mathf.Clamp01(data.music);data.sound=Mathf.Clamp01(data.sound);
    }
    public static void Store(){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(Data));PlayerPrefs.Save();Changed?.Invoke();}
    public static int Stars(string scene){foreach(var r in Data.results)if(r.scene==scene)return Mathf.Clamp(r.stars,0,3);return 0;}
    public static bool IsUnlocked(int number)=>number<=1||Stars("Level"+(number-1).ToString("00"))>0;
    public static int TotalStars
    {
        get { int total=0;foreach(var r in Data.results)total+=Mathf.Clamp(r.stars,0,3);return Mathf.Max(0,total); }
    }
    public static int Award(string scene,int lives,int startingLives)
    {
        float ratio=lives/(float)Mathf.Max(1,startingLives);int stars=ratio>=.8f?3:ratio>=.4f?2:1;
        Result found=null;foreach(var r in Data.results)if(r.scene==scene){found=r;break;}
        if(found==null){found=new Result{scene=scene};Data.results.Add(found);}
        if(stars>found.stars){found.stars=stars;Store();}return stars;
    }
}

// Difficulty is captured once at restart for a consistent match.
public static class GuiRunRules
{
    public static int Difficulty {get;private set;}=1;
    public static float EnemyHealthMultiplier=>Difficulty==0?.8f:Difficulty==2?1.3f:1f;
    public static void Capture(){Difficulty=PlayerProgress.Data.difficulty;}
}
