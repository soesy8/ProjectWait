using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Runtime-only geometry. No mirror crop, editor hierarchy, or projection overrides.
    public static class StereoUISafeArea
    {
        public const float VisualScale = .9f;
        public const float Margin = .06f;
        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public struct Frame
        {
            public Matrix4x4 leftView, rightView, leftProjection, rightProjection;
            public float near, far;

            public bool Project(Vector3 world, int eye, out Vector3 viewport)
            {
                var view = eye == 0 ? leftView : rightView;
                var projection = eye == 0 ? leftProjection : rightProjection;
                var point = view * new Vector4(world.x, world.y, world.z, 1);
                var clip = projection * point;
                viewport = default;
                float depth = -point.z;
                if (!Finite(depth) || depth < near || depth > far || !Finite(clip.w) || clip.w <= .00001f) return false;
                viewport = new Vector3(clip.x / clip.w * .5f + .5f, clip.y / clip.w * .5f + .5f, depth);
                return Finite(viewport.x) && Finite(viewport.y);
            }

            public bool Contains(Vector3 world, float margin = Margin, float lower = Margin)
            {
                for (int eye = 0; eye < 2; eye++)
                    if (!Project(world, eye, out var p) || p.x < margin || p.x > 1-margin || p.y < lower || p.y > 1-margin) return false;
                return true;
            }

            public bool ProjectQuad(Vector3 center, Quaternion rotation, Vector2 halfSize, out Rect bounds, float lower = Margin)
            {
                Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
                bool visible = true;
                for (int i = 0; i < 4; i++)
                {
                    var corner = center + rotation * new Vector3((i&1)==0 ? -halfSize.x : halfSize.x, (i&2)==0 ? -halfSize.y : halfSize.y, 0);
                    if (!Contains(corner, Margin, lower)) visible = false;
                    for (int eye = 0; eye < 2; eye++)
                    {
                        if (!Project(corner, eye, out var p)) { bounds = default; return false; }
                        min = Vector2.Min(min, p); max = Vector2.Max(max, p);
                    }
                }
                bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                return visible;
            }

            // Common is an inscribed rectangle for text; coverage is the union for black backgrounds.
            public bool Plane(Transform plane, out Rect common, out Rect coverage)
                => Plane(plane,out common,out coverage,out _,out _);

            public bool Plane(Transform plane,out Rect common,out Rect coverage,out Rect left,out Rect right)
            {
                common = coverage = default;
                left=right=default;
                if (!EyeRect(plane, 0, out left) || !EyeRect(plane, 1, out right)) return false;
                coverage = Rect.MinMaxRect(Mathf.Min(left.xMin,right.xMin), Mathf.Min(left.yMin,right.yMin),
                    Mathf.Max(left.xMax,right.xMax), Mathf.Max(left.yMax,right.yMax));
                var intersection = Rect.MinMaxRect(Mathf.Max(left.xMin,right.xMin), Mathf.Max(left.yMin,right.yMin),
                    Mathf.Min(left.xMax,right.xMax), Mathf.Min(left.yMax,right.yMax));
                if (intersection.width <= 0 || intersection.height <= 0) return false;
                var center = intersection.center;
                if (!Contains(plane.TransformPoint(center))) return false;
                // Eye frusta may be canted. Do not mistake their bounding box for a safe rectangle.
                float low = 0, high = 1;
                for (int iteration = 0; iteration < 18; iteration++)
                {
                    float scale = (low+high)*.5f;
                    bool fits = true;
                    for (int i = 0; i < 4; i++)
                    {
                        var point = center + Vector2.Scale(intersection.size * (.5f*scale), new Vector2((i&1)==0?-1:1,(i&2)==0?-1:1));
                        if (!Contains(plane.TransformPoint(point))) { fits = false; break; }
                    }
                    if (fits) low = scale; else high = scale;
                }
                common = new Rect(center-intersection.size*(low*.5f),intersection.size*low);
                return low > .01f;
            }

            public bool EyeRect(Transform plane, int eye, out Rect rect)
            {
                rect = default;
                var view = eye == 0 ? leftView : rightView;
                var inverse = ((eye == 0 ? leftProjection : rightProjection)*view).inverse;
                var origin = view.inverse.MultiplyPoint3x4(Vector3.zero);
                Vector2 min = Vector2.one*float.MaxValue, max = Vector2.one*float.MinValue;
                for (int i = 0; i < 4; i++)
                {
                    // z=0 avoids far-plane points at infinity in reversed/infinite projections.
                    var point = inverse*new Vector4((i&1)==0?-1:1,(i&2)==0?-1:1,0,1);
                    if (!Finite(point.w) || Mathf.Abs(point.w)<.000001f) return false;
                    var direction = new Vector3(point.x,point.y,point.z)/point.w-origin;
                    float denominator = Vector3.Dot(direction,plane.forward);
                    if (!Finite(denominator) || Mathf.Abs(denominator)<.000001f) return false;
                    float t = Vector3.Dot(plane.position-origin,plane.forward)/denominator;
                    if (!Finite(t) || t <= 0) return false;
                    var world = origin+direction*t;
                    if (!Project(world,eye,out _)) return false;
                    var local = plane.InverseTransformPoint(world);
                    if (!Finite(local.x) || !Finite(local.y)) return false;
                    min = Vector2.Min(min,local); max = Vector2.Max(max,local);
                }
                rect = Rect.MinMaxRect(min.x,min.y,max.x,max.y);
                return rect.width>0 && rect.height>0;
            }

            // Central native eye height, not the enlarged bounding box of a
            // canted frustum. Use this for a normalized letterbox-height fraction.
            public bool EyeVerticalRange(Transform plane,int eye,out Vector2 range)
            {
                range=default;
                var view=eye==0?leftView:rightView;
                var inverse=((eye==0?leftProjection:rightProjection)*view).inverse;
                var origin=view.inverse.MultiplyPoint3x4(Vector3.zero);
                for(int i=0;i<2;i++)
                {
                    var point=inverse*new Vector4(0,i==0?-1:1,0,1);
                    if(!Finite(point.w)||Mathf.Abs(point.w)<.000001f)return false;
                    var direction=new Vector3(point.x,point.y,point.z)/point.w-origin;
                    float denominator=Vector3.Dot(direction,plane.forward);
                    if(!Finite(denominator)||Mathf.Abs(denominator)<.000001f)return false;
                    float t=Vector3.Dot(plane.position-origin,plane.forward)/denominator;
                    if(!Finite(t)||t<=0)return false;
                    float y=plane.InverseTransformPoint(origin+direction*t).y;
                    if(!Finite(y))return false;
                    range[i]=y;
                }
                return range.y>range.x;
            }
        }

        // Cache local geometry, never world projections or gaze visibility. A new
        // before-render pose, projection, plane scale or IPD invalidates this cache.
        public struct PlaneCache
        {
            Matrix4x4 leftView,rightView,leftProjection,rightProjection;
            Rect common,coverage,left,right;
            float near,far;
            bool ready;
            public Vector2 LeftVertical {get;private set;}
            public Vector2 RightVertical {get;private set;}
            public bool Get(Frame frame,Transform plane,out Rect safe,out Rect union,out Rect leftEye,out Rect rightEye)
            {
                var l=frame.leftView*plane.localToWorldMatrix;var r=frame.rightView*plane.localToWorldMatrix;
                if(!ready||Different(l,leftView)||Different(r,rightView)||Different(frame.leftProjection,leftProjection)||
                    Different(frame.rightProjection,rightProjection)||near!=frame.near||far!=frame.far)
                {
                    ready=frame.Plane(plane,out common,out coverage,out left,out right);
                    if(ready)
                    {
                        bool validLeft=frame.EyeVerticalRange(plane,0,out var lv),validRight=frame.EyeVerticalRange(plane,1,out var rv);
                        ready=validLeft&&validRight;
                        if(ready){LeftVertical=lv;RightVertical=rv;}
                    }
                    leftView=l;rightView=r;leftProjection=frame.leftProjection;rightProjection=frame.rightProjection;
                    near=frame.near;far=frame.far;
                }
                safe=common;union=coverage;leftEye=left;rightEye=right;return ready;
            }
            static bool Different(Matrix4x4 a,Matrix4x4 b)
            {
                for(int i=0;i<16;i++)if(!Finite(a[i])||!Finite(b[i])||Mathf.Abs(a[i]-b[i])>.00001f)return true;
                return false;
            }
        }

        public static bool TryCapture(Camera camera, out Frame frame)
        {
            frame = default;
            if (!camera || !camera.stereoEnabled) return false; // Wait for real XR projection, never use monitor aspect as a fallback.
            frame.leftView = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            frame.rightView = camera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
            frame.leftProjection = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            frame.rightProjection = camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            frame.near = camera.nearClipPlane; frame.far = camera.farClipPlane;
            return Finite(frame.near) && Finite(frame.far) && frame.near>0 && frame.far>frame.near &&
                Valid(frame.leftView) && Valid(frame.rightView) && Valid(frame.leftProjection) && Valid(frame.rightProjection) &&
                Mathf.Abs(frame.leftProjection.m32)>.5f && Mathf.Abs(frame.rightProjection.m32)>.5f &&
                Mathf.Abs(frame.leftProjection.m33)<.001f && Mathf.Abs(frame.rightProjection.m33)<.001f;
        }

        static bool Valid(Matrix4x4 matrix)
        {
            for (int i = 0; i < 16; i++) if (!Finite(matrix[i])) return false;
            return Mathf.Abs(matrix.determinant) > .0000001f;
        }

        // Projection/IPD changes invalidate layout; head motion itself must not reanchor world cards.
        public struct LayoutStamp
        {
            Matrix4x4 left, right, leftEye, rightEye;
            bool ready;
            public bool Update(Camera camera, Frame frame)
            {
                var l = frame.leftView*camera.transform.localToWorldMatrix;
                var r = frame.rightView*camera.transform.localToWorldMatrix;
                bool changed = !ready || Different(left,frame.leftProjection) || Different(right,frame.rightProjection) || Different(leftEye,l) || Different(rightEye,r);
                if(changed){left = frame.leftProjection; right = frame.rightProjection; leftEye = l; rightEye = r; ready = true;}
                return changed;
            }
            public void Reset() { ready = false; }
            static bool Different(Matrix4x4 a, Matrix4x4 b)
            {
                for (int i=0;i<16;i++) if (Mathf.Abs(a[i]-b[i])>.001f) return true;
                return false;
            }
        }

        public static void Place(RectTransform panel, Rect area, Vector2 alignment)
        {
            if (!panel || !(panel.parent is RectTransform parent)) return;
            var size = Vector2.Scale(panel.rect.size,new Vector2(panel.localScale.x,panel.localScale.y));
            var center = new Vector2(Mathf.Lerp(area.xMin+size.x*.5f,area.xMax-size.x*.5f,alignment.x),
                Mathf.Lerp(area.yMin+size.y*.5f,area.yMax-size.y*.5f,alignment.y));
            var pivot = center+Vector2.Scale(panel.pivot-Vector2.one*.5f,size);
            var anchor = parent.rect.min+Vector2.Scale(parent.rect.size,(panel.anchorMin+panel.anchorMax)*.5f);
            panel.anchoredPosition = pivot-anchor;
        }
    }
}
