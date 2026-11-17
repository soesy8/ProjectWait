using System.Collections;
using TMPro;
using UnityEngine;
using ProjectWait.M.Shared;

namespace ProjectWait.YB.OfficeBlackout
{
    // The existing B2 artwork is retained. Only its reading plane and the actual
    // speaker change: text never belongs to the camera-locked letterbox.
    public sealed class OfficeBlackoutPresentation : MonoBehaviour
    {
        public Camera viewer;
        public OfficeBlackoutDirector director;
        public OfficeBlackoutCinematicOverlay cinematic;
        public Transform friend, teacher;
        public Canvas readingCanvas, promptCanvas;
        public Canvas deskMarker;
        public RectTransform storyCard, promptCard, notebook;
        public TMP_Text dialogueText, speakerText, promptText;
        public GameObject friendPortrait;
        public UnityEngine.UI.Button restartButton, lobbyButton;
        public string introduction;
        public bool DialogueVisible => storyCard && storyCard.gameObject.activeSelf;
        public string CurrentLine => dialogueText ? dialogueText.text : "";
        public UnityEngine.UI.Button[] CompletionButtons => new[] { restartButton, lobbyButton };
        CanvasGroup readingGroup, promptGroup, notebookGroup;
        bool menuVisible;
        Transform lastSubject;

        void Awake()
        {
            readingGroup = readingCanvas.GetComponent<CanvasGroup>();
            promptGroup = promptCanvas.GetComponent<CanvasGroup>();
            notebookGroup = notebook.GetComponent<CanvasGroup>();
            storyCard.gameObject.SetActive(false); promptCard.gameObject.SetActive(false);
            restartButton.onClick.AddListener(() => OfficeSceneIntegration.Current.Restart());
            lobbyButton.onClick.AddListener(() => OfficeSceneIntegration.Current.ReturnToLobby());
            ExperiencePreferences.Changed += ApplySettings; ApplySettings();
        }
        void LateUpdate()
        {
            if (!deskMarker) return;
            bool visible = !menuVisible && !OfficeStoryTime.Frozen && director && director.Flow != null && director.Flow.CurrentStep == OfficeBlackoutFlow.Step.Errand;
            deskMarker.gameObject.SetActive(visible);
            if (visible) deskMarker.transform.rotation = Quaternion.LookRotation(deskMarker.transform.position - viewer.transform.position, Vector3.up);
        }
        IEnumerator Start()
        {
            while (OfficeStoryTime.Frozen) yield return null;
            cinematic.ShowLetterbox(); ShowLine("나: " + introduction);
            yield return new OfficeStoryWait(5f / ExperiencePreferences.Current.dialogue);
            ClearLine(); cinematic.HideLetterbox();
        }
        public void ApplySettings()
        {
            if (dialogueText) dialogueText.fontSize = 28 * ExperiencePreferences.Current.text;
            if (speakerText) speakerText.fontSize = 23 * ExperiencePreferences.Current.text;
            if (promptText) promptText.fontSize = 23 * ExperiencePreferences.Current.text;
        }
        public void ShowLine(string line)
        {
            int colon = line.IndexOf(':');
            string speaker = colon >= 0 ? line.Substring(0, colon).Trim() : "안내";
            dialogueText.text = colon >= 0 ? line.Substring(colon + 1).Trim() : line;
            speakerText.text = speaker;
            Transform subject = speaker == "선생님" && teacher && teacher.gameObject.activeInHierarchy ? teacher : friend;
            // Player responses remain next to the conversation partner, not a floating head HUD.
            if (friendPortrait) friendPortrait.SetActive(speaker == "친구");
            if (!DialogueVisible || subject != lastSubject) PlaceReading(subject);
            lastSubject = subject; storyCard.gameObject.SetActive(true);
        }
        void PlaceReading(Transform subject)
        {
            Vector3 focus = subject ? subject.position + Vector3.up * 1.15f : viewer.transform.position + viewer.transform.forward * 2;
            Vector3 point = viewer.WorldToViewportPoint(focus);
            float x = point.z > 0 ? Mathf.Clamp(point.x + (point.x < .5f ? .26f : -.26f), .32f, .68f) : .5f;
            // Stay inside the shared bands; avoid faces while retaining the subject side.
            Place(readingCanvas.transform, new Vector2(x, .46f), 2.15f, .0018f);
        }
        void Place(Transform root, Vector2 viewport, float distance, float scale)
        {
            Ray ray = viewer.ViewportPointToRay(viewport);
            Vector3 position = ray.origin + ray.direction * distance;
            Vector3 normal = position - viewer.transform.position;
            root.SetPositionAndRotation(position, Quaternion.LookRotation(normal, Vector3.up));
            root.localScale = Vector3.one * scale;
        }
        public void ClearLine() { if (storyCard) storyCard.gameObject.SetActive(false); lastSubject = null; }
        public void PromptChanged()
        {
            if (!promptCard || !promptCard.gameObject.activeSelf) return;
            Place(promptCanvas.transform, new Vector2(.66f, .70f), 2.15f, .0016f);
        }
        public void SetMenuVisible(bool value)
        {
            menuVisible = value;
            if (readingGroup) readingGroup.alpha = value ? 0 : 1;
            if (promptGroup) promptGroup.alpha = value ? 0 : 1;
            if (notebookGroup) { notebookGroup.alpha = value ? 0 : 1; notebookGroup.blocksRaycasts = !value; }
        }
        public void ShowCompletion()
        {
            ClearLine(); promptCard.gameObject.SetActive(false);
            Vector3 forward = UnityEngine.XR.XRSettings.isDeviceActive ? Vector3.ProjectOnPlane(viewer.transform.forward, Vector3.up).normalized : viewer.transform.forward;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            notebook.SetPositionAndRotation(viewer.transform.position + forward * 2.2f - Vector3.up * .08f, Quaternion.LookRotation(forward));
            notebook.localScale = Vector3.one * 1.65f;
            notebook.gameObject.SetActive(true);
            SetMenuVisible(menuVisible);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = !UnityEngine.XR.XRSettings.isDeviceActive;
            OfficeSceneIntegration.Current.RequireFreshClick();
        }
        void OnDestroy() { ExperiencePreferences.Changed -= ApplySettings; }
    }
}
