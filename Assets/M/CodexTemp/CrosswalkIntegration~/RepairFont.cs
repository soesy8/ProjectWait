using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using ProjectWait.M.Crosswalk;
using ProjectWait.M.SceneTransition;

public static class RepairCrosswalkFont
{
    public static object Inspect()
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/CrosswalkIntegration/CrosswalkIntegrationKorean.asset");
        var original=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/SceneTransition/StoryTransitionKorean.asset");
        var pixels=font.atlasTexture.GetPixels32();
        return new {font=font.name,characters=font.characterTable.Count,mode=font.atlasPopulationMode.ToString(),nonzero=pixels.Count(p=>p.a>0),maxAlpha=pixels.Max(p=>p.a),material=font.material.name,shader=font.material.shader.name,mainTexture=font.material.mainTexture?font.material.mainTexture.name:"null",atlas=font.atlasTexture.name,face=font.material.GetColor("_FaceColor"),gradient=font.material.GetFloat("_GradientScale"),originalGradient=original.material.GetFloat("_GradientScale"),width=font.material.GetFloat("_TextureWidth"),originalShader=original.material.shader.name};
    }
    public static object MaterialDiff()
    {
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/CrosswalkIntegration/CrosswalkIntegrationKorean.asset");
        var original=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/SceneTransition/StoryTransitionKorean.asset");
        return Enumerable.Range(0,font.material.shader.GetPropertyCount()).Select(i=>font.material.shader.GetPropertyName(i)).Where(n=>font.material.shader.GetPropertyType(font.material.shader.FindPropertyIndex(n))==UnityEngine.Rendering.ShaderPropertyType.Float||font.material.shader.GetPropertyType(font.material.shader.FindPropertyIndex(n))==UnityEngine.Rendering.ShaderPropertyType.Range).Select(n=>new{name=n,value=font.material.GetFloat(n),original=original.material.GetFloat(n)}).Where(v=>v.value!=v.original).ToArray();
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode");
        const string path="Assets/M/Arts/Fonts/CrosswalkIntegration/CrosswalkIntegrationKorean.asset";
        var original=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/SceneTransition/StoryTransitionKorean.asset");
        var source=AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath("4b050669ad79a1e4ebd78d5a9c5f2976"));
        if(!source)throw new InvalidOperationException("Korean source font missing");
        string required=CrosswalkSceneIntegration.Context+"이야기 로비 잠깐의 이야기를 마치고, 다음 이야기를 만나러 가요. 이야기 잠깐 쉬어가요 이야기를 잠시 멈췄어요. 준비되면 계속해요. 계속하기 처음부터 다시 로비로 돌아가기 끝내기 PC: Esc VR: 메뉴 버튼 장면을 열지 못했어요. 다시 시도해 주세요. 장면을 준비하지 못했어요. 잠시 후 다시 시도해 주세요. 새로운 이야기가 곧 시작돼요.";
        required+=new string(original.characterTable.Where(c=>c.unicode<=char.MaxValue).Select(c=>(char)c.unicode).ToArray());
        required=new string(required.Where(c=>!char.IsControl(c)).Distinct().ToArray());
        var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if(!font)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));AssetDatabase.Refresh();
            font=TMP_FontAsset.CreateFontAsset(source,64,8,GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic);
            font.name="CrosswalkIntegrationKorean";font.isMultiAtlasTexturesEnabled=false;
            if(!font.TryAddCharacters(required,out string missing))throw new InvalidOperationException("Glyphs missing: "+missing);
            foreach(var texture in font.atlasTextures)texture.Apply(false,false);
            font.material.shader=original.material.shader;
            font.material.SetFloat("_ZTestMode",0);
            font.atlasPopulationMode=AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(font,path);
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var texture in font.atlasTextures)AssetDatabase.AddObjectToAsset(texture,font);
            EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
        }
        foreach(var texture in font.atlasTextures) { texture.Apply(false,false);EditorUtility.SetDirty(texture); }
        font.material.mainTexture=font.atlasTexture;
        // The paper overlay renders at 4100. Default TMP Overlay is only 4000;
        // copying the shader alone leaves every glyph hidden behind the card.
        font.material.renderQueue=original.material.renderQueue;
        font.ReadFontAssetDefinition();
        EditorUtility.SetDirty(font.material);EditorUtility.SetDirty(font);
        if(!font.HasCharacters(required))throw new InvalidOperationException("Baked character coverage check failed");
        foreach(string scenePath in new[]{CrosswalkSceneIntegration.ScenePath,CrosswalkSceneIntegration.LobbyPath})
        {
            var scene=EditorSceneManager.OpenScene(scenePath);
            foreach(var t in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StorybookSceneTransition>(true))) { t.font=font;EditorUtility.SetDirty(t); }
            foreach(var integration in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CrosswalkSceneIntegration>(true)))
                foreach(var text in integration.menuCanvas.GetComponentsInChildren<TMP_Text>(true)) { text.font=font;text.fontSharedMaterial=font.material;EditorUtility.SetDirty(text); }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        return "Static Korean font baked: "+font.characterTable.Count+" characters, atlas "+font.atlasWidth+"x"+font.atlasHeight+", source font unchanged.";
    }
}
