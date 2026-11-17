using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ProjectWait.M.TitleScene
{
    // A scene-local runtime override. The shared project pipeline asset is never edited.
    [DefaultExecutionOrder(-100)]
    public sealed class TitleSceneRenderProfile : MonoBehaviour
    {
        public UniversalRenderPipelineAsset profile;
        RenderPipelineAsset previous;
        bool applied;
        void OnEnable()
        {
            if (!Application.isPlaying || !profile) return;
            previous = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = profile;
            applied = true;
        }
        void OnDisable()
        {
            if (applied && QualitySettings.renderPipeline == profile) QualitySettings.renderPipeline = previous;
            applied = false;
        }
    }
}
