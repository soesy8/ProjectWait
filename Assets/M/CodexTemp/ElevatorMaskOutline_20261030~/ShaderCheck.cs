using System.Linq;
using UnityEngine;
using UnityEditor;
public static class ShaderCheckMO{public static string Main(){return Newtonsoft.Json.JsonConvert.SerializeObject(new[]{"ProjectWait/M/Outline Coverage","ProjectWait/M/Outline Distance"}.Select(n=>{var s=Shader.Find(n);return new{name=n,found=s!=null,error=s&&ShaderUtil.ShaderHasError(s),messages=s?ShaderUtil.GetShaderMessages(s).Select(m=>m.message).ToArray():new string[0]};}).ToArray());}}