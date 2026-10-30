using System.Linq;
using UnityEngine;
namespace ProjectWait.M.Elevator
{
 [DefaultExecutionOrder(120)]
 public sealed class ElevatorCharacterPerformance:MonoBehaviour
 {
  public ElevatorLesson lesson;public Transform panicFace;public Transform[] spiralEyes;
  public Mesh normalFaceMesh,panicFaceMesh;public SkinnedMeshRenderer faceRenderer;
  Transform leftHip,leftKnee,leftFoot,rightHip,rightKnee,rightFoot,head;
  Vector3 leftRest,rightRest,previous;Quaternion leftFootRest,rightFootRest;float phase;int run=-1;
  SkinnedMeshRenderer face;int blink=-1,surprised=-1;
  public bool PanicVisible=>panicFace&&panicFace.gameObject.activeSelf;
  public float WalkPhase=>phase;
  void Awake()
  {
   var bones=lesson.friend.GetComponentsInChildren<Transform>(true);leftHip=Find(bones,"thigh.L");leftKnee=Find(bones,"shin.L");leftFoot=Find(bones,"foot.L");rightHip=Find(bones,"thigh.R");rightKnee=Find(bones,"shin.R");rightFoot=Find(bones,"foot.R");head=Find(bones,"head");
   if(leftFoot){leftRest=lesson.friend.InverseTransformPoint(leftFoot.position);leftFootRest=Quaternion.Inverse(lesson.friend.rotation)*leftFoot.rotation;}if(rightFoot){rightRest=lesson.friend.InverseTransformPoint(rightFoot.position);rightFootRest=Quaternion.Inverse(lesson.friend.rotation)*rightFoot.rotation;}
   face=lesson.friend.GetComponentsInChildren<SkinnedMeshRenderer>().FirstOrDefault(s=>!s.name.StartsWith("SkyGlow"));if(face){blink=face.sharedMesh.GetBlendShapeIndex("Blink");surprised=face.sharedMesh.GetBlendShapeIndex("Surprised");}
   if(panicFaceMesh){panicFaceMesh.SetTriangles(panicFaceMesh.triangles,0,false);panicFaceMesh.UploadMeshData(false);}
   previous=lesson.friend.position;if(panicFace)panicFace.gameObject.SetActive(false);
  }
  static Transform Find(Transform[] a,string name)=>a.FirstOrDefault(t=>t.name==name);
  void LateUpdate()
  {
   if(!lesson||lesson.Flow==null||lesson.IsPaused)return;var f=lesson.Flow;var root=lesson.friend;
   if(run!=lesson.RunVersion){run=lesson.RunVersion;phase=0;previous=root.position;}
   var displacement=Vector3.ProjectOnPlane(root.position-previous,Vector3.up);previous=root.position;
   bool walking=f.Step==ElevatorStep.Entering||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.GoingHome;
   if(walking&&displacement.sqrMagnitude>.000001f&&displacement.magnitude<.5f)
   {
    phase+=displacement.magnitude/.46f*Mathf.PI*2;Vector3 forward=displacement.normalized;
    Leg(leftHip,leftKnee,leftFoot,root.TransformPoint(leftRest),forward,phase,root.rotation*leftFootRest);
    Leg(rightHip,rightKnee,rightFoot,root.TransformPoint(rightRest),forward,phase+Mathf.PI,root.rotation*rightFootRest);
    root.position+=Vector3.up*(Mathf.Abs(Mathf.Sin(phase))*.009f);
   }
   if(f.Step==ElevatorStep.Jump)
   {
    float t=(f.Elapsed-lesson.settings.friendReplySeconds)/lesson.settings.jumpSeconds;
    float lift=t>0&&t<1?Mathf.Sin(t*Mathf.PI)*.23f:0;
    root.position+=Vector3.up*lift;
   }
   bool panic=!f.Reassured&&(f.Step==ElevatorStep.Stopped||f.Step==ElevatorStep.Connecting||f.Step==ElevatorStep.Unanswered||f.Step==ElevatorStep.Call||f.Step==ElevatorStep.Waiting);
   if(face&&normalFaceMesh&&panicFaceMesh)face.sharedMesh=panic?panicFaceMesh:normalFaceMesh;
   if(panicFace)panicFace.gameObject.SetActive(panic);
   if(panic)
   {
    if(head)head.localRotation*=Quaternion.Euler(Mathf.Sin(f.Elapsed*4)*3,Mathf.Sin(f.Elapsed*2.2f)*4,Mathf.Sin(f.Elapsed*3.7f)*5);
    if(face){if(blink>=0)face.SetBlendShapeWeight(blink,0);if(surprised>=0)face.SetBlendShapeWeight(surprised,0);}
    if(spiralEyes!=null)for(int i=0;i<spiralEyes.Length;i++)if(spiralEyes[i])spiralEyes[i].localRotation=Quaternion.Euler(0,0,Mathf.Sin(f.Elapsed*4+i)*12);
   }
  }
  void OnDisable(){if(faceRenderer&&normalFaceMesh)faceRenderer.sharedMesh=normalFaceMesh;if(panicFace)panicFace.gameObject.SetActive(false);}
  static void Leg(Transform hip,Transform knee,Transform foot,Vector3 rest,Vector3 forward,float phase,Quaternion flatFoot)
  {
   if(!hip||!knee||!foot)return;float swing=Mathf.Sin(phase);Vector3 target=rest+forward*(swing*.080f)+Vector3.up*(.012f+Mathf.Max(0,swing)*.045f);
   float a=Vector3.Distance(hip.position,knee.position),b=Vector3.Distance(knee.position,foot.position);Vector3 delta=target-hip.position;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);Vector3 direction=delta.normalized;Vector3 bend=Vector3.ProjectOnPlane(forward,direction).normalized;float x=(a*a-b*b+d*d)/(2*d);float h=Mathf.Sqrt(Mathf.Max(0,a*a-x*x));Vector3 k=hip.position+direction*x+bend*h;
   hip.rotation=Quaternion.FromToRotation(knee.position-hip.position,k-hip.position)*hip.rotation;knee.rotation=Quaternion.FromToRotation(foot.position-knee.position,target-knee.position)*knee.rotation;foot.rotation=flatFoot;
  }
 }
}
