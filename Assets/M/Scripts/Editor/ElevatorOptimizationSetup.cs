using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProjectWait.M.Elevator;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    // All persistent writes are confined to M. Original character sources remain editable.
    public static class ElevatorOptimizationSetup
    {
        public const string Work="Assets/M/CodexTemp/Optimization_20261105~/";
        public const string Evidence="Assets/M/Documentation/Optimization_20261105/";
        static readonly string[] Scenes={"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
        const string MapPrefab="Assets/M/Prefabs/SportsCentre3F/SportsCentreMap.prefab";
        const string ThoughtSource="Assets/Shared Assets/UI/Prefabs/ThoughtCloud.prefab";
        const string ThoughtCopy="Assets/M/UI/Storybook/Prefabs/ThoughtCloudOptimized.prefab";
        public static void Write(string path,object value)
        {
            if(!path.StartsWith("Assets/M/",StringComparison.Ordinal))throw new Exception("Write outside M: "+path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path,JsonConvert.SerializeObject(value,Formatting.Indented));
        }
        public static object Measure(string label)
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var renderers=transforms.SelectMany(t=>t.GetComponents<Renderer>()).ToArray();
            var meshes=renderers.Select(MeshOf).Where(m=>m).Distinct().ToArray();
            var deps=AssetDatabase.GetDependencies(scene.path,true);
            var textures=deps.Where(p=>p.EndsWith(".asset")||p.EndsWith(".png")||p.EndsWith(".tif")||p.EndsWith(".jpg")).SelectMany(AssetDatabase.LoadAllAssetsAtPath).OfType<Texture2D>().Distinct().ToArray();
            var collisions=transforms.SelectMany(t=>t.GetComponents<MeshCollider>()).ToArray();
            var report=new{scene=scene.path,playing=Application.isPlaying,frame=Time.frameCount,objects=transforms.Length,renderers=renderers.Length,activeRenderers=renderers.Count(r=>r.enabled&&r.gameObject.activeInHierarchy),canvases=transforms.Sum(t=>t.GetComponents<Canvas>().Length),meshColliders=collisions.Length,collisionTriangles=collisions.Where(c=>c.sharedMesh).Sum(c=>TriangleCount(c.sharedMesh)),meshTriangles=meshes.Sum(TriangleCount),meshMemory=meshes.Sum(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong),textureMemory=textures.Sum(UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong),rgbaPixels=textures.Where(t=>t.format==TextureFormat.RGBA32).Sum(t=>(long)t.width*t.height*4),sourceBytes=deps.Where(File.Exists).Sum(p=>new FileInfo(p).Length),fonts=deps.Where(p=>p.EndsWith(".otf")).ToArray(),textures=textures.Select(t=>new{path=AssetDatabase.GetAssetPath(t),t.width,t.height,format=t.format.ToString(),memory=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)}).ToArray(),skins=renderers.OfType<SkinnedMeshRenderer>().Where(r=>r.sharedMesh).Select(r=>new{r.name,path=AssetDatabase.GetAssetPath(r.sharedMesh),r.sharedMesh.vertexCount,triangles=TriangleCount(r.sharedMesh),r.sharedMesh.blendShapeCount,r.updateWhenOffscreen}).ToArray()};
            Write(Evidence+label+".json",report);return report;
        }
        static Mesh MeshOf(Renderer r){if(r is SkinnedMeshRenderer sk)return sk.sharedMesh;var f=r.GetComponent<MeshFilter>();return f?f.sharedMesh:null;}
        static long TriangleCount(Mesh m)=>Enumerable.Range(0,m.subMeshCount).Sum(i=>(long)m.GetIndexCount(i)/3);
        [Serializable] public sealed class Geometry
        {
            public string name,path;
            public float[][] vertices,normals,colors,style,shade,ink,bands,weights;
            public int[][] bones;
            public int[] triangles;
            public float ratio,error;
        }
        static float[][] Vectors(Vector3[] a)=>a.Select(v=>new[]{v.x,v.y,v.z}).ToArray();
        static float[][] UV(Mesh m,int channel){var a=new List<Vector4>();m.GetUVs(channel,a);return a.Select(v=>new[]{v.x,v.y,v.z,v.w}).ToArray();}
        public static object ExportGeometry()
        {
            Directory.CreateDirectory(Work+"Input");
            var map=AssetDatabase.LoadAssetAtPath<GameObject>(MapPrefab);
            var collisions=map.GetComponentsInChildren<MeshCollider>(true).Select(c=>c.sharedMesh).Where(m=>m&&TriangleCount(m)>200).Distinct().ToArray();
            foreach(var m in collisions)Write(Work+"Input/"+m.name+".json",new Geometry{name=m.name,path=AssetDatabase.GetAssetPath(m),vertices=Vectors(m.vertices),triangles=m.triangles});
            foreach(var id in new[]{"A","B"})
            {
                var p="Assets/M/Arts/Models/Rescuers/Generated/Rescuer_"+id+"_PaletteSkin.asset";var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);var bw=m.boneWeights;
                Write(Work+"Input/Rescuer_"+id+".json",new Geometry{name="Rescuer_"+id,path=p,vertices=Vectors(m.vertices),normals=Vectors(m.normals),colors=m.colors.Select(c=>new[]{c.r,c.g,c.b,c.a}).ToArray(),style=UV(m,1),shade=UV(m,2),ink=UV(m,3),bands=UV(m,4),triangles=m.triangles,bones=bw.Select(w=>new[]{w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3}).ToArray(),weights=bw.Select(w=>new[]{w.weight0,w.weight1,w.weight2,w.weight3}).ToArray()});
            }
            return new{collisions=collisions.Length,skins=2};
        }
        static Mesh SaveMesh(string p,Mesh created)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(p));var existing=AssetDatabase.LoadAssetAtPath<Mesh>(p);
            if(existing){EditorUtility.CopySerialized(created,existing);Object.DestroyImmediate(created);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(created,p);return created;
        }
        public static object ImportGeometry()
        {
            var reports=new List<object>();var collisionMap=new Dictionary<Mesh,Mesh>();
            foreach(var file in Directory.GetFiles(Work+"Output","*.json"))
            {
                var g=JsonConvert.DeserializeObject<Geometry>(File.ReadAllText(file));if(g==null||g.vertices==null)continue;
                var source=AssetDatabase.LoadAssetAtPath<Mesh>(g.path);if(!source)throw new Exception("Missing source: "+g.path);
                var m=new Mesh{name=g.name+"_Runtime",indexFormat=g.vertices.Length>65535?IndexFormat.UInt32:IndexFormat.UInt16};
                m.vertices=g.vertices.Select(v=>new Vector3(v[0],v[1],v[2])).ToArray();m.SetTriangles(g.triangles,0);m.bounds=source.bounds;
                bool skin=g.name.StartsWith("Rescuer_");
                if(skin)
                {
                    m.normals=g.normals.Select(v=>new Vector3(v[0],v[1],v[2]).normalized).ToArray();
                    m.colors32=g.colors.Select(v=>(Color32)new Color(v[0],v[1],v[2],v[3])).ToArray();
                    int channel=1;foreach(var data in new[]{g.style,g.shade,g.ink})m.SetUVs(channel++,data.Select(v=>new Vector4(v[0],v[1],v[2],v[3])).ToList());
                    m.SetUVs(4,g.bands.Select(v=>new Vector2(v[0],v[1])).ToList());
                    m.bindposes=source.bindposes;m.boneWeights=g.bones.Select((b,i)=>new BoneWeight{boneIndex0=b[0],boneIndex1=b[1],boneIndex2=b[2],boneIndex3=b[3],weight0=g.weights[i][0],weight1=g.weights[i][1],weight2=g.weights[i][2],weight3=g.weights[i][3]}).ToArray();
                }
                string output=skin?"Assets/M/Arts/Models/Rescuers/Generated/Optimized/"+g.name+"_RuntimeSkin.asset":"Assets/M/Arts/Models/SportsCentre3F/Collision/"+g.name+"_Collision.asset";
                var saved=SaveMesh(output,m);if(!skin)collisionMap.Add(source,saved);
                reports.Add(new{g.name,before=TriangleCount(source),after=TriangleCount(saved),g.ratio,g.error,output});
            }
            var root=PrefabUtility.LoadPrefabContents(MapPrefab);
            try
            {
                foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))if(c.sharedMesh&&collisionMap.TryGetValue(c.sharedMesh,out var replacement))c.sharedMesh=replacement;
                PrefabUtility.SaveAsPrefabAsset(root,MapPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
            foreach(var pair in new[]{new[]{"A","Assets/M/Arts/Models/Rescuers/Rescuer_A_Firefighter.prefab"},new[]{"B","Assets/M/Arts/Models/Rescuers/Rescuer_B_LiftSpecialist.prefab"}})
            {
                root=PrefabUtility.LoadPrefabContents(pair[1]);
                try
                {
                    var runtime=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/M/Arts/Models/Rescuers/Generated/Optimized/Rescuer_"+pair[0]+"_RuntimeSkin.asset");
                    if(!runtime)throw new Exception("Missing optimized rescuer "+pair[0]);
                    foreach(var sk in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)){sk.sharedMesh=runtime;sk.skinnedMotionVectors=false;}
                    PrefabUtility.SaveAsPrefabAsset(root,pair[1]);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();Write(Evidence+"GeometryOptimization.json",reports);return reports;
        }
        static void OptimizeTextures()
        {
            foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/M/UI/Storybook/Art/Cartoon"}))
            {
                var p=AssetDatabase.GUIDToAssetPath(guid);var t=AssetImporter.GetAtPath(p) as TextureImporter;if(!t)continue;
                t.maxTextureSize=Mathf.Min(1024,t.maxTextureSize);t.isReadable=false;t.npotScale=TextureImporterNPOTScale.None;
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(p);
                bool blockCompatible=texture&&texture.width%4==0&&texture.height%4==0;
                foreach(var platform in new[]{"Standalone","Android"})
                {
                    var settings=t.GetPlatformTextureSettings(platform);settings.overridden=true;settings.maxTextureSize=t.maxTextureSize;
                    settings.format=blockCompatible?(platform=="Standalone"?TextureImporterFormat.BC7:TextureImporterFormat.ASTC_4x4):TextureImporterFormat.RGBA32;settings.compressionQuality=100;
                    t.SetPlatformTextureSettings(settings);
                }
                t.SaveAndReimport();
            }
        }
        static void PrepareThoughtTemplate()
        {
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(ThoughtCopy))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThoughtCopy));if(!AssetDatabase.CopyAsset(ThoughtSource,ThoughtCopy))throw new Exception("Template copy failed");
            }
            var root=PrefabUtility.LoadPrefabContents(ThoughtCopy);
            try
            {
                foreach(var component in root.GetComponentsInChildren<Component>(true))
                {
                    if(!component)continue;var so=new SerializedObject(component);var it=so.GetIterator();bool changed=false;
                    while(it.Next(true))
                    {
                        if(it.propertyType!=SerializedPropertyType.ObjectReference||!it.objectReferenceValue)continue;
                        var p=AssetDatabase.GetAssetPath(it.objectReferenceValue);string replacement=null;
                        if(p.StartsWith("Assets/Shared Assets/UI/Fonts/"))replacement="Assets/M/Arts/Fonts/"+Path.GetFileName(p);
                        if(p.StartsWith("Assets/Shared Assets/UI/Art/"))replacement="Assets/M/UI/Storybook/Art/Cartoon/"+Path.GetFileName(p);
                        if(replacement==null)continue;
                        var match=AssetDatabase.LoadAllAssetsAtPath(replacement).FirstOrDefault(a=>a.GetType()==it.objectReferenceValue.GetType()&&a.name==it.objectReferenceValue.name)??AssetDatabase.LoadAllAssetsAtPath(replacement).FirstOrDefault(a=>a.GetType()==it.objectReferenceValue.GetType());
                        if(match){it.objectReferenceValue=match;changed=true;}
                    }
                    if(changed)so.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root,ThoughtCopy);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static int CleanScene()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var roots=scene.GetRootGameObjects();
            var transforms=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var references=new HashSet<Object>();
            foreach(var component in roots.SelectMany(g=>g.GetComponentsInChildren<Component>(true)))
            {
                if(!component||component is Transform)continue;var so=new SerializedObject(component);var it=so.GetIterator();bool changed=false;
                while(it.Next(true))
                {
                    if(it.propertyType!=SerializedPropertyType.ObjectReference)continue;
                    if(!it.objectReferenceValue)
                    {
                        if(it.objectReferenceInstanceIDValue!=0&&component is MeshCollider collider&&it.propertyPath=="m_Mesh")
                        {
                            var source=PrefabUtility.GetCorrespondingObjectFromSource(collider);
                            var filter=collider.GetComponent<MeshFilter>();
                            var mesh=source&&source.sharedMesh?source.sharedMesh:filter?filter.sharedMesh:null;
                            if(!mesh)throw new Exception("Missing collision source: "+collider.name);
                            it.objectReferenceValue=mesh;changed=true;
                        }
                        else if(it.objectReferenceInstanceIDValue!=0&&component is UnityEngine.UI.Image&&it.propertyPath=="m_Sprite")
                        {it.objectReferenceValue=null;changed=true;}
                        continue;
                    }
                    references.Add(it.objectReferenceValue);
                    if(AssetDatabase.GetAssetPath(it.objectReferenceValue)==ThoughtSource){it.objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(ThoughtCopy);changed=true;}
                }
                if(changed)so.ApplyModifiedPropertiesWithoutUndo();
            }
            var lesson=roots.SelectMany(g=>g.GetComponentsInChildren<ElevatorLesson>(true)).First();var map=lesson.presentation.space.sportsCentre;
            if(map&&map.geometry){var floor=map.geometry.Find("1F");if(floor)map.firstFloorGeometry=floor.gameObject;EditorUtility.SetDirty(map);}
            int removed=0;
            foreach(var t in transforms.Reverse())
            {
                if(!t||t.childCount!=0||t.GetComponents<Component>().Length!=1||references.Contains(t)||references.Contains(t.gameObject))continue;
                string n=t.name.StartsWith("3F · ")?t.name.Substring(5):t.name;
                if(n.Length<5||!char.IsDigit(n[0])||!char.IsDigit(n[1])||!n.StartsWith(n.Substring(0,2)+" · ")||!map||!t.IsChildOf(map.geometry))continue;
                Object.DestroyImmediate(t.gameObject);removed++;
            }
            foreach(var r in roots.SelectMany(g=>g.GetComponentsInChildren<SkinnedMeshRenderer>(true)))
                if(r.sharedMesh&&AssetDatabase.GetAssetPath(r.sharedMesh).StartsWith("Assets/M/")){r.skinnedMotionVectors=false;EditorUtility.SetDirty(r);}
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");return removed;
        }
        public static object ApplyAssetsAndScenes()
        {
            if(Application.isPlaying)throw new Exception("Edit mode required");
            var mapRoot=PrefabUtility.LoadPrefabContents(MapPrefab);
            try
            {
                foreach(var collider in mapRoot.GetComponentsInChildren<MeshCollider>(true))
                {
                    if(collider.sharedMesh)continue;var filter=collider.GetComponent<MeshFilter>();
                    if(filter&&filter.sharedMesh)collider.sharedMesh=filter.sharedMesh;
                    else throw new Exception("Missing collision source: "+collider.name);
                }
                PrefabUtility.SaveAsPrefabAsset(mapRoot,MapPrefab);
            }
            finally{PrefabUtility.UnloadPrefabContents(mapRoot);}
            OptimizeTextures();PrepareThoughtTemplate();
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/M/Arts/Rendering/ElevatorOutlineRenderer.asset");
            var old=renderer.rendererFeatures.OfType<ElevatorSilhouetteOutlineFeature>().FirstOrDefault();
            if(old)
            {
                var feature=ScriptableObject.CreateInstance<ElevatorOptimizedOutlineFeature>();feature.name="M optimized silhouette";feature.maskShader=old.maskShader;feature.distanceShader=old.distanceShader;
                AssetDatabase.AddObjectToAsset(feature,renderer);int index=renderer.rendererFeatures.IndexOf(old);renderer.rendererFeatures[index]=feature;
                Object.DestroyImmediate(old,true);renderer.SetDirty();EditorUtility.SetDirty(renderer);
            }
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/M/Arts/Rendering/ElevatorScenePipeline.asset");
            pipeline.shadowCascadeCount=2;pipeline.shadowDistance=35;EditorUtility.SetDirty(pipeline);
            var meshPaths=AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/M/Arts/Models/Elevator/Optimized"}).Select(AssetDatabase.GUIDToAssetPath).Distinct();
            foreach(var p in meshPaths)
            {
                var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(!m||m.blendShapeCount>0||m.bindposeCount>0)continue;
                m.tangents=Array.Empty<Vector4>();m.SetUVs(0,new List<Vector4>());EditorUtility.SetDirty(m);
            }
            foreach(var p in new[]{"Assets/M/Arts/Materials/Elevator/UnifiedPalette.mat","Assets/M/Arts/Materials/Rescuers/RescueTeamPalette.mat"})
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(p);if(!material||!material.HasProperty("_Cull"))continue;
                // Enable hardware culling only when every vertex previously discarded back faces.
                var uses=AssetDatabase.GetDependencies(Scenes,true).Where(q=>q.EndsWith(".prefab")).Select(q=>AssetDatabase.LoadAssetAtPath<GameObject>(q)).Where(g=>g).SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.sharedMaterials.Contains(material)).Select(MeshOf).Where(m=>m).Distinct().ToArray();
                bool closed=uses.Length>0;
                foreach(var m in uses){var styles=new List<Vector4>();m.GetUVs(1,styles);if(styles.Count!=m.vertexCount||styles.Any(v=>v.z<=1.5f))closed=false;}
                material.SetFloat("_Cull",closed?2:0);EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();var reports=new List<object>();
            foreach(var p in Scenes){EditorSceneManager.OpenScene(p,OpenSceneMode.Single);int removed=CleanScene();reports.Add(new{scene=p,removed});Measure(Path.GetFileNameWithoutExtension(p)+"_After");}
            EditorSceneManager.OpenScene(Scenes[0],OpenSceneMode.Single);AssetDatabase.SaveAssets();Write(Evidence+"SceneCleanup.json",reports);return reports;
        }
        public static object ValidateCollisions()
        {
            var checks=new List<object>();var rejected=new HashSet<string>();
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                foreach(var file in Directory.GetFiles(Work+"Output","SC_*.json"))
                {
                    var g=JsonConvert.DeserializeObject<Geometry>(File.ReadAllText(file));
                    var original=AssetDatabase.LoadAssetAtPath<Mesh>(g.path);var p="Assets/M/Arts/Models/SportsCentre3F/Collision/"+g.name+"_Collision.asset";var optimized=AssetDatabase.LoadAssetAtPath<Mesh>(p);
                    var a=new GameObject("Original collider");var b=new GameObject("Optimized collider");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(a,preview);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(b,preview);
                    var ca=a.AddComponent<MeshCollider>();ca.sharedMesh=original;var cb=b.AddComponent<MeshCollider>();cb.sharedMesh=optimized;Physics.SyncTransforms();
                    int rays=0,misses=0;float worst=0;var bounds=original.bounds;
                    for(int axis=0;axis<3;axis++)for(int side=-1;side<=1;side+=2)for(int u=0;u<11;u++)for(int v=0;v<11;v++)
                    {
                        int ax=(axis+1)%3,ay=(axis+2)%3;var origin=bounds.center;origin[axis]+=side*(bounds.extents[axis]+1);origin[ax]+=bounds.size[ax]*((u+.37f)/11-.5f);origin[ay]+=bounds.size[ay]*((v+.61f)/11-.5f);var direction=Vector3.zero;direction[axis]=-side;var ray=new Ray(origin,direction);float distance=bounds.size[axis]+2;
                        bool ha=ca.Raycast(ray,out var ah,distance),hb=cb.Raycast(ray,out var bh,distance);rays++;
                        if(ha!=hb)misses++;else if(ha)worst=Mathf.Max(worst,Mathf.Abs(ah.distance-bh.distance));
                    }
                    bool pass=misses==0&&worst<=.01f;if(!pass)rejected.Add(g.name);
                    checks.Add(new{g.name,rays,misses,worst,pass});Object.DestroyImmediate(a);Object.DestroyImmediate(b);
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            if(rejected.Count>0)
            {
                var root=PrefabUtility.LoadPrefabContents(MapPrefab);
                try
                {
                    foreach(var c in root.GetComponentsInChildren<MeshCollider>(true))
                    {
                        if(!c.sharedMesh)continue;string name=c.sharedMesh.name.Replace("_Collision","").Replace("_Runtime","");
                        if(rejected.Contains(name))c.sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/M/Arts/Models/SportsCentre3F/Meshes/"+name+".asset");
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,MapPrefab);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
                foreach(var name in rejected)AssetDatabase.DeleteAsset("Assets/M/Arts/Models/SportsCentre3F/Collision/"+name+"_Collision.asset");
                AssetDatabase.SaveAssets();
            }
            Write(Evidence+"CollisionRayChecks.json",checks);return new{checkedMeshes=checks.Count,rays=checks.Count*726,reverted=rejected.ToArray()};
        }
        public static object ReleaseUnusedMeshCopies()
        {
            var colliderMeshes=new HashSet<Mesh>();
            foreach(var p in AssetDatabase.GetDependencies(Scenes,true).Where(p=>p.EndsWith(".prefab")))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(p);if(!prefab)continue;
                foreach(var collider in prefab.GetComponentsInChildren<MeshCollider>(true))if(collider.sharedMesh)colliderMeshes.Add(collider.sharedMesh);
            }
            var changed=new List<string>();
            foreach(var p in AssetDatabase.GetDependencies(Scenes,true).Where(p=>p.EndsWith(".asset")&&p.StartsWith("Assets/M/Arts/Models/")))
            {
                var m=AssetDatabase.LoadAssetAtPath<Mesh>(p);if(!m||!m.isReadable||colliderMeshes.Contains(m)||p.Contains("/Collision/"))continue;
                m.UploadMeshData(true);EditorUtility.SetDirty(m);changed.Add(p);
            }
            AssetDatabase.SaveAssets();Write(Evidence+"MeshCpuCopiesReleased.json",changed);return new{meshes=changed.Count};
        }
        public static object ValidateScene()
        {
            var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var components=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).ToArray();
            int missingScripts=components.Count(c=>!c);var missing=new List<string>();
            foreach(var c in components)
            {
                if(!c)continue;var so=new SerializedObject(c);var p=so.GetIterator();
                while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&!p.objectReferenceValue&&p.objectReferenceInstanceIDValue!=0)missing.Add(c.name+"."+p.propertyPath);
            }
            var shaderErrors=components.OfType<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m&&m.shader).Select(m=>m.shader).Distinct().Where(ShaderUtil.ShaderHasError).Select(sh=>sh.name).ToArray();
            var lesson=components.OfType<ElevatorLesson>().First();var map=lesson.presentation.space.sportsCentre;
            var report=new{scene=s.path,missingScripts,missingReferences=missing,shaderErrors,firstFloorConnected=map.firstFloorGeometry&&map.firstFloorGeometry.name=="1F",guid=AssetDatabase.AssetPathToGUID(s.path)};
            Write(Evidence+Path.GetFileNameWithoutExtension(s.path)+"_Validation.json",report);
            if(missingScripts>0||missing.Count>0||shaderErrors.Length>0||!map.firstFloorGeometry)throw new Exception("Scene validation failed; see report");
            return report;
        }
    }
}
