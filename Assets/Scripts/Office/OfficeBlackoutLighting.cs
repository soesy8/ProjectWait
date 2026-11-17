using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectWait.YB.OfficeBlackout
{
    // Scene-local light story; never edits shared materials or the project pipeline.
    public sealed class OfficeBlackoutLighting : MonoBehaviour
    {
        public OfficeBlackoutDirector director;
        public Light mainLight;
        public Light[] powerLights;
        public float transitionSeconds = .65f;
        public float Darkness { get; private set; }
        float target, savedIntensity;
        Color savedColor, savedAmbient;
        AmbientMode savedMode;
        bool[] enabledLights;
        static readonly int DarknessId = Shader.PropertyToID("_OfficeDarkness");

        void OnEnable()
        {
            savedMode = RenderSettings.ambientMode; savedAmbient = RenderSettings.ambientLight;
            if (mainLight) { savedIntensity = mainLight.intensity; savedColor = mainLight.color; }
            enabledLights = new bool[powerLights.Length];
            for (int i = 0; i < powerLights.Length; i++) enabledLights[i] = powerLights[i] && powerLights[i].enabled;
            director.StepChanged += OnStep;
            Darkness = target = 0; Apply();
        }
        void OnStep(OfficeBlackoutFlow.Step step)
        {
            if (step == OfficeBlackoutFlow.Step.Errand || step == OfficeBlackoutFlow.Step.NotStarted) target = 0;
            else if (step == OfficeBlackoutFlow.Step.BlackoutReaction) target = 1;
        }
        void Update()
        {
            if (OfficeStoryTime.Frozen) return;
            Darkness = Mathf.MoveTowards(Darkness, target, OfficeStoryTime.Delta / Mathf.Max(.01f, transitionSeconds));
            Apply();
        }
        void Apply()
        {
            Shader.SetGlobalFloat(DarknessId, Darkness);
            if (mainLight)
            {
                mainLight.intensity = Mathf.Lerp(savedIntensity, .14f, Darkness);
                mainLight.color = Color.Lerp(savedColor, new Color(.48f, .68f, .76f), Darkness);
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(new Color(.66f, .72f, .67f), new Color(.07f, .11f, .14f), Darkness);
            for (int i = 0; i < powerLights.Length; i++)
                if (powerLights[i]) powerLights[i].enabled = enabledLights[i] && Darkness < .1f;
        }
        void OnDisable()
        {
            if (director) director.StepChanged -= OnStep;
            Shader.SetGlobalFloat(DarknessId, 0);
            RenderSettings.ambientMode = savedMode; RenderSettings.ambientLight = savedAmbient;
            if (mainLight) { mainLight.intensity = savedIntensity; mainLight.color = savedColor; }
            if (enabledLights != null) for (int i = 0; i < powerLights.Length; i++)
                if (powerLights[i]) powerLights[i].enabled = enabledLights[i];
        }
    }
}
