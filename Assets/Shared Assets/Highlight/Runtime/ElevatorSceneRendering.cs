using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace ProjectWait.M.Elevator
{
    [DefaultExecutionOrder(-1000)]
    public sealed class ElevatorSceneRendering : MonoBehaviour
    {
        public RenderPipelineAsset scenePipeline;
        static readonly List<ElevatorSceneRendering> owners=new List<ElevatorSceneRendering>();
        static RenderPipelineAsset previous,installed;
        bool applied;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOwners(){owners.Clear();previous=installed=null;}
        void OnEnable()
        {
            if(!Application.isPlaying||!scenePipeline)return;
            if(owners.Count==0)previous=QualitySettings.renderPipeline;
            owners.Add(this);applied=true;
            installed=scenePipeline;QualitySettings.renderPipeline=installed;
        }
        void OnDisable()
        {
            if(!applied)return;
            bool last=owners.Count>0&&owners[owners.Count-1]==this;
            owners.Remove(this);applied=false;
            if(last&&QualitySettings.renderPipeline==installed)
            {
                installed=owners.Count>0?owners[owners.Count-1].scenePipeline:previous;
                QualitySettings.renderPipeline=installed;
            }
            if(owners.Count==0)previous=installed=null;
        }
    }
}
