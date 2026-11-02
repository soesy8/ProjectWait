using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.UI;
using DwtD.TestScene;
using ProjectWait.M.Elevator;
using Object = UnityEngine.Object;

// Editor authoring recipe only. The delivered prefabs use existing project components.
public static class BuildStorybookUI
{
    const string Root = "Assets/M/UI/Storybook/";
    const string Prefabs = Root + "Prefabs/";
    static TMP_FontAsset font;
    static Color Ink = Hex("403C32"), Muted = Hex("646D60"), Paper = Hex("FFF9E8"),
        Teal = Hex("287B70"), Mint = Hex("DDEAD6"), Gold = Hex("F6C66E"),
        LineColor = Hex("B6C8AF"), White = Hex("FFFFF8"), Bg = Hex("EDF0E3");
    static List<string> created = new List<string>();
    static Scene scene;

    static Color Hex(string s) { ColorUtility.TryParseHtmlString("#" + s, out var c); return c; }

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit mode.");
        var original = SceneManager.GetActiveScene();
        if (original.isDirty) throw new Exception("Current scene has unsaved edits.");
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/M/Arts/Fonts/ElevatorReadable.asset");
        if (!font) throw new Exception("Project Korean font is missing.");
        Directory.CreateDirectory(Prefabs); Directory.CreateDirectory(Root + "Scenes");
        AssetDatabase.Refresh();
        scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            Save("DialogueCard", Dialogue(false));
            Save("IntercomDialogueCard", Dialogue(true));
            Save("ThoughtCloud", Thought());
            Save("ObservationCard", Observation());
            Save("ChoiceCard", Choices());
            Save("MissionCard", Mission());
            Save("CompletionCard", Completion());
            Save("PausePanel", Pause());
            Save("NextButton", Next());
            BuildBoard();
            var cam = new GameObject("PreviewCamera", typeof(Camera)).GetComponent<Camera>();
            cam.tag = "MainCamera"; cam.transform.position = new Vector3(0, 0, -10);
            cam.orthographic = true; cam.orthographicSize = 6; cam.backgroundColor = Bg;
            cam.clearFlags = CameraClearFlags.SolidColor;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            EditorSceneManager.SaveScene(scene, Root + "Scenes/StorybookUI_Showcase.unity");
            AssetDatabase.SaveAssets();
            return new { prefabs = created.ToArray(), scene = scene.path, originalScene = original.path, runtimeScriptsAdded = 0 };
        }
        finally
        {
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    static RectTransform Rect(string name, Transform parent, float w, float h, float x = 0, float y = 0)
    {
        var r = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        if (parent) r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = Vector2.one * .5f;
        r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(x, y); return r;
    }

    static void Stretch(RectTransform r, float left, float top, float right, float bottom)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
    }

    static CorridorBubbleGraphic Surface(RectTransform r, Color color, float radius = 22, float depth = 5, float border = 2)
    {
        var g = r.gameObject.AddComponent<CorridorBubbleGraphic>();
        g.color = color; g.borderColor = Ink; g.borderWidth = border;
        g.cornerRadius = radius; g.extrusion = depth; g.bevelWidth = 2;
        g.shadeStrength = .10f; g.shadowOffset = new Vector2(2, -7);
        g.shadowOpacity = .08f; g.raycastTarget = false; return g;
    }

    static RectTransform Card(string name, Transform p, float w, float h, float x, float y, Color c, float radius = 22)
    { var r = Rect(name, p, w, h, x, y); Surface(r, c, radius); return r; }

    static TMP_Text Text(string name, Transform p, string value, float size, Color color, float w, float h, float x, float y, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
    {
        var r = Rect(name, p, w, h, x, y);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>(); t.font = font;
        t.text = value; t.fontSize = size; t.color = color; t.raycastTarget = false;
        t.alignment = align; t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
        t.lineSpacing = 8; t.extraPadding = true; return t;
    }

    static void Fill(string name, Transform p, float w, float h, float x, float y, Color c)
    { var i = Rect(name, p, w, h, x, y).gameObject.AddComponent<UnityEngine.UI.Image>(); i.color = c; i.raycastTarget = false; }

    static RectTransform Pill(string name, Transform p, string label, float w, float h, float x, float y, Color color, Color text, float point = 20)
    {
        var r = Card(name, p, w, h, x, y, color, h * .5f);
        var g = r.GetComponent<CorridorBubbleGraphic>(); g.extrusion = 2; g.borderWidth = 1.6f; g.shadowOpacity = 0;
        float padding = w < 64 ? 4 : 12;
        var t = Text("Label", r, label, point, text, w - padding * 2, h - 6, 0, 0, true, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, padding, 3, padding, 3); return r;
    }

    static void Symbol(string name, Transform p, CorridorSymbolGraphic.Symbol type, float size, float x, float y, Color color)
    {
        var g = Rect(name, p, size, size, x, y).gameObject.AddComponent<CorridorSymbolGraphic>();
        g.symbol = type; g.color = color; g.raycastTarget = false;
    }

    static void Memory(string name, Transform p, ElevatorMemoryGraphic.Illustration type, float size, float x, float y)
    {
        var g = Rect(name, p, size, size, x, y).gameObject.AddComponent<ElevatorMemoryGraphic>();
        g.illustration = type; g.raycastTarget = false;
    }

    static UnityEngine.UI.Button Button(RectTransform r)
    {
        var g = r.GetComponent<CorridorBubbleGraphic>(); g.raycastTarget = true;
        var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = g;
        var colors = b.colors; colors.normalColor = Color.white; colors.highlightedColor = Hex("E4F4DD");
        colors.selectedColor = Hex("E4F4DD"); colors.pressedColor = Hex("B5C7B7"); colors.disabledColor = Hex("A5ADA3");
        colors.fadeDuration = .1f; b.colors = colors;
        var nav = b.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.Automatic; b.navigation = nav;
        return b;
    }

    static RectTransform Dialogue(bool intercom)
    {
        var r = Rect(intercom ? "IntercomDialogueCard" : "DialogueCard", null, 760, 260);
        var panel = Card("SpeechSurface", r, 760, 226, 0, 0, Paper, 26);
        Stretch(panel, 0, 12, 0, 22);
        panel.GetComponent<CorridorBubbleGraphic>().speechTail = true;
        var tab = Pill("SpeakerTab", panel, intercom ? "관리실" : "친구", 136, 48, -266, 106, intercom ? Gold : Teal, intercom ? Ink : White, 23);
        tab.anchorMin = tab.anchorMax = new Vector2(0, 1); tab.pivot = new Vector2(0, .5f); tab.anchoredPosition = new Vector2(24, -4);
        Pill("Channel", panel, intercom ? "비상 통화 중" : "함께 나누는 이야기", intercom ? 148 : 202, 32, 220, 75, intercom ? Gold : Mint, intercom ? Ink : Teal, 15);
        var body = Text("Dialogue", panel, intercom ? "괜찮아요. 지금 계신 곳을\n천천히 알려 주세요." : "조금 놀랐어…\n우리 같이 천천히 기다릴까?", 34, Ink, 668, 112, -4, 3);
        Stretch(body.rectTransform, 40, 60, 40, 48);
        Symbol("ListeningMark", panel, CorridorSymbolGraphic.Symbol.Wave, 22, -329, -77, Teal);
        Text("Footnote", panel, intercom ? "상대의 안내를 듣고 답해요" : "친구의 이야기를 듣고 있어요", 16, Muted, 530, 26, -35, -77);
        for (int i = 0; i < 3; i++)
            Pill("SpeechDot" + i, panel, "", 7, 7, 312 + 15 * i, -77, i == 0 ? Teal : LineColor, Teal, 1);
        return r;
    }

    static RectTransform Thought()
    {
        var r = Rect("ThoughtCloud", null, 520, 320);
        var cloud = Rect("CloudSurface", r, 520, 300, 0, 20);
        var g = Surface(cloud, Paper, 24, 3, 2); g.thoughtCloud = true; g.bevelWidth = 2;
        Pill("ThoughtTag", cloud, "나의 생각", 128, 36, 0, 78, Mint, Teal, 18);
        var body = Text("Thought", cloud, "친구가 많이 놀랐구나.\n내가 옆에 있어 줘야겠어.", 27, Ink, 356, 100, 0, 1, false, TextAlignmentOptions.Center);
        Text("Whisper", cloud, "마음을 먼저 살펴봐요", 16, Muted, 310, 26, 0, -72, false, TextAlignmentOptions.Center);
        return r;
    }

    static RectTransform Observation()
    {
        var r = Rect("ObservationCard", null, 520, 170);
        var panel = Card("ObservationSurface", r, 520, 156, 0, 0, Mint, 20);
        Stretch(panel, 0, 0, 0, 14);
        var badge = Pill("EyeBadge", panel, "", 58, 58, -205, 23, Paper, Teal, 1);
        Symbol("Eye", badge, CorridorSymbolGraphic.Symbol.Eye, 34, 0, 0, Teal);
        Text("ObservationTitle", panel, "살펴보니", 20, Teal, 350, 30, 26, 43, true);
        Text("Description", panel, "이 버튼으로 관리실에\n도움을 요청할 수 있겠어.", 25, Ink, 366, 72, 36, -10);
        return r;
    }

    static RectTransform Choices()
    {
        var r = Rect("ChoiceCard", null, 760, 302);
        Text("Prompt", r, "나는 어떻게 말할까?", 21, Muted, 720, 32, 0, 128, true);
        var rows = Rect("Options", r, 760, 248, 0, -21);
        var layout = rows.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.spacing = 14; layout.childControlHeight = true; layout.childControlWidth = true;
        layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;
        string[] labels = { "괜찮아, 내가 옆에 있을게.", "우리 같이 도움을 요청하자.", "주변을 조금 더 살펴볼래." };
        for (int i = 0; i < labels.Length; i++)
        {
            var row = Card("Option" + (i + 1), rows, 760, 72, 0, 0, i == 0 ? Teal : Paper, 18);
            var e = row.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); e.preferredHeight = e.minHeight = 72;
            row.GetComponent<CorridorBubbleGraphic>().extrusion = 4;
            Pill("Number", row, (i + 1).ToString("00"), 42, 38, -338, 0, i == 0 ? Paper : Mint, Teal, 18);
            var t = Text("Label", row, labels[i], 27, i == 0 ? White : Ink, 614, 52, 0, 0, i == 0);
            Stretch(t.rectTransform, 78, 10, 60, 10);
            Symbol("Arrow", row, i == 2 ? CorridorSymbolGraphic.Symbol.Eye : CorridorSymbolGraphic.Symbol.Play, 22, 337, 0, i == 0 ? Paper : Teal);
            Button(row);
        }
        return r;
    }

    static RectTransform Mission()
    {
        var r = Rect("MissionCard", null, 520, 178);
        var panel = Card("MissionSurface", r, 520, 164, 0, 0, Paper, 20);
        Stretch(panel, 0, 0, 0, 14);
        Pill("Step", panel, "02", 48, 46, -208, 34, Gold, Ink, 21);
        Text("Eyebrow", panel, "지금 우리가 할 일", 17, Muted, 330, 28, 7, 51);
        Text("Objective", panel, "도움을 요청해요", 29, Ink, 370, 44, 26, 12, true);
        string[] stages = { "살펴보기", "도움 요청", "함께 대기" };
        for (int i = 0; i < 3; i++)
        {
            var segment = Card("StepSegment" + i, panel, 143, 5, -157 + i * 157, -29, i <= 1 ? Teal : LineColor, 2);
            var g = segment.GetComponent<CorridorBubbleGraphic>(); g.borderWidth = g.extrusion = g.bevelWidth = g.shadowOpacity = 0;
            Text("StepLabel" + i, panel, stages[i], 15, i == 1 ? Teal : Muted, 143, 24, -157 + i * 157, -53, i == 1, TextAlignmentOptions.Center);
        }
        return r;
    }

    static RectTransform Completion()
    {
        var r = Rect("CompletionCard", null, 520, 394);
        var panel = Card("PaperSurface", r, 520, 380, 0, 0, Paper, 25);
        Stretch(panel, 0, 0, 0, 14);
        Pill("MemoryTag", panel, "오늘의 안전 기록", 186, 34, -126, 144, Mint, Teal, 17);
        Memory("Together", panel, ElevatorMemoryGraphic.Illustration.Together, 74, 190, 124);
        Text("Title", panel, "우리, 함께 해냈어!", 34, Ink, 440, 55, 0, 85, true);
        Text("Detail", panel, "도움을 청하고, 친구 곁을 지켰어요.", 21, Muted, 440, 40, 0, 35);
        Fill("Divider", panel, 436, 1.5f, 0, 1, LineColor);
        string[] labels = { "도움 요청", "위치 전달", "함께 대기" };
        for (int i = 0; i < 3; i++)
        {
            Symbol("Check" + i, panel, CorridorSymbolGraphic.Symbol.Check, 22, -203, -32 - i * 38, Teal);
            Text("Accomplishment" + i, panel, labels[i], 22, Ink, 300, 32, -16, -32 - i * 38);
            Text("Done" + i, panel, "완료", 17, Teal, 80, 30, 183, -32 - i * 38, true, TextAlignmentOptions.Right);
        }
        var b = Card("KeepMemory", panel, 440, 50, 0, -151, Teal, 16); Button(b);
        Text("Label", b, "이 마음, 기억할게", 22, White, 400, 42, 0, 0, true, TextAlignmentOptions.Center);
        return r;
    }

    static RectTransform Pause()
    {
        var r = Rect("PausePanel", null, 520, 396);
        var panel = Card("PauseSurface", r, 520, 380, 0, 0, Paper, 25);
        Stretch(panel, 0, 0, 0, 16);
        Pill("PauseTag", panel, "잠깐 쉬어 가요", 176, 34, 0, 141, Mint, Teal, 17);
        Text("Title", panel, "준비되면 다시 함께!", 31, Ink, 448, 53, 0, 82, true, TextAlignmentOptions.Center);
        string[] labels = { "이어서 하기", "처음부터 다시 하기", "타이틀로 돌아가기" };
        for (int i = 0; i < 3; i++)
        {
            var b = Card("PauseOption" + i, panel, 436, 57, 0, 15 - i * 73, i == 0 ? Teal : Paper, 16); Button(b);
            Symbol("Icon", b, i == 0 ? CorridorSymbolGraphic.Symbol.Play : i == 1 ? CorridorSymbolGraphic.Symbol.Retry : CorridorSymbolGraphic.Symbol.Footprints, 23, -179, 0, i == 0 ? Paper : Teal);
            Text("Label", b, labels[i], 23, i == 0 ? White : Ink, 340, 43, 20, 0, i == 0, TextAlignmentOptions.Center);
        }
        return r;
    }

    static RectTransform Next()
    {
        var r = Rect("NextButton", null, 190, 64);
        Surface(r, Teal, 18, 4); Button(r);
        Text("Label", r, "다음", 25, White, 106, 44, -15, 0, true, TextAlignmentOptions.Center);
        Symbol("Arrow", r, CorridorSymbolGraphic.Symbol.Play, 22, 58, 0, Paper); return r;
    }

    static void Save(string name, RectTransform content)
    {
        var root = Rect(name + "_WorldCanvas", null, content.rect.width, content.rect.height);
        var c = root.gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace; c.sortingOrder = 40;
        var scaler = root.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>(); scaler.dynamicPixelsPerUnit = 10;
        root.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        root.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        content.SetParent(root, false); root.localScale = Vector3.one * .0015f;
        PrefabUtility.SaveAsPrefabAsset(root.gameObject, Prefabs + name + ".prefab");
        created.Add(Prefabs + name + ".prefab"); Object.DestroyImmediate(root.gameObject);
    }

    static void Section(Transform p, string number, string title, string detail, float x, float y, float w)
    {
        Pill("Section" + number, p, number, 38, 31, x - w / 2 + 19, y, Teal, White, 15);
        Text("SectionTitle" + number, p, title, 21, Ink, w - 62, 34, x + 31, y, true);
        if (!string.IsNullOrEmpty(detail)) Text("SectionDetail" + number, p, detail, 16, Muted, w, 28, x, y - 34);
    }

    static void Place(RectTransform r, Transform p, float x, float y, float scale = 1)
    { r.SetParent(p, false); r.anchoredPosition = new Vector2(x, y); r.localScale = Vector3.one * scale; }

    static void BuildBoard()
    {
        var board = Rect("StorybookUI_DesignBoard", null, 1920, 1320);
        var c = board.gameObject.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = board.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1320); scaler.matchWidthOrHeight = .5f;
        board.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        Fill("Background", board, 2400, 1800, 0, 0, Bg);
        for (int y = -12; y <= 12; y++) for (int x = -18; x <= 18; x++)
            Fill("GridDot", board, 2, 2, x * 56, y * 56, Hex("D5DECB"));
        Pill("Brand", board, "W", 58, 58, -846, 577, Teal, White, 29);
        Text("Title", board, "함께, 천천히.", 51, Ink, 860, 78, -346, 584, true);
        Text("Subtitle", board, "멈춰도 함께, 안전하게  /  이야기 UI 컬렉션", 21, Muted, 960, 38, -314, 525);
        Text("Edition", board, "PROJECT DwtD   ·   UI DESIGN 01", 15, Teal, 430, 30, 620, 598, true, TextAlignmentOptions.Right);
        Color[] colors = { Paper, Mint, Teal, Gold, Ink };
        for (int i = 0; i < colors.Length; i++) Pill("Swatch" + i, board, "", 30, 30, 656 + i * 44, 551, colors[i], Ink, 1);
        Fill("HeaderRule", board, 1760, 2, 0, 482, LineColor);
        // Left column: dialogue + choices retain their actual game proportions.
        Section(board, "01", "대화", "이야기는 크게, 화자는 또렷하게.", -457, 435, 800);
        Place(Dialogue(false), board, -457, 231, 1.02f);
        Place(Next(), board, -165, 57, .88f);
        Section(board, "02", "행동 선택", "말과 행동을 고르는 순간.", -457, -32, 800);
        Place(Choices(), board, -457, -258, 1.02f);
        Text("CallVariantCaption", board, "통화 중에는 따뜻한 노란색 화자 탭", 16, Muted, 530, 27, -574, -458);
        Place(Dialogue(true), board, -637, -533, .47f);
        Text("ButtonStatesTitle", board, "기본과 누름 상태", 16, Muted, 330, 27, -183, -458);
        var normal = Next(); Place(normal, board, -286, -514, .62f);
        var pressed = Next(); Place(pressed, board, -129, -514, .62f);
        pressed.GetComponent<CorridorBubbleGraphic>().SetInteraction(true, true);
        Text("NormalCaption", board, "기본", 14, Muted, 130, 24, -286, -560, false, TextAlignmentOptions.Center);
        Text("PressedCaption", board, "선택 · 누름", 14, Muted, 140, 24, -129, -560, false, TextAlignmentOptions.Center);
        // Middle-right: an airy cloud, and compact environmental information.
        Section(board, "03", "속마음", "내 마음은 부드러운 구름 속에.", 248, 435, 470);
        Place(Thought(), board, 253, 220, .91f);
        Section(board, "04", "관찰 · 발견", "찾아낸 단서를 짧고 편안하게.", 248, 6, 470);
        Place(Observation(), board, 248, -150, .90f);
        Section(board, "05", "지금 할 일", "한 번에 하나씩, 차근차근.", 248, -318, 470);
        Place(Mission(), board, 248, -467, .90f);
        // Right column: end-of-scene feedback and the quiet pause state.
        Section(board, "06", "함께한 기록", "결과보다 함께한 과정을 기억해요.", 714, 435, 380);
        Place(Completion(), board, 712, 192, .73f);
        Section(board, "07", "잠깐 쉬기", "돌아올 자리를 편안하게.", 714, -69, 380);
        Place(Pause(), board, 712, -313, .73f);
        Fill("FooterRule", board, 1760, 2, 0, -594, LineColor);
        Text("Footer", board, "크림빛 종이  ·  청록색 약속  ·  둥근 모서리  ·  부드러운 입체감", 16, Muted, 1200, 30, -260, -626);
        Text("FooterRight", board, "이야기 속 작은 용기를 위해", 16, Teal, 430, 30, 665, -626, true, TextAlignmentOptions.Right);
    }
}
