using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
public static class SportsFrameProof
{
 public static async Task<string> Main(){int before=Time.frameCount;bool original=Application.runInBackground;try{Application.runInBackground=true;await Task.Delay(750);var report=Newtonsoft.Json.JsonConvert.SerializeObject(new{playing=Application.isPlaying,before,after=Time.frameCount,advanced=Time.frameCount>before,unity=Application.unityVersion},Newtonsoft.Json.Formatting.Indented);File.WriteAllText("Assets/M/CodexTemp/UnitySportsCentreRebuild~/FrameProof.json",report);return report;}finally{Application.runInBackground=original;}}
}
