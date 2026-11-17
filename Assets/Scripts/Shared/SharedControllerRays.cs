using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace ProjectWait.M.Shared
{
    // Installed on the shared scene bootstrap. Apply before its duplicate music
    // owner is retired, so direct entry and every subsequent scene get one style.
    [DefaultExecutionOrder(-2600)]
    public sealed class SharedControllerRays : MonoBehaviour
    {
        public Material baseMaterial, emptyMaterial, lineMaterial;
        public Gradient gradient = new Gradient();
        public float widthMultiplier = 1;
        public LineProperties noHit, uiHit, uiPress, hover, select;
        public float startOffset,endOffset=.005f,animationSpeed=8;
        public int pointCount=20;
        public bool customizeState=true,swapMaterials;

        void Awake()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                foreach (var visual in root.GetComponentsInChildren<CurveVisualController>(true)) Apply(visual);
                foreach (var caster in root.GetComponentsInChildren<CurveInteractionCaster>(true))
                    caster.castDistance = Mathf.Max(30, caster.castDistance);
            }
        }

        public void Apply(CurveVisualController visual)
        {
            visual.lineDynamicsMode = LineDynamicsMode.Traditional;
            visual.maxVisualCurveDistance = visual.restingVisualLineLength = 30;
            visual.extendLineToEmptyHit = true;
            visual.extensionRate = 0; // Immediate full length after leaving a target.
            visual.curveStartOffset=startOffset;visual.curveEndOffset=endOffset;visual.linePropertyAnimationSpeed=animationSpeed;
            visual.visualPointCount=pointCount;visual.customizeLinePropertiesForState=customizeState;visual.swapMaterials=swapMaterials;
            visual.noValidHitProperties = Copy(noHit);
            visual.uiHitProperties = Copy(uiHit);
            visual.uiPressHitProperties = Copy(uiPress);
            visual.hoverHitProperties = Copy(hover);
            visual.selectHitProperties = Copy(select);
            visual.baseLineMaterial = baseMaterial;
            visual.emptyHitMaterial = emptyMaterial;
            if (visual.lineRenderer)
            {
                visual.lineRenderer.sharedMaterial = lineMaterial;
                visual.lineRenderer.colorGradient = gradient;
                visual.lineRenderer.widthMultiplier = widthMultiplier;
            }
        }

        // Each controller owns its mutable state properties, not another hand's.
        static LineProperties Copy(LineProperties source) => source == null ? new LineProperties() :
            JsonUtility.FromJson<LineProperties>(JsonUtility.ToJson(source));
    }
}
