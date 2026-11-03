# ‘나의 생각’ 제목/리본 잔존 수정 — 2026-11-03

## 조사 범위와 보존

- 현재 열린 `Assets/M/Scene/Elevator_Safety_Prototype.unity`의 계층/참조는 `Inspect.cs`로 **읽기 전용 조회**했다. 씬 저장, 교체, dirtiness 해제는 하지 않았다.
- 최초 조회에서 `isDirty=true`였으므로 다른 씬을 열지 않았다. 테스트는 현재 씬의 Play Mode 복사본에서만 실행하고 종료했다. 종료 후에도 같은 씬, `isDirty=true` 상태가 유지된다.
- 두 M 씬 파일과 공용 `ThoughtCloud.prefab`, `ObservationCard.prefab`, M/공용 폰트 파일의 SHA256이 요청 전후 동일하다. 기존 사용자 변경도 보존했다. 이번 요청에서 해당 자산을 수정하거나 되돌리지 않았다.
- 원본 공용 ThoughtCloud에는 ‘나의 생각’ 제목이 남아 있다. 공용 자산을 바꾸는 대신 M 런타임 인스턴스에서 확실히 숨긴다.

## 원인과 재현

- `ReadOnlyInspectionBefore.json`: 레거시 ThoughtHeading/ThoughtRibbon은 이미 비활성인데, 참조된 공용 프리팹의 `ThoughtCloud/Heading`은 ‘나의 생각’으로 활성 상태였다.
- `ReproducedBefore.json`: 엘리베이터 내부 관리실 통화 → 상황 보고 → 식별판 검색 → 관찰 hover 진입/종료 때, `baseThought`로 복귀하면서 실제 ‘나의 생각’과 리본이 다시 켜지는 것을 재현했다.
- 직전 구현은 관찰 모드만 제목을 숨기고, 미션/힌트 모드는 의도적으로 제목을 다시 켜고 있었다. `ElevatorSpatialUI.UpdateThought`, 프리팹 레이아웃, 프리팹 없는 fallback 모두 같은 불일치가 있었다.
- 일반 이름의 이전 전환 복제 텍스트도 이름만으로는 누락될 수 있어, 정확한 제목 문자열 변형까지 말풍선 범위 안에서 구분한다.

## 수정

- 새 `Assets/M/Scripts/Elevator/ElevatorThoughtHeaderPolicy.cs`에 생각/힌트/관찰의 **본문 전용 표시 정책**을 모았다.
- ThoughtHeading/Heading, ThoughtRibbon, 정확한 ‘나의 생각’/‘나의 생각 · 힌트’/‘살펴보니…’ 제목 변형을 말풍선 내부에서만 비활성화하고 제목 문자열을 비운다.
- `ElevatorSpatialUI`는 기본 힌트 복귀, 같은 값의 ShowThought 조기 반환, Layout, Tick 경로에서도 제목을 다시 켜지 않는다.
- `ElevatorStorybookUI`의 prefab/fallback 양쪽에서 제목과 리본 재활성화를 제거했다. 제목용 상단 빈 공간을 줄이되 실제 설명 본문과 구름 아트는 유지한다.
- `ElevatorUITransitions`는 생각 말풍선 제목의 추적/복제를 차단하고, 이전에 추적한 텍스트가 제목으로 바뀐 경우에도 원본과 outgoing copy를 정리한다.
- 정책 범위는 PlayerThought뿐이다. 관리실 화자 리본은 같은 `thoughtRibbon` 스프라이트를 사용해도 유지하고, 일시정지 제목도 건드리지 않는다.
- 실제 설명 문장에 ‘생각’이라는 단어가 들어 있다는 이유만으로 제거하지 않는다. 정확한 제목 문자열 또는 헤더 오브젝트만 대상으로 한다.
- 카메라/시나리오/입력/씬/공용 프리팹/셰이더/스카이박스는 변경하지 않았다. 전환은 이전의 alpha-only를 유지한다.

## 검증

현재 씬에서 **617/617 체크 통과**:

| 검사 | 통과 |
|---|---:|
| 내부 단계·힌트/관찰 전환·제목 재활성화/복제·fallback·정책 범위 | 144 |
| alpha-only/잔여 UI/스카이박스 회귀 | 41 |
| 가독성/줄바꿈/선택지 회귀 | 220 |
| 이전 polish 회귀 | 54 |
| 시점·입력 잠금 회귀 | 126 |
| 개별 강조·거리 무관 선택 회귀 | 32 |

- 내부 탑승, 고장, 식별판 찾기, 지연 힌트, 6종 오브젝트 Inspect/Clear, 대기, 구조, 일반/오브젝트 가이드 설정을 확인했다.
- 제목/리본을 테스트에서 고의로 다시 켠 뒤 동일값 갱신/Layout/Tick으로 숨겨지는지 확인했다.
- 공용 프리팹 없는 fallback 및 storybook 없는 경로, generic TransitionText 제목, 이미 추적된 텍스트의 제목 변환도 확인했다.
- 관리실 화자 이름/리본, ‘일시 정지’ 제목, 본문 내용과 overflow, UI 위치 고정, 기존 상호작용은 유지된다.
- 이전 polish 검사 중 “미션 제목은 유지” 기대값 한 개만 새 정책인 “미션 복귀도 제목 없음”으로 변경했다. 나머지 기존 기대값은 유지했다.
- 컴파일·콘솔 오류 없음. Play Mode 종료. 실물 HMD는 미검증이다.
- `FinalSummary.txt`, `ThoughtHeaderVerification.json`, 각 회귀 JSON, `Cabin_ThoughtBodyOnly.png`, `CompletionCheck.txt`가 결과다.

이 폴더의 도구/스냅샷은 런타임이 참조하지 않는다.