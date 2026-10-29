using System.Collections.Generic;
using UnityEngine;

namespace ProjectWait.M.Elevator
{
    // Resolution-independent notebook illustrations, drawn directly on each 3D page's Canvas.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ElevatorMemoryGraphic : UnityEngine.UI.MaskableGraphic
    {
        public enum Illustration { Teamwork, Phone, Location, Together }
        public Illustration illustration;
        static readonly Color Ink=new Color(.28f,.19f,.14f), Paper=new Color(1,.965f,.86f), Teal=new Color(.16f,.48f,.44f);
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            if(illustration!=Illustration.Teamwork)
            {
                Ellipse(vh,new Vector2(.5f,.5f),new Vector2(.48f,.48f),Teal);
                if(illustration==Illustration.Phone)
                {
                    Shape(vh,Paper,new Vector2(.28f,.75f),new Vector2(.20f,.65f),new Vector2(.22f,.50f),new Vector2(.35f,.31f),new Vector2(.55f,.20f),new Vector2(.72f,.23f),new Vector2(.79f,.34f),new Vector2(.62f,.47f),new Vector2(.53f,.39f),new Vector2(.39f,.53f),new Vector2(.46f,.62f));
                }
                else if(illustration==Illustration.Location)
                {
                    Ellipse(vh,new Vector2(.5f,.61f),new Vector2(.23f,.23f),Paper);
                    Shape(vh,Paper,new Vector2(.30f,.50f),new Vector2(.70f,.50f),new Vector2(.50f,.16f));
                    Ellipse(vh,new Vector2(.5f,.62f),new Vector2(.09f,.09f),Teal);
                }
                else
                {
                    foreach(float x in new[]{.33f,.67f})
                    {Ellipse(vh,new Vector2(x,.66f),new Vector2(.12f,.12f),Paper);Ellipse(vh,new Vector2(x,.33f),new Vector2(.20f,.19f),Paper);}
                }
                return;
            }
            Ellipse(vh,new Vector2(.5f,.52f),new Vector2(.46f,.44f),new Color(.94f,.86f,.64f));
            Ellipse(vh,new Vector2(.5f,.56f),new Vector2(.39f,.36f),new Color(1,.94f,.76f));
            for(int i=0;i<7;i++)
            {
                float x=.12f+i*.12f;
                Ellipse(vh,new Vector2(x,.19f+Mathf.Sin(i)*.04f),new Vector2(.10f,.15f),i%2==0?new Color(.61f,.75f,.40f):new Color(.79f,.89f,.60f));
            }
            Arm(vh,false,new Color(1,.55f,.176f)); Arm(vh,true,new Color(.28f,.56f,.51f));
            for(int i=0;i<3;i++)
            {
                float a=(65+i*25)*Mathf.Deg2Rad;
                Vector2 c=new Vector2(.5f,.7f)+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.27f;
                var d=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*.085f;var side=new Vector2(-d.y,d.x)*.25f;
                Shape(vh,new Color(1,.67f,.24f),c-side,c+side,c+side+d,c-side+d);
            }
        }
        void Arm(UnityEngine.UI.VertexHelper vh,bool mirror,Color sleeve)
        {
            Vector2 P(float x,float y)=>new Vector2(mirror?1-x:x,y);
            Shape(vh,Ink,P(.12f,.11f),P(.31f,.07f),P(.48f,.40f),P(.30f,.48f));
            Shape(vh,sleeve,P(.135f,.12f),P(.30f,.09f),P(.46f,.39f),P(.31f,.45f));
            Shape(vh,Color.Lerp(sleeve,Color.white,.24f),P(.15f,.13f),P(.20f,.12f),P(.35f,.43f),P(.31f,.45f));
            var hand=new[]{P(.33f,.42f),P(.28f,.51f),P(.25f,.59f),P(.26f,.63f),P(.30f,.65f),P(.34f,.61f),P(.33f,.75f),P(.35f,.79f),P(.39f,.79f),P(.41f,.74f),P(.42f,.83f),P(.46f,.85f),P(.49f,.82f),P(.49f,.72f),P(.51f,.76f),P(.54f,.75f),P(.55f,.71f),P(.53f,.54f),P(.48f,.45f),P(.44f,.40f)};
            Polygon(vh,hand,Ink);
            Vector2 center=P(.41f,.62f);
            for(int i=0;i<hand.Length;i++)hand[i]=center+(hand[i]-center)*.89f;
            Polygon(vh,hand,new Color(1,.75f,.54f));
            Shape(vh,new Color(1,.84f,.64f),P(.35f,.48f),P(.36f,.64f),P(.40f,.67f),P(.44f,.62f),P(.45f,.45f));
        }
        void Ellipse(UnityEngine.UI.VertexHelper vh,Vector2 center,Vector2 radius,Color tint)
        {
            var p=new Vector2[48];for(int i=0;i<p.Length;i++){float a=i*Mathf.PI*2/p.Length;p[i]=center+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),radius);}Polygon(vh,p,tint);
        }
        void Shape(UnityEngine.UI.VertexHelper vh,Color tint,params Vector2[] p)=>Polygon(vh,p,tint);
        void Polygon(UnityEngine.UI.VertexHelper vh,IReadOnlyList<Vector2> points,Color tint)
        {
            if(points.Count<3)return;
            var rect=rectTransform.rect;int first=vh.currentVertCount;
            // Ear clipping handles the concave hand and phone silhouettes without fan artefacts.
            var indices=new List<int>();
            float area=0;for(int i=0;i<points.Count;i++){var a=points[i];var b=points[(i+1)%points.Count];area+=a.x*b.y-b.x*a.y;}
            for(int i=0;i<points.Count;i++){var p=points[i];vh.AddVert(new Vector3(rect.xMin+p.x*rect.width,rect.yMin+p.y*rect.height),tint,Vector2.zero);indices.Add(area>0?i:points.Count-1-i);}
            int guard=0;
            while(indices.Count>2&&guard++<points.Count*points.Count)
            {
                bool found=false;
                for(int i=0;i<indices.Count;i++)
                {
                    int a=indices[(i+indices.Count-1)%indices.Count],b=indices[i],c=indices[(i+1)%indices.Count];
                    if(Cross(points[b]-points[a],points[c]-points[b])<=0)continue;
                    bool inside=false;
                    foreach(int n in indices)if(n!=a&&n!=b&&n!=c&&Cross(points[b]-points[a],points[n]-points[a])>=0&&Cross(points[c]-points[b],points[n]-points[b])>=0&&Cross(points[a]-points[c],points[n]-points[c])>=0){inside=true;break;}
                    if(inside)continue;
                    vh.AddTriangle(first+a,first+b,first+c);indices.RemoveAt(i);found=true;break;
                }
                if(!found)break;
            }
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    }
}
