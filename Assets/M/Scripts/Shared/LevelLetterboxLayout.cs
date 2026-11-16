using UnityEngine;

namespace ProjectWait.M.Shared
{
    // Same native-frustum geometry as the crosswalk: remove roll, never tracking/FOV.
    public static class LevelLetterboxLayout
    {
        public static Quaternion LevelRotation(Vector3 forward,ref Vector3 previousRight)
        {
            if(forward.sqrMagnitude<.000001f)return Quaternion.identity;
            forward.Normalize();var right=Vector3.Cross(Vector3.up,forward);
            if(right.sqrMagnitude<.000001f)right=Vector3.ProjectOnPlane(previousRight,forward);
            if(right.sqrMagnitude<.000001f)right=Vector3.ProjectOnPlane(Vector3.right,forward);
            right.Normalize();previousRight=right;return Quaternion.LookRotation(forward,Vector3.Cross(forward,right));
        }
        public static bool LayoutDesktop(Camera camera,RectTransform plane,RectTransform top,RectTransform bottom,float amount)
        {
            if(!ViewBounds(camera,plane,camera.worldToCameraMatrix,camera.projectionMatrix,out var coverage,out var range))return false;
            LayoutBands(plane,top,bottom,coverage,range,range,amount);return true;
        }
        public static bool LayoutStereo(Camera camera,ProjectWait.M.Elevator.DialoguePreview.StereoUISafeArea.Frame frame,RectTransform plane,RectTransform top,RectTransform bottom,float amount,out Rect coverage)
        {
            coverage=default;
            if(!ViewBounds(camera,plane,frame.leftView,frame.leftProjection,out var l,out var lv)||!ViewBounds(camera,plane,frame.rightView,frame.rightProjection,out var r,out var rv))return false;
            coverage=Rect.MinMaxRect(Mathf.Min(l.xMin,r.xMin),Mathf.Min(l.yMin,r.yMin),Mathf.Max(l.xMax,r.xMax),Mathf.Max(l.yMax,r.yMax));
            LayoutBands(plane,top,bottom,coverage,lv,rv,amount);return true;
        }
        public static void LayoutBands(RectTransform plane,RectTransform top,RectTransform bottom,Rect coverage,Vector2 left,Vector2 right,float amount)
        {
            plane.sizeDelta=coverage.size;float overscan=coverage.height*.02f;
            float band=Mathf.Min(left.y-left.x,right.y-right.x)*(200f/720f),p=Mathf.SmoothStep(0,1,amount);
            float low=Mathf.Lerp(coverage.yMin,Mathf.Max(left.x,right.x)+band,p),high=Mathf.Lerp(coverage.yMax,Mathf.Min(left.y,right.y)-band,p);
            float thickness=Mathf.Max(low-coverage.yMin+overscan,coverage.yMax+overscan-high);
            top.sizeDelta=bottom.sizeDelta=new Vector2(coverage.width+overscan*2,thickness);
            top.anchoredPosition=new Vector2(coverage.center.x,high+thickness*.5f);bottom.anchoredPosition=new Vector2(coverage.center.x,low-thickness*.5f);
        }
        static bool ViewBounds(Camera camera,Transform plane,Matrix4x4 view,Matrix4x4 projection,out Rect bounds,out Vector2 vertical)
        {
            bounds=default;vertical=default;var inverse=(projection*view).inverse;var origin=view.inverse.MultiplyPoint3x4(Vector3.zero);
            Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
            for(int i=0;i<4;i++){if(!Point(camera,plane,inverse,origin,view,new Vector2((i&1)==0?-1:1,(i&2)==0?-1:1),out var local))return false;min=Vector2.Min(min,local);max=Vector2.Max(max,local);}
            if(!Point(camera,plane,inverse,origin,view,new Vector2(0,-1),out var low)||!Point(camera,plane,inverse,origin,view,new Vector2(0,1),out var high))return false;
            bounds=Rect.MinMaxRect(min.x,min.y,max.x,max.y);float centre=(low.y+high.y)*.5f,half=Vector2.Distance(low,high)*.5f;
            vertical=new Vector2(centre-half,centre+half);return bounds.width>0&&bounds.height>0;
        }
        static bool Point(Camera camera,Transform plane,Matrix4x4 inverse,Vector3 origin,Matrix4x4 view,Vector2 ndc,out Vector2 local)
        {
            local=default;var p=inverse*new Vector4(ndc.x,ndc.y,0,1);if(Mathf.Abs(p.w)<.000001f)return false;
            var d=new Vector3(p.x,p.y,p.z)/p.w-origin;float denominator=Vector3.Dot(d,plane.forward);if(Mathf.Abs(denominator)<.000001f)return false;
            float t=Vector3.Dot(plane.position-origin,plane.forward)/denominator;if(t<=0||float.IsNaN(t)||float.IsInfinity(t))return false;
            var world=origin+d*t;float depth=-view.MultiplyPoint3x4(world).z;if(depth<camera.nearClipPlane||depth>camera.farClipPlane)return false;
            local=plane.InverseTransformPoint(world);return true;
        }
    }
}
