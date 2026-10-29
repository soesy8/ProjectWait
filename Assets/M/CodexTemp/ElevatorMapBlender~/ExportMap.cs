// Evaluated by Unity Pipeline; no source scene or source asset is saved.
const string outputDir = "Assets/M/CodexTemp/ElevatorMapBlender~";
const string scenePath = "Assets/M/Scene/Elevator_Safety_Prototype.unity";
System.IO.Directory.CreateDirectory(outputDir + "/Textures");
if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Export requires Edit Mode.");
var originalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (originalScene.path != scenePath) throw new Exception("Open the elevator scene first.");
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(scenePath);
float[] V(Vector3 v) => new[] {v.x,v.y,v.z};
float[] C(Color c) => new[] {c.r,c.g,c.b,c.a};
float[] Q(Quaternion q) => new[] {q.x,q.y,q.z,q.w};
float[] U(Vector4 v) => new[] {v.x,v.y,v.z,v.w};
var materials = new Dictionary<string, Material>();
var textures = new Dictionary<int, string>();
var warnings = new List<string>();
string MaterialId(Material m) { if (!m) return "missing"; var id=m.GetInstanceID().ToString(); materials[id]=m; return id; }
string TextureFile(Texture texture) {
    if (!texture) return null;
    int id=texture.GetInstanceID(); if(textures.TryGetValue(id,out var previous))return previous;
    string path=outputDir+"/Textures/atlas_"+textures.Count+".png";
    var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);
    var old=RenderTexture.active; var copy=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,true);
    try { Graphics.Blit(texture,rt); RenderTexture.active=rt;copy.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);copy.Apply();System.IO.File.WriteAllBytes(path,copy.EncodeToPNG()); }
    finally { RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(copy); }
    textures[id]=path;return path;
}
object Geometry(Mesh mesh, Mesh styles) {
    if (!mesh || mesh.vertexCount==0) return null;
    var channels = new Dictionary<string, float[][]>();
    for(int i=0;i<8;i++){var uv=new List<Vector4>();styles.GetUVs(i,uv);if(uv.Count==mesh.vertexCount)channels[i.ToString()]=uv.Select(U).ToArray();}
    return new {name=styles.name,path=AssetDatabase.GetAssetPath(styles),vertices=mesh.vertices.Select(V).ToArray(),normals=mesh.normals.Select(V).ToArray(),
        colors=styles.colors.Select(C).ToArray(),uv=channels,
        submeshes=Enumerable.Range(0,mesh.subMeshCount).Select(i=>mesh.GetTriangles(i)).ToArray()};
}
try {
    var root=scene.GetRootGameObjects().Single(g=>g.name=="Elevator_Safety_Lesson").transform;
    bool Excluded(Transform t){while(t && t!=root){if(t.name=="CinematicLetterbox")return true;t=t.parent;}return false;}
    foreach(var text in root.GetComponentsInChildren<TMPro.TextMeshPro>(true).Where(t=>!Excluded(t.transform))) text.ForceMeshUpdate(true,true);
    var parts=new List<object>();int meshCount=0;long vertexCount=0,triangles=0;
    foreach(var t in root.GetComponentsInChildren<Transform>(true).Where(t=>!Excluded(t))) {
        var r=t.GetComponent<Renderer>();var mf=t.GetComponent<MeshFilter>();var skin=t.GetComponent<SkinnedMeshRenderer>();var text=t.GetComponent<TMPro.TMP_Text>();
        Mesh mesh=mf?mf.sharedMesh:null;Mesh styles=mesh;Mesh temporary=null;
        if(skin && skin.sharedMesh){temporary=new Mesh();skin.BakeMesh(temporary,false);mesh=temporary;styles=skin.sharedMesh;}
        object geometry=null;
        try { if(mesh && mesh.vertexCount>0){geometry=Geometry(mesh,styles);meshCount++;vertexCount+=mesh.vertexCount;triangles+=Enumerable.Range(0,mesh.subMeshCount).Sum(i=>(long)mesh.GetIndexCount(i)/3);} }
        finally {if(temporary)UnityEngine.Object.DestroyImmediate(temporary);}
        var parent=t==root?null:t.parent.GetInstanceID().ToString();
        var hierarchy=new List<string>();for(var p=t;p && p!=root.parent;p=p.parent)hierarchy.Add(p.name);hierarchy.Reverse();
        parts.Add(new {id=t.GetInstanceID().ToString(),name=t.name,parent,path=string.Join("/",hierarchy),
            localPosition=V(t.localPosition),localRotation=Q(t.localRotation),localScale=V(t.localScale),worldPosition=V(t.position),
            active=t.gameObject.activeInHierarchy,visible=t.gameObject.activeInHierarchy && (!r || r.enabled),
            materials=r?r.sharedMaterials.Select(MaterialId).ToArray():new string[0],geometry,
            text=text?new {content=text.text,fontPath=AssetDatabase.GetAssetPath(text.font.sourceFontFile),fontSize=text.fontSize,color=C(text.color),alignment=text.alignment.ToString(),rect=new[]{text.rectTransform.rect.width,text.rectTransform.rect.height}}:null,
            note=skin?"Skinned character exported as current-pose mesh; game animation and armature not exported.":null});
    }
    var materialData=new List<object>();
    foreach(var pair in materials){var m=pair.Value;var colors=new Dictionary<string,float[]>();var floats=new Dictionary<string,float>();
        foreach(var prop in new[]{"_BaseColor","_Color","_FaceColor","_ShadeColor","_OutlineColor"})if(m.HasProperty(prop))colors[prop]=C(m.GetColor(prop));
        foreach(var prop in new[]{"_MidTone","_ShadowThreshold","_LightThreshold","_BandSoftness","_OutlineWidth","_EmissionStrength","_FaceDilate"})if(m.HasProperty(prop))floats[prop]=m.GetFloat(prop);
        materialData.Add(new{id=pair.Key,name=m.name,path=AssetDatabase.GetAssetPath(m),shader=m.shader.name,colors,floats,texture=TextureFile(m.mainTexture)});
    }
    var camera=originalScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));
    var sun=originalScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l=>l.type==LightType.Directional);
    var data=new {format="ProjectWait.ElevatorMapBlender.v1",sourceScene=scenePath,unityVersion=Application.unityVersion,linearColors=QualitySettings.activeColorSpace==ColorSpace.Linear,
        parts,materials=materialData,meshes=meshCount,vertices=vertexCount,triangles,
        camera=new{position=V(camera.transform.position),rotation=Q(camera.transform.rotation),fov=camera.fieldOfView,near=camera.nearClipPlane,far=camera.farClipPlane},
        sun=sun?new{direction=V(-sun.transform.forward),color=C(sun.color),intensity=sun.intensity}:null,warnings};
    string output=outputDir+"/Elevator_Map_Source.json";
    System.IO.File.WriteAllText(output,Newtonsoft.Json.JsonConvert.SerializeObject(data),new System.Text.UTF8Encoding(false));
    return new {output,parts=parts.Count,meshes=meshCount,vertices=vertexCount,triangles,materials=materials.Count,textures=textures.Count,warnings,sourceSceneDirty=originalScene.isDirty};
}
finally {UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
