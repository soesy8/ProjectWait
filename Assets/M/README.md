# 엘리베이터 체험

## 실행·빌드 씬

`Assets/M/Scene/Elevator_Obsevation_Choice_Test.unity`

- 위 씬 하나만 유지하고 Build Settings에 등록합니다. 이름은 요청한 `Obsevation` 표기를 그대로 사용합니다.
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
| `Assets/M/Scenes/` | 현재 씬의 설정 자산 |
| `Assets/M/Scripts/ObservationTrial/` | 현재 체험 런타임 |
| `Assets/M/Scripts/Elevator/` | 공통 설정 타입·최적화 외곽선 피처 |
| `Assets/M/Arts/`, `Prefabs/`, `UI/` | 현재 씬에서 사용하는 아트·UI·모델 |
| `Assets/Shared Assets/`, `Assets/Test/Scripts/` | 현재 씬이 사용하는 공통 외곽선·버튼·말풍선 코드 |
| `Assets/Samples/`, `Assets/VRTemplateAssets/` | 현재 XR 리그에 필요한 샘플 코드·입력·아이콘만 유지 |
| `Assets/XR/`, `Assets/XRI/`, `Assets/Settings/`, `Assets/TextMesh Pro/` | XR·렌더링·텍스트 실행 설정과 필수 자산 |
| `Assets/M/CodexTemp/SingleSceneTools~/` | 이번 작업의 검증 도구·결과·삭제 목록; 런타임 의존성 없음 |

다른 씬, 해당 씬 전용 자산, 구형 재구성 도구, 과거 캡처·검증 기록, 빈 G/Y 및 빈 상위 폴더는 제거했습니다. 코드 전용 참조·동적 셰이더·셰이더 include·Resources·프로젝트 설정 의존성은 보존했습니다. Packages와 기존 사용자 공용 설정은 정리 대상으로 삼지 않았습니다.

## 검증

`CodexTemp/SingleSceneTools~/`의 검증 JSON을 확인합니다. 입력 바인딩·가상 컨트롤러 이벤트·충돌·안내·두 통화 경로·일시정지·재시작 검사와 씬 참조 검사를 실행했습니다. 실제 빌드는 Assets 내부 출력 제한으로 착수 전에 거부되었으며, 이후 사용자 결정에 따라 빌드 등록·컴파일·에디터 실행 검증까지만 진행했습니다.

## 추가 개선

- HMD 높이: 착용/명시적 확인·추적 유효성·자세 안정화를 확인하고 자동 보정하며, 재착용·추적 원점 변경·메뉴 재보정을 지원합니다.
- 계단 전환: 1층 상태 진입 시 도착을 확정해 전환 도중 긴 프레임이 발생해도 플레이어가 3층에 남지 않도록 합니다.
- 자동 테스트: `Tests/README.md`의 Test Runner 자동 발견 테스트와 **Tools > Wait > Tests > Run all regressions** 메뉴로 회귀 검사를 실행합니다.
- 공통 렌더링: 직교 깊이를 별도 변환하고, 겹쳐 활성화된 렌더링/FPS 소유자는 해제 순서와 무관하게 마지막 해제 때 원래 설정을 복원합니다.

실제 HMD 착용감·양안 가독성·초심자 이해도·기기 빌드 성능은 자동 검사와 별개입니다. `Documentation/DeviceAcceptance.md`에 미검증 체크리스트와 결과 기록 형식을 정리했습니다.
