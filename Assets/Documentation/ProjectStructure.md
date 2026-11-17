# Assets 폴더 안내

2026-11-17: 작성자별 `M` 폴더와 루트에 분산된 자산을 **용도별 구조**로 통합했다. 클래스/네임스페이스는 바꾸지 않았으며 Unity 자산의 기존 GUID와 meta를 보존했다.

## 어디에 저장하나요?

```text
Assets/
  Art/
    Animations/                 캐릭터 애니메이션, Animator Controller
    Fonts/                      TMP 폰트, 원본 폰트와 라이선스
    Materials/                  환경·캐릭터·UI 머티리얼
    Models/                     3D 원본과 생성 메시
    Shaders/                    셰이더와 HLSL
    Textures/                   텍스처·UI 이미지·RenderTexture
  Audio/                        음악·효과음, Lobby/ 오디오
  Prefabs/
    Audio/                      StoryMusic
    Characters/                 Elevator/, Rescuers/, Shared/
    Controllers/                양손 시각 프리팹
    Crosswalk/                  횡단보도 캐릭터·환경·교통 프리팹
    Environment/                스포츠센터 환경
    UI/                         Shared/ 일시정지 메뉴, Signage/
  Scenes/
    Lobby/Wait_Robby.unity       시작 씬
    Elevator/Wait_Elevator.unity 엘리베이터
    Crosswalk/CrosswalkScene.unity
    Office/OfficeScene.unity     B2 교무실 정전 체험
    Recovery/                   기존 복구 씬, 삭제하지 않고 보존
  Scripts/
    Lobby/                      로비 이동·선택·메뉴
    Elevator/Runtime/           현재 엘리베이터 체험
    Elevator/Shared/            설정 타입·공통 렌더링
    Crosswalk/                  Audio·Characters·Input·Rendering·Scenarios·Traffic·UI
    Crosswalk/Integration/      공통 메뉴·전환·시간 연결
    Office/                    교무실 정전 진행·입력·월드 대사·공통 UI 연결
    Characters/Shared/          공유 표정
    Shared/                     BGM·설정·페이드·FPS
    SceneTransition/            물방울 씬 전환
    Rendering/Highlight/        공통 강조 렌더링
    UI/                         Storybook/, Feedback/
    Editor/Validation/          Editor 전용 검사
    Development/VRPreview/     기존 임시 VR 코드, Editor/ 경계 유지
  Settings/
    Gameplay/                   엘리베이터 시나리오 설정
    Input/Lobby/                로비 Input Actions 및 참조
    Rendering/                  URP/Volume·씬별 렌더 설정
    XR/Management/              XR 로더·OpenXR·시뮬레이션
    XR/InteractionToolkit/      XRI 설정
    XR/CompositionLayers/       Composition Layers 설정
  ThirdParty/
    TextMeshPro/                TMP 필수 자산, Resources/ 구조 유지
    Samples/                    XRI 샘플, 버전·asmdef·라이선스 유지
    VRTemplate/                 VR 템플릿의 필요한 코드·이미지
  Documentation/
    ProjectStructure.md        현재 구조와 작업 원칙
    VRPreview.md               기존 VR 도구 안내와 경로 변경 주의
    History/                   이전 인수인계·측정 기록, 당시 내용 보존
  Development/
    Local/                     기존 CodexTemp, 로컬 검증·캡처·임시 프로젝트
    VRPreviewSupport~/         과거 VR 백업·해시 기록, 수정하지 않고 보존
```

## 실행 및 유지보수

- 현재 빌드 씬 순서는 **로비 → 엘리베이터 → 횡단보도 → 교무실 정전**이며 네 씬 모두 활성화되어 있다. 폴더 이동에 맞춰 빌드 목록과 포털·런타임 경로 문자열도 갱신했다. 교무실은 같은 날 뒤이어 B2에서 선별 통합했으며 상세 내용은 `OfficeIntegration.md`를 참고한다.
- `Art` 하위에서는 다시 `Crosswalk`, `TitleScene`, `Elevator`, `SportsCentre3F` 등 기능/출처별로 구분한다. 이름이 같다는 이유만으로 머티리얼·메시를 병합하거나 GUID를 바꾸지 않는다.
- `.meta`는 자산과 함께 이동한다. `ProjectWait.M.*`, `Y.Crosswalk`, `DwtD.TestScene` 네임스페이스는 저장된 컴포넌트·호출자 호환성을 위해 유지한다.
- `Resources` 폴더의 내부 상대 경로, `Editor` 폴더의 컴파일 경계, XRI의 asmdef, 상대 셰이더 include, `.gitattributes` LFS 규칙을 유지한다.
- Unity/XR 패키지는 설정 창·패키지 갱신 중 루트 `Assets/XR/Settings/` 같은 기본 **빈 폴더**를 다시 만들 수 있다. 실제 설정은 `Settings/XR/`에 있으며, 이를 이유로 설정 자산을 복제하거나 Packages 코드를 수정하지 않는다.
- 패키지 매니저로 Samples/TMP 필수 자산을 다시 가져오면 기본 경로에 중복 생성될 수 있다. 기존 자산과 GUID를 확인하고 중복 재임포트를 피한다.

## 과거 도구와 로컬 자료

- `Documentation/History/`와 `Development/Local/`의 과거 결과·백업은 **작성 당시 경로와 측정값**을 보존한다. 현재 실행 안내보다 우선하지 않는다.
- 과거 `Assets/M/CodexTemp/...` 자료의 물리 위치는 이제 `Assets/Development/Local/...`이다. 과거 원복/생성 스크립트 내부 경로까지 일괄 치환하지 않았다. 실행 전 현재 구조에 맞는지 확인한다.
- 기존 VR 완전 원복 매니페스트는 정리 이전 구조의 스냅샷이다. `VRPreviewTools`는 현재 씬 경로가 백업에 없으면 파일 쓰기·삭제 전에 안전하게 차단한다. 백업을 새 기준으로 위조하거나 해시 보호를 해제하지 않는다.
- 새 검증 산출물은 `Development/Local/`에 둔다. `.gitignore`에는 이동 전부터 Git이 추적하던 자료만 정확한 파일명으로 예외 처리했다. 새 캡처·캐시·임시 프로젝트를 강제 추가하지 않는다.
- 이번 이동 기록·909개 자산 GUID/메타/리소스 경로 검사는 `Development/Local/AssetOrganization1117~/`에 있다. 이전 한글 글꼴 수정과 검증 자료는 `Development/Local/Resume1117~/`에 보존했다.

## 주요 경로 이전표

| 이전 | 현재 |
|---|---|
| `M/Arts/` | `Art/` (오디오·렌더 설정·프리팹은 각 용도로 추가 분리) |
| 루트 `Materials/`, `Shaders/`, `Fonts/`, `Models/`, `Animations/` | `Art/`의 해당 종류 |
| `M/Scripts/DialoguePreview/` | `Scripts/Elevator/Runtime/` |
| `M/Scripts/TitleScene/` | `Scripts/Lobby/` |
| 기존 루트 Scripts의 횡단보도 폴더들 | `Scripts/Crosswalk/` |
| `Shared Assets/`·`Test/Scripts/` | `Scripts/Rendering/Highlight/`, `Scripts/UI/`, `Art/Shaders/Highlight/` |
| `M/Scene/` | `Scenes/Lobby/`, `Scenes/Elevator/`, `Settings/Gameplay/` |
| 루트 `XR/`, `XRI/`, `CompositionLayers/` | `Settings/XR/` |
| 루트 `TextMesh Pro/`, `Samples/`, `VRTemplateAssets/` | `ThirdParty/` |
| `M/CodexTemp/`, `M/Documentation/` | `Development/Local/`, `Documentation/History/` |

이 작업은 폴더 정리와 경로 연결 수정이다. 기능·대사·아트·네임스페이스 변경, 패키지 업데이트, Player 빌드, 커밋·푸시는 포함하지 않는다.

## 폴더 정리 시점의 확인 결과 (교무실 통합 이전)

- 기존 자산 **909개 모두 보존**, meta 변경 0개, Resources 내부 키 변경 0개.
- 기존 추적 파일 2057개 중 2039개가 새 경로에 존재하며, 나머지 18개는 제거한 빈 폴더의 meta다. 파일 데이터 손실은 없다.
- 로비·엘리베이터·횡단보도 모두 Missing Script/끊어진 Object Reference 0개, 현재 셰이더 오류 0개.
- 새 경로에서 실제 로비↔엘리베이터/횡단보도 전환, 각 메뉴, 연속 BGM 및 기존 한글 수정 확인 **19개 통과**. 과거 파괴적 원복 차단 검사도 통과했다.
- 최종 컴파일/콘솔 오류 0개. 시선 추적 기기가 연결되지 않았다는 기존 XRI 경고는 별개다.
- 사용자 승인 후 Editor를 종료하고 검증으로 자동 갱신된 동적 폰트 2개를 시작 상태로 복원했다. 기존 `TitleKorean` 수정은 그대로 유지했다.
- 현재 최상위는 위 9개 폴더다. Unity/XR가 다음 실행에서 기본 빈 폴더를 재생성할 수 있다는 점은 위 유지보수 항목을 참고한다.
- Player 빌드·실제 HMD는 이 정리 작업에서 검증하지 않았다. 현재 IL2CPP 모듈 미설치 상태를 변경하지 않았다.

Git에는 이동이 아직 스테이징되지 않아 이전 경로가 삭제(D), 새 경로가 미추적(??)으로 보일 수 있다. 이동 명세와 GUID 보존 검사 없이 이를 파일 손실로 판단하거나 전체 reset/checkout을 하지 않는다. `AGENTS.md`는 계속 로컬 전용이다.
