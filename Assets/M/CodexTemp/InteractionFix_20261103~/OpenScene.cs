using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class OpenInteractionScene
{
    public static string Main() => Open("Elevator_Safety_ObjectInteraction_Test");
    public static string Restore() => Open("Elevator_Safety_Prototype");
    static string Open(string name)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Preserve the current scene before switching.");
        if (name != "Elevator_Safety_Prototype" && name != "Elevator_Safety_ObjectInteraction_Test")
            throw new ArgumentException("Only M interaction scenes are allowed.");
        return EditorSceneManager.OpenScene("Assets/M/Scene/" + name + ".unity", OpenSceneMode.Single).path;
    }
}