using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectWait.M.TitleScene
{
    public sealed class TitlePortal : MonoBehaviour
    {
        public string experienceTitle;
        public string destinationScene;
        public TitlePlazaPlayer player;
        public UnityEngine.UI.Image buttonFace;
        public TMP_Text statusText;
        float statusUntil;
        public bool IsLoading { get; private set; }
        public bool CanEnter
        {
            get
            {
                if (string.IsNullOrEmpty(destinationScene)) return false;
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(destinationScene) != null;
#else
                return Application.CanStreamedLevelBeLoaded(destinationScene);
#endif
            }
        }
        public void SetHovered(bool hovered)
        {
            if (buttonFace) buttonFace.color = hovered ? new Color(.12f, .37f, .34f) : new Color(.16f, .48f, .44f);
        }
        void Update()
        {
            if (statusUntil > 0 && Time.unscaledTime >= statusUntil)
            { statusUntil = 0; if (statusText) statusText.text = experienceTitle; }
        }
        public void Activate()
        {
            if (IsLoading) return;
            if (!CanEnter)
            {
                if (statusText) statusText.text = "준비 중";
                statusUntil = Time.unscaledTime + 3;
                if (player) player.ShowMessage(experienceTitle + " 체험을 준비하고 있어요.");
                return;
            }
            IsLoading = true;
            if (statusText) statusText.text = "이동 중";
            if (player) player.ShowMessage(experienceTitle + " 체험으로 이동합니다.");
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#if UNITY_EDITOR
            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(destinationScene, new LoadSceneParameters(LoadSceneMode.Single));
            else
#endif
                SceneManager.LoadSceneAsync(destinationScene, LoadSceneMode.Single);
        }
    }
}
