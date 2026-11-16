// Dialogue-preview snapshot of Assets/M/Scripts/ObservationTrial/ElevatorCameraEffects.cs. Original is not modified.
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using ProjectWait.M.Shared;
namespace ProjectWait.M.Elevator.DialoguePreview
{
 [DefaultExecutionOrder(600)]
 public sealed class ElevatorCameraEffects:MonoBehaviour
 {
  public ElevatorLesson lesson;public Transform shakePivot;
  public float LastShakeAmplitude{get;private set;}
  ElevatorStep previous=(ElevatorStep)(-1);int run=-1;float remaining;AudioSource sound;AudioClip clunk;
  public int HapticAttempts{get;private set;}public int HapticSuccesses{get;private set;}
  public Transform cabinPivot;
  Vector3 cabinPosition;Quaternion cabinRotation;int nextPulse;
  static readonly float[] PulseTimes={0,.28f,.62f},PulseStrength={.85f,.65f,.35f};
  const float FaultSeconds=1.25f;
  void Awake()
  {
   sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.volume=.18f;sound.spatialBlend=0;const int n=17640;var data=new float[n];var random=new System.Random(31);for(int i=0;i<n;i++){float t=i/22050f;float impulse=Mathf.Exp(-t*12)+.5f*Mathf.Exp(-Mathf.Abs(t-.19f)*35);data[i]=((float)(random.NextDouble()*2-1)*.34f+Mathf.Sin(t*2*Mathf.PI*53)*.66f)*impulse;}clunk=AudioClip.Create("Elevator mechanical clunk",n,1,22050,false);clunk.SetData(data,0);
   if(!cabinPivot&&lesson)cabinPivot=lesson.transform.Find("Cabin");
   if(cabinPivot){cabinPosition=cabinPivot.localPosition;cabinRotation=cabinPivot.localRotation;}
  }
  void LateUpdate()
  {
   if(!lesson||lesson.Flow==null||lesson.IsPaused)return;var f=lesson.Flow;
   if(run!=lesson.RunVersion){run=lesson.RunVersion;previous=f.Step;remaining=0;ResetPivot();}
   if(previous!=f.Step&&f.Step==ElevatorStep.Stopped){remaining=FaultSeconds;nextPulse=0;if(sound)sound.PlayOneShot(clunk,1.5f);}previous=f.Step;
   if(remaining<=0){ResetPivot();return;}
   remaining=Mathf.Max(0,remaining-Time.deltaTime);float t=FaultSeconds-remaining;
   float env=Mathf.Exp(-t*4)+.65f*Mathf.Exp(-Mathf.Abs(t-.32f)*18)+.35f*Mathf.Exp(-Mathf.Abs(t-.67f)*20);
   LastShakeAmplitude=env;
   while(nextPulse<3&&t>=PulseTimes[nextPulse])
   {
    if(ExperiencePreferences.Current.haptics)
    {HapticAttempts+=2;if(HapticsUtility.SendHapticImpulse(PulseStrength[nextPulse],.11f,HapticsUtility.Controller.Left))HapticSuccesses++;if(HapticsUtility.SendHapticImpulse(PulseStrength[nextPulse],.11f,HapticsUtility.Controller.Right))HapticSuccesses++;}
    nextPulse++;
   }
   float strength=ExperiencePreferences.Current.reducedShake?.25f:1;
   // Amplify the cabin, not the tracked eye. HMD pose/horizon remain untouched.
   if(cabinPivot){cabinPivot.localRotation=cabinRotation*Quaternion.Euler(Mathf.Sin(t*36)*env*1.5f*strength,0,Mathf.Sin(t*29)*env*2.2f*strength);cabinPivot.localPosition=cabinPosition+new Vector3(Mathf.Sin(t*43),Mathf.Sin(t*35),0)*(.022f*env*strength);}
   if(shakePivot){float amount=XRSettings.isDeviceActive?0:.8f*strength;shakePivot.localRotation=Quaternion.Euler(Mathf.Sin(t*43)*env*amount,0,Mathf.Sin(t*37)*env*amount*.45f);shakePivot.localPosition=Vector3.zero;}
   if(lesson.cabinLight)lesson.cabinLight.intensity=1.6f-.65f*Mathf.Abs(Mathf.Sin(t*18))*Mathf.Clamp01(env)*strength;
  }
  void ResetPivot(){if(shakePivot){shakePivot.localRotation=Quaternion.identity;shakePivot.localPosition=Vector3.zero;}if(cabinPivot){cabinPivot.localPosition=cabinPosition;cabinPivot.localRotation=cabinRotation;}LastShakeAmplitude=0;}
  void OnDisable(){ResetPivot();if(lesson&&lesson.cabinLight)lesson.cabinLight.intensity=1.6f;}
  void OnDestroy(){if(clunk)Destroy(clunk);}
 }
}
