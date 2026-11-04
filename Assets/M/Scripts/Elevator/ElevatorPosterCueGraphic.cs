using UnityEngine;

namespace ProjectWait.M.Elevator
{
    // Vector shapes keep the cue crisp at both screen and world canvas scales.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ElevatorPosterCueGraphic : UnityEngine.UI.MaskableGraphic
    {
        public bool ring;
        [Range(0,1)] public float progress;
        static readonly Color Ink=new Color(.28f,.19f,.14f),Paper=new Color(1,.965f,.86f),Teal=new Color(.16f,.48f,.44f);
        public void SetProgress(float value)
        {
            value=Mathf.Clamp01(value);
            if(progress==value||value>0&&value<1&&Mathf.Abs(progress-value)<.001f)return;
            progress=value;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            if(ring)
            {
                float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.42f;
                Band(mesh,radius+6,radius-8,Ink,1);
                Band(mesh,radius+3,radius-5,Paper,1);
                Band(mesh,radius+1,radius-3,Teal,1);
                Band(mesh,radius-10,radius-16,Paper,1);
                Band(mesh,radius-10,radius-16,Teal,progress);
            }
            else
            {
                Arrow(mesh,Vector2.zero,1,Ink);
                Arrow(mesh,new Vector2(-1,1),.82f,Paper);
                Arrow(mesh,new Vector2(-2,1),.63f,Teal);
            }
        }
        static void Band(UnityEngine.UI.VertexHelper mesh,float outer,float inner,Color tint,float fraction)
        {
            int count=Mathf.CeilToInt(64*fraction);
            for(int i=0;i<count;i++)
            {
                float a=Mathf.PI*.5f-Mathf.PI*2*i/64,b=Mathf.PI*.5f-Mathf.PI*2*Mathf.Min((i+1)/64f,fraction);
                Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a)),q=new Vector2(Mathf.Cos(b),Mathf.Sin(b));int k=mesh.currentVertCount;
                mesh.AddVert(p*inner,tint,Vector2.zero);mesh.AddVert(p*outer,tint,Vector2.zero);
                mesh.AddVert(q*outer,tint,Vector2.zero);mesh.AddVert(q*inner,tint,Vector2.zero);
                mesh.AddTriangle(k,k+1,k+2);mesh.AddTriangle(k,k+2,k+3);
            }
        }
        static void Arrow(UnityEngine.UI.VertexHelper mesh,Vector2 offset,float scale,Color tint)
        {
            Vector2[] points={new Vector2(-35,-11),new Vector2(0,-11),new Vector2(0,-25),new Vector2(35,0),new Vector2(0,25),new Vector2(0,11),new Vector2(-35,11)};
            int k=mesh.currentVertCount;
            foreach(var p in points)mesh.AddVert(offset+p*scale,tint,Vector2.zero);
            mesh.AddTriangle(k,k+1,k+5);mesh.AddTriangle(k,k+5,k+6);
            mesh.AddTriangle(k+2,k+3,k+4);
        }
    }
}
