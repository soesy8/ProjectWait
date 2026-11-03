using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectWait.M.Editor;

public static class ApplyElevatorReadability
{
    public static object Main()
    {
        const string dir="Assets/M/CodexTemp/UIReadability_20261103~/Before/";
        Directory.CreateDirectory(dir);
        foreach(string name in new[]{"Elevator_Safety_Prototype","Elevator_Safety_ObjectInteraction_Test"})
            if(!File.Exists(dir+name+".unity"))File.Copy("Assets/M/Scene/"+name+".unity",dir+name+".unity");
        ElevatorReadabilitySetup.Apply();
        return new{success=true,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path};
    }
}