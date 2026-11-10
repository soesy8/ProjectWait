using UnityEngine;

namespace ProjectWait.TeamShare
{
    /// <summary>Standalone facial controls; no elevator lesson dependency.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class SharedFriendExpressions : MonoBehaviour
    {
        public enum Expression { Happy, Surprised, Worried, Sad, Neutral }

        public SkinnedMeshRenderer face;
        public Expression expression = Expression.Happy;
        [Range(0, 100)] public float strength = 100;
        public bool automaticBlink = true;

        Mesh cachedMesh;
        int blink = -1, smile = -1, surprised = -1, worried = -1, sad = -1;

        void Awake()
        {
            if (!face) face = GetComponentInChildren<SkinnedMeshRenderer>();
            Cache();
        }

        void Cache()
        {
            if (!face || !face.sharedMesh) return;
            cachedMesh = face.sharedMesh;
            blink = cachedMesh.GetBlendShapeIndex("Blink");
            smile = cachedMesh.GetBlendShapeIndex("Smile");
            surprised = cachedMesh.GetBlendShapeIndex("Surprised");
            worried = cachedMesh.GetBlendShapeIndex("Worried");
            sad = cachedMesh.GetBlendShapeIndex("Sad");
        }

        public void SetExpression(Expression value, float amount = 100)
        {
            expression = value;
            strength = Mathf.Clamp(amount, 0, 100);
            if (!face || !face.sharedMesh) return;
            if (face.sharedMesh != cachedMesh) Cache();
            Apply();
        }

        void LateUpdate()
        {
            if (!face || !face.sharedMesh) return;
            if (face.sharedMesh != cachedMesh) Cache();
            Apply();
            float distance = Mathf.Abs(Time.unscaledTime % 4.2f - 4.05f);
            Weight(blink, automaticBlink ? Mathf.Clamp01(1 - distance / .12f) * 100 : 0);
        }

        void Apply()
        {
            Weight(smile, expression == Expression.Happy ? strength * .25f : 0);
            Weight(surprised, expression == Expression.Surprised ? strength : 0);
            Weight(worried, expression == Expression.Worried ? strength : 0);
            Weight(sad, expression == Expression.Sad ? strength : 0);
        }

        void Weight(int index, float value)
        {
            if (face && face.sharedMesh && index >= 0 && index < face.sharedMesh.blendShapeCount)
                if (!Mathf.Approximately(face.GetBlendShapeWeight(index), value))
                    face.SetBlendShapeWeight(index, value);
        }
    }
}
