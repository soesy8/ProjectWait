using UnityEngine;
using UnityEngine.XR;
namespace ProjectWait.M.Elevator
{
 [DefaultExecutionOrder(600)]
 public sealed class ElevatorCameraEffects:MonoBehaviour
 {
  public ElevatorLesson lesson;public Transform shakePivot;
  public float LastShakeAmplitude{get;private set;}
  ElevatorDesktopControls desktop;ElevatorStep previous=(ElevatorStep)(-1);int run=-1;float remaining;bool xrViewAligned;int focusRevision=-1;readonly Vector3[] frameCorners=new Vector3[4];AudioSource sound;AudioClip clunk;
  void Awake()
  {
   desktop=FindFirstObjectByType<ElevatorDesktopControls>();sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.volume=.18f;sound.spatialBlend=0;const int n=17640;var data=new float[n];var random=new System.Random(31);for(int i=0;i<n;i++){float t=i/22050f;float impulse=Mathf.Exp(-t*12)+.5f*Mathf.Exp(-Mathf.Abs(t-.19f)*35);data[i]=((float)(random.NextDouble()*2-1)*.34f+Mathf.Sin(t*2*Mathf.PI*53)*.66f)*impulse;}clunk=AudioClip.Create("Elevator mechanical clunk",n,1,22050,false);clunk.SetData(data,0);
  }
  void LateUpdate()
  {
   if(!lesson||lesson.Flow==null||lesson.IsPaused)return;var f=lesson.Flow;
   if(run!=lesson.RunVersion){run=lesson.RunVersion;previous=f.Step;remaining=0;ResetPivot();}
   if(previous!=f.Step&&f.Step==ElevatorStep.Stopped){remaining=.85f;if(sound)sound.PlayOneShot(clunk);}previous=f.Step;
   var map=lesson.presentation.space?lesson.presentation.space.sportsCentre:null;
   if(f.Elapsed<.05f||focusRevision!=f.Revision){xrViewAligned=false;focusRevision=f.Revision;}
   if(lesson.IsReframing||lesson.scenario&&lesson.scenario.LockView||f.Step==ElevatorStep.Jump||f.Step==ElevatorStep.Connecting||f.Step==ElevatorStep.Stopped&&f.Phase==InteractionPhase.Dialogue||f.Step==ElevatorStep.Rescue&&!f.ReadyToExit)
   {
    Vector3 actor=lesson.friend.position;
    if(f.Step==ElevatorStep.Rescue||f.Step==ElevatorStep.Exiting||f.Step==ElevatorStep.Returning)actor=lesson.presentation.rescuer.transform.position;
    var frame=new Bounds(actor+Vector3.up*.9f,new Vector3(.55f,1.35f,.35f));
    // Include speech and choices in the shot instead of aiming only at the actor's chest.
    if(lesson.view.dialoguePanel.gameObject.activeInHierarchy){Include(ref frame,lesson.view.dialoguePanel);if(lesson.view.spatial)Include(ref frame,lesson.view.spatial.speakerTab);}
    foreach(var button in lesson.view.ChoiceButtons)if(button&&button.IsActive())Include(ref frame,(RectTransform)button.transform);
    Vector3 target=frame.center;
    if(f.Step==ElevatorStep.GoingHome&&map)target=map.groundExit+Vector3.up*desktop.eyeHeight;
    var delta=target-lesson.playerCamera.transform.position;
    if(delta.sqrMagnitude>.01f)
    {
     var r=Quaternion.LookRotation(delta).eulerAngles;
     if(!XRSettings.isDeviceActive){float blend=1-Mathf.Exp(-Time.deltaTime*10);desktop.SetLookAngles(Mathf.LerpAngle(desktop.LookAngles.x,r.y,blend),Mathf.LerpAngle(desktop.LookAngles.y,Mathf.DeltaAngle(0,r.x),blend));}
     else if(map&&map.IsFraming){if(!xrViewAligned&&map.ReframeFade>.98f&&map.ReframeProgress>=.5f){desktop.xrOrigin.RotateAround(lesson.playerCamera.transform.position,Vector3.up,Mathf.DeltaAngle(lesson.playerCamera.transform.eulerAngles.y,r.y));xrViewAligned=true;}}
     else if(!map||!map.IsInteriorBeat){float angle=Mathf.DeltaAngle(lesson.playerCamera.transform.eulerAngles.y,r.y);desktop.xrOrigin.Rotate(Vector3.up,Mathf.Clamp(angle,-40*Time.deltaTime,40*Time.deltaTime),Space.World);}
    }
   }
   remaining=Mathf.Max(0,remaining-Time.deltaTime);float t=.85f-remaining;float env=remaining/.85f;LastShakeAmplitude=env;
   if(shakePivot){float amount=XRSettings.isDeviceActive?.16f:1.15f;shakePivot.localRotation=Quaternion.Euler(Mathf.Sin(t*43)*env*amount,Mathf.Sin(t*29)*env*amount*.45f,Mathf.Sin(t*37)*env*amount*.55f);shakePivot.localPosition=new Vector3(Mathf.Sin(t*51),Mathf.Sin(t*41),0)*env*(XRSettings.isDeviceActive?.0015f:.007f);}
   if(remaining>0)lesson.cabinLight.intensity=1.6f-.24f*Mathf.Abs(Mathf.Sin(t*18))*env;
  }
  void Include(ref Bounds bounds,RectTransform rect){if(!rect||!rect.gameObject.activeInHierarchy)return;rect.GetWorldCorners(frameCorners);foreach(var corner in frameCorners)bounds.Encapsulate(corner);}
  void ResetPivot(){if(shakePivot){shakePivot.localRotation=Quaternion.identity;shakePivot.localPosition=Vector3.zero;}LastShakeAmplitude=0;}
  void OnDisable(){ResetPivot();if(lesson&&lesson.cabinLight)lesson.cabinLight.intensity=1.6f;}
  void OnDestroy(){if(clunk)Destroy(clunk);}
 }
}
