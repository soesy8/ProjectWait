using UnityEngine;
using System.Collections.Generic;

namespace ProjectWait.M
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ProjectWait/Frame Rate Limiter")]
    public sealed class FrameRateLimiter : MonoBehaviour
    {
        [SerializeField, Min(1)]
        [Tooltip("활성화된 동안 적용할 최대 FPS. 마지막 제한이 해제되면 이전 설정을 복원합니다.")]
        private int targetFrameRate = 60;

        private static readonly List<FrameRateLimiter> owners = new List<FrameRateLimiter>();
        private static int previousVSyncCount, previousFrameRate, installedFrameRate;
        private bool hasSavedSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOwners() { owners.Clear(); }

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (owners.Count == 0)
            {
                previousVSyncCount = QualitySettings.vSyncCount;
                previousFrameRate = Application.targetFrameRate;
            }
            owners.Add(this);
            hasSavedSettings = true;
            ApplyLimit();
        }

        private void OnDisable()
        {
            if (!hasSavedSettings)
                return;

            bool last = owners.Count > 0 && owners[owners.Count - 1] == this;
            owners.Remove(this);
            if (last && Application.targetFrameRate == installedFrameRate && QualitySettings.vSyncCount == 0)
            {
                if (owners.Count > 0) owners[owners.Count - 1].ApplyLimit();
                else
                {
                    Application.targetFrameRate = previousFrameRate;
                    QualitySettings.vSyncCount = previousVSyncCount;
                }
            }
            hasSavedSettings = false;
        }

        private void OnValidate()
        {
            targetFrameRate = Mathf.Max(1, targetFrameRate);
            if (Application.isPlaying && isActiveAndEnabled && hasSavedSettings && owners.Count > 0 && owners[owners.Count - 1] == this)
                ApplyLimit();
        }

        private void ApplyLimit()
        {
            // Desktop VSync takes precedence over Application.targetFrameRate.
            QualitySettings.vSyncCount = 0;
            installedFrameRate = targetFrameRate;
            Application.targetFrameRate = installedFrameRate;
        }
    }
}
