using System;
using UnityEngine;
using ProjectWait.M.Elevator;

namespace ProjectWait.M
{
    /// <summary>Facial controls for the single-material Friend model.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class FriendImprovedExpressions : MonoBehaviour
    {
        public enum Expression { Happy, Surprised, Worried, Sad, Neutral }
        public SkinnedMeshRenderer face;
        public Expression expression = Expression.Happy;
        [Range(0, 100)] public float strength = 100;
        public bool automaticBlink = true;
        public bool followElevatorLesson = true;
        ElevatorLesson lesson;
        Mesh cachedMesh;
        int blink = -1, smile = -1, surprised = -1, worried = -1, sad = -1;

        void Awake()
        {
            if (!face) face = GetComponentInChildren<SkinnedMeshRenderer>();
            lesson = GetComponentInParent<ElevatorLesson>();
            Cache();
        }

        void Cache()
        {
            if (!face || !face.sharedMesh) return;
            var mesh = face.sharedMesh;
            cachedMesh=mesh;
            blink = mesh.GetBlendShapeIndex("Blink");
            smile = mesh.GetBlendShapeIndex("Smile");
            surprised = mesh.GetBlendShapeIndex("Surprised");
            worried = mesh.GetBlendShapeIndex("Worried");
            sad = mesh.GetBlendShapeIndex("Sad");
        }

        public void SetExpression(Expression value, float amount = 100)
        {
            expression = value;
            strength = Mathf.Clamp(amount, 0, 100);
            if (smile < 0) Cache();
            Apply(value, strength);
        }

        void LateUpdate()
        {
            if (!face || (lesson && lesson.IsPaused)) return;
            if(face.sharedMesh!=cachedMesh)Cache();
            var value = expression;
            if (followElevatorLesson && lesson && lesson.Flow != null)
            {
                var flow = lesson.Flow;
                if (flow.Step == ElevatorStep.Stopped || flow.Step == ElevatorStep.Connecting || flow.Step == ElevatorStep.DoorPosterWarning)
                    value = Expression.Surprised;
                else if (!flow.Reassured && (flow.Step == ElevatorStep.Waiting || flow.Step == ElevatorStep.Unanswered || flow.Step == ElevatorStep.Call || flow.IsCallCutscene || flow.Step == ElevatorStep.DoorLean || flow.Step == ElevatorStep.DoorPosterPointing))
                    value = Expression.Worried;
                else if (flow.Step == ElevatorStep.SafeWallMove) value = Expression.Neutral;
                else value = Expression.Happy;
            }
            Apply(value, strength);
            if (automaticBlink)
            {
                float distance = Mathf.Abs(Time.unscaledTime % 4.2f - 4.05f);
                Weight(blink, Mathf.Clamp01(1 - distance / .12f) * 100);
            }
        }

        void Apply(Expression value, float amount)
        {
            Weight(smile, value == Expression.Happy ? amount * .25f : 0);
            Weight(surprised, value == Expression.Surprised ? amount : 0);
            Weight(worried, value == Expression.Worried ? amount : 0);
            Weight(sad, value == Expression.Sad ? amount : 0);
        }

        void Weight(int index, float value)
        {
            if (face && index >= 0 && index < face.sharedMesh.blendShapeCount)
                if(!Mathf.Approximately(face.GetBlendShapeWeight(index),value))face.SetBlendShapeWeight(index, value);
        }
    }
}
