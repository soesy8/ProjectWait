using UnityEngine;

namespace ProjectWait.M
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ProjectWait/Frame Rate Limiter")]
    public sealed class FrameRateLimiter : MonoBehaviour
    {
        [SerializeField, Min(1)]
        [Tooltip("활성화된 동안 적용할 최대 FPS. 오브젝트나 컴포넌트를 끄면 FPS 제한을 해제합니다.")]
        private int targetFrameRate = 60;

        private int previousVSyncCount;
        private bool hasSavedSettings;

        private void OnEnable()
        {
            previousVSyncCount = QualitySettings.vSyncCount;
            hasSavedSettings = true;
            ApplyLimit();
        }

        private void OnDisable()
        {
            if (!hasSavedSettings)
                return;

            Application.targetFrameRate = -1;
            QualitySettings.vSyncCount = previousVSyncCount;
            hasSavedSettings = false;
        }

        private void OnValidate()
        {
            targetFrameRate = Mathf.Max(1, targetFrameRate);
            if (Application.isPlaying && isActiveAndEnabled && hasSavedSettings)
                ApplyLimit();
        }

        private void ApplyLimit()
        {
            // Desktop VSync takes precedence over Application.targetFrameRate.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
        }
    }
}
