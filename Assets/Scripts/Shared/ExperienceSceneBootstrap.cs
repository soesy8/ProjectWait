using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectWait.M.Shared
{
    [DefaultExecutionOrder(-2700)]
    public sealed class ExperienceSceneBootstrap : MonoBehaviour
    {
        public Shader blackoutShader;
        void Awake()
        {
            var scene=gameObject.scene;
            foreach(var root in scene.GetRootGameObjects())foreach(var camera in root.GetComponentsInChildren<Camera>(true))
                if(camera.CompareTag("MainCamera")&&blackoutShader)
                {var fade=camera.GetComponent<ComfortScreenFade>();if(!fade)fade=camera.gameObject.AddComponent<ComfortScreenFade>();fade.Configure(blackoutShader);}
            foreach(var root in scene.GetRootGameObjects())if(root.GetComponent<ExperienceSceneSettings>())return;
            var host=new GameObject("Shared experience settings");SceneManager.MoveGameObjectToScene(host,scene);host.AddComponent<ExperienceSceneSettings>();
        }
    }
}
