using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace ProjectWait.YB.OfficeBlackout
{
    /// <summary>
    /// World-space alternative to the explicit papers action card.
    /// Aiming at the visible desk target does not require entering a hidden volume.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class OfficeBlackoutInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] OfficeBlackoutDirector director;
        [SerializeField] OfficeBlackoutDeskPromptTrigger deskArea;
        [SerializeField] Camera playerCamera;
        [SerializeField, Min(0.1f)] float interactionRayDistance = 8f;
        [SerializeField, Min(0.001f)] float aimRayWidth = 0.008f;

        [Header("Input")]
        [Tooltip("Optional action override. Default: left mouse click, either XR trigger, or E.")]
        [SerializeField] InputActionReference interactAction;
        [SerializeField] Key keyboardKey = Key.E;

        InputAction fallbackAction;
        bool hasLoggedPlayerDetection;
        LineRenderer aimRay;
        Transform aimMarker;
        Material aimMaterial;

        void Reset()
        {
            var trigger = GetComponent<Collider>();
            if (trigger) trigger.isTrigger = true;
        }

        void OnEnable()
        {
            var trigger = GetComponent<Collider>();
            if (trigger && !trigger.isTrigger)
                Debug.LogWarning($"{nameof(OfficeBlackoutInteraction)} expects an Is Trigger collider on {name}.", this);
            if (!playerCamera) playerCamera = Camera.main;
            CreateAimVisuals();

            if (interactAction != null && interactAction.action != null)
            {
                interactAction.action.performed += OnInteract;
                interactAction.action.Enable();
            }
            else
            {
                fallbackAction = new InputAction("OfficeBlackoutInteract", InputActionType.Button);
                fallbackAction.AddBinding("<Mouse>/leftButton");
                fallbackAction.AddBinding("<XRController>/{TriggerButton}");
                fallbackAction.AddBinding("<Keyboard>/" + keyboardKey.ToString().ToLowerInvariant());
                fallbackAction.performed += OnInteract;
                fallbackAction.Enable();
            }
        }

        void OnDisable()
        {
            if (aimRay) aimRay.enabled = false;
            if (aimMarker) aimMarker.gameObject.SetActive(false);
            if (interactAction != null && interactAction.action != null)
                interactAction.action.performed -= OnInteract;
            if (fallbackAction != null)
            {
                fallbackAction.performed -= OnInteract;
                fallbackAction.Dispose();
                fallbackAction = null;
            }
        }

        void LateUpdate()
        {
            bool canAim = !OfficeSceneIntegration.GameplayBlocked && director && director.ActionReady && director.Flow != null &&
                          director.Flow.CurrentStep == OfficeBlackoutFlow.Step.Errand && playerCamera;
            if (!aimRay || !aimMarker)
            {
                if (canAim) CreateAimVisuals();
                if (!aimRay || !aimMarker) return;
            }

            // XR already has shared hand rays. Desktop needs only the target marker.
            aimRay.enabled = false;
            aimMarker.gameObject.SetActive(canAim);
            if (!canAim) return;

            Ray ray = GetAimRay();
            Collider targetCollider = GetComponent<Collider>();
            RaycastHit hitInfo = default;
            bool hit = targetCollider && targetCollider.Raycast(ray, out hitInfo, interactionRayDistance);
            Vector3 end = hit ? hitInfo.point : targetCollider.bounds.center;
            aimRay.SetPosition(0, ray.origin + ray.direction * 0.12f);
            aimRay.SetPosition(1, end);
            Color color = hit ? new Color(.16f, .48f, .44f) : new Color(.61f, .79f, .72f);
            aimRay.startColor = aimRay.endColor = color;
            SetAimMaterialColor(color);
            aimMarker.position = end;
            aimMarker.localScale = Vector3.one * (hit ? 0.055f : 0.035f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (hasLoggedPlayerDetection || !IsPlayer(other)) return;
            hasLoggedPlayerDetection = true;
            Debug.Log($"[OfficeBlackout] Player detected by {name} trigger.", this);
        }

        void OnInteract(InputAction.CallbackContext context)
        {
            if (OfficeSceneIntegration.GameplayBlocked || !OfficeSceneIntegration.Current || !OfficeSceneIntegration.Current.ClickReady) return;
            if (!director || !director.ActionReady || director.Flow == null) return;
            if (director.Flow.CurrentStep != OfficeBlackoutFlow.Step.Errand) return;
            if (!HitDropoffZone(OfficeSceneIntegration.Current.AimRay(context.control))) return;

            if (director.PerformAction()) OfficeSceneIntegration.Current.RequireFreshClick();
        }

        bool HitDropoffZone(Ray ray)
        {
            if (!playerCamera) return false;

            Collider targetCollider = GetComponent<Collider>();
            if (!targetCollider || !targetCollider.Raycast(ray, out var target, interactionRayDistance)) return false;
            foreach (var hit in Physics.RaycastAll(ray, target.distance, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.transform.IsChildOf(targetCollider.transform) && !hit.transform.GetComponentInParent<CharacterController>()) return false;
            return true;
        }

        Ray GetAimRay()
        {
            if (OfficeSceneIntegration.Current) return OfficeSceneIntegration.Current.DefaultRay();
            Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (!XRSettings.isDeviceActive && Mouse.current != null && Cursor.lockState != CursorLockMode.Locked)
                screenPoint = Mouse.current.position.ReadValue();
            return playerCamera.ScreenPointToRay(screenPoint);
        }

        void CreateAimVisuals()
        {
            if (aimRay && aimMarker) return;

            var rayObject = new GameObject("DropoffAimRay");
            rayObject.layer = 2;
            aimRay = rayObject.AddComponent<LineRenderer>();
            aimRay.useWorldSpace = true;
            aimRay.positionCount = 2;
            aimRay.widthMultiplier = aimRayWidth;
            aimRay.numCapVertices = 4;
            aimRay.alignment = LineAlignment.View;
            aimRay.enabled = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader)
            {
                aimMaterial = new Material(shader);
                SetAimMaterialColor(new Color(1f, 0.25f, 0.15f, 1f));
                aimRay.material = aimMaterial;
            }

            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            markerObject.name = "DropoffAimPoint";
            markerObject.layer = 2;
            var markerCollider = markerObject.GetComponent<Collider>();
            if (markerCollider) Destroy(markerCollider);
            var markerRenderer = markerObject.GetComponent<Renderer>();
            if (markerRenderer && aimMaterial) markerRenderer.sharedMaterial = aimMaterial;
            aimMarker = markerObject.transform;
            aimMarker.gameObject.SetActive(false);
        }

        void SetAimMaterialColor(Color color)
        {
            if (!aimMaterial) return;
            if (aimMaterial.HasProperty("_BaseColor")) aimMaterial.SetColor("_BaseColor", color);
            else if (aimMaterial.HasProperty("_Color")) aimMaterial.SetColor("_Color", color);
        }
        void OnDestroy()
        {
            if (aimRay) Destroy(aimRay.gameObject);
            if (aimMarker) Destroy(aimMarker.gameObject);
            if (aimMaterial) Destroy(aimMaterial);
        }

        bool IsPlayer(Collider other)
        {
            Transform candidate = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            return candidate.CompareTag("Player") || candidate.GetComponentInParent<CharacterController>() != null;
        }
    }
}
