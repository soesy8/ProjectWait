# 로비 컨트롤러·조준 표시 보완 — 2026-11-13

## 실제 원인 확인

사용자가 Quest 2에서 로비의 손/컨트롤러와 선택 레이가 보이지 않는다고 보고했다. 로비에는 컨트롤러 몸체를 그리는 구현이 없었으며, 선택 레이는 Aim이 유효하지 않으면 비활성화하는 구조였다.

실제 기기의 `BeforeRender` 입력 문맥에서 다음을 확인했다(12:35:37):

- OpenXR 및 로비 VR 모드 활성, `appFocus=True`.
- 컨트롤러 레이아웃: `XRInputV1::Oculus::OculusTouchControllerOpenXR`.
- 오른손 `pointer`의 위치·회전은 정상, 추적 플래그 `15`(Position/Rotation/Velocity/AngularVelocity).
- `pointer.isTracked`의 형식은 `BIT`, 크기 8비트, 읽은 값은 `0.003921569`(바이트 1의 정규화 값, 1/255).
- 묶음 `PoseState.isTracked`는 내부 0.5 기준 때문에 `False`였다. 기존 코드가 이 값을 검사해 정상 Aim을 거부했다.
- 수정된 개별 컨트롤 검사에서는 `aimValid=True`, `line active=True`, `line enabled=True`였다. 실제 Aim 원점 `(0.13, 1.05, -2.44)`, 방향 `(-0.20, 0.52, 0.83)` 확인.

초기 EditorApplication.update 문맥의 읽기는 플레이 입력 버퍼와 달라 0을 반환하므로 근거로 사용하지 않았다. 진단은 `Application.onBeforeRender`에서 실제 플레이 입력 문맥을 확인한 후 수행한다.

## 수정

- `Assets/M/Scripts/TitleScene/TitlePlazaPlayer.cs`
  - 같은 오른손 OpenXR `pointer`의 개별 추적·위치·회전을 읽는다. 8비트 불리언의 1과 255 표현을 모두 처리한다.
  - 위치·회전 플래그, 유한 좌표, 유효 quaternion, 활성 장치 확인은 유지한다. 머리/Grip 레이를 선택에 대입하지 않는다.
  - 디지털 트리거 외에 같은 오른손의 아날로그 검지 트리거도 바인딩한다.
  - URP의 레거시 `Camera.stereoTargetEye` getter를 모드 래치로 사용하지 않고 명시적으로 모드 전환을 기록한다. 매 프레임 입력 복구 상태가 초기화되는 가능성과 해당 로비 경고를 제거한다.
  - 8m 거리·최근접 장애물·호버/끝점 일치·추적 복구 시 누른 트리거 자동 선택 방지는 유지한다.
- `Assets/M/Scripts/TitleScene/TitleLobbyControllerVisuals.cs`
  - Grip 추적으로 양손의 단순 크림/청록 컨트롤러 모형을 표시한다. 손가락/손 추적 구현이 아니다.
  - 오른손 Aim 레이와 끝점을 표시하며, 유효 체험판을 가리킨 끝점은 주황색으로 강조한다. 체험별 정답/가용성을 색으로 구분하지 않는다.
  - 모형의 collider는 즉시 비활성화해 선택·이동을 막지 않는다. 전환/PC 모드/추적 상실 시 표시를 숨기고, 종료 시 런타임 모형과 전용 머티리얼을 정리한다. 기존 머티리얼 자산은 변경하지 않는다.
- `Assets/M/Scripts/Editor/VRImprovementValidation.cs`에 바이트 1 추적 회귀검사를 추가했다.
- `Assets/M/Scripts/Editor/LobbyVRInputDiagnostics.cs`는 선택적 읽기 전용 진단 도구다. 이 폴더에 `enabled.txt`가 있을 때만 Play의 BeforeRender 문맥에서 `snapshot.txt`를 기록한다. 작업 후 활성화 파일은 제거했다.

## 검증 및 현재 상태

- Editor/Player API 컴파일 및 Editor 검증 도구 컴파일: 오류 없음. 기존 VRPreview `camera` 멤버 가림 경고 3개는 남아 있다.
- 독립 Unity 입력/기하 회귀검사 7개 그룹 통과: 바이트 1, 추적 상실, 트리거를 누른 채 복구 후 새 누름, 8m/장애물/끝점 포함. 모의 입력 검사와 실제 착용 조작 결과는 구분한다.
- 사용자가 착용 및 Link 실행을 확인한 뒤 수정본 Play 재실행. OpenXR `FOCUSED` 진입과 실기기 Aim/레이 활성 확인.
- 이후 Unity의 현재 씬이 `Wait_Elevator`로 전환된 것을 확인했다. 직접 조작을 시뮬레이션하거나 포털을 강제로 활성화하지 않았다. 기존 로비 전용 캡처 도구는 씬이 바뀌자 안전 가드로 캡처를 거부했다.
- Play는 계속 유지해 사용자가 직접 테스트하도록 했다. 컨트롤러 모형의 주관적 가독성·착용감, 전체 스토리 플레이 완료는 별도 확인이 필요하다.
- 모든 새 구현·검증 결과는 `Assets/M/` 내부다. 씬·Packages·ProjectSettings·Assets/XR 설정은 직접 수정하지 않았다. 기존 사용자 변경과 11월 12일 증거는 보존했다.

자료: 현재 폴더의 `snapshot.txt`, `fixed-native-aim.txt`(씬 전환 후 장치 상태), `Assets/M/CodexTemp/VRImprovement_20261113~/harness-validation.txt`. 실제 로비 입력 수치는 위 12:35:37 확인 기록이며, 지속 기록 때문에 최종 snapshot은 엘리베이터 진입 후 장치 상태다.
