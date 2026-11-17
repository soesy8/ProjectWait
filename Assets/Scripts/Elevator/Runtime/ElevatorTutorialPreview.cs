using System.Collections.Generic;
using DwtD.TestScene;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    /// <summary>Fixed screen-space VR instructions and small, non-interactive object markers.</summary>
    [DefaultExecutionOrder(180)]
    [DisallowMultipleComponent]
    public sealed class ElevatorTutorialPreview : MonoBehaviour
    {
        public enum GuideStep { None, CallLift, ChooseFloor, ReceiveBall, BoardingActions }

        public ElevatorLesson lesson;
        public TMP_FontAsset font;
        public Material worldMaterial;
        public ElevatorObservation callButton, ball;
        public ElevatorObservation[] floorButtons;
        public string selectButtonName = "검지 트리거";
        [Min(1)] public float reminderDelay = 8;
        [Min(1)] public float repeatControlDelay = 16;
        [Min(.01f)] public float fadeDuration = 1.5f;
        [Range(.4f, 1)] public float visualScale = .70f;
        [Range(.15f, .8f)] public float markerMaximumWorldWidth = .5f;

        public GuideStep Step { get; private set; }
        public GuideStep DisplayedStep { get; private set; }
        public ElevatorObservation Target { get; private set; }
        public bool IsVisible => instructionCanvas && instructionCanvas.enabled && InstructionAlpha > .001f;
        public bool MarkerVisible => markerCanvas && markerCanvas.enabled;
        public float InstructionAlpha => instructionGroup ? instructionGroup.alpha : 0;
        public bool Revealed { get; private set; }
        public bool Expanded { get; private set; }
        public float StepElapsed { get; private set; }
        public RectTransform Tip => marker;
        public RectTransform InstructionPanel => instructionPanel;
        public Canvas InstructionCanvas => instructionCanvas;
        public string Instruction => instruction ? instruction.text : "";
        public string MarkerText => markerLabel ? markerLabel.text : "";

        static readonly Color Ink = new Color(.28f, .19f, .14f);
        static readonly Color Paper = new Color(1, .965f, .86f);
        static readonly Color Teal = new Color(.16f, .48f, .44f);
        static readonly Color Mint = new Color(.79f, .89f, .76f);
        const float MarkerWidth = 92, MarkerHeight = 44;
        Canvas instructionCanvas, markerCanvas;
        CanvasGroup instructionGroup;
        RectTransform instructionPanel, marker;
        TMP_Text title, instruction, markerLabel;
        ElevatorVRSelectGraphic inputIcon;
        bool contentXR;
        bool stereoReady;
        string vrTitle,vrInstruction;
        public bool InstructionFits { get; private set; }
        Material textMaterial;
        int run = -1;
        bool placed;
        float fadeGoal, fadeStart, fadeElapsed;
        Vector3 anchorOffset;
        readonly Vector3[] corners = new Vector3[4];
        readonly List<Rect> obstacles = new List<Rect>();
        readonly List<ElevatorObservation> candidates = new List<ElevatorObservation>();

        void Start() { BuildViews(); }

        void OnEnable()
        {
            Application.onBeforeRender += PositionInstruction;
            Step = DisplayedStep = GuideStep.None;
            Target = null;
            Revealed = placed = false;
            HideImmediately();
        }

        GuideStep RequiredStep()
        {
            var f = lesson.Flow;
            if (f.Step == ElevatorStep.Lobby) return GuideStep.CallLift;
            if (f.Step == ElevatorStep.Boarding) return !f.DestinationSelected && !f.BallReceived ? GuideStep.BoardingActions :
                f.DestinationSelected ? GuideStep.ReceiveBall : GuideStep.ChooseFloor;
            return GuideStep.None;
        }

        void LateUpdate() { Tick(Time.unscaledDeltaTime); }

        public void Tick(float seconds)
        {
            if (!Application.isPlaying || !isActiveAndEnabled || !lesson || lesson.Flow == null) { HideImmediately(); return; }
            if (!instructionCanvas) BuildViews();
            RefreshScreenMode();
            seconds = Mathf.Max(0, seconds);
            if (run != lesson.RunVersion)
            {
                run = lesson.RunVersion;
                Step = DisplayedStep = GuideStep.None;
                Target = null;
                HideImmediately();
            }
            var required = RequiredStep();
            if (required != Step)
            {
                Step = required;
                Target = null;
                StepElapsed = 0;
                Revealed = placed = false;
                markerCanvas.enabled = false;
                // Keep the previous instruction text intact while it fades out.
            }
            if (lesson.IsPaused) { HideImmediately(); return; }
            bool relevant = Step != GuideStep.None && !lesson.CinematicBlocking;
            if (relevant)
            {
                StepElapsed += seconds;
                var next = ChooseTarget();
                if (next != Target) { Target = next; placed = false; }
                relevant = Target;
            }
            if (relevant)
            {
                Revealed |= Step == GuideStep.CallLift || lesson.Flow.Step == ElevatorStep.Boarding || Target.IsHovered || StepElapsed >= reminderDelay;
                UpdateMarker();
            }
            else markerCanvas.enabled = false;

            // Instructions are screen-fixed, so looking away hides only the object marker.
            UpdateInstruction(relevant && Revealed, Step == GuideStep.CallLift || Step == GuideStep.BoardingActions || StepElapsed >= repeatControlDelay, seconds);
        }

        void UpdateInstruction(bool wanted, bool expanded, float seconds)
        {
            bool matching = DisplayedStep == Step && Expanded == expanded && contentXR == XRSettings.isDeviceActive;
            if (wanted && !matching && InstructionAlpha <= .00001f)
            {
                SetContent(expanded);
                matching = true;
            }
            FadeTo(wanted && matching ? 1 : 0);
            fadeElapsed = Mathf.Min(fadeDuration, fadeElapsed + seconds);
            float t = Mathf.SmoothStep(0, 1, fadeElapsed / Mathf.Max(.01f, fadeDuration));
            instructionGroup.alpha = Mathf.Lerp(fadeStart, fadeGoal, t);
            if (fadeElapsed >= fadeDuration) instructionGroup.alpha = fadeGoal;
            instructionCanvas.enabled = (!XRSettings.isDeviceActive || stereoReady) && (InstructionAlpha > 0 || fadeGoal > 0);
        }

        void FadeTo(float goal)
        {
            if (fadeGoal == goal) return;
            fadeStart = InstructionAlpha;
            fadeGoal = goal;
            fadeElapsed = 0;
        }

        ElevatorObservation ChooseTarget()
        {
            if (Step == GuideStep.CallLift) return Usable(callButton) ? callButton : null;
            if (Step == GuideStep.ReceiveBall) return Usable(ball) ? ball : null;
            candidates.Clear();
            if (Step == GuideStep.BoardingActions && Usable(ball)) candidates.Add(ball);
            if (floorButtons != null) foreach (var candidate in floorButtons) if (Usable(candidate)) candidates.Add(candidate);
            foreach (var candidate in candidates) if (candidate.IsHovered) return candidate;
            if (candidates.Contains(Target)) return Target;
            float best = float.PositiveInfinity;
            ElevatorObservation selected = null;
            foreach (var candidate in candidates)
            {
                var p = lesson.playerCamera.WorldToViewportPoint(AnchorPosition(candidate));
                float score = Vector3.Distance(lesson.playerCamera.transform.position, AnchorPosition(candidate));
                if (p.z <= 0 || p.x < 0 || p.x > 1 || p.y < 0 || p.y > 1) score += 100;
                if (score < best) { best = score; selected = candidate; }
            }
            return selected;
        }

        static bool Usable(ElevatorObservation observation) => observation && observation.CanActivate;

        public static Vector3 AnchorPosition(ElevatorObservation observation)
        {
            var collider = observation.GetComponent<Collider>();
            return collider ? collider.bounds.center : observation.transform.position;
        }

        void SetContent(bool expanded) => SetContent(expanded,XRSettings.isDeviceActive);

        public void SetContent(bool expanded,bool vr)
        {
            DisplayedStep = Step;
            Expanded = expanded;
            contentXR = vr;
            bool combined = Step == GuideStep.BoardingActions;
            title.text = combined ? "1층 선택 · 공 받기" : Step == GuideStep.CallLift ? "내려가는 버튼" : Step == GuideStep.ChooseFloor ? "1층 버튼" : "공 받기";
            string controls = contentXR ? "가리킨 뒤\n" + selectButtonName + "를\n눌러 주세요" : "화면 가운데에\n맞추고 클릭하세요";
            instruction.text = combined ? "순서는 자유예요\n" + (contentXR ? "레이로 가리키고\n" + selectButtonName + "를 눌러요" : controls) : expanded ? controls :
                Step == GuideStep.ChooseFloor ? "1층을 선택하세요" : "공을 선택해 받으세요";
            instructionPanel.sizeDelta = new Vector2(combined ? 400 : 320, expanded ? 158 : 98);
            ((RectTransform)inputIcon.transform.parent).anchoredPosition = new Vector2(combined ? -156 : -116, 0);
            title.rectTransform.sizeDelta = new Vector2(combined ? 296 : 216, 42);
            title.rectTransform.anchoredPosition = new Vector2(34, expanded ? 54 : 23);
            instruction.rectTransform.anchoredPosition = new Vector2(34, expanded ? -18 : -20);
            instruction.rectTransform.sizeDelta = new Vector2(combined ? 296 : 216, expanded ? 100 : 38);
            if(contentXR)LayoutVRContent();
        }

        void UpdateMarker()
        {
            markerLabel.text = Step == GuideStep.CallLift ? "선택" : Target == ball ? "받기" : "1층";
            if (!placed) PlaceBesideTarget();
            marker.position = AnchorPosition(Target) + anchorOffset;
            FaceEyes();
            markerCanvas.enabled = (!XRSettings.isDeviceActive || stereoReady) && TargetInView() && TargetUnobstructed();
        }

        void PlaceBesideTarget()
        {
            var camera = lesson.playerCamera;
            var anchor = AnchorPosition(Target);
            float distance = Vector3.Distance(anchor, camera.transform.position);
            float unit = 2 * distance * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) / 720 * visualScale;
            unit = Mathf.Min(unit, markerMaximumWorldWidth / MarkerWidth);
            if (XRSettings.isDeviceActive) unit *= StereoUISafeArea.VisualScale;
            marker.localScale = Vector3.one * unit;
            var right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
            var towardEyes = (camera.transform.position - anchor).normalized * .07f;
            float halfWidth = MarkerWidth * .5f * unit, halfHeight = MarkerHeight * .5f * unit;
            var offsets = new[] {
                right * (halfWidth + .08f) + Vector3.up * .03f,
                -right * (halfWidth + .08f) + Vector3.up * .03f,
                Vector3.up * (halfHeight + .12f),
                Vector3.down * (halfHeight + .12f)
            };
            obstacles.Clear();
            var spatial = lesson.view.spatial;
            if (spatial && lesson.view.dialoguePanel.gameObject.activeInHierarchy && lesson.view.GetComponent<CanvasGroup>().alpha > .1f)
                obstacles.Add(Project(lesson.view.dialoguePanel));
            if (spatial && spatial.mission.gameObject.activeInHierarchy) obstacles.Add(XRSettings.isDeviceActive?Project(spatial.mission):spatial.HudViewportRect);
            float best = float.PositiveInfinity;
            Vector3 bestOffset = offsets[0] + towardEyes;
            foreach (var offset in offsets)
            {
                marker.position = anchor + offset + towardEyes;
                FaceEyes();
                var rect = Project(marker);
                float score = Mathf.Max(0, .025f - rect.xMin) + Mathf.Max(0, rect.xMax - .975f) +
                    Mathf.Max(0, .025f - rect.yMin) + Mathf.Max(0, rect.yMax - .975f);
                score *= 100;
                foreach (var obstacle in obstacles) score += Overlap(rect, obstacle) * 50;
                if (score < best) { best = score; bestOffset = offset + towardEyes; }
            }
            anchorOffset = bestOffset;
            placed = true;
        }

        Rect Project(RectTransform rect)
        {
            rect.GetWorldCorners(corners);
            Vector2 min = Vector2.one * float.MaxValue, max = Vector2.one * float.MinValue;
            if(XRSettings.isDeviceActive)
            {
                if(!StereoUISafeArea.TryCapture(lesson.playerCamera,out var frame))return new Rect(-10,-10,0,0);
                foreach(var corner in corners)for(int eye=0;eye<2;eye++)
                {
                    if(!frame.Project(corner,eye,out var p))return new Rect(-10,-10,0,0);
                    min=Vector2.Min(min,p);max=Vector2.Max(max,p);
                }
                return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
            }
            foreach (var corner in corners)
            {
                var p = lesson.playerCamera.WorldToViewportPoint(corner);
                min = Vector2.Min(min, p); max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        static float Overlap(Rect a, Rect b) => Mathf.Max(0, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin)) *
            Mathf.Max(0, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));

        void FaceEyes()
        {
            var facing = marker.position - lesson.playerCamera.transform.position;
            if (facing.sqrMagnitude > .001f) marker.rotation = Quaternion.LookRotation(facing, Vector3.up);
        }

        bool TargetInView()
        {
            if(XRSettings.isDeviceActive)
            {
                if(!StereoUISafeArea.TryCapture(lesson.playerCamera,out var frame))return false;
                marker.GetWorldCorners(corners);
                foreach(var corner in corners)if(!frame.Contains(corner,.02f,.02f))return false;
                return true;
            }
            var p = lesson.playerCamera.WorldToViewportPoint(AnchorPosition(Target));
            return p.z > 0 && p.x > -.02f && p.x < 1.02f && p.y > -.02f && p.y < 1.02f;
        }

        bool TargetUnobstructed()
        {
            var origin = lesson.playerCamera.transform.position;
            var direction = AnchorPosition(Target) - origin;
            if (!Physics.Raycast(origin, direction.normalized, out var hit, direction.magnitude, ~0, QueryTriggerInteraction.Ignore)) return true;
            return hit.collider.GetComponentInParent<ElevatorObservation>() == Target;
        }

        void HideImmediately()
        {
            fadeGoal = fadeStart = fadeElapsed = 0;
            if (instructionGroup) instructionGroup.alpha = 0;
            if (instructionCanvas) instructionCanvas.enabled = false;
            if (markerCanvas) markerCanvas.enabled = false;
        }

        void OnDisable() { Application.onBeforeRender -= PositionInstruction; HideImmediately(); }

        void RefreshScreenMode()
        {
            var mode = XRSettings.isDeviceActive ? RenderMode.WorldSpace : RenderMode.ScreenSpaceOverlay;
            if (instructionCanvas.renderMode != mode) instructionCanvas.renderMode = mode;
            instructionCanvas.worldCamera = lesson.playerCamera;
            if (inputIcon) inputIcon.SetInputMode(XRSettings.isDeviceActive);
            PositionInstruction();
        }

        void PositionInstruction()
        {
            if (!instructionCanvas || !instructionPanel || !lesson || !lesson.playerCamera) return;
            bool xr = XRSettings.isDeviceActive;
            instructionPanel.localScale = Vector3.one * (xr ? StereoUISafeArea.VisualScale : 1);
            if (!xr) { instructionPanel.anchoredPosition = new Vector2(-24,-24); return; }
            var camera = lesson.playerCamera;
            var root = (RectTransform)instructionCanvas.transform;
            VRUIReadingLayout.SetPlane(root,camera.transform);
            if(!StereoUISafeArea.TryCapture(camera,out var frame)){stereoReady=false;instructionCanvas.enabled=false;return;}
            LayoutVR(frame);
        }

        // Data-driven checks call this with the captured native eye matrices, without a live HMD.
        public bool LayoutVR(StereoUISafeArea.Frame frame)
        {
            if(!instructionCanvas||!instructionPanel||!lesson||!lesson.playerCamera)return false;
            var root=(RectTransform)instructionCanvas.transform;
            VRUIReadingLayout.SetPlane(root,lesson.playerCamera.transform);
            instructionPanel.localScale=Vector3.one*StereoUISafeArea.VisualScale;
            if(vrTitle!=title.text||vrInstruction!=instruction.text)LayoutVRContent();
            stereoReady=VRUIReadingLayout.Area(frame,root,out var area,out _);
            InstructionFits=stereoReady&&VRUIReadingLayout.Place(instructionPanel,root,area,VRUIReadingLayout.GuideYaw,VRUIReadingLayout.GuidePitch,out _)&&
                VRUIReadingLayout.Project(frame,instructionPanel,out _,true);
            if(!InstructionFits){instructionCanvas.enabled=false;return false;}
            instructionCanvas.enabled = InstructionAlpha > 0 || fadeGoal > 0;
            return true;
        }

        void LayoutVRContent()
        {
            if(!instructionPanel||!title||!instruction||!inputIcon)return;
            vrTitle=title.text;vrInstruction=instruction.text;
            const float width=VRUIReadingLayout.GuideWidth;
            const float textWidth=width-104;
            // Reflow the title and instructions independently; preserve type size instead of fitting by shrink.
            float titleHeight=Mathf.Max(34,title.GetPreferredValues(title.text,textWidth,0).y+4);
            float bodyHeight=Mathf.Max(34,instruction.GetPreferredValues(instruction.text,textWidth,0).y+6);
            float height=titleHeight+bodyHeight+36;
            instructionPanel.sizeDelta=new Vector2(width,height);
            ((RectTransform)inputIcon.transform.parent).anchoredPosition=new Vector2(-width*.5f+38,0);
            title.rectTransform.sizeDelta=new Vector2(textWidth,titleHeight);
            title.rectTransform.anchoredPosition=new Vector2(38,height*.5f-14-titleHeight*.5f);
            instruction.rectTransform.sizeDelta=new Vector2(textWidth,bodyHeight);
            instruction.rectTransform.anchoredPosition=new Vector2(38,-height*.5f+14+bodyHeight*.5f);
        }

        void BuildViews()
        {
            if (instructionCanvas || !lesson) return;
            var screen = MakeRect("VR_Instruction_ScreenSpace", transform, new Vector2(1280, 720), Vector2.zero);
            instructionCanvas = screen.gameObject.AddComponent<Canvas>();
            RefreshScreenMode();
            instructionCanvas.planeDistance = Mathf.Max(.75f, lesson.playerCamera.nearClipPlane + .05f);
            instructionCanvas.sortingOrder = 220;
            var scaler = screen.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1;
            instructionPanel = MakeRect("TopRight_Instruction", screen, new Vector2(320, 134), new Vector2(-24, -24));
            instructionPanel.anchorMin = instructionPanel.anchorMax = instructionPanel.pivot = Vector2.one;
            instructionPanel.anchoredPosition = new Vector2(-24, -24);
            instructionGroup = instructionPanel.gameObject.AddComponent<CanvasGroup>();
            instructionGroup.blocksRaycasts = instructionGroup.interactable = false;
            Surface(instructionPanel, Paper, 18, 2.5f);
            var badge = MakeRect("ControllerBadge", instructionPanel, new Vector2(56, 70), new Vector2(-116, 0));
            Surface(badge, Mint, 13, 1);
            var iconRect = MakeRect("VR_Select_Icon", badge, new Vector2(48, 62), Vector2.zero);
            inputIcon = iconRect.gameObject.AddComponent<ElevatorVRSelectGraphic>();
            inputIcon.raycastTarget = false; inputIcon.material = worldMaterial;
            inputIcon.SetInputMode(XRSettings.isDeviceActive);
            title = MakeLabel("ObjectName", instructionPanel, new Vector2(216, 42), 26, Teal);
            instruction = MakeLabel("VRInstruction", instructionPanel, new Vector2(216, 76), 22, Ink);

            marker = MakeRect("VR_Object_Marker", transform, new Vector2(MarkerWidth, MarkerHeight), Vector2.zero);
            markerCanvas = marker.gameObject.AddComponent<Canvas>();
            markerCanvas.renderMode = RenderMode.WorldSpace;
            markerCanvas.worldCamera = lesson.playerCamera;
            markerCanvas.sortingOrder = 140;
            var markerGroup = marker.gameObject.AddComponent<CanvasGroup>();
            markerGroup.blocksRaycasts = markerGroup.interactable = false;
            Surface(marker, Mint, 13, 2);
            markerLabel = MakeLabel("ActionBadge", marker, new Vector2(MarkerWidth - 16, 38), 22, Ink);
            markerLabel.alignment = TextAlignmentOptions.Center;
            var shader = Shader.Find("TextMeshPro/Distance Field Overlay");
            if (shader && font)
            {
                textMaterial = new Material(font.material) { name = "Screen tutorial text (runtime)", shader = shader, renderQueue = 4201 };
                title.fontSharedMaterial = instruction.fontSharedMaterial = markerLabel.fontSharedMaterial = textMaterial;
            }
            HideImmediately();
        }

        void Surface(RectTransform rect, Color color, float radius, float border)
        {
            var surface = rect.gameObject.AddComponent<CorridorBubbleGraphic>();
            surface.color = color; surface.borderColor = Ink; surface.borderWidth = border;
            surface.cornerRadius = radius; surface.extrusion = border > 1 ? 3 : 1; surface.bevelWidth = 2;
            surface.shadowOffset = new Vector2(2, -4); surface.shadowOpacity = .12f;
            surface.raycastTarget = false; surface.material = worldMaterial;
        }

        TMP_Text MakeLabel(string name, Transform parent, Vector2 size, float points, Color color)
        {
            var rect = MakeRect(name, parent, size, Vector2.zero);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.color = color;
            ElevatorTypography.Apply(label, points);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.lineSpacing = 2;
            return label;
        }

        static RectTransform MakeRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }

        void OnDestroy() { if (textMaterial) Destroy(textMaterial); }
    }
}
