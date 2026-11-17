using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectWait.M.Shared
{
    // A head-tracked, inward-facing shell works in both eyes and at any head roll.
    // Its final overlay queue also covers controller rays and world-space UI.
    [DefaultExecutionOrder(32500)]
    public sealed class ComfortScreenFade : MonoBehaviour
    {
        public float RequestedOpacity {get;private set;}
        public float VisibleOpacity {get;private set;}
        Renderer shell;
        Material material;
        Camera view;
        static readonly int ColorId=Shader.PropertyToID("_BaseColor");
        public static ComfortScreenFade For(Camera camera)=>camera?camera.GetComponent<ComfortScreenFade>():null;
        public void Configure(Shader shader)
        {
            if(shell)return;
            view=GetComponent<Camera>();
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.name="Shared comfort blackout";sphere.layer=2;
            Destroy(sphere.GetComponent<Collider>());sphere.transform.SetParent(transform,false);
            float radius=Mathf.Max(1,view.nearClipPlane*4);
            sphere.transform.localScale=Vector3.one*(radius*2);
            shell=sphere.GetComponent<Renderer>();material=new Material(shader){name="Shared comfort blackout (runtime)"};
            shell.sharedMaterial=material;shell.shadowCastingMode=ShadowCastingMode.Off;shell.receiveShadows=false;
            shell.lightProbeUsage=LightProbeUsage.Off;shell.reflectionProbeUsage=ReflectionProbeUsage.Off;Apply();
        }
        public void SetOpacity(float value){RequestedOpacity=Mathf.Clamp01(value);Apply();}
        bool Paused=>ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.Current&&ProjectWait.M.Crosswalk.CrosswalkSceneIntegration.Current.IsPaused||
            elevator&&elevator.IsPaused ||
            ProjectWait.YB.OfficeBlackout.OfficeSceneIntegration.Current&&ProjectWait.YB.OfficeBlackout.OfficeSceneIntegration.Current.IsPaused;
        ProjectWait.M.Elevator.DialoguePreview.ElevatorLesson elevator;
        void Start(){elevator=FindFirstObjectByType<ProjectWait.M.Elevator.DialoguePreview.ElevatorLesson>();}
        void OnEnable(){Application.onBeforeRender+=Apply;}
        void LateUpdate(){Apply();}
        void Apply()
        {
            if(!shell)return;
            // The inter-scene droplet transition owns visibility while loading.
            VisibleOpacity=Paused||ProjectWait.M.SceneTransition.StorybookSceneTransition.IsBusy?0:RequestedOpacity;
            material.SetColor(ColorId,new Color(0,0,0,VisibleOpacity));shell.enabled=VisibleOpacity>0;
        }
        void OnDisable(){Application.onBeforeRender-=Apply;if(shell)shell.enabled=false;}
        void OnDestroy(){if(material)Destroy(material);}
    }
}
