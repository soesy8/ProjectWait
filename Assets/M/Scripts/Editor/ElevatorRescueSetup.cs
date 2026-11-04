#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorRescueSetup
    {
        const string Root="Assets/M/Arts/Models/Rescuers/";
        const string Mats="Assets/M/Arts/Materials/Rescuers/";
        const string Work="Assets/M/CodexTemp/RescueIntegration_20261104~/";
        static readonly string[] Scenes={"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
        static Color Linear(Color c)=>QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c;
        static Color Hex(string h){if(!ColorUtility.TryParseHtmlString(h,out var c))throw new Exception("Invalid colour "+h);return Linear(c);}
        static Mesh Save(Mesh asset,string path)
        {
            if(!path.StartsWith("Assets/M/",StringComparison.Ordinal))throw new Exception("M paths only");
            var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old)
            {
                // Recreate the native buffers when the re-export changes the vertex count.
                // CopySerialized alone can leave a stale skinning stride on the GPU.
                old.Clear(false);old.indexFormat=asset.indexFormat;old.name=asset.name;
                old.vertices=asset.vertices;old.normals=asset.normals;old.colors=asset.colors;
                if(asset.tangents.Length>0)old.tangents=asset.tangents;
                for(int channel=0;channel<8;channel++)
                {
                    var attribute=(VertexAttribute)((int)VertexAttribute.TexCoord0+channel);
                    if(!asset.HasVertexAttribute(attribute))continue;
                    int dimension=asset.GetVertexAttributeDimension(attribute);
                    if(dimension==2){var uv=new List<Vector2>();asset.GetUVs(channel,uv);old.SetUVs(channel,uv);}
                    else if(dimension==3){var uv=new List<Vector3>();asset.GetUVs(channel,uv);old.SetUVs(channel,uv);}
                    else {var uv=new List<Vector4>();asset.GetUVs(channel,uv);old.SetUVs(channel,uv);}
                }
                old.boneWeights=asset.boneWeights;old.bindposes=asset.bindposes;
                old.subMeshCount=asset.subMeshCount;
                for(int i=0;i<asset.subMeshCount;i++)old.SetIndices(asset.GetIndices(i),asset.GetTopology(i),i,false);
                old.bounds=asset.bounds;old.UploadMeshData(false);
                Object.DestroyImmediate(asset);EditorUtility.SetDirty(old);AssetDatabase.SaveAssetIfDirty(old);return old;
            }
            AssetDatabase.CreateAsset(asset,path);return asset;
        }
        static Material Palette()
        {
            string path=Mats+"RescueTeamPalette.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/M/Arts/Shaders/ElevatorPaletteToon.shader");
            if(!shader||!shader.isSupported)throw new Exception("M vertex palette shader unavailable");
            if(!mat){mat=new Material(shader){name="RescueTeamPalette"};AssetDatabase.CreateAsset(mat,path);}
            mat.shader=shader;mat.SetColor("_BaseColor",Color.white);mat.enableInstancing=true;EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);return mat;
        }
        static Vector3 PoseScale(string id)
        {
            var source=JObject.Parse(File.ReadAllText(Root+"Sources/Rescuer_"+id+"_Manifest.json"));
            var scale=source["pose_scale"] as JArray;
            return scale!=null?new Vector3((float)scale[0],(float)scale[1],(float)scale[2]):Vector3.one;
        }
        static GameObject Actor(string id,Scene preview,Material palette,List<object> reports)
        {
            string source=Root+"Sources/Rescuer_"+id+".fbx";
            var importer=AssetImporter.GetAtPath(source) as ModelImporter;
            if(!importer)throw new Exception("Missing model "+source);
            importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=false;
            importer.importCameras=importer.importLights=false;importer.optimizeGameObjects=false;importer.isReadable=true;
            importer.importNormals=ModelImporterNormals.Import;importer.SaveAndReimport();
            var manifest=JObject.Parse(File.ReadAllText(Root+"Sources/Rescuer_"+id+"_Manifest.json"));
            var objects=((JArray)manifest["objects"]).ToDictionary(x=>(string)x["name"]);
            var materials=((JArray)manifest["materials"]).ToDictionary(x=>(string)x["name"]);
            var root=new GameObject(id=="A"?"Rescuer_A_Firefighter":"Rescuer_B_LiftSpecialist");SceneManager.MoveGameObjectToScene(root,preview);
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(source),preview);
            PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            model.name="Skeleton";model.transform.SetParent(root.transform,false);
            foreach(var animator in model.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(animator);
            var renderers=model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(renderers.Length<20)throw new Exception("Unexpected imported renderer count "+renderers.Length);
            Func<SkinnedMeshRenderer,Vector3> centre=r=>root.transform.InverseTransformPoint(r.transform.TransformPoint(r.sharedMesh.bounds.center));
            var face=renderers.First(r=>objects.TryGetValue(r.name,out var x)&&(string)x["part"]=="Head • 둥근 얼굴");
            var eye=renderers.First(r=>objects.TryGetValue(r.name,out var x)&&((string)x["part"]).StartsWith("Eye."));
            if((centre(eye)-centre(face)).z<0)model.transform.localRotation=Quaternion.Euler(0,180,0);
            var all=renderers.SelectMany(r=>r.sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();
            float bottom=all.Min(v=>v.y),height=all.Max(v=>v.y)-bottom;
            if(height<1.3f||height>2.1f)throw new Exception("Unexpected source height "+height);
            model.transform.localPosition-=Vector3.up*bottom;
            var bones=renderers.SelectMany(r=>r.bones).Where(b=>b).Distinct().ToArray();
            if(bones.Length!=18)throw new Exception("Expected 18 bones, got "+bones.Length);
            var boneMap=bones.Select((b,i)=>(b,i)).ToDictionary(x=>x.b,x=>x.i);
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var colours=new List<Color>();var weights=new List<BoneWeight>();var triangles=new List<int>();
            var styles=new List<Vector4>();var shades=new List<Vector4>();var inks=new List<Vector4>();var bands=new List<Vector2>();
            var shade=Hex("#BDC8C3");var ink=Hex("#493327");
            foreach(var r in renderers)
            {
                if(!objects.TryGetValue(r.name,out var descriptor))throw new Exception("Missing object manifest: "+r.name);
                var mesh=r.sharedMesh;var positions=mesh.vertices;var sourceNormals=mesh.normals;var sourceWeights=mesh.boneWeights;
                var toRoot=root.transform.worldToLocalMatrix*r.transform.localToWorldMatrix;var normalMatrix=toRoot.inverse.transpose;
                var remap=r.bones.Select(b=>boneMap[b]).ToArray();
                for(int slot=0;slot<mesh.subMeshCount;slot++)
                {
                    string materialName=r.sharedMaterials[slot].name;
                    if(!materials.TryGetValue(materialName,out var material))throw new Exception("Missing material manifest: "+materialName);
                    var color=Hex((string)material["hex"]);bool flat=(bool)material["flat"],outlined=(bool)descriptor["outlined"];
                    var lookup=new Dictionary<int,int>();
                    foreach(int v in mesh.GetTriangles(slot))
                    {
                        if(!lookup.TryGetValue(v,out int mapped))
                        {
                            mapped=vertices.Count;lookup.Add(v,mapped);vertices.Add(toRoot.MultiplyPoint3x4(positions[v]));
                            normals.Add(normalMatrix.MultiplyVector(sourceNormals[v]).normalized);colours.Add(color);
                            var w=sourceWeights[v];
                            w.boneIndex0=remap[w.boneIndex0];w.boneIndex1=remap[w.boneIndex1];w.boneIndex2=remap[w.boneIndex2];w.boneIndex3=remap[w.boneIndex3];weights.Add(w);
                            styles.Add(new Vector4(outlined?1.65f:0,flat?1:0,flat?0:2,.012f));
                            shades.Add(new Vector4(shade.r,shade.g,shade.b,.90f));inks.Add(ink);bands.Add(new Vector2(.02f,.62f));
                        }
                        triangles.Add(mapped);
                    }
                }
            }
            var combined=new Mesh{name="Rescuer_"+id+"_PaletteSkin",indexFormat=IndexFormat.UInt32};
            combined.SetVertices(vertices);combined.SetNormals(normals);combined.SetColors(colours);combined.SetUVs(1,styles);combined.SetUVs(2,shades);combined.SetUVs(3,inks);combined.SetUVs(4,bands);
            combined.boneWeights=weights.ToArray();combined.bindposes=bones.Select(b=>b.worldToLocalMatrix*root.transform.localToWorldMatrix).ToArray();
            combined.SetTriangles(triangles,0);combined.RecalculateBounds();
            var asset=Save(combined,Root+"Generated/Rescuer_"+id+"_PaletteSkin.asset");
            var body=new GameObject("CharacterMesh");body.transform.SetParent(root.transform,false);
            var skin=body.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=asset;skin.bones=bones;skin.rootBone=bones.First(b=>b.name=="root");skin.sharedMaterial=palette;
            skin.localBounds=asset.bounds;skin.updateWhenOffscreen=true;skin.quality=SkinQuality.Bone4;skin.shadowCastingMode=ShadowCastingMode.On;skin.receiveShadows=true;
            foreach(var r in renderers)
            {
                var go=r.gameObject;Object.DestroyImmediate(r);
                if(go.transform.childCount==0&&!bones.Contains(go.transform))Object.DestroyImmediate(go);
            }
            var box=root.AddComponent<BoxCollider>();box.center=new Vector3(0,height*.5f,0);box.size=new Vector3(id=="A"?.65f:.56f,height,.43f);box.isTrigger=false;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+root.name+".prefab");
            reports.Add(new{id,height,sourceRenderers=renderers.Length,runtimeRenderers=1,materials=1,vertices=asset.vertexCount,triangles=asset.triangles.Length/3,bones=bones.Length,facing="+Z"});
            Object.DestroyImmediate(root);return prefab;
        }
        [MenuItem("Tools/Wait/Elevator/Integrate both rescue characters")]
        public static void ApplyBothMenu(){ApplyBoth();}
        public static object ApplyBoth()
        {
            if(Application.isPlaying)throw new Exception("Stop Play Mode before integration");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scene changes first");
            Directory.CreateDirectory(Root+"Generated");Directory.CreateDirectory(Mats);Directory.CreateDirectory(Work);AssetDatabase.Refresh();
            var reports=new List<object>();var preview=EditorSceneManager.NewPreviewScene();GameObject a,b;
            try{var palette=Palette();a=Actor("A",preview,palette,reports);b=Actor("B",preview,palette,reports);}
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            foreach(var path in Scenes)
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var lesson=Object.FindFirstObjectByType<ElevatorLesson>();var p=lesson.presentation;
                var previous=p.rescuer;var oldOutline=previous?previous.GetComponentInChildren<ElevatorOutlineTarget>(true):null;
                var teamRoot=new GameObject("RescueTeam");teamRoot.transform.SetParent(lesson.transform,false);teamRoot.SetActive(false);
                var team=teamRoot.AddComponent<ElevatorRescueTeam>();
                var actorA=(GameObject)PrefabUtility.InstantiatePrefab(a,scene);actorA.transform.SetParent(teamRoot.transform,false);
                var actorB=(GameObject)PrefabUtility.InstantiatePrefab(b,scene);actorB.transform.SetParent(teamRoot.transform,false);
                team.firefighter=actorA.transform;team.specialist=actorB.transform;
                team.firefighterPoseScale=PoseScale("A");team.specialistPoseScale=PoseScale("B");
                actorA.transform.position=team.firefighterAtDoor+Vector3.up*ElevatorSportsCentreMap.ThirdFloorHeight;
                actorB.transform.position=team.specialistAtDoor+Vector3.up*ElevatorSportsCentreMap.ThirdFloorHeight;
                foreach(var actor in new[]{actorA,actorB})
                {
                    var observation=actor.AddComponent<ElevatorObservation>();observation.lesson=lesson;observation.clue=ElevatorClue.Rescuer;observation.hasDescription=true;
                    var interactable=actor.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                    interactable.colliders.Clear();interactable.colliders.Add(actor.GetComponent<Collider>());
                    ElevatorInteractionSetup.AddActorOutline(observation,actor==actorA?960:961);
                    // The crew is saved inactive. Resolve its geometry explicitly even when
                    // GetComponentInParent skips inactive observation ancestors in edit mode.
                    observation.outlineTarget.geometry=new[]{new ElevatorOutlineGeometry{sourceRenderer=actor.GetComponentInChildren<SkinnedMeshRenderer>(true)}};
                    if(oldOutline)
                    {
                        var t=observation.outlineTarget;t.idleColor=oldOutline.idleColor;t.hoverColor=oldOutline.hoverColor;
                        t.idleWidth=oldOutline.idleWidth;t.hoverWidth=oldOutline.hoverWidth;t.idleHalo=oldOutline.idleHalo;t.hoverHalo=oldOutline.hoverHalo;t.colorTransitionDuration=oldOutline.colorTransitionDuration;
                    }
                }
                p.rescueTeam=team;p.rescuer=teamRoot;
                foreach(var ui in Object.FindObjectsByType<ElevatorSpatialUI>(FindObjectsInactive.Include,FindObjectsSortMode.None))ui.rescuerAnchor=teamRoot.transform;
                if(previous)Object.DestroyImmediate(previous);
                EditorUtility.SetDirty(p);EditorUtility.SetDirty(lesson);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene(Scenes[0],OpenSceneMode.Single);
            File.WriteAllText(Work+"Integration.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scenes=Scenes,actors=reports},Newtonsoft.Json.Formatting.Indented));
            return reports;
        }
        [MenuItem("Tools/Wait/Elevator/Update specialist from original model")]
        public static void UpdateSpecialistMenu(){UpdateSpecialist();}
        public static object UpdateSpecialist()
        {
            if(Application.isPlaying)throw new Exception("Stop Play Mode before updating the original model");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scene changes first");
            AssetDatabase.Refresh();var reports=new List<object>();var preview=EditorSceneManager.NewPreviewScene();
            try{Actor("B",preview,Palette(),reports);}
            finally{EditorSceneManager.ClosePreviewScene(preview);}
            var scale=PoseScale("B");
            foreach(var path in Scenes)
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                var lesson=Object.FindFirstObjectByType<ElevatorLesson>();var team=lesson.presentation.rescueTeam;
                if(!team||!team.specialist)throw new Exception("Existing rescue team missing in "+path);
                if(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(team.specialist.gameObject)!=Root+"Rescuer_B_LiftSpecialist.prefab")throw new Exception("Specialist source has changed in "+path);
                team.specialistPoseScale=scale;
                var skin=team.specialist.GetComponentInChildren<SkinnedMeshRenderer>(true);
                var box=team.specialist.GetComponent<BoxCollider>();float height=skin.sharedMesh.bounds.size.y;
                box.center=new Vector3(0,height*.5f,0);box.size=new Vector3(box.size.x,height,box.size.z);
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
                var observation=team.specialist.GetComponent<ElevatorObservation>();
                observation.outlineTarget.geometry=new[]{new ElevatorOutlineGeometry{sourceRenderer=skin}};
                EditorUtility.SetDirty(team);EditorUtility.SetDirty(observation.outlineTarget);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene(Scenes[0],OpenSceneMode.Single);
            File.WriteAllText(Work+"SpecialistUpdate.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{scenes=Scenes,actors=reports,poseScaleY=scale.y},Newtonsoft.Json.Formatting.Indented));
            return reports;
        }
    }
}
#endif
