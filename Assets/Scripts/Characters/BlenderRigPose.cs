using System; // 원본 관절 연결을 인스펙터에 직렬화할 때 사용합니다.
using UnityEngine; // Blender 관절의 원본 회전축으로 동작 좌표를 변환합니다.

namespace Y.Crosswalk // 현재 횡단보도 캐릭터의 동작 연결 범위입니다.
{ // 모델의 관절을 바꾸지 않고 동작의 좌표만 변환합니다.
    [DisallowMultipleComponent] // 같은 모델에 서로 다른 원본 관절 기준이 중복 연결되지 않게 합니다.
    [DefaultExecutionOrder(450)] // 기존 관절 연출에서 정한 놀람과 대사 상태를 같은 프레임의 스킨에 적용합니다.
    public sealed class BlenderRigPose : MonoBehaviour // Blender에서 전달한 바인드 회전을 보관합니다.
    { // 기존 연출이 원본 골격의 축에서 동작하도록 필요한 기준만 제공합니다.
        [Serializable] // 관절마다 다른 Blender 원본 회전축을 프리팹에 저장합니다.
        public struct BoneSpace // 한 관절과 그 부모의 원본 모델 좌표입니다.
        { // 실행 중의 포즈를 원본 바인드 회전으로 잘못 저장하지 않도록 명시적인 값을 사용합니다.
            [InspectorName("원본 관절"), Tooltip("Blender 원본에서 전달한 실제 관절입니다.")] // 원본 관절 참조의 의미를 설명합니다.
            public Transform Bone; // 동작을 적용할 실제 Blender 관절입니다.
            [InspectorName("관절 바인드 회전"), Tooltip("모델 좌표에서 읽은 Blender 원본 관절의 바인드 회전입니다.")] // 원본 관절축을 설명합니다.
            public Quaternion Bind; // 원본 관절의 모델 기준 회전입니다.
            [InspectorName("부모 바인드 회전"), Tooltip("모델 좌표에서 읽은 Blender 원본 부모 관절의 바인드 회전입니다.")] // 로컬 회전 변환의 기준을 설명합니다.
            public Quaternion ParentBind; // 원본 부모 관절의 모델 기준 회전입니다.
        } // 한 관절의 원본 좌표 정의를 마칩니다.
        [SerializeField, InspectorName("Blender 원본 관절 좌표"), Tooltip("원본 골격을 수정하지 않고 동작의 회전 좌표만 변환하는 관절별 기준입니다.")] // 모델 적용 시 저장하는 원본 정보를 안내합니다.
        private BoneSpace[] m_BoneSpaces = Array.Empty<BoneSpace>(); // 원본에서 내보낸 실제 관절들의 회전 기준입니다.
        [SerializeField, InspectorName("원본 캐릭터 렌더러"), Tooltip("Blender 원본의 스킨과 눈·입 블렌드셰이프가 연결된 렌더러입니다.")] // 임시 표정 도형을 원본 표정으로 연결할 대상을 안내합니다.
        private SkinnedMeshRenderer m_Renderer; // 원본 메시의 실제 표정 가중치를 변경합니다.
        private bool m_Surprised, m_FaceApplied; // 현재 놀람과 이전 프레임의 표정 적용 여부를 따로 기록합니다.
        private float m_SpeechStart, m_SpeechEnd, m_SmileEnd; // 말하는 시간과 성공 미소를 유지할 시간을 독립적으로 보관합니다.
        private static bool TrySpace(Transform bone, out BoneSpace space) // 연결된 원본 골격에서 대상 관절의 기준을 찾습니다.
        { // 원본 정보를 가진 모델에만 좌표 변환을 적용합니다.
            BlenderRigPose rig = bone != null ? bone.GetComponentInParent<BlenderRigPose>() : null; // 해당 관절이 속한 모델의 원본 좌표를 읽습니다.
            if (rig != null) foreach (BoneSpace item in rig.m_BoneSpaces) if (item.Bone == bone) { space = item; return true; } // 같은 이름의 다른 친구 관절과 혼동하지 않고 실제 참조로 찾습니다.
            space = default; return false; // 원본 좌표가 없는 대상은 현재 동작을 그대로 사용합니다.
        } // 관절 좌표 조회를 마칩니다.
        public static Quaternion LocalRotation(Transform bone, Quaternion rotation) // 모델 축으로 작성된 연출을 원본 관절의 로컬 회전으로 옮깁니다.
        { // 원본 바인드 회전과 관절 길이를 변경하지 않습니다.
            return TrySpace(bone, out BoneSpace space) ? Quaternion.Inverse(space.ParentBind) * rotation * space.Bind : rotation; // 부모와 자식의 서로 다른 원본 회전축을 함께 반영합니다.
        } // 절대 자세의 좌표 변환을 마칩니다.
        public static Quaternion DeltaRotation(Transform bone, Quaternion rotation) // 현재 애니메이션 위에 더할 회전의 좌표만 변환합니다.
        { // Animator가 만든 현재 자세를 보존한 채 같은 모델 방향으로 연출합니다.
            return TrySpace(bone, out BoneSpace space) ? Quaternion.Inverse(space.Bind) * rotation * space.Bind : rotation; // 원본 관절의 축으로 회전량을 전달합니다.
        } // 추가 회전의 좌표 변환을 마칩니다.
        public static Vector3 ModelOffset(Transform bone, Vector3 offset) // 모델 축의 접지 오프셋을 원본 관절의 로컬 축으로 옮깁니다.
        { // 원본 발의 기울어진 바인드 축 때문에 밑창 기준이 달라지지 않게 합니다.
            return TrySpace(bone, out BoneSpace space) ? Quaternion.Inverse(space.Bind) * offset : offset; // 위치 계산의 기준만 변환하고 뼈 길이는 바꾸지 않습니다.
        } // 원본 관절의 오프셋 변환을 마칩니다.
        public static Vector3 LocalScale(Transform bone, Vector3 scale) // 모델 축으로 작성한 눈 표정 크기를 원본 눈 관절축에 대응시킵니다.
        { // 원본 눈의 높이 대신 두께가 변하는 축 오류를 방지합니다.
            if (!TrySpace(bone, out BoneSpace space)) return scale; // 원본 관절 정보가 없는 기존 대상은 같은 값을 사용합니다.
            return new Vector3(Vector3.Scale(space.Bind * Vector3.right, scale).magnitude, Vector3.Scale(space.Bind * Vector3.up, scale).magnitude, Vector3.Scale(space.Bind * Vector3.forward, scale).magnitude); // 원본 눈 관절의 각 축에 해당하는 실제 배율을 반환합니다.
        } // 눈 표정 크기의 축 변환을 마칩니다.
        public static void SetSurprise(Transform bone, GameObject legacyMouth, bool active) // 기존 위험 반응을 꺾인 눈과 둥근 입에 연결합니다.
        { // 기존 입 도형은 원본 표정 채널이 없는 구성에서만 화면에 나타납니다.
            BlenderRigPose rig = FindFace(bone); // 같은 친구의 얼굴 스킨을 찾습니다.
            bool hasSurprise = rig != null && (rig.m_Renderer.sharedMesh.GetBlendShapeIndex("Surprise") >= 0 || rig.m_Renderer.sharedMesh.GetBlendShapeIndex("Surprised") >= 0); // 기존 모델과 M 공유 모델의 실제 놀람 표정 이름을 함께 지원합니다.
            if (legacyMouth != null && legacyMouth.activeSelf != (active && !hasSurprise)) legacyMouth.SetActive(active && !hasSurprise); // 같은 얼굴 위에 이전 입 도형과 원본 입이 겹치지 않게 합니다.
            if (rig != null) rig.m_Surprised = active; // 깜빡임이나 웃음보다 놀라는 순간의 표정을 우선합니다.
        } // 상황에서 정한 놀람 상태를 스킨에 전달합니다.

        public static void PlayFace(Transform bone, float speechSeconds, float smileSeconds = 0f) // 말풍선이 시작되는 순간에 말하기와 미소의 길이를 설정합니다.
        { // 같은 친구의 눈과 입만 움직이며 대사나 상황 진행 시간을 바꾸지 않습니다.
            BlenderRigPose rig = FindFace(bone); // 말하거나 웃는 친구의 스킨만 선택합니다.
            if (rig == null) return; // 얼굴 렌더러가 제거되었으면 남은 상황 연출을 그대로 이어갑니다.
            rig.m_SpeechStart = Time.unscaledTime; // 일시 정지된 관찰 캐릭터도 대사 시간에 맞게 표정을 움직입니다.
            rig.m_SpeechEnd = rig.m_SpeechStart + speechSeconds; // 말하는 동안에만 입 모양과 깜빡임을 바꿉니다.
            rig.m_SmileEnd = rig.m_SpeechStart + smileSeconds; // 성공 대사가 끝나도 정해진 시간 동안 웃는 얼굴을 이어갑니다.
        } // 소리가 없는 기존 텍스트 대사에도 같은 타이밍을 사용합니다.

        public static void ResetFace(Transform bone) // 재시도나 대사 취소에서 이전 얼굴을 남기지 않습니다.
        { // 소속 친구의 얼굴에만 초기화 요청을 보냅니다.
            BlenderRigPose rig = FindFace(bone); // 씬 종료 중에도 살아 있는 원본 얼굴만 찾습니다.
            if (rig != null) rig.ClearFace(); // 이전 대사와 성공·놀람 상태를 함께 정리합니다.
        } // 원본 캐릭터 위치와 관절 애니메이션은 계속 유지합니다.

        private static BlenderRigPose FindFace(Transform bone) // 머리 관절로부터 원본 얼굴의 소유자를 찾습니다.
        { // 두 친구가 같은 스크립트를 사용하더라도 각자 다른 렌더러를 사용합니다.
            BlenderRigPose rig = bone != null ? bone.GetComponentInParent<BlenderRigPose>() : null; // 현재 머리가 소속된 캐릭터를 찾습니다.
            return rig != null && rig.m_Renderer != null && rig.m_Renderer.sharedMesh != null ? rig : null; // 유효한 스킨 메시를 가진 캐릭터만 반환합니다.
        } // 오브젝트 이름으로 다른 친구의 얼굴을 찾지 않습니다.

        private void LateUpdate() { ApplyFace(); } // 관절 연출 뒤 현재 얼굴 상태의 최종 가중치를 적용합니다.

        private void ApplyFace() // 현재 얼굴 상태를 실제 스킨 가중치로 변환합니다.
        { // 각 눈이나 입에 동시에 반대되는 두 표정이 가득 겹치지 않도록 보간합니다.
            float now = Time.unscaledTime; // 말풍선이 사용하는 시간과 같은 기준으로 계산합니다.
            bool speaking = !m_Surprised && now < m_SpeechEnd; // 놀라는 순간에는 말하는 입을 잠깐 멈춥니다.
            bool smiling = !m_Surprised && now < m_SmileEnd; // 놀람이 없을 때만 성공 미소를 표시합니다.
            bool active = speaking || smiling || m_Surprised; // 어느 표정도 없는 프레임은 기본 얼굴을 사용합니다.
            if (!m_FaceApplied && !active) return; // 기본 얼굴을 매 프레임 다시 쓰지 않습니다.
            m_FaceApplied = active; // 마지막 활성 프레임 다음에만 가중치가 한 번 초기화됩니다.
            if (m_Renderer == null || m_Renderer.sharedMesh == null) return; // 씬 해제 중에는 없는 스킨에 표정을 쓰지 않습니다.
            float elapsed = now - m_SpeechStart; // 대사가 시작된 시점부터의 박자를 계산합니다.
            float blinkTime = Mathf.Repeat(elapsed + 1.30f, 1.55f); // 짧은 대사에서도 첫 깜빡임이 나오고 긴 대사에서는 간격을 둡니다.
            float blink = speaking ? 1f - Mathf.Clamp01(Mathf.Abs(blinkTime - 0.075f) / 0.075f) : 0f; // 눈을 짧게 접었다가 원래 표정으로 복원합니다.
            float syllable = Mathf.Repeat(elapsed * 4.7f, 1f); // 말하는 동안 입이 벌어졌다 다물리는 한 음절의 진행도를 구합니다.
            float emphasis = Mathf.Sin(elapsed * 9f); // 음절 박자와 다른 속도로 강약을 바꿔 같은 입 모양이 반복되지 않게 합니다.
            float closed = speaking ? 1f - Mathf.Sin(syllable * Mathf.PI) * (0.65f + 0.35f * emphasis * emphasis) : 0f; // 열고 닫는 박자에 강약을 섞어 현재 입이 다물린 정도를 구합니다.
            SetWeight("Blink", blink); // 두 눈의 기존 깜빡임 키를 같은 박자로 적용합니다.
            SetWeight("SurpriseEyes", m_Surprised ? 1f : 0f); // 두 눈을 코 쪽을 향한 꺾인 선으로 모읍니다.
            SetWeight("SmileEyes", smiling ? 1f - blink : 0f); // 깜빡이는 짧은 순간을 제외하고 위로 휜 눈웃음을 유지합니다.
            SetWeight("Surprise", m_Surprised ? 1f : 0f); // 놀라는 동안 입을 둥글게 벌립니다.
            SetWeight("Surprised", m_Surprised ? 1f : 0f); // M 공유 친구도 기존 사고·놀람 상태에 같은 순간 반응합니다.
            SetWeight("Smile", smiling ? 0.75f : 0f); // M 공유 친구의 미소를 기존 성공·감사 대사에 연결합니다.
            SetWeight("Happy", smiling ? 1f - closed : 0f); // 웃는 대사의 열린 입과 대사 뒤 미소를 표현합니다.
            SetWeight("SmileClosed", smiling ? closed : 0f); // 웃으며 말할 때 입꼬리를 유지한 채 입을 다뭅니다.
            SetWeight("Worried", smiling ? 0f : closed); // 일반 대사에서는 기존 다문 입 키를 말 사이의 입 모양으로 사용합니다.
        } // 최종 표정은 기존 캐릭터 메시 하나에만 적용합니다.

        private void SetWeight(string shape, float weight) // 이름으로 현재 모델의 표정 채널을 찾습니다.
        { // 모델을 다시 내보낼 때 채널 순서가 달라져도 같은 눈과 입을 사용합니다.
            int index = m_Renderer.sharedMesh.GetBlendShapeIndex(shape); // 현재 스킨에서 요청한 표정의 실제 번호를 읽습니다.
            if (index >= 0) m_Renderer.SetBlendShapeWeight(index, weight * 100f); // Unity가 읽는 0~100 가중치로 변환해 원본 표정에 반영합니다.
        } // 없는 채널에는 가중치를 쓰지 않습니다.

        private void ClearFace() // 완료 전 취소되거나 오브젝트가 꺼질 때 표정을 정리합니다.
        { // 정해진 길이보다 일찍 끝난 대사가 다음 상황에 이어지지 않게 합니다.
            m_Surprised = false; // 이전 위험 반응을 해제합니다.
            m_SpeechEnd = m_SmileEnd = 0f; // 말하는 입과 성공 미소의 재생을 모두 끝냅니다.
            ApplyFace(); // 다음 프레임을 기다리지 않고 마지막 표정 가중치를 기본 얼굴로 돌립니다.
        } // 현재 얼굴의 상태만 정리합니다.
        private void OnDisable() { ClearFace(); } // 캐릭터가 다시 켜질 때 이전 입과 눈 모양이 남지 않게 합니다.
    } // 원본 관절 좌표 연결을 마칩니다.
} // 횡단보도 모델 좌표 범위를 마칩니다.
