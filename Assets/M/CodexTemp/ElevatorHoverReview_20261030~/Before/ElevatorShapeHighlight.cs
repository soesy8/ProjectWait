using UnityEngine;

namespace ProjectWait.M.Elevator
{
    public sealed class ElevatorShapeHighlight : MonoBehaviour
    {
        public Renderer[] shells;
        public SkinnedMeshRenderer[] skinnedSources,skinnedShells;
        public bool Visible {get;private set;}
        public void SetVisible(bool value)
        {
            Visible=value;if(shells==null)return;
            foreach(var r in shells)if(r)r.enabled=value;
        }
        void LateUpdate()
        {
            if(!Visible||skinnedShells==null||skinnedSources==null)return;
            for(int i=0;i<skinnedShells.Length;i++)
            {
                var s=skinnedSources[i];var d=skinnedShells[i];if(!s||!d||!s.sharedMesh)continue;
                for(int b=0;b<s.sharedMesh.blendShapeCount;b++)d.SetBlendShapeWeight(b,s.GetBlendShapeWeight(b));
            }
        }
        void OnDisable(){SetVisible(false);}
    }
}
