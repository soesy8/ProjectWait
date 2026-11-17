using UnityEngine;

namespace ProjectWait.YB.OfficeBlackout
{
    // The B-derived teacher keeps its baked Blender skeleton. This component
    // chooses in-place clips; the existing waypoint mover alone owns translation.
    public sealed class OfficeTeacherAnimation : MonoBehaviour
    {
        public Animator animator;
        public SkinnedMeshRenderer skin;
        public OfficeBlackoutWaypointMover mover;
        public OfficeBlackoutPresentation presentation;
        public Camera viewer;

        static readonly int Walking = Animator.StringToHash("Walking");
        static readonly int Speaking = Animator.StringToHash("Speaking");
        int blink = -1, talk = -1;
        float clock;

        void Awake()
        {
            if (skin && skin.sharedMesh)
            {
                blink = skin.sharedMesh.GetBlendShapeIndex("Blink");
                talk = skin.sharedMesh.GetBlendShapeIndex("Talk");
            }
            if (animator)
            {
                animator.applyRootMotion = false;
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        void Update()
        {
            bool frozen = OfficeStoryTime.Frozen;
            if (animator) animator.speed = frozen ? 0 : 1;
            if (frozen) return;

            clock += OfficeStoryTime.Delta;
            bool moving = mover && mover.IsMoving;
            bool speaking = !moving && presentation && presentation.DialogueVisible &&
                presentation.speakerText && presentation.speakerText.text == "선생님";
            if (animator)
            {
                animator.SetBool(Walking, moving);
                animator.SetBool(Speaking, speaking);
            }
            if (skin)
            {
                float phase = clock % 4.3f;
                float blinkWeight = phase > 4.08f ?
                    Mathf.Sin((phase - 4.08f) / .22f * Mathf.PI) * 100 : 0;
                if (blink >= 0) skin.SetBlendShapeWeight(blink, blinkWeight);
                if (talk >= 0)
                    skin.SetBlendShapeWeight(talk, speaking ? 22 + 18 * Mathf.Sin(clock * 10) : 0);
            }
        }

        void LateUpdate()
        {
            if (OfficeStoryTime.Frozen || !viewer || mover && mover.IsMoving) return;
            Vector3 direction = Vector3.ProjectOnPlane(viewer.transform.position - transform.position, Vector3.up);
            if (direction.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction),
                    OfficeStoryTime.Delta * 3);
        }
    }
}
