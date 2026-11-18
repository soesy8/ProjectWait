using TMPro;
using UnityEngine;
using ProjectWait.M.Elevator.DialoguePreview;
using ProjectWait.M.SceneTransition;

namespace ProjectWait.M.Shared
{
    // A reading plane placed once in front of the player. Looking around does not drag it along.
    public sealed class LookAroundHint : MonoBehaviour
    {
        public const string Message = "주변을 둘러보고 선택할 수 있는 것을 확인해 보세요.";
        const float Hold = 2.5f, Fade = .4f;
        static LookAroundHint current;
        CanvasGroup group;
        float age;

        static RectTransform Create(Camera eyes, out Transform plane)
        {
            plane = null;
            if (!eyes) return null;
            var prefab = Resources.Load<GameObject>("LookAroundHint");
            if (!prefab) { Debug.LogError("Missing LookAroundHint prefab."); return null; }
            if (current) { current.gameObject.SetActive(false); Destroy(current.gameObject); }
            var root = Instantiate(prefab);
            var hint = root.GetComponent<LookAroundHint>();
            current = hint;
            hint.group = root.GetComponent<CanvasGroup>();
            root.GetComponent<Canvas>().worldCamera = eyes;
            var label = root.GetComponentInChildren<TMP_Text>();
            ElevatorTypography.Apply(label, ElevatorTypography.HintSize);
            plane = root.transform;
            return (RectTransform)label.transform.parent;
        }

        public static RectTransform ShowAt(Camera eyes, Transform station, float heightOffset, float unit)
        {
            if (!station) return null;
            var card = Create(eyes, out var plane);
            if (!card) return null;
            plane.SetPositionAndRotation(station.position + Vector3.up * heightOffset, station.rotation);
            plane.localScale = Vector3.one * unit;
            card.anchoredPosition = Vector2.zero;
            return card;
        }

        public static RectTransform Show(Camera eyes)
        {
            var card = Create(eyes, out var plane);
            if (!card) return null;
            // Keep the reading plane upright at eye height, even when looking down at a clue.
            Vector3 heading = Vector3.ProjectOnPlane(eyes.transform.forward, Vector3.up);
            if (heading.sqrMagnitude < .0001f)
                heading = Vector3.Cross(eyes.transform.right, Vector3.up);
            heading.Normalize();
            plane.SetPositionAndRotation(eyes.transform.position + heading * VRUIReadingLayout.Distance,
                Quaternion.LookRotation(heading, Vector3.up));
            plane.localScale = Vector3.one * (VRUIReadingLayout.Distance / VRUIReadingLayout.PointsPerRadian);
            var xr = UnityEngine.XR.XRSettings.isDeviceActive;
            // Leave the central reticle/target clear. The panel stays above the aiming point.
            card.anchoredPosition = VRUIReadingLayout.AnglePoint(0, 10);
            if (xr && StereoUISafeArea.TryCapture(eyes, out var frame) &&
                VRUIReadingLayout.Area(frame, (RectTransform)plane, out var area, out _))
                VRUIReadingLayout.Place(card, plane, area, 0, 10, out _);
            else if (!xr)
            {
                float units = 2 * VRUIReadingLayout.Distance * Mathf.Tan(eyes.fieldOfView * .5f * Mathf.Deg2Rad) / 720f;
                plane.localScale = Vector3.one * units;
                card.anchoredPosition = new Vector2(0, 120);
            }
            return card;
        }

        void Update()
        {
            if (!group) return;
            if (StorybookSceneTransition.IsBusy) { Destroy(gameObject); return; }
            if (Time.timeScale == 0) { group.alpha = 0; return; }
            group.alpha = 1 - Mathf.SmoothStep(0, 1, (age - Hold) / Fade);
            age += Time.deltaTime;
            if (age >= Hold + Fade) Destroy(gameObject);
        }
    }
}
