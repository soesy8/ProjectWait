using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEditor;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

public static class CaptureElevatorReadability
{
    public static async Task<object> Main()
    {
        var l = Object.FindFirstObjectByType<ElevatorLesson>();
        if (!Application.isPlaying || !l || l.Flow == null) throw new System.Exception("Enter Play Mode first.");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        l.enabled = false;
        var c = Object.FindFirstObjectByType<ElevatorDesktopControls>();
        c.SetMouseLook(false);
        var p = l.friend.position + Vector3.up * 1.15f;
        var angle = Quaternion.LookRotation(p - l.playerCamera.transform.position).eulerAngles;
        c.SetLookAngles(angle.y, Mathf.DeltaAngle(0, angle.x));
        l.view.spatial.Tick();
        await Task.Delay(300);
        Canvas.ForceUpdateCanvases();
        string dir = "Assets/M/CodexTemp/UIReadability_20261103~/";
        ScreenCapture.CaptureScreenshot(dir + "BeforeLobby.png");
        var report = new
        {
            buttons = l.view.ChoiceButtons.Select(b => new { b.name, label = b.GetComponentInChildren<TMP_Text>().text,
                size = b.GetComponentInChildren<TMP_Text>().fontSize, style = b.GetComponentInChildren<TMP_Text>().fontStyle.ToString(),
                rect = ((RectTransform)b.transform).sizeDelta.ToString(), overflow = b.GetComponentInChildren<TMP_Text>().isTextOverflowing }).ToArray(),
            thought = l.view.spatial.storybook ? AssetDatabase.GetAssetPath(l.view.spatial.storybook.thoughtBubblePrefab) : "",
            font = l.view.font.name, weight = l.view.font.boldStyle,
            fallback = l.view.font.fallbackFontAssetTable.Select(f => new { f.name, f.boldStyle, f.atlasPadding, mode = f.atlasPopulationMode.ToString() }).ToArray()
        };
        File.WriteAllText(dir + "LiveBefore.json", Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
        await Task.Delay(200);
        l.enabled = true;
        return new { success = true };
    }
}