using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Level01AcceptanceRunner
{
    static Level01AcceptanceRunner() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists("Temp/level01-configure.request") && !EditorApplication.isPlayingOrWillChangePlaymode)
        { Level01Setup.Configure(); File.Delete("Temp/level01-configure.request"); return; }
        if (File.Exists("Temp/level01-acceptance.request"))
        {
            if (File.Exists("Temp/level01-configure.request") && !EditorApplication.isPlayingOrWillChangePlaymode)
            { Level01Setup.Configure(); File.Delete("Temp/level01-configure.request"); return; }
            if (!EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying=true;
            }
            else
            {
                File.Delete("Temp/level01-acceptance.request");
                new GameObject("Level01Acceptance_RuntimeOnly").AddComponent<Level01Acceptance>();
            }
        }
    }
}
