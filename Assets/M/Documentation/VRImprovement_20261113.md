# VR 개선 적용 기록 — 2026-11-13

기준: `E:\ProjectWait\ProjectWait_VR_Improvement_Plan_20261112.md`.

**상태: 구현 적용. 실제 헤드셋 착용 및 전체 시나리오 재검증은 미완료.**

## 적용 내용

- 로비 오른손 입력을 OpenXR `pointer` Pose(`/input/aim/pose`)로 변경했다. 위치와 회전에 같은 Pose를 사용하며, 기기별 고정 각도 보정이나 머리/Grip 대체 레이를 사용하지 않는다.
- 로비 선택 거리를 코드와 `Wait_Robby.unity` 모두 **8m**로 맞췄다. 표시 끝점·호버·선택이 같은 최근접 충돌 결과를 사용한다. 장애물을 통과하지 않는다.
- 조준 추적이 끊기면 레이와 호버를 해제한다. 추적 복구/장면 전환 직후 이미 누르고 있던 트리거로 자동 선택하지 않는다.
- 런타임 `StereoUISafeArea`에서 실제 좌·우 view/projection을 읽는다. 텍스트는 공통 가시영역과 약 6% 안전 여백을, 검은 배경은 두 눈 전체를 덮는 합집합을 사용한다. 모니터 종횡비나 미러 크롭을 안전영역으로 사용하지 않는다.
- 가이드·상태·생각 카드·대화·선택지·자막·오브젝트 표시는 VR에서 상위 표시 배율 **0.9를 한 번만** 적용한다. 전역 폰트 크기와 PC 배율은 유지했다. 3D 소품·캐릭터 크기는 변경하지 않았다.
- 가이드는 양안 안전영역 우측 상단, 상태 카드는 우측 하단에 둔다. 상태 카드 아래에는 자막 공간을 남긴다.
- 이름표·본문·장식·선택지의 실제 RectTransform 모서리를 묶어 검사한다. 1.4m 읽기 거리를 유지하고 두 눈에 들어오는 후보 중 얼굴·비상벨·층 버튼·가이드와의 겹침을 줄이는 위치를 고른다. 이후 VR 대화의 위치와 회전은 월드 고정이다.
- XR 시작/재연결·투영/IPD 변경 시 다시 배치한다. 머리 회전이나 모니터 크기 변화만으로 VR 대화를 다시 배치하지 않는다. 유효하지 않은 XR 투영에서는 임의의 모니터 배치로 확정하지 않는다.
- VR에서는 렌더 직전에도 상단 레터박스를 비활성화한다. 하단은 기존 82/720 높이 비율을 기준으로 두 눈을 덮으며, 상단을 없앤 만큼 늘리지 않는다.
- 자막 폭·줄바꿈·위치는 하단 배경과 별도로 계산한다. 여러 줄 자막에는 글자 뒤의 작은 검은 가독성 배경을 제공하며, 전체 하단 띠 높이를 늘리거나 글자를 자동 축소하지 않는다. 기존 전체 암전은 유지한다.
- PC 대화 배치·글자 크기·상하단 레터박스와 게임 대사·진행·판정 코드는 유지했다.

## 변경 파일

- `Assets/M/Scene/Wait_Robby.unity`
- `Assets/M/Scripts/TitleScene/TitlePlazaPlayer.cs`
- `Assets/M/Scripts/DialoguePreview/StereoUISafeArea.cs` — 신규
- `Assets/M/Scripts/DialoguePreview/ElevatorUIPlacement.cs`
- `Assets/M/Scripts/DialoguePreview/ElevatorSpatialUI.cs`
- `Assets/M/Scripts/DialoguePreview/ElevatorTutorialPreview.cs`
- `Assets/M/Scripts/DialoguePreview/ElevatorCinematicOverlay.cs`
- `Assets/M/Scripts/Editor/VRImprovementValidation.cs` — 신규 검증 도구
- 이 기록 및 `Assets/M/CodexTemp/VRImprovement_20261113~/`의 임시 검증 도구/결과

기존 폰트 자산과 `Assets/XR/Settings/OpenXRPackageSettings.asset`의 사용자 변경은 수정/복구하지 않았다. `Packages/`, `ProjectSettings/` 원본은 수정하지 않았다. 임시 독립 Unity 검증 프로젝트는 `CodexTemp` 안에만 생성했다.

## 검증 범위와 자료

자료: `Assets/M/CodexTemp/VRImprovement_20261113~/`.

확인 결과: **독립 Unity Play Mode 검사 7개 그룹 통과**, 관련 씬 컴포넌트 8개의 정적 참조 검사 통과, 런타임/에디터 도구 C# API 컴파일 오류 없음. 에디터 분기에는 기존 VRPreview의 `camera` 멤버 이름 경고 3건이 남아 있다.

- `editor-api/compile.log`, `player-api/compile.log`: 현재 프로젝트 Unity 참조를 사용한 C# 컴파일 검사. Player 검사는 `UNITY_EDITOR` 분기를 제외한 **API 컴파일**이며, Android/Quest APK 빌드 성공을 뜻하지 않는다.
- `editor-tests/compile.log`: 에디터 검증 도구 컴파일.
- `scene-reference-check.json`: 저장된 두 M 씬의 관련 컴포넌트 fileID/GUID 해석, 로비 8m, 대화 1.4m, 대사/진행/전역 타이포그래피 불변 검사. 정적 검사이며 실행 중 참조 검사를 대신하지 않는다.
- `harness-validation.txt`: 독립 Unity Play Mode에서 실제 안전영역/로비 코드의 기하·입력·충돌 검사. 장면 전환 협력 객체만 무동작 테스트 대역이다. 실제 헤드셋이나 전체 프로젝트 플레이 결과가 아니다.
- 양안 기하 검사는 대칭/비대칭/기울어진 눈 투영, 11월 12일 기록된 Quest 투영값, 뒤/near/far 후보 제외, 가이드·상태 카드 모서리, 검은 배경 가로/하단 커버리지를 포함한다.
- 현재 M 원본 씬을 저장/전환하지 않고 검증했다. 새 실제 양안 렌더 캡처는 확보하지 못했다. 기존 `VRInspection_20261112~`는 그대로 보존했다.

일반 프로젝트 에디터에서 추가 정적/PC 검사를 하려면 Play Mode 밖에서 **Tools > Wait > M > Validate VR Improvement**를 실행한다. 이 메뉴는 씬을 저장하지 않으며 결과는 위 M 임시 폴더에만 기록한다. 입력 테스트는 별도 Play Mode 검증에서 수행한다.

## 실제 기기에서 남은 확인

- [ ] 시작 위치에서 세 로비 패널을 Aim 방향으로 자연스럽게 선택한다.
- [ ] 실제 컨트롤러 추적 끊김/재연결, 트리거 누른 채 복구를 확인한다.
- [ ] VR 가이드·상태·이름표·본문·모든 선택지와 긴 자막이 양쪽 눈에서 잘리지 않는다.
- [ ] 90% 한글/아이콘 크기의 실제 가독성과 선택 조작성을 확인한다.
- [ ] 하단 띠·자막·상태 카드가 연출 전환 중 겹치지 않고, 상단 띠는 순간적으로도 나타나지 않는다.
- [ ] 고개 회전·숙이기·앉은 자세·재센터링과 재연결 시 안정성을 확인한다.
- [ ] 실제 하단 띠 높이의 답답함과 자막 가독성을 확인한다.
- [ ] PC 및 VR 전체 시나리오의 진행·선택·장면 전환·전체 암전을 플레이로 재검증한다.

월드 고정 대화는 처음 표시/의도적 재배치 시점에만 양안 가시성을 보장하도록 계산한다. 이후 사용자가 고개를 돌려 시야 밖으로 나간 것은 실패로 판정하지 않는다. 비정상적으로 좁은 투영이나 매우 긴 문장으로 고정 90% 묶음이 들어가지 못하면 추가 축소/거리 증가를 하지 않으며, `StereoPlacementFits` 진단과 실제 착용 결과를 보고 레이아웃을 조정해야 한다.
