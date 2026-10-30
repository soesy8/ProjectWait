using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.Elevator
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ElevatorReticleGraphic : MaskableGraphic
    {
        public bool focused;
        public void SetFocused(bool value){if(focused==value)return;focused=value;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();Color c=focused?new Color(.38f,.87f,1,1):new Color(1,.98f,.87f,.95f);
            const int segments=32;float radius=focused?7:5.5f;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Vector2 n=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),m=new Vector2(Mathf.Cos(b),Mathf.Sin(b));int k=vh.currentVertCount;
                vh.AddVert(n*(radius-1),c,Vector2.zero);vh.AddVert(n*(radius+1),c,Vector2.zero);vh.AddVert(m*(radius+1),c,Vector2.zero);vh.AddVert(m*(radius-1),c,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
            }
            int index=vh.currentVertCount;vh.AddVert(new Vector2(-1,-1),c,Vector2.zero);vh.AddVert(new Vector2(-1,1),c,Vector2.zero);vh.AddVert(new Vector2(1,1),c,Vector2.zero);vh.AddVert(new Vector2(1,-1),c,Vector2.zero);vh.AddTriangle(index,index+1,index+2);vh.AddTriangle(index,index+2,index+3);
        }
    }
}
