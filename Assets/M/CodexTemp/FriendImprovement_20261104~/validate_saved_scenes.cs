var results = new System.Collections.Generic.List<object>();
foreach (var path in new[] { "Assets/M/Scene/Elevator_Safety_Prototype.unity", "Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity" })
{
    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);
    var lesson = UnityEngine.Object.FindFirstObjectByType<ProjectWait.M.Elevator.ElevatorLesson>();
    var avatar = lesson.friend.GetComponentInChildren<ProjectWait.M.FriendImprovedExpressions>();
    var mesh = avatar.GetComponentInChildren<SkinnedMeshRenderer>();
    var phone = lesson.friend.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Cabin_Optional119Phone");
    var outline = lesson.friend.GetComponent<ProjectWait.M.Elevator.ElevatorOutlineTarget>();
    bool valid = mesh.sharedMaterials.Length==1 && mesh.sharedMesh.subMeshCount==1 && mesh.bones.Length==22 && mesh.bones.All(b=>b) && phone.parent.name=="pelvis" && outline.geometry.Length==1 && outline.geometry[0].sourceRenderer==mesh;
    if (!valid) throw new System.Exception("Saved scene validation failed: "+path);
    results.Add(new {path,valid,model=avatar.name,materials=mesh.sharedMaterials.Length,bones=mesh.bones.Length,phoneParent=phone.parent.name,shaderErrors=UnityEditor.ShaderUtil.ShaderHasError(mesh.sharedMaterial.shader)});
}
UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/M/Scene/Elevator_Safety_Prototype.unity");
return results.ToArray();
