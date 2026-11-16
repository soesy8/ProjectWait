using System.Collections.Generic; // 기존 동작 관절과 새 모델 관절을 이름으로 한 번 연결합니다.
using UnityEngine; // 두 모델의 바인드 좌표를 변환하고 새 모델의 실제 뼈대에 자세를 적용합니다.

namespace Y.Crosswalk // 기존 횡단보도 상황에서 사용하는 친구의 시각 연결만 담당합니다.
{ // 상황 진행과 이동 명령은 기존 제어기에 그대로 맡깁니다.
    [DisallowMultipleComponent, DefaultExecutionOrder(380)] // 관절 연출 뒤이면서 첫 상황의 이마 접촉 판정 전에 새 모델을 맞춥니다.
    public sealed class CrosswalkFriendVisual : MonoBehaviour // 기존 보행과 연출을 M 폴더 친구의 다른 골격에 연결합니다.
    { // 원본 공유 프리팹과 기존 애니메이션 자산은 변경하지 않습니다.
        [SerializeField, InspectorName("기존 동작 기준"), Tooltip("기존 Animator와 연출 관절이 들어 있는 VisualRoot입니다.")] private Transform m_Frame; // 두 골격의 바인드 좌표를 같은 모델 공간에서 비교합니다.
        [SerializeField, InspectorName("기존 동작 스킨"), Tooltip("표시는 끈 기존 친구 스킨입니다. 원래 관절과 바인드 행렬만 읽습니다.")] private SkinnedMeshRenderer m_SourceSkin; // 기존 이동·휴대전화·상황 연출이 제어하는 골격을 유지합니다.
        [SerializeField, InspectorName("M 친구 모델"), Tooltip("씬에 배치한 Wait_Friend_Shared 프리팹의 루트입니다.")] private Transform m_Model; // 공유 모델 안의 독립 대기 Animator만 보류합니다.
        [SerializeField, InspectorName("M 친구 스킨"), Tooltip("M 친구의 실제 메시와 표정이 연결된 스킨입니다.")] private SkinnedMeshRenderer m_VisualSkin; // 새 모델의 뼈 길이와 바인드 회전을 그대로 사용합니다.
        [SerializeField, InspectorName("기존 휴대전화"), Tooltip("기존 휴대전화와 관찰 Collider를 함께 옮길 소품 루트입니다.")] private Transform m_Phone; // 소품과 조사 입력이 새 모델의 손을 함께 따라갑니다.
        [SerializeField, InspectorName("머리 부착물"), Tooltip("기존 이마 접촉 범위와 입 도형 등 새 머리 위치를 따라가야 하는 오브젝트입니다.")] private Transform[] m_HeadAttachments; // 실제 이마 접촉 판정도 화면에 표시한 모델 위치를 사용합니다.
        private sealed class BoneLink // 한 관절의 원래 바인드 좌표와 새 모델 연결을 보관합니다.
        { // 애니메이션에서 바뀐 회전량만 새 모델에 전달합니다.
            public Transform Source, Target; // 기존 연출 관절과 화면에 표시할 새 관절입니다.
            public Vector3 SourceRest, TargetRest, LocalPosition; // 골반·발의 이동 기준과 새 관절의 원래 길이를 보관합니다.
            public Quaternion RotationOffset; // 두 모델에서 서로 다른 관절 축을 보정합니다.
        } // 새 골격을 기존 골격의 로컬 회전으로 직접 덮어쓰지 않습니다.
        private sealed class Attachment // 기존 소품의 손·머리 부착 좌표를 새 관절축으로 옮깁니다.
        { // 재활성화할 때 이전 프레임의 보정이 누적되지 않게 원래 값을 보관합니다.
            public Transform Item, Target; // 실제 소품과 새 모델의 부착 관절입니다.
            public Vector3 OriginalPosition, Position; // 기존 로컬 위치와 새 관절축의 로컬 위치입니다.
            public Quaternion OriginalRotation, Rotation; // 기존 로컬 회전과 새 관절축의 로컬 회전입니다.
        } // 관찰 Collider와 소품의 부모 관계는 변경하지 않습니다.
        private readonly Dictionary<string, BoneLink> m_Links = new Dictionary<string, BoneLink>(); // 새 관절 이름으로 발과 골반 연결을 찾습니다.
        private readonly List<BoneLink> m_Ordered = new List<BoneLink>(); // 부모에서 자식 순서로 최종 회전을 적용합니다.
        private readonly List<Attachment> m_Attachments = new List<Attachment>(); // 휴대전화와 머리 부착물만 함께 이동합니다.
        private readonly List<Animator> m_PausedAnimators = new List<Animator>(); // 이 연결이 보류한 공유 모델의 대기 Animator만 복원합니다.
        private static string SourceName(string target) // M 모델의 관절 이름을 기존 연출 관절 이름에 대응시킵니다.
        { // 새 모델에는 없는 기존 손가락과 눈 관절을 억지로 생성하지 않습니다.
            switch (target) // 실제 공유 모델에 있는 몸통·팔·다리 관절만 연결합니다.
            { // 기존 Chest와 UpperChest의 합쳐진 회전은 새 chest 한 관절에 전달합니다.
                case "pelvis": return "Hips"; // 골반의 이동과 자세를 연결합니다.
                case "spine": return "Spine"; // 허리의 보행과 반동을 연결합니다.
                case "chest": return "UpperChest"; // 기존 상체 전체의 최종 회전을 전달합니다.
                case "neck": return "Neck"; // 고개를 돌리기 전의 목 자세를 연결합니다.
                case "head": return "Head"; // 신호·차량 응시와 놀람 고개를 연결합니다.
                case "shoulder.L": return "LeftShoulder"; // 왼팔의 어깨 기준을 연결합니다.
                case "upper_arm.L": return "LeftUpperArm"; // 왼팔의 휴대전화와 놀람 자세를 연결합니다.
                case "forearm.L": return "LeftLowerArm"; // 왼쪽 팔꿈치 굽힘을 연결합니다.
                case "hand.L": return "LeftHand"; // 휴대전화를 받치는 손목을 연결합니다.
                case "shoulder.R": return "RightShoulder"; // 오른팔의 어깨 기준을 연결합니다.
                case "upper_arm.R": return "RightUpperArm"; // 손짓과 균형 잡기의 오른팔을 연결합니다.
                case "forearm.R": return "RightLowerArm"; // 오른쪽 팔꿈치 굽힘을 연결합니다.
                case "hand.R": return "RightHand"; // 오른손의 방향을 연결합니다.
                case "thigh.L": return "LeftUpperLeg"; // 왼쪽 허벅지의 보행 회전을 연결합니다.
                case "shin.L": return "LeftLowerLeg"; // 왼쪽 무릎의 굽힘을 연결합니다.
                case "foot.L": return "LeftFoot"; // 왼발의 실제 발 디딤을 연결합니다.
                case "toe.L": return "LeftToes"; // 왼쪽 신발 앞부분의 회전을 연결합니다.
                case "thigh.R": return "RightUpperLeg"; // 오른쪽 허벅지의 보행 회전을 연결합니다.
                case "shin.R": return "RightLowerLeg"; // 오른쪽 무릎의 굽힘을 연결합니다.
                case "foot.R": return "RightFoot"; // 오른발의 실제 발 디딤을 연결합니다.
                case "toe.R": return "RightToes"; // 오른쪽 신발 앞부분의 회전을 연결합니다.
                default: return null; // 모델 루트처럼 기존 동작을 직접 받지 않는 관절은 유지합니다.
            } // 대응하는 이름이 없는 골격은 변경하지 않습니다.
        } // 관절 이름은 현재 프로젝트의 두 실제 모델에 한정합니다.
        private void Awake() // Animator가 첫 자세를 평가하기 전에 두 스킨의 바인드 행렬을 읽습니다.
        { // 런타임 포즈를 원본 바인드 회전으로 잘못 저장하지 않습니다.
            Transform[] sources = m_SourceSkin.bones, targets = m_VisualSkin.bones; // 두 메시가 실제로 사용하는 스킨 관절만 읽습니다.
            Matrix4x4[] sourceBinds = m_SourceSkin.sharedMesh.bindposes, targetBinds = m_VisualSkin.sharedMesh.bindposes; // 임포트된 원본 바인드 정보를 사용합니다.
            for (int i = 0; i < targets.Length; i++) // M 모델의 각 관절에 대응하는 기존 관절을 찾습니다.
            { // 원본 뼈 길이와 새 모델의 메시를 수정하지 않습니다.
                string sourceName = SourceName(targets[i].name); // 현재 관절에 필요한 기존 연출 이름을 선택합니다.
                if (sourceName == null) continue; // 이동 명령을 받지 않는 모델 루트는 건너뜁니다.
                for (int j = 0; j < sources.Length; j++) // 같은 친구 스킨의 관절 안에서만 찾습니다.
                { // 다른 캐릭터나 씬의 이름이 같은 관절을 사용하지 않습니다.
                    if (sources[j].name != sourceName) continue; // 대응하지 않는 원본 관절은 건너뜁니다.
                    Matrix4x4 source = m_Frame.worldToLocalMatrix * m_SourceSkin.transform.localToWorldMatrix * sourceBinds[j].inverse; // 기존 관절의 원래 모델 좌표를 구합니다.
                    Matrix4x4 target = m_Frame.worldToLocalMatrix * m_VisualSkin.transform.localToWorldMatrix * targetBinds[i].inverse; // 새 관절의 원래 모델 좌표를 같은 공간으로 옮깁니다.
                    BoneLink link = new BoneLink { Source = sources[j], Target = targets[i], SourceRest = source.GetColumn(3), TargetRest = target.GetColumn(3), LocalPosition = targets[i].localPosition, RotationOffset = Quaternion.Inverse(source.rotation) * target.rotation }; // 실제 바인드 차이로 관절 축 보정을 보관합니다.
                    m_Links.Add(targets[i].name, link); m_Ordered.Add(link); break; // 한 새 관절에는 한 기존 동작만 연결합니다.
                } // 원본 스킨 관절 검색을 마칩니다.
            } // 모든 바인드 기준은 첫 애니메이션 평가 전에 저장합니다.
            m_Ordered.Sort((left, right) => Depth(left.Target).CompareTo(Depth(right.Target))); // 부모 회전이 자식의 최종 회전을 덮어쓰지 않게 합니다.
            AddAttachment(m_Phone, m_Links["hand.L"]); // 기존 휴대전화 전체를 새 왼손 좌표에 연결합니다.
            foreach (Transform item in m_HeadAttachments) AddAttachment(item, m_Links["head"]); // 접촉 범위와 기존 입 도형을 새 머리 좌표에 연결합니다.
        } // 보행과 상황별 체크포인트는 기존 제어기가 계속 관리합니다.
        private static int Depth(Transform bone) // 새 모델 계층에서 부모와 자식의 적용 순서를 구합니다.
        { // 관절 수가 작은 초기 연결에서만 사용합니다.
            int depth = 0; while (bone.parent != null) { depth++; bone = bone.parent; } return depth; // 실제 부모 깊이를 반환합니다.
        } // 매 프레임 계층을 다시 검색하지 않습니다.
        private void AddAttachment(Transform item, BoneLink link) // 기존 소품의 로컬 좌표를 새 관절의 로컬 축으로 변환합니다.
        { // 선택 부착물이 없는 구성은 건너뜁니다.
            if (item == null) return; // 실제 연결된 휴대전화와 접촉 범위만 다룹니다.
            Quaternion correction = Quaternion.Inverse(link.RotationOffset); // 기존 관절축에서 새 관절축으로 가는 회전을 구합니다.
            m_Attachments.Add(new Attachment { Item = item, Target = link.Target, OriginalPosition = item.localPosition, OriginalRotation = item.localRotation, Position = correction * item.localPosition, Rotation = correction * item.localRotation }); // 복원할 원래 값과 새 부착 좌표를 함께 보관합니다.
        } // 부모를 바꾸지 않아 기존 관찰·초기화 연결을 유지합니다.
        private void OnEnable() // 공유 모델의 제자리 대기가 기존 횡단 동작을 덮어쓰지 않게 합니다.
        { // 표정과 자동 눈 깜빡임 컴포넌트는 계속 사용합니다.
            foreach (Animator animator in m_Model.GetComponentsInChildren<Animator>(true)) if (animator.enabled) { m_PausedAnimators.Add(animator); animator.enabled = false; } // 새 시각 모델의 Animator만 보류합니다.
        } // 기존 이동 Animator의 활성 상태와 재생 속도는 바꾸지 않습니다.
        private void LateUpdate() // 기존 관절 연출이 끝난 같은 프레임의 자세를 새 모델에 적용합니다.
        { // 머리 추적·플레이어 이동·상황 상태에는 개입하지 않습니다.
            foreach (BoneLink link in m_Ordered) // 새 관절의 원래 길이를 유지하면서 기존 최종 회전을 전달합니다.
            { // 부모에서 자식 순서로 적용하여 모델 축 보정이 누적되지 않게 합니다.
                link.Target.localPosition = link.LocalPosition; link.Target.rotation = link.Source.rotation * link.RotationOffset; // 바인드 축이 다른 두 골격의 최종 방향을 맞춥니다.
            } // 원본 관절의 위치나 회전은 변경하지 않습니다.
            BoneLink hips = m_Links["pelvis"]; hips.Target.position = m_Frame.TransformPoint(hips.TargetRest + m_Frame.InverseTransformPoint(hips.Source.position) - hips.SourceRest); // 새 골반 높이에서 기존 보행·놀람의 이동량만 더합니다.
            Vector3 leftFoot = FootPosition(m_Links["foot.L"]), rightFoot = FootPosition(m_Links["foot.R"]); // 기존 걸음 거리와 바닥 높이에 대응하는 새 발목 목표를 구합니다.
            float lower = Mathf.Max(RequiredLowering("L", leftFoot), RequiredLowering("R", rightFoot)); // 다리가 짧은 새 모델도 발 디딤 목표에 닿을 만큼만 골반을 낮춥니다.
            hips.Target.position -= Vector3.up * lower; // 새 뼈 길이를 늘리지 않고 보행 중 발 접지를 확보합니다.
            SolveLeg("L", leftFoot); SolveLeg("R", rightFoot); // 실제 이동 거리와 기존 발소리에 맞춰 양발을 배치합니다.
            foreach (Attachment item in m_Attachments) if (item.Item != null) item.Item.SetPositionAndRotation(item.Target.TransformPoint(item.Position), item.Target.rotation * item.Rotation); // 소품과 이마 판정을 새 모델의 최종 손·머리에 함께 맞춥니다.
        } // 관찰 정지와 재시도에서도 같은 저장된 기존 자세를 표시합니다.
        private Vector3 FootPosition(BoneLink foot) // 새 발목의 원래 높이에 기존 발 디딤 이동량을 더합니다.
        { // 다른 다리 길이 때문에 신발이 공중에 뜨거나 보폭이 이동과 달라지지 않게 합니다.
            return m_Frame.TransformPoint(foot.TargetRest + m_Frame.InverseTransformPoint(foot.Source.position) - foot.SourceRest); // 기존 거리 기반 걸음의 전후 이동과 발 들기를 그대로 사용합니다.
        } // 새 모델의 바인드 발목 높이는 유지합니다.
        private float RequiredLowering(string side, Vector3 goal) // 목표 발에 닿기 위해 필요한 골반 하강량을 계산합니다.
        { // 무릎을 반대로 꺾거나 뼈 길이를 늘리는 보정은 사용하지 않습니다.
            Transform thigh = m_Links["thigh." + side].Target, shin = m_Links["shin." + side].Target, foot = m_Links["foot." + side].Target; // 이번 다리의 실제 세 관절을 읽습니다.
            float reach = Vector3.Distance(thigh.position, shin.position) + Vector3.Distance(shin.position, foot.position) - 0.001f; // 완전히 편 무릎보다 조금 짧은 최대 도달 거리를 구합니다.
            Vector3 difference = thigh.position - goal; float horizontal = Vector3.ProjectOnPlane(difference, Vector3.up).sqrMagnitude; // 목표 발까지의 수평 거리를 따로 계산합니다.
            return Mathf.Max(0f, difference.y - Mathf.Sqrt(Mathf.Max(0f, reach * reach - horizontal))); // 필요한 경우에만 골반을 낮춥니다.
        } // 기본 대기 높이를 임의의 고정값으로 덮어쓰지 않습니다.
        private void SolveLeg(string side, Vector3 goal) // 새 모델의 두 다리뼈 길이를 유지하며 발을 기존 보행 목표에 맞춥니다.
        { // 기존 보행 위상과 연출 결과를 새로 만들지 않습니다.
            Transform thigh = m_Links["thigh." + side].Target, shin = m_Links["shin." + side].Target, foot = m_Links["foot." + side].Target; // 현재 다리의 허벅지·무릎·발목을 읽습니다.
            Quaternion footRotation = foot.rotation; Vector3 origin = thigh.position, delta = goal - origin; // 발바닥 방향과 허벅지 시작점을 보관합니다.
            float upper = Vector3.Distance(origin, shin.position), lower = Vector3.Distance(shin.position, foot.position); // 새 모델의 원래 위·아래 다리 길이를 읽습니다.
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(upper - lower) + 0.0001f, upper + lower - 0.0001f); Vector3 direction = delta.normalized; // 실제 다리로 닿을 수 있는 거리 안에서만 목표를 사용합니다.
            Vector3 bend = Vector3.ProjectOnPlane(shin.position - origin, direction); if (bend.sqrMagnitude < 0.000001f) bend = Vector3.ProjectOnPlane(m_Frame.forward, direction); // 기존 무릎의 굽힘 방향을 유지하고 일직선에서는 앞쪽으로 접습니다.
            float along = (upper * upper + distance * distance - lower * lower) / (2f * distance); Vector3 knee = origin + direction * along + bend.normalized * Mathf.Sqrt(Mathf.Max(0f, upper * upper - along * along)); // 두 다리 길이로 실제 무릎 위치를 계산합니다.
            thigh.rotation = Quaternion.FromToRotation(shin.position - origin, knee - origin) * thigh.rotation; // 허벅지를 돌려 무릎이 계산한 위치를 향하게 합니다.
            shin.rotation = Quaternion.FromToRotation(foot.position - shin.position, origin + direction * distance - shin.position) * shin.rotation; // 종아리를 돌려 발목을 목표에 맞춥니다.
            foot.rotation = footRotation; // 기존 애니메이션의 뒤꿈치·발끝 방향을 복원합니다.
        } // 접지 계산은 새 시각 골격에만 적용합니다.
        private void OnDisable() // 중단과 재활성화에서 소품 보정과 대기 Animator 상태를 정리합니다.
        { // 이 컴포넌트가 보류한 상태만 복원합니다.
            foreach (Attachment item in m_Attachments) if (item.Item != null) { item.Item.localPosition = item.OriginalPosition; item.Item.localRotation = item.OriginalRotation; } // 기존 부모 아래의 소품을 원래 로컬 좌표로 돌립니다.
            foreach (Animator animator in m_PausedAnimators) if (animator != null) animator.enabled = true; m_PausedAnimators.Clear(); // 원래 켜져 있던 새 모델의 대기 Animator만 복원합니다.
        } // 기존 친구의 상황 완료 기록과 체크포인트는 변경하지 않습니다.
    } // 기존 동작과 공유 모델의 연결을 마칩니다.
} // 횡단보도 시각 연결 범위를 마칩니다.
