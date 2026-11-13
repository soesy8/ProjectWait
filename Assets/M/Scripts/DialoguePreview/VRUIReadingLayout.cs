using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // A reading layout, not a replacement headset FOV. All areas are clipped to both native eyes.
    public static class VRUIReadingLayout
    {
        public const float Distance = 1.4f;
        // At 1.4m one point is .002m, matching the scene's existing dialogue typography.
        public const float PointsPerRadian = 700;
        public const float HorizontalLimit = 38, VerticalLimit = 26;
        public const float GuideWidth = 340, StatusWidth = 340, CaptionWidth = 1000;
        public const float GuideYaw = 21, GuidePitch = 14, StatusYaw = 20, StatusPitch = -10, CaptionPitch = -28;
        public const float LetterboxHeight = 123;
        public const float Separation = 16;

        public static Vector2 AnglePoint(float yaw, float pitch) =>
            new Vector2(Mathf.Tan(yaw*Mathf.Deg2Rad),Mathf.Tan(pitch*Mathf.Deg2Rad))*PointsPerRadian;

        public static Rect Intersect(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin,b.xMin),Mathf.Max(a.yMin,b.yMin),
            Mathf.Min(a.xMax,b.xMax),Mathf.Min(a.yMax,b.yMax));

        public static void SetPlane(Transform plane, Transform eyes)
        {
            plane.SetPositionAndRotation(eyes.position+eyes.forward*Distance,eyes.rotation);
            float unit=Distance/PointsPerRadian;
            var parent=plane.parent?plane.parent.lossyScale:Vector3.one;
            plane.localScale=new Vector3(unit/parent.x,unit/parent.y,unit/parent.z);
        }

        public static bool Area(StereoUISafeArea.Frame frame, RectTransform plane, out Rect area, out Rect coverage)
        {
            area=coverage=default;
            if(!frame.Plane(plane,out var binocular,out coverage))return false;
            var edge=AnglePoint(HorizontalLimit,VerticalLimit);
            area=Intersect(binocular,new Rect(-edge,edge*2));
            // Keep the origin at head-forward; do not shift the angular reference to a frustum's optical centre.
            plane.sizeDelta=coverage.size;
            return area.width>0&&area.height>0;
        }

        public static bool Place(RectTransform panel, Transform plane, Rect area, float yaw, float pitch, out Rect occupied)
        {
            occupied=default;
            var scale=new Vector2(panel.lossyScale.x/plane.lossyScale.x,panel.lossyScale.y/plane.lossyScale.y);
            var size=Vector2.Scale(panel.rect.size,scale);
            if(area.width<size.x||area.height<size.y)return false; // Never silently shrink a font/card to fit.
            var preferred=AnglePoint(yaw,pitch);
            var center=new Vector2(Mathf.Clamp(preferred.x,area.xMin+size.x*.5f,area.xMax-size.x*.5f),
                Mathf.Clamp(preferred.y,area.yMin+size.y*.5f,area.yMax-size.y*.5f));
            var pivot=center+Vector2.Scale(panel.pivot-Vector2.one*.5f,size);
            panel.position=plane.TransformPoint(new Vector3(pivot.x,pivot.y,0));
            occupied=new Rect(center-size*.5f,size);
            return true;
        }

        public static bool Project(StereoUISafeArea.Frame frame, RectTransform panel, out Rect viewport, bool requireSafe=false)
        {
            Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            for(int i=0;i<4;i++)
            {
                var corner=panel.TransformPoint(new Vector3((i&1)==0?panel.rect.xMin:panel.rect.xMax,(i&2)==0?panel.rect.yMin:panel.rect.yMax,0));
                if(requireSafe&&!frame.Contains(corner)){viewport=default;return false;}
                for(int eye=0;eye<2;eye++)
                {
                    if(!frame.Project(corner,eye,out var p)){viewport=default;return false;}
                    min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                }
            }
            viewport=Rect.MinMaxRect(min.x,min.y,max.x,max.y);return true;
        }

        public static Rect LocalRect(RectTransform panel, Transform plane)
        {
            Vector2 min=Vector2.one*float.MaxValue,max=Vector2.one*float.MinValue;
            for(int i=0;i<4;i++)
            {
                var world=panel.TransformPoint(new Vector3((i&1)==0?panel.rect.xMin:panel.rect.xMax,(i&2)==0?panel.rect.yMin:panel.rect.yMax,0));
                var p=plane.InverseTransformPoint(world);min=Vector2.Min(min,p);max=Vector2.Max(max,p);
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        public static Rect Above(Rect area, Rect occupied)
        {
            area.yMin=Mathf.Max(area.yMin,occupied.yMax+Separation);
            return area;
        }

        public static bool Comfortable(Transform eyes, Vector3 world)
        {
            var point=eyes.InverseTransformPoint(world);
            return point.z>0&&Mathf.Abs(point.x/point.z)<=Mathf.Tan(HorizontalLimit*Mathf.Deg2Rad)&&
                Mathf.Abs(point.y/point.z)<=Mathf.Tan(VerticalLimit*Mathf.Deg2Rad);
        }
    }
}
