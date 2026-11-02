using System;
using System.Collections.Generic;
using UnityEditor;

public static class PrepareSharedAssets
{
    static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var split = path.LastIndexOf('/');
        Folder(path.Substring(0, split));
        AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
    }

    public static string Main()
    {
        Folder("Assets/Shared Assets/Highlight/Runtime");
        Folder("Assets/Shared Assets/Highlight/Shaders");
        Folder("Assets/Shared Assets/UI/Runtime");
        var moves = new Dictionary<string, string>
        {
            { "Assets/M/Scripts/Editor/OutlineRendererTool", "Assets/Shared Assets/Highlight/Editor" },
            { "Assets/M/Scripts/Elevator/ElevatorOutlineTarget.cs", "Assets/Shared Assets/Highlight/Runtime/ElevatorOutlineTarget.cs" },
            { "Assets/M/Scripts/Elevator/ElevatorSilhouetteOutlineFeature.cs", "Assets/Shared Assets/Highlight/Runtime/ElevatorSilhouetteOutlineFeature.cs" },
            { "Assets/M/Scripts/Elevator/ElevatorSceneRendering.cs", "Assets/Shared Assets/Highlight/Runtime/ElevatorSceneRendering.cs" },
            { "Assets/M/Arts/Shaders/ElevatorOutlineCoverage.shader", "Assets/Shared Assets/Highlight/Shaders/ElevatorOutlineCoverage.shader" },
            { "Assets/M/Arts/Shaders/ElevatorOutlineDistance.shader", "Assets/Shared Assets/Highlight/Shaders/ElevatorOutlineDistance.shader" },
            { "Assets/M/Scripts/Elevator/StorybookButtonFeedback.cs", "Assets/Shared Assets/UI/Runtime/StorybookButtonFeedback.cs" }
        };
        var moved = new List<string>();
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var move in moves)
            {
                if (AssetDatabase.LoadMainAssetAtPath(move.Value)) continue;
                var error = AssetDatabase.MoveAsset(move.Key, move.Value);
                if (!string.IsNullOrEmpty(error)) throw new Exception(error);
                moved.Add(move.Value);
            }
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.SaveAssets();
        return string.Join("\n", moved);
    }
}
