using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectWait.M.Elevator
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ElevatorSceneRendering : MonoBehaviour
    {
        public RenderPipelineAsset scenePipeline;
        RenderPipelineAsset previous;
        bool applied;
        void OnEnable()
        {
            if(!Application.isPlaying||!scenePipeline)return;
            previous=QualitySettings.renderPipeline;QualitySettings.renderPipeline=scenePipeline;applied=true;
        }
        void OnDisable(){if(applied&&QualitySettings.renderPipeline==scenePipeline)QualitySettings.renderPipeline=previous;applied=false;}
    }
}
