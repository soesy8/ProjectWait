using System.Collections.Generic;
using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Placement-time screen-space exclusion regions. No per-frame card chasing,
    // camera steering, gaze tests or changes to the learning state.
    public static class ElevatorUIPlacement
    {
        public struct Region
        {
            public Rect rect;
            public float weight;
            public string name;
            public Region(Rect value, float importance, string label) { rect=value;weight=importance;name=label; }
        }

        public static float Overlap(Rect a, Rect b) => Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin))*
            Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));

        public static bool Project(Camera camera, Bounds bounds, out Rect rect)
        {
            Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            int visible=0;
            for(int i=0;i<8;i++)
            {
                var corner=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var p=camera.WorldToViewportPoint(corner);
                if(p.z<=camera.nearClipPlane)continue;
                min=Vector2.Min(min,p);max=Vector2.Max(max,p);visible++;
            }
            if(visible==0||max.x<0||min.x>1||max.y<0||min.y>1){rect=default;return false;}
            const float margin=.012f;
            rect=Rect.MinMaxRect(Mathf.Max(0,min.x-margin),Mathf.Max(0,min.y-margin),Mathf.Min(1,max.x+margin),Mathf.Min(1,max.y+margin));
            return rect.width>0&&rect.height>0;
        }

        static void Add(List<Region> result, Camera camera, Bounds bounds, float weight, string name)
        {
            if(Project(camera,bounds,out var rect))result.Add(new Region(rect,weight,name));
        }

        public static void Collect(ElevatorLesson lesson, List<Region> result)
        {
            result.Clear();var camera=lesson.playerCamera;
            bool side=false,front=false;Bounds sidePanel=default,frontPanel=default;
            foreach(var observation in lesson.CollectObservations())
            {
                if(!observation||!observation.gameObject.activeInHierarchy)continue;
                var collider=observation.GetComponent<Collider>();
                if(!collider||!collider.enabled)continue;
                var bounds=collider.bounds;
                switch(observation.clue)
                {
                    case ElevatorClue.Intercom:case ElevatorClue.FloorButtons:
                        if(side)sidePanel.Encapsulate(bounds);else{sidePanel=bounds;side=true;}
                        Add(result,camera,bounds,100,observation.clue.ToString());break;
                    case ElevatorClue.FrontIntercom:case ElevatorClue.FrontFloorButtons:case ElevatorClue.Identity:
                        if(front)frontPanel.Encapsulate(bounds);else{frontPanel=bounds;front=true;}
                        Add(result,camera,bounds,100,observation.clue.ToString());break;
                    case ElevatorClue.Notice:case ElevatorClue.FacilityGuide:case ElevatorClue.Display:
                        Add(result,camera,bounds,12,observation.clue.ToString());break;
                    case ElevatorClue.Door:
                        // A blank closed door leaf is usable space, but keep the seam
                        // and, during rescue, the open passage clear.
                        if(lesson.Flow!=null&&lesson.Flow.Step==ElevatorStep.Rescue)
                            Add(result,camera,bounds,25,"Doorway");
                        else Add(result,camera,new Bounds(bounds.center,new Vector3(.12f,bounds.size.y,.12f)),3,"Door seam");
                        break;
                    case ElevatorClue.Rescuer:
                        Add(result,camera,new Bounds(observation.transform.position+Vector3.up*1.52f,new Vector3(.48f,.55f,.45f)),60,"Rescuer face");break;
                }
            }
            // Protect the whole control surface, not merely the tiny selectable 1F
            // button. These margins cover the surrounding button rows in this cabin.
            if(side){sidePanel.Expand(new Vector3(.08f,.8f,.6f));Add(result,camera,sidePanel,100,"Side control panel");}
            if(front){frontPanel.Expand(new Vector3(.35f,.4f,.08f));Add(result,camera,frontPanel,100,"Front control panel");}
            if(lesson.friend&&lesson.friend.gameObject.activeInHierarchy)
                Add(result,camera,new Bounds(lesson.friend.position+Vector3.up*1.24f,new Vector3(.66f,.78f,.66f)),80,"Friend face");
        }

        public static Vector2 Choose(Vector2 size, List<Region> regions, Vector2 preferred, float lower, float upper, out float overlap)
        {
            var half=size*.5f;
            float left=.025f+half.x,right=.975f-half.x,bottom=lower+half.y,top=upper-half.y;
            if(right<left)left=right=.5f;
            if(top<bottom)bottom=top=(lower+upper)*.5f;
            Vector2 best=new Vector2(left,top);float score=float.PositiveInfinity;overlap=float.PositiveInfinity;
            for(int x=0;x<=8;x++)for(int y=0;y<=6;y++)
            {
                var center=new Vector2(Mathf.Lerp(left,right,x/8f),Mathf.Lerp(bottom,top,y/6f));
                var rect=new Rect(center-half,size);float collision=0;
                foreach(var area in regions)collision+=Overlap(rect,area.rect)*area.weight;
                float candidate=collision*10000+Vector2.SqrMagnitude(center-preferred)*.01f;
                if(candidate>=score)continue;
                score=candidate;best=center;overlap=collision;
            }
            return best;
        }
    }
}
