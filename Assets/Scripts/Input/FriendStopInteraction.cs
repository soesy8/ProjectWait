using TMPro; // 친구 근처에 작은 월드 공간 한국어 안내 문구를 표시합니다.
using Unity.XR.CoreUtils; // 기존 XR Origin에서 플레이어 카메라와 컨트롤러 계층을 참조합니다.
using UnityEngine; // 친구의 표시 위치와 피드백 유지 시간을 관리합니다.
using UnityEngine.XR.Interaction.Toolkit; // 기존 XR 상호작용의 조준·선택 이벤트를 수신합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactables; // 물체를 잡아 이동시키지 않는 기존 XRSimpleInteractable을 사용합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors; // 컨트롤러 레이와 이미 설정된 선택 입력을 구분합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters; // 기존 Near-Far 레이의 도달 거리만 조정합니다.
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals; // 기존 레이 표시도 실제 친구 위치까지 이어지도록 범위를 맞춥니다.

namespace Y.Crosswalk // 친구 이동과 동일한 학교 앞 횡단보도 작업 영역에 둡니다.
{ // 입력 전달과 작은 안내 표시를 이동 상태 관리에서 분리합니다.
    [DisallowMultipleComponent] // 같은 친구에 선택 이벤트가 중복 연결되는 것을 방지합니다.
    [RequireComponent(typeof(FriendCharacterMovement), typeof(XRSimpleInteractable), typeof(FriendCrossingDecision))] // 기존 XR 이벤트와 이동 컴포넌트 사이에 신호별 명령 판단을 연결합니다.
    public sealed class FriendStopInteraction : MonoBehaviour // 기존 선택 입력으로 친구 상태에 맞는 멈춤 또는 횡단 명령을 전달합니다.
    { // 이 친구의 상호작용과 피드백에 필요한 값만 보관합니다.
        [Header("건너편 친구 조준")] // 도로를 건너지 않고 신호를 보낼 수 있는 범위를 설정합니다.
        [SerializeField, Min(12f), InspectorName("상호작용 거리(m)")] // 현재 인도 사이 거리보다 긴 레이 도달 범위를 지정합니다.
        [Tooltip("기본값 30m입니다. 이 씬의 기존 양손 Near-Far 레이 범위를 필요한 만큼 늘리며 입력 액션과 XR Origin의 위치·추적은 변경하지 않습니다.")] // 거리 조정이 기존 컨트롤러에 적용됨을 설명합니다.
        private float m_InteractionDistance = 30f; // 시작 시 약 11.25m 떨어진 친구까지 여유 있게 도달합니다.

        [Header("친구 근처 안내")] // 얼굴 앞이나 전체 화면 대신 친구 위쪽에 표시할 문구를 설정합니다.
        [SerializeField, InspectorName("한국어 안내 글꼴")] // VR 기기의 시스템 글꼴에 의존하지 않는 포함 글꼴을 지정합니다.
        [Tooltip("프로젝트에 포함한 NotoSansKR-Regular 글꼴로 멈춰!·건너와!·알겠어!·빨간불이야!를 월드 공간에서 표시합니다.")] // 제공받은 한국어 글꼴을 친구 명령 안내에도 동일하게 사용함을 설명합니다.
        private Font m_KoreanFont; // 친구 프리팹에서 참조하는 한국어 원본 글꼴입니다.

        [SerializeField, InspectorName("안내 위치(옆·높이 m)")] // 캐릭터 발 위치를 기준으로 표시할 옆 거리와 높이를 지정합니다.
        [Tooltip("X는 플레이어가 보는 오른쪽 거리, Y는 친구 발 위 높이입니다. 기본값은 옆 0.7m, 높이 1.95m로 얼굴과 도로를 가리지 않게 합니다.")] // 안내가 플레이어를 따라 이동하지 않고 친구 근처에 머무는 기준을 설명합니다.
        private Vector2 m_FeedbackOffset = new Vector2(0.7f, 1.95f); // 기존 학생 모델의 머리보다 위이면서 옆인 위치를 사용합니다.

        [SerializeField, Min(0.5f), InspectorName("응답 표시 시간(초)")] // 명령 수락 문구를 관찰할 수 있는 짧은 시간을 지정합니다.
        [Tooltip("기본값 2초 동안 알겠어!를 표시합니다. 반복 입력으로 표시 시간이나 손들기를 다시 시작하지 않습니다.")] // 응답 문구와 중복 입력의 동작을 안내합니다.
        private float m_AcknowledgementDuration = 2f; // 친구의 짧은 손들기를 볼 동안 응답을 유지합니다.

        private FriendCharacterMovement m_Movement; // 실제 정지 상태와 반응을 담당하는 기존 이동 컴포넌트입니다.
        private FriendCrossingDecision m_CrossingDecision; // 명령 순간의 신호 판정과 위험 반응·차량 통과 대기를 담당합니다.
        private XRSimpleInteractable m_Interactable; // 이 친구의 Collider에 도달한 기존 XR 조준·선택 이벤트를 제공합니다.
        private Camera m_PlayerCamera; // 표시를 읽기 쉽게 향하게 할 추적 카메라이며 위치와 회전은 변경하지 않습니다.
        private TextMeshPro m_FeedbackText; // 친구 옆에 생성하는 작은 3D 문구로 화면 공간 UI를 만들지 않습니다.
        private TMP_FontAsset m_RuntimeFont; // 포함된 한국어 글꼴에서 멈춤·횡단·응답 문구의 글리프를 동적으로 준비합니다.
        private float m_AcknowledgementRemaining; // 실제 명령이 수락된 뒤에만 줄어드는 응답 표시 시간입니다.
        private bool m_DesktopHovered; // 마우스로 기존 친구를 가리킬 때도 같은 명령 안내를 표시합니다.

        private void Awake() // 이벤트 수신 전에 같은 친구의 기존 컴포넌트와 안내 문구를 준비합니다.
        { // 씬에 이미 존재하는 XR 입력과 Interaction Manager를 그대로 사용합니다.
            m_Movement = GetComponent<FriendCharacterMovement>(); // 명령 수락 여부를 친구의 이동 상태에 직접 질의합니다.
            m_CrossingDecision = GetComponent<FriendCrossingDecision>(); // 신호 판정은 별도 컴포넌트에 맡겨 VR 입력 구조를 유지합니다.
            m_Interactable = GetComponent<XRSimpleInteractable>(); // 친구 전용 Collider에 등록된 XR 이벤트 발신기를 찾습니다.
            m_PlayerCamera = Camera.main; // 기존 MainCamera를 읽기만 하여 카메라 추적을 유지합니다.
            if (m_KoreanFont == null) return; // 글꼴 연결이 없더라도 멈춤 명령 자체의 처리는 유지합니다.
            m_RuntimeFont = TMP_FontAsset.CreateFontAsset(m_KoreanFont); // VR 기기에서도 원본 글꼴로 한국어 SDF 문자를 만들 수 있게 합니다.
            m_RuntimeFont.TryAddCharacters("멈춰!건너와!알겠어!빨간불이야!"); // 빨간불 반응에 필요한 한글도 기존 안내용 아틀라스에 준비합니다.
            GameObject label = new GameObject("친구 멈춤 안내", typeof(TextMeshPro)); // 친구 근처에서만 보이는 작은 월드 공간 텍스트를 만듭니다.
            label.transform.SetParent(transform, false); // 친구가 이동하는 동안에도 안내 오브젝트가 같은 친구에 속하게 합니다.
            m_FeedbackText = label.GetComponent<TextMeshPro>(); // 화면 전체 Canvas 없이 3D 텍스트를 설정합니다.
            m_FeedbackText.font = m_RuntimeFont; // 한글 글리프를 포함한 전용 글꼴을 연결합니다.
            m_FeedbackText.fontSize = 3.2f; // 월드 공간에서 글자 높이가 약 0.3m인 작은 안내를 사용합니다.
            m_FeedbackText.rectTransform.sizeDelta = new Vector2(1.7f, 0.5f); // 한 문구가 들어갈 크기로 제한하여 캐릭터 전체를 덮지 않게 합니다.
            m_FeedbackText.alignment = TextAlignmentOptions.Center; // 두 문구가 같은 안내 위치에 가운데 정렬되게 합니다.
            m_FeedbackText.textWrappingMode = TextWrappingModes.NoWrap; // 짧은 명령 문구를 한 줄로 표시합니다.
            m_FeedbackText.color = new Color(1f, 0.95f, 0.6f, 1f); // 낮 환경에서도 알아보기 쉬운 부드러운 밝은 색을 사용합니다.
            m_FeedbackText.outlineColor = Color.black; // 배경과 겹쳐도 글자 형태를 읽을 수 있게 어두운 외곽선을 사용합니다.
            m_FeedbackText.outlineWidth = 0.15f; // 점멸 없이 고정된 얇은 테두리로 가독성을 높입니다.
            label.SetActive(false); // 정상 조준하거나 명령이 성공하기 전에는 안내를 숨깁니다.
        } // 새 카메라·입력 액션·Interaction Manager는 생성하지 않습니다.

        private void Start() // 기존 XR 컴포넌트의 초기화가 끝난 뒤 건너편까지 레이 범위를 확보합니다.
        { // 카메라가 속한 XR Origin의 기존 컨트롤러만 대상으로 삼습니다.
            XROrigin origin = m_PlayerCamera != null ? m_PlayerCamera.GetComponentInParent<XROrigin>() : null; // 현재 씬의 플레이어 추적 계층을 찾습니다.
            if (origin == null) return; // XR Origin이 없는 별도 미리보기에서는 다른 오브젝트 설정을 바꾸지 않습니다.
            foreach (NearFarInteractor interactor in origin.GetComponentsInChildren<NearFarInteractor>(true)) // 추적 후 활성화될 양손 컨트롤러의 기존 레이도 포함합니다.
            { // 기존 입력 바인딩·상호작용 레이어·레이 종류는 그대로 사용합니다.
                if (interactor.TryGetComponent(out CurveInteractionCaster caster)) caster.castDistance = Mathf.Max(caster.castDistance, m_InteractionDistance); // 현재 범위를 줄이지 않고 이 스테이지에 필요한 30m를 확보합니다.
                foreach (CurveVisualController visual in interactor.GetComponentsInChildren<CurveVisualController>(true)) // 각 컨트롤러가 이미 사용하는 레이 표시 컴포넌트를 찾습니다.
                { // 판정만 멀어지고 화면의 선은 10m에서 잘리는 불일치를 방지합니다.
                    visual.maxVisualCurveDistance = Mathf.Max(visual.maxVisualCurveDistance, m_InteractionDistance); // 선의 모양과 피드백은 유지하며 최대 표시 거리만 판정 범위에 맞춥니다.
                } // 새 LineRenderer나 별도의 조준 레이는 생성하지 않습니다.
            } // 이동·텔레포트·카메라 추적 설정에는 접근하지 않습니다.
        } // 친구 프리팹이 놓인 씬에서만 기존 Near-Far 레이의 거리가 설정됩니다.

        private void OnEnable() // 활성화된 친구만 컨트롤러 선택을 수신합니다.
        { // 동일한 컴포넌트의 기존 XR 이벤트에 한 번 연결합니다.
            m_Interactable.selectEntered.AddListener(OnSelectEntered); // 기존 Select 액션이 이 친구를 선택했을 때만 명령 전달을 시도합니다.
        } // 조준 안내는 기존 interactorsHovering 목록을 읽어 양손 상태를 함께 반영합니다.

        private void OnDisable() // 친구가 꺼지면 선택 수신과 남아 있는 안내를 정리합니다.
        { // 다시 켜져도 이벤트가 누적되거나 이전 성공 문구가 재생되지 않게 합니다.
            m_Interactable.selectEntered.RemoveListener(OnSelectEntered); // 활성화 시 연결한 선택 이벤트만 해제합니다.
            m_AcknowledgementRemaining = 0f; // 이전 응답 표시 시간을 남기지 않습니다.
            m_DesktopHovered = false; // 비활성화된 친구에 마우스 조준 안내가 남지 않게 합니다.
            if (m_FeedbackText != null) m_FeedbackText.gameObject.SetActive(false); // 친구가 비활성화된 동안 월드 안내를 숨깁니다.
        } // 이동 컴포넌트가 보관하는 명령 정지 상태는 변경하지 않습니다.

        private void OnSelectEntered(SelectEnterEventArgs args) // 도로나 신호등이 아니라 이 친구가 선택된 경우에만 호출됩니다.
        { // 새 입력 액션 없이 기존 컨트롤러의 선택 입력이 눌린 프레임을 사용합니다.
            if (!(args.interactorObject is XRBaseInputInteractor controller) || !(controller is IXRRayProvider)) return; // 레이 선택 입력이 없는 직접 접촉·소켓 상호작용은 명령으로 처리하지 않습니다.
            if (!controller.selectInput.ReadWasPerformedThisFrame()) return; // 계속 누르거나 누른 채 친구 쪽으로 옮기는 동작을 새 신호로 처리하지 않습니다.
            XROrigin origin = controller.GetComponentInParent<XROrigin>(); // 선택한 컨트롤러와 같은 플레이어의 카메라를 찾습니다.
            if (origin == null || origin.Camera == null) return; // 반응할 플레이어 방향을 알 수 없는 선택은 처리하지 않습니다.
            ReceiveCommand(origin.Camera); // 기존 명령 판단을 마우스 선택과 동일한 진입점으로 전달합니다.
        } // XR Select의 수행 프레임 확인은 기존대로 유지합니다.

        public void SetDesktopHover(bool hovered) { m_DesktopHovered = hovered && isActiveAndEnabled; } // PC 마우스의 조준 상태를 기존 친구 안내에 전달합니다.
        public void SendPointerCommand(Camera viewer) // 마우스로 선택해도 기존 멈춤·횡단 판단을 그대로 사용합니다.
        { // 비활성화된 친구나 해제 중인 상호작용은 명령을 받지 않습니다.
            if (isActiveAndEnabled && m_Interactable != null && m_Interactable.isActiveAndEnabled) ReceiveCommand(viewer); // 기존 XR 친구가 선택 가능한 경우에만 같은 명령을 전달합니다.
        } // 새 횡단 이벤트나 입력별 별도 이동 상태를 만들지 않습니다.

        private void ReceiveCommand(Camera viewer) // VR과 PC의 유효한 선택이 같은 상태·신호·반응 제한을 거칩니다.
        { // 이전 멈춤 응답 중 연속 클릭을 새 횡단 요청으로 처리하지 않습니다.
            if (viewer == null || m_AcknowledgementRemaining > 0f) return; // 관찰자나 재입력 조건이 유효하지 않으면 상태를 유지합니다.
            bool crossing = m_CrossingDecision.CanReceiveCrossCommand; // 반응 중이 아니고 건너편에서 대기하는 친구에게만 새 횡단 명령을 선택합니다.
            if (crossing) // 횡단 명령이 가능한 상태에서는 기존 멈춤 명령을 다시 실행하지 않습니다.
            { // 신호 스냅샷과 차량 진입 보호를 거친 뒤 기존 이동 또는 짧은 위험 반응을 실행합니다.
                if (!m_CrossingDecision.TryRequestCrossing()) return; // 신호별 판단 담당자가 수락하지 않은 입력은 이동이나 안내를 다시 시작하지 않습니다.
            } // 횡단 중 선택 입력은 아래의 기존 멈춤 경로로 전달됩니다.
            else if (!m_CrossingDecision.TryStopCommand(viewer.transform)) return; // 입력 장치와 관계없이 기존 정지·대기 취소·중복 반응 제한을 유지합니다.
            m_PlayerCamera = viewer; // 실제 명령을 보낸 플레이어에게 안내 문구를 향하게 합니다.
            m_AcknowledgementRemaining = crossing ? 0f : m_AcknowledgementDuration; // 기존 알겠어! 응답은 멈춤에만 유지하며 횡단에는 추가 교육 피드백을 만들지 않습니다.
        } // 친구의 위치·속도·Animator는 이동 컴포넌트에서만 변경합니다.

        private void LateUpdate() // 추적 카메라와 친구의 이번 프레임 위치를 기준으로 안내만 갱신합니다.
        { // 두 컨트롤러 중 하나라도 이 친구를 조준하면 안내를 유지합니다.
            bool acknowledged = m_AcknowledgementRemaining > 0f; // 성공 문구가 남아 있으면 조준 안내보다 우선합니다.
            m_AcknowledgementRemaining = Mathf.Max(0f, m_AcknowledgementRemaining - Time.deltaTime); // 안내 생성 여부와 관계없이 연속 입력 제한과 응답 시간을 함께 종료합니다.
            if (m_FeedbackText == null || m_PlayerCamera == null) return; // 입력 제한 시간을 갱신한 뒤 표시 가능한 경우에만 안내를 조절합니다.
            bool aimed = m_DesktopHovered; // 마우스 조준과 기존 양손 레이 조준을 같은 안내 상태에 합칩니다.
            bool redReaction = m_CrossingDecision.IsReactingToRed; // 최초 명령이 빨간불이었던 짧은 반응을 신호의 현재 색과 독립적으로 표시합니다.
            bool canCross = m_CrossingDecision.CanReceiveCrossCommand; // 차량 통과 대기나 위험 반응 중에는 새 횡단 안내를 띄우지 않습니다.
            if (canCross || m_CrossingDecision.CanReceiveStopCommand) // 보행과 차량 통과 대기에는 멈춤을 안내하고 새 요청이 가능한 인도 대기에는 횡단을 안내합니다.
            { // XR Toolkit이 판정한 이 친구의 조준 목록만 사용합니다.
                foreach (IXRHoverInteractor interactor in m_Interactable.interactorsHovering) // 다른 도로 오브젝트나 신호등의 조준 상태는 포함하지 않습니다.
                { // 직접 접촉과 소켓은 원거리 명령 안내를 켜지 않습니다.
                    if (!(interactor is XRBaseInputInteractor) || !(interactor is IXRRayProvider)) continue; // 기존 선택 입력이 있는 레이 상호작용만 안내 대상으로 삼습니다.
                    aimed = true; // 최소 한 컨트롤러가 친구를 정상 조준한 상태입니다.
                    break; // 양손이 동시에 조준해도 안내는 하나만 표시합니다.
                } // 한쪽 손이 벗어나도 다른 손이 조준 중이면 안내를 유지합니다.
            } // 성공한 뒤의 반복 조준은 캐릭터 상태를 바꾸지 않습니다.
            m_FeedbackText.gameObject.SetActive(redReaction || acknowledged || aimed); // 위험 반응 안내는 조준을 벗어나도 정해진 시간 동안 친구 근처에 유지합니다.
            if (!redReaction && !acknowledged && !aimed) return; // 표시할 반응과 조준 안내가 없으면 기존처럼 숨깁니다.
            m_FeedbackText.text = redReaction ? "빨간불이야!" : acknowledged ? "알겠어!" : canCross ? "건너와!" : "멈춰!"; // 위험 반응을 우선 표시하고 나머지는 실제 선택 입력이 수행할 기존 명령과 일치시킵니다.
            m_FeedbackText.transform.position = transform.position + Vector3.up * m_FeedbackOffset.y + m_PlayerCamera.transform.right * m_FeedbackOffset.x; // 카메라 앞이 아닌 친구 위쪽 옆에 안내를 배치합니다.
            Vector3 facing = m_FeedbackText.transform.position - m_PlayerCamera.transform.position; // 문구를 관찰자에게 읽히는 방향으로 향하게 할 벡터입니다.
            if (facing.sqrMagnitude > 0.001f) m_FeedbackText.transform.rotation = Quaternion.LookRotation(facing, Vector3.up); // 안내 오브젝트만 회전하며 플레이어의 시점은 제어하지 않습니다.
        } // 카메라 흔들림·화면 점멸·전체 화면 UI는 사용하지 않습니다.

        private void OnDestroy() // 친구 또는 이 컴포넌트를 제거하면 생성한 표시 리소스만 해제합니다.
        { // 원본 한국어 글꼴과 기존 XR 리소스는 그대로 유지합니다.
            if (m_FeedbackText != null) // 외곽선을 위해 텍스트가 만든 개별 머티리얼도 정리합니다.
            { // 원본 동적 글꼴 머티리얼과 중복해서 해제하지 않습니다.
                if (m_RuntimeFont != null && m_FeedbackText.fontSharedMaterial != m_RuntimeFont.material) Destroy(m_FeedbackText.fontSharedMaterial); // 텍스트 전용 외곽선 머티리얼을 해제합니다.
                Destroy(m_FeedbackText.gameObject); // 컴포넌트만 제거된 경우에도 친구 옆 안내가 남지 않게 합니다.
            } // 글꼴이 없어서 표시를 만들지 않은 경우도 허용합니다.
            if (m_RuntimeFont == null) return; // 동적 글꼴을 만들지 않았다면 추가로 해제할 리소스가 없습니다.
            foreach (Texture2D atlas in m_RuntimeFont.atlasTextures) Destroy(atlas); // 멈춤·횡단·응답 문구를 위해 생성한 글리프 아틀라스를 해제합니다.
            Destroy(m_RuntimeFont.material); // 런타임 글꼴 전용 머티리얼을 해제합니다.
            Destroy(m_RuntimeFont); // 이 친구의 표시용 글꼴 인스턴스를 해제합니다.
        } // 기존 Animator·원본 글꼴·컨트롤러 입력은 소유하거나 해제하지 않습니다.
    } // 친구에게 멈춤 신호를 전달하는 기능을 마칩니다.
} // 자동차·신호등·횡단보도 환경 코드는 참조하지 않습니다.
