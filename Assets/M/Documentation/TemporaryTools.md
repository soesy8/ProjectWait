# 임시 기능·도구 관리

2026-11-16 재개 시점의 분류다. 증거·백업을 일괄 삭제하지 않으며, 정식 씬은 `CodexTemp`에 의존하지 않는다.

## 런타임에 필요한 것 — 이름만 보고 삭제하지 않기

| 대상 | 현재 역할 / 처리 |
|---|---|
| 씬의 `PC_Test_Controls` / `ElevatorDesktopControls` | 정식 PC 조준·클릭·재시작, XR 스폰 준비·눈높이·시점 인계 소유자. 활성 유지 |
| `LobbyDissolveTest`와 물방울 렌더러·음원 풀 | 실제 로비→엘리베이터 전환 및 단독 시작 연출. 보존 |
| `StorybookSceneTransition` | 실제 씬 이동·전환 입력 차단·시점 연결. 보존 |

## 개발 환경으로 제한한 것

- 숫자 1–4 선택 우회와 엘리베이터 F8 개발 패널 바인딩: `UNITY_EDITOR || DEVELOPMENT_BUILD`에서만 등록한다.
- `ElevatorView.ToggleDeveloper`: 같은 분기에서만 패널을 연다.
- 로비 K 및 전환 F8/F9 미리보기 키: 같은 분기에서만 처리한다. 정식 포털·물방울·팝 사운드는 배포 빌드에도 유지한다.
- 에디터·Development Build에서의 진단 API와 컨텍스트 메뉴는 남긴다. 일반 PC 조작과 R 재시작·Esc 메뉴는 제거하지 않는다.

## 꺼 둔 VR 미리보기

- `VR_Preview_Test`는 비활성 + `EditorOnly`이며 모의 실행 코드는 `UNITY_EDITOR` 분기에 있다.
- `Assets/M/VRPreview/`와 `Assets/M/VRPreviewSupport~/`는 함께 보존한다. 후자는 초기 백업·사용자 상태 보호용이다.
- 이전 전체 원복 도구를 자동 실행하지 않는다. 백업 이후 실제 OpenXR 설정과 씬이 변경됐고, 원복 스크립트에 예전 다른 PC의 절대 경로도 남아 있다. 해시 보호를 우회하거나 기준을 다시 찍어 강제로 복구하지 않는다.
- Mock HMD 패키지는 설치된 상태다. 실제 제거에는 `Packages/manifest.json`, `Packages/packages-lock.json`, 연결된 `Assets/XR/` 설정의 검토·변경과 미리보기 코드 의존성 처리가 필요하다. 현재 M 전용 범위에서는 수행하지 않았다. 별도 범위 승인 전에는 배포 정리 완료로 간주하지 않는다.

## CodexTemp 분류

| 폴더 | 용도 / 실행 정책 |
|---|---|
| `ResumeWork_20261116~/` | 최신 원본 씬 검사·PC 통합 콜백 검사·캡처·인수인계 보조 자료. `ResumeValidation.cs`는 Pipeline `run_script`로 명시적으로 실행 |
| `VRUIComfort_20261113~/` | 현재 UI API 컴파일·독립 Unity 검사. `Compile.ps1`, `RunHarness.ps1` 및 `Runs/` 실행별 기록 |
| `VRImprovement_20261113~/` | 양안 기하·컨트롤러 입력 검사와 저장 씬 참조 검사. 실제 착용을 대신하지 않음 |
| `LobbyVRInput_20261113~/`, `VRDeviceCheck_20261113~/`, `VRInspection_20261112~/`, `QuestLinkDiagnostics~/`, `VRIntegration_20261113~/` | 과거 실제 기기 측정·연결·통합 증거. 보존, 최신 구현의 통과 근거로 전용하지 않음 |
| `VRStageTools~/` | 재시작 백업·기존 에디터 로그. 보호 자료 |
| `ShadowReadyPC~/`, `HeadPoseContinuity~/`, `WorldTransitionLerp~/` | 과거 전환·그림자·시점 검증 증거. 현재 코드 통과 여부는 최신 검사로 판단 |
| `TitleLayoutTools~/`, `LobbyRevision~/`, `LobbyPanelReadability~/`, `LobbyPopFade~/` | 일회성 아트 변경·검증 기록. `Setup`/`Prepare`/최적화 도구를 수정된 씬에 중복 실행하지 않음 |
| `LobbyDissolveTest~/`, `LobbyLightweightFade~/`, `LobbyLowQualityFade~/`, `LobbyMetaballUpdate~/`, `SceneTransitionConcept~/`, `SceneTransitionDroplets~/`, `SceneTransitionPreview~/`, `SkyboxGround40~/`, `SkyboxGround48~/`, `SkyboxRatio55~/` | 비교·실험 기록. 제거된 API를 사용할 수 있어 현재 버전에 그대로 실행하지 않음 |
| `EssentialChecks~/` | 현재 로컬에서는 빈 과거 검사 폴더. 예전 문서의 결과 경로가 항상 존재한다고 가정하지 않음 |

실제 폴더별 파일 수·용량 목록은 `CodexTemp/ResumeWork_20261116~/temp-inventory.json`에 기록한다. 이번에는 원본 증거·백업을 삭제하거나 GUID를 교체하지 않았다.
