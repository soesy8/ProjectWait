# 엘리베이터 체험

## 실행·빌드 씬

`Assets/M/Scene/Wait_Elevator.unity`

- 대사 개선 테스트 씬을 승격한 메인 씬입니다. Build Profiles의 공유 씬 목록에 이 씬만 활성화되어 있으며 시작 인덱스는 0입니다.
- PC: 마우스 둘러보기, 좌클릭 선택, WASD 이동, Esc 일시정지, R 재시작.
- XR: 레이로 가리킨 뒤 **검지 트리거**로 물체와 UI를 선택합니다. 왼손 스틱 이동, 메뉴/B 일시정지입니다.
- 높이: 착용 감지 후 안정된 자세에서 보정하며, 메뉴의 **높이 다시 맞추기**로 재보정합니다. 착용 감지 미지원 기기는 메뉴의 **정면 보고 높이 맞추기**로 확인합니다.
- 진행: 도입 → 탑승·공 받기 → 비상통화 → 식별판 확인·위치 전달 → 안전 포스터·친구 대답 → 구조·귀가.
- 식별판 찾기 중 포스터·시설 안내를 선택하면 설명 카드가 열리며, 측면 비상벨을 가리키거나 선택하면 전면 식별판 위치를 안내합니다.
- XR 충돌은 HMD 수평 위치를 캡슐에 반영하고 머리 이동 경로를 검사합니다. 실제 HMD 착용감·양안 표현은 별도 검증이 필요합니다.

## 유지 구조

| 경로 | 용도 |
|---|---|
| `Assets/M/Scene/` | 유일한 체험 씬 |
| `Assets/M/Scene/Elevator_Dialogue_Preview_Settings.asset` | 메인 씬 설정 자산 |
| `Assets/M/Scripts/DialoguePreview/` | 현재 체험 런타임. 승격 전 폴더·네임스페이스를 유지하여 GUID와 타입 참조를 보존 |
| `Assets/M/Scripts/Elevator/` | 공통 설정 타입·최적화 외곽선 피처 |
| `Assets/M/Arts/`, `Prefabs/`, `UI/` | 현재 씬에서 사용하는 아트·UI·모델 |
| `Assets/Shared Assets/`, `Assets/Test/Scripts/` | 현재 씬이 사용하는 공통 외곽선·버튼·말풍선 코드 |
| `Assets/Samples/`, `Assets/VRTemplateAssets/` | 현재 XR 리그에 필요한 샘플 코드·입력·아이콘만 유지 |
| `Assets/XR/`, `Assets/XRI/`, `Assets/Settings/`, `Assets/TextMesh Pro/` | XR·렌더링·텍스트 실행 설정과 필수 자산 |
| `Assets/M/CodexTemp/MainSceneTools~/` | 이번 정리의 의존성·삭제 목록·검증 도구와 결과. 런타임 의존성 없음 |

정리 대상은 이전 ObservationTrial 씬·전용 런타임·회귀 검사, 별도 TeamShare 산출물, 메인 씬에서 사용하지 않는 ControlGuide 이미지·제작 원본입니다. 씬의 직렬화 의존성뿐 아니라 코드 전용 참조·동적 셰이더·셰이더 include·Resources·폰트 원본과 라이선스·필수 XR 및 프로젝트 설정 의존성을 보존합니다. 기존 폴더와 폴더 `.meta`는 비어도 남깁니다. Packages와 빌드 씬 목록 외의 프로젝트 설정은 변경하지 않습니다.

## 검증

`CodexTemp/MainSceneTools~/`의 검증 JSON을 확인합니다. 씬 이동 시 본문·`.meta` 해시와 GUID를 유지합니다. 에디터 컴파일·씬 참조·셰이더 검사와 실제 씬 콜백을 사용하는 관리실/119 모의 통화 경로·일시정지·재시작 검사는 결과 파일에 기록합니다. 빌드 대상 씬 등록과 실제 실행 파일 빌드는 별개이며, 이번 요청은 실행 파일 빌드를 포함하지 않습니다.

## 추가 개선

- HMD 높이: 착용/명시적 확인·추적 유효성·자세 안정화를 확인하고 자동 보정하며, 재착용·추적 원점 변경·메뉴 재보정을 지원합니다.
- 계단 전환: 1층 상태 진입 시 도착을 확정해 전환 도중 긴 프레임이 발생해도 플레이어가 3층에 남지 않도록 합니다.
- 현재 메인 씬의 정리 회귀 검사: `CodexTemp/MainSceneTools~/MainSceneSmoke.cs`. 이전 ObservationTrial 전용 Test Runner 테스트·메뉴는 정리 대상입니다.
- 공통 렌더링: 직교 깊이를 별도 변환하고, 겹쳐 활성화된 렌더링/FPS 소유자는 해제 순서와 무관하게 마지막 해제 때 원래 설정을 복원합니다.

실제 HMD 착용감·양안 가독성·초심자 이해도·기기 빌드 성능은 자동 검사와 별개입니다. `Documentation/DeviceAcceptance.md`에 미검증 체크리스트와 결과 기록 형식을 정리했습니다.

## 물방울 씬 전환

- `Wait_Robby`의 엘리베이터 시작 → 로비 소멸·월드 상황 카드 → 엘리베이터 물방울 등장. `Wait_Elevator` 단독 Play에도 시작 연출을 적용한다.
- 연출 중 시점 회전은 유지하고 이동/선택만 차단한다. 카드가 카메라를 따라오지 않고 월드 공간에서 Lerp로 나타났다 사라진다.
- 미리보기: 로비 **F8**, 엘리베이터 **F9**. 기존 엘리베이터 F8 개발자 기능 및 로비 K 시험은 유지한다.
- 조정·검증·무음 시각 미리보기: `Documentation/SceneTransition.md`, `CodexTemp/WorldTransitionLerp~/`.

## VR 기기 없이 임시 확인

- 지금도 헤드셋 없이 Play하면 PC 모드로 시나리오·대사·물체 선택·메뉴를 확인할 수 있습니다. 이는 VR 모드나 양안 렌더링 검증이 아닙니다.
- VR 입력은 XR Interaction Simulator로, 가상 XR 디스플레이와 양안 렌더링은 Mock HMD로 나누어 확인하는 구성이 적합합니다. 이번 작업에서는 시뮬레이터 설치나 XR 로더 변경을 하지 않습니다.
- 현재 PC/VR 분기는 `XRSettings.isDeviceActive`를 사용합니다. 가상 컨트롤러 입력만 추가하면 PC 모드에서 벗어나지 못하므로, XR 디스플레이의 실제 기동과 Tracked Pose Driver 입력을 함께 확인해야 합니다.
- Mock HMD는 입력을 제공하지 않습니다. 또한 현재 높이 보정은 `UnityEngine.XR.InputDevices`의 추적·착용 상태를 관찰하므로 별도 모의 입력 연결 검증이 필요합니다. 실제 HMD에서의 착용감·멀미·기기별 추적·성능을 대신하지 않습니다.
