using TMPro;
using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Serialization-compatible retirement of the old look-at arrows/rings.
    // Keep existing scene references and GUID intact; none of this controls play.
    public sealed class ElevatorPosterCutscene : MonoBehaviour
    {
        public ElevatorLesson lesson;
        public Transform posterAnchor;
        public Collider posterSurface;
        public Canvas screenCanvas,worldCanvas;
        public CanvasGroup arrowGroup,ringGroup,instructionGroup;
        public RectTransform worldRing,arrow,instructionPanel;
        public ElevatorPosterCueGraphic ringGraphic;
        public TMP_Text instruction;
        public float holdSeconds=1.5f,pulseSeconds=1.5f,pulseGrowth=.2f,targetRadius=.19f;
        public bool Awaiting=>false;
        public float HoldElapsed=>0;
        public float PulseScale=>1;
        public float ArrowAlpha=>0;
        public float RingAlpha=>0;
        public bool RingFading=>false;
        public bool ArrowClamped=>false;
        public bool GuideTargetCentered=>false;
        public float GuideDistance=>0;
        public float ArrowTargetAlpha=>0;
        public Vector3 TargetPosition=>posterAnchor?posterAnchor.position:Vector3.zero;
        void Awake(){Hide();}
        void OnEnable(){Hide();}
        void OnDisable(){Hide();}
        public void Advance(float seconds){Hide();}
        public bool HitsTarget(Ray ray,out float distance){distance=0;return false;}
        public bool IsInAttentionArea(Vector3 target)=>false;
        void Hide()
        {
            if(screenCanvas)screenCanvas.gameObject.SetActive(false);
            if(worldCanvas)worldCanvas.gameObject.SetActive(false);
            if(arrowGroup)arrowGroup.alpha=0;
            if(ringGroup)ringGroup.alpha=0;
            if(instructionGroup)instructionGroup.alpha=0;
        }
    }
}
