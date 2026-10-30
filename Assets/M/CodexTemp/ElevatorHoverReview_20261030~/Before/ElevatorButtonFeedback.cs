using UnityEngine;
namespace ProjectWait.M.Elevator
{
 public sealed class ElevatorButtonFeedback:MonoBehaviour
 {
  public Transform movingPart;public Vector3 localPressDirection;public Renderer[] pulseRenderers;
  public int PressCount{get;private set;}public bool IsAnimating=>remaining>0;
  Vector3 rest;bool initialized;float remaining;AudioSource sound;AudioClip click;
  void Awake(){initialized=true;if(movingPart)rest=movingPart.localPosition;sound=gameObject.AddComponent<AudioSource>();sound.playOnAwake=false;sound.spatialBlend=.65f;sound.volume=.22f;const int n=4410;var samples=new float[n];for(int i=0;i<n;i++){float t=i/22050f;samples[i]=Mathf.Sin(t*2*Mathf.PI*850)*Mathf.Exp(-t*26)*.5f;}click=AudioClip.Create("Elevator button confirm",n,1,22050,false);click.SetData(samples,0);SetPulse(false);}
  public void Press(){PressCount++;remaining=.48f;if(sound&&click)sound.PlayOneShot(click);SetPulse(true);}
  void Update(){if(remaining<=0)return;remaining=Mathf.Max(0,remaining-Time.deltaTime);float amount=Mathf.Sin(Mathf.Clamp01((.48f-remaining)/.20f)*Mathf.PI);if(movingPart)movingPart.localPosition=rest+localPressDirection*amount;if(remaining<=0){if(movingPart)movingPart.localPosition=rest;SetPulse(false);}}
  void SetPulse(bool value){if(pulseRenderers!=null)foreach(var r in pulseRenderers)if(r)r.enabled=value;}
  public void ResetFeedback(){remaining=0;PressCount=0;if(initialized&&movingPart)movingPart.localPosition=rest;if(sound)sound.Stop();SetPulse(false);}
  void OnDisable(){ResetFeedback();}
  void OnDestroy(){if(click)Destroy(click);}
 }
}
