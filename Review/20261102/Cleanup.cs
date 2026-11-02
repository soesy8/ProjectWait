using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ReviewCleanup
{
    public static object Main()
    {
        if (EditorApplication.isPlaying) throw new Exception("Play mode is still active.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.isDirty) throw new Exception("Refusing cleanup with unsaved scene edits.");
        var before = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Review/20261102/editor_before.json"));
        var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var view = EditorWindow.GetWindow(type);
        type.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(view, (int)before["sizeIndex"]);
        Application.runInBackground = (bool)before["background"];
        return new { scene = scene.path, scene.isDirty, playing = EditorApplication.isPlaying, background = Application.runInBackground };
    }
}
