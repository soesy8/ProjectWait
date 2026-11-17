using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using ProjectWait.M.Shared;

namespace ProjectWait.M.TitleScene
{
    // The same XRI renderer and state properties as the two experience scenes.
    public sealed class LobbyControllerRay : ICurveInteractionDataProvider, IDisposable
    {
        readonly GameObject root;
        NativeArray<Vector3> points=new NativeArray<Vector3>(2,Allocator.Persistent);
        public CurveVisualController Visual {get;}
        public bool isActive {get;private set;}
        public bool hasValidSelect {get;private set;}
        public Transform curveOrigin=>root.transform;
        public NativeArray<Vector3> samplePoints=>points;
        public Vector3 lastSamplePoint=>points[1];
        Vector3 end;
        EndPointType kind;
        public LobbyControllerRay(Transform parent,LineRenderer line,SharedControllerRays style)
        {
            root=new GameObject("Lobby shared controller ray");root.SetActive(false);root.transform.SetParent(parent,false);
            Visual=root.AddComponent<CurveVisualController>();Visual.lineRenderer=line;Visual.curveInteractionDataProvider=this;
            Visual.overrideLineOrigin=false;Visual.lineOriginTransform=null;style.Apply(Visual);Visual.customizeLinePropertiesForState=true;
            root.SetActive(true);
        }
        public void Set(Ray ray,bool valid,Vector3 hit,bool hasTarget,bool pressed)
        {
            isActive=valid;hasValidSelect=hasTarget&&pressed;
            root.transform.SetPositionAndRotation(ray.origin,Quaternion.LookRotation(ray.direction,Vector3.up));
            points[0]=ray.origin;points[1]=ray.GetPoint(30);end=hasTarget?hit:points[1];kind=hasTarget?EndPointType.ValidCastHit:EndPointType.None;
        }
        public void Hide(){isActive=false;if(Visual.lineRenderer)Visual.lineRenderer.enabled=false;}
        public EndPointType TryGetCurveEndPoint(out Vector3 point,bool snapToSelectedAttachIfAvailable=false,bool snapToSnapVolumeIfAvailable=false){point=end;return kind;}
        public EndPointType TryGetCurveEndNormal(out Vector3 normal,bool snapToSelectedAttachIfAvailable=false){normal=-curveOrigin.forward;return kind;}
        public void Dispose(){root.SetActive(false);UnityEngine.Object.Destroy(root);if(points.IsCreated)points.Dispose();}
    }
}
