공 받기 → 안전 포스터 지목 → 친구 점프 → 고장 컷씬 / 2026-11-04

적용 씬:
Assets/M/Scene/Elevator_Safety_Prototype.unity
Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity

변경 동작:
1. 목적지 선택 후 친구 또는 농구공을 선택해 공을 받으면 즉시 컷씬에 들어갑니다.
2. 친구가 ‘아까 내 슛, 멋있었지? 내가 어떻게 넣었는지 보여 줄게!’라고 말합니다.
3. 5.2초 뒤 안전 포스터 지목을 기다립니다. 시간만 지나서는 진행하지 않습니다.
4. 상하 레터박스와 마우스 시점 조절은 유지합니다. 이동·클릭·숫자 선택은 차단합니다.
5. 별도의 Screen Space Overlay 캔버스에서 화살표가 포스터의 화면 방향을 가리킵니다.
   포스터가 화면 밖 또는 뒤에 있어도 화살표가 화면 가장자리에서 방향을 안내합니다.
6. World Space 캔버스의 원은 포스터 앞에 고정되며 플레이어를 바라봅니다.
   1.5초 주기로 100% → 120% → 100% 크기를 반복합니다. 안쪽 호는 유지 진행도입니다.
7. PC 중앙 레이저를 원 안에 1.5초 연속 유지하면 이어집니다. 벗어나면 초기화합니다.
   원 앞에 장애물이 있으면 지목으로 인정하지 않습니다. 일시정지는 진행 시간을 멈춥니다.
8. XR은 현재 프로젝트의 Poke 및 입력 기반 손·컨트롤러 인터랙터 위치를 사용합니다.
   손 또는 컨트롤러가 원의 면에 닿으면 이어지며, 머리의 Gaze 인터랙터는 제외합니다.
9. 플레이어가 ‘여기 봐, 안에서 뛰면 안 된대. 위험하니까 하지 마!’라고 말합니다(3.2초).
10. 친구가 ‘에이, 한 번만! 잘 봐.’라고 말한 뒤 슛을 흉내 내며 한 번 점프합니다.
    점프 전 친구 방향을 짧게 안내하지만 플레이어의 시점을 강제로 돌리지 않습니다.
11. 덜컹 소리·감쇠 흔들림·놀란 표정과 ‘어? 왜 멈췄지?’ 자막을 보여준 뒤
    기존 비상통화·위치 전달·친구 안심·두 구조요원 동행 흐름으로 돌아갑니다.

검증:
- Prototype: 실제 PC Play Mode 입력·상태·표시 44개 PASS.
- ObjectInteraction_Test: 실제 PC Play Mode 입력·상태·표시 44개 PASS.
- SavedValidation.json: 저장된 두 씬 및 일반/무응답 통화의 전체 상태 전환 44개 PASS.
- 재생 중 새 런타임 예외와 렌더링 오류는 두 씬 모두 0개입니다.
- 실제 마우스·키보드 입력, 시선 이탈, 일시정지, 1.5초 유지, 20% 크기 변화,
  원·화살표의 실제 UI 메시, 플레이어 접지, 친구의 렌더 프레임 점프와 고장을 확인했습니다.
- 1920×1080 실제 화면에서 자막 한글·외곽선·팔레트·원과 화살표를 확인했습니다.
- 최신 B 원본의 Unity 키(1.4785m), 기존 두 구조요원 참조와 임시 경로 미의존을 확인했습니다.
- 물리 HMD 착용, 손 접촉의 실제 기기 검증 및 기기 빌드는 진행하지 않았습니다.

이미지: 각 씬 이름 뒤 01_Story / 02_FindPoster / 03_Pointing / 04_PlayerWarning /
05_FriendJump / 06_Fault.png. 최종 캡처와 PlayValidation.json을 함께 보관합니다.
Before/에는 이번 작업 직전 코드·씬·설정 사본을 보관했습니다.
ApplyResult.json에는 첫 적용 실패 기록이 남아 있습니다. Ensure<T>에서 Unity 객체의
null 처리를 수정한 뒤 재적용했으며, 최종 저장·재생 검증은 모두 통과했습니다.
DebugVisual.cs는 원·화살표의 CanvasRenderer 누락을 확인하기 위한 임시 조회 도구입니다.

구현: Scripts/Elevator/ElevatorPosterCutscene.cs, ElevatorPosterCueGraphic.cs와 기존 흐름·입력·자막.
편집 도구: Tools/Wait/Elevator/Apply poster pointing cutscene to both scenes.
빠른 확인: Play → F8 → ‘슛 이야기·포스터’. 처음부터는 호출 → 1층 → 공 받기로 확인합니다.
런타임은 CodexTemp에 의존하지 않습니다. 모든 신규 구현·씬 변경·검증 자료는 M 안에 있습니다.
Assets/XR/Settings/OpenXRPackageSettings.asset의 기존 사용자 변경은 보존했습니다.
프로토타입 씬을 편집 상태로 복원했습니다.
