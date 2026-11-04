var setup=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup();
if(UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Stop Play Mode before refreshing the source model.");
if(Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Any(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))throw new System.Exception("Preserve unsaved scene changes before capturing.");
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
try
{
    ProjectWait.M.Editor.FriendImprovedSetup.BuildShowcase();
    var camera=Camera.main;
    camera.orthographicSize=.195f;
    camera.transform.position=new Vector3(1.25f,.48f,2.5f);
    camera.transform.LookAt(new Vector3(0,.14f,0));
    ProjectWait.M.Editor.FriendImprovedSetup.Capture(camera,"Assets/M/CodexTemp/FriendImprovement_20261104~/socks_shoes_closeup.png");
}
finally
{
    UnityEditor.SceneManagement.EditorSceneManager.RestoreSceneManagerSetup(setup);
}
var validation=ProjectWait.M.Editor.FriendImprovedSetup.InspectPrefab();
System.IO.File.WriteAllText("Assets/M/CodexTemp/FriendImprovement_20261104~/socks_shoes_unity_validation.json",validation);
return validation;
