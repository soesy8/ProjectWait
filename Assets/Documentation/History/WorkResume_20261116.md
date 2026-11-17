# 중단 작업 재개·인수인계 — 2026-11-16

기준 커밋: `c4ed80c`(2026-11-13, 최종 작업 진행 중). 구현·검증 결과는 M 내부에 작성했다. 커밋/푸시는 수행하지 않았다.

## 완료한 작업

### 마지막 플레이어 대사 변경 검증

- PC/VR 상하 레터박스 `200/720`, ‘나’만 화면 고정 패널, 다른 화자는 대상 근처의 월드 말풍선인 현재 정책을 유지했다.
- `VRUIComfortValidation`과 `VRImprovementValidation`의 이전 PC `82` 높이·레터박스 자막·플레이어 월드 말풍선 표시 기대값을 갱신했다.
- 화면 패널만 대사를 소유하는지, 이름표 포함 양안 영역, 5종 눈 투영 × 대사 3종 × 애니메이션 4단계(60개 배치), 고개 회전, 잘못된 XR 기하·긴 문장 이후 복구, PC 모드 복귀를 검사했다.
- 일시정지·전체 암전·화자 전환·소유자 비활성화 때 숨김, 시선 판정과 HUD 간격도 검사했다.
- 발견한 결함: `ElevatorSpatialUI`를 꺼도 독립 화면 패널이 남을 수 있었다. 즉시 Hide하고 비활성 소유자의 후속 Refresh를 거부하도록 수정했다.
- `ElevatorPlayerSpeechPanel.tag`를 `speakerTag`로 바꿔 상속 멤버 이름 가림 경고를 제거했다. 직렬화된 필드가 아니므로 씬/GUID는 바꾸지 않았다.

### 임시 기능 분리·분류

- 숫자 1–4 선택 우회, F8 개발 패널, 로비 K 및 전환 F8/F9 미리보기 키를 `UNITY_EDITOR || DEVELOPMENT_BUILD`로 제한했다. 공개 PC R 재시작·Esc 메뉴·마우스 선택과 정식 전환은 보존한다.
- `PC_Test_Controls`는 실제 PC 조작·XR 스폰/높이/시점 인계 코드이며 삭제하지 않았다. `LobbyDissolveTest`도 실제 물방울 전환 구현으로 보존했다.
- `VR_Preview_Test`는 비활성 + `EditorOnly` 상태를 유지했다. 과거 증거·백업·GUID를 삭제하지 않고 `TemporaryTools.md`에 보존/실행 정책을 정리했다.
- `VRUIComfort` 하네스의 프로세스 대기를 수정했다. Windows `Start-Process -Wait`가 상주 라이선스 자식 프로세스까지 기다릴 수 있어, 이제 해당 Unity 프로세스의 `WaitForExit()`와 실제 종료 코드로 판정한다.

## 검증 결과와 근거

| 항목 | 결과 / 근거 |
|---|---|
| Editor API·Player API·독립 런타임 DLL·Editor 검사 코드 | 컴파일 오류 0. 기존 `VRPreview*.camera` 이름 가림 경고 3건은 보존 |
| 독립 Unity UI 검사 | **19개 그룹 통과**, Unity/셸 정상 종료 코드 0. `CodexTemp/VRUIComfort_20261113~/Runs/20261116T002334777Z-resume-final-player-screen-61fc2134/run.json` |
| 원본 프로젝트 UI·저장 씬 검사 | 통과. `CodexTemp/ResumeWork_20261116~/20261116T002417179Z-edit.json` |
| 두 M 씬 | Missing Script·깨진 직렬화 참조·사용 렌더러 셰이더 오류·임시 경로 의존성 없음 |
| 실제 PC 씬 통합 | **25개 확인 통과**. 실제 로비 포털로 엘리베이터 이동, 관리실/119 두 경로, 두 탑승 순서, 식별판/위치 전달, 잘못된 대답 후 재시도, 구조·계단·1층·귀가·재시작. `CodexTemp/ResumeWork_20261116~/20261116T002537100Z-pc-smoke.json` |
| 화면 패널·메뉴 | 두 통화 경로에서 플레이어 패널 단독 표시, 일시정지 시 시간/패널 정지, 재개 시 복구 확인 |
| PC 시각 확인 | 실제 Game View의 Overlay 포함 1280×720 캡처. `CodexTemp/ResumeWork_20261116~/player-screen-pc-final.png`. ‘나’ 이름표·두 줄 한글·하단 띠와 패널 경계·중복 표시 없음을 확인 |
| 최종 Console | 컴파일 실패 false, 오류 **0**, 경고 3. `CodexTemp/ResumeWork_20261116~/console-final.json` |
| 배포용 키 분리 | Player API DLL에 F8/숫자 선택 바인딩 및 K/F8/F9 키 참조가 없고 R은 유지됨. Editor DLL을 양성 대조로 검사. `release-shortcuts.txt` |
| 원본 보존 | 씬·패키지·빌드 목록·OpenXR 설정의 Git 내용 해시가 기준 커밋과 동일. `preservation.json`. M 밖의 내용 diff 없음 |

PC 통합 검사는 **원본 씬과 실제 물체/UI 콜백**을 사용하지만 시나리오 시간을 결정적으로 진행했다. 실제 물리 마우스/컨트롤러 입력, 원래 속도의 읽기 경험, FPS·GPU 성능을 검증한 것은 아니다. 컴파일의 Player API 분기는 APK/실행 파일 빌드 성공을 뜻하지 않는다.

처음 두 하네스 호출은 제한시간에 걸렸다. 해당 실행 로그는 보존하고 성공 근거로 쓰지 않았다. 수정 후 별도 실행에서 결과 파일·Unity 종료 코드 0을 함께 확인했다. 원본 에디터 첫 시작의 검사 코드 타입 이름 오류는 수정 후 재컴파일·재시작했다. 이전 진단 과정의 CLI 구 명령 호환 오류는 `console-before-final.json`에 보존했으며, 새 Play 실행의 최종 Console은 오류 0이다.

## 재검증 방법

- 일반 에디터, Play 밖: `Tools > Wait > M > Validate VR UI Comfort`, `Validate VR Improvement`.
- 독립 UI 검사: `CodexTemp/VRUIComfort_20261113~/Compile.ps1`로 Editor/Player/Plugin/EditorTests를 각각 컴파일한 뒤 `RunHarness.ps1 -RunTag <고유태그>` 실행. PowerShell 실행 정책 변경이 필요하면 시스템 정책 대신 해당 프로세스에만 `-ExecutionPolicy Bypass`를 적용한다.
- 원본 씬 정적 검사: Pipeline `run_script`로 `CodexTemp/ResumeWork_20261116~/ResumeValidation.cs`, 진입점 `ResumeValidation.Edit`를 실행한다.
- PC 통합 검사: 깨끗한 `Wait_Robby`를 Play한 뒤 같은 도구의 `ResumeValidation.Smoke`를 실행한다. **현재 사용자가 플레이 중인 세션에는 실행하지 않는다.** 새 검증 Play에서만 사용하고, 완료 후 Play를 종료한다.
- `ResumeValidation.PlayerVisual`은 검증용 PC 실행을 대사 위치까지 진행하고 에디터를 일시정지한다. 한 프레임 Step/Canvas 갱신 후 Game View를 캡처해야 이전 프레임이 섞이지 않는다. 캡처 뒤 Play를 종료한다.
- `FinishRecord.ps1`은 배포 키 분기·원본 보존·현재 소스 해시와 임시 폴더 목록을 M 내부에 기록한다. 과거 `Setup`/최적화/전체 원복 도구를 대신 실행하지 않는다.

## 남은 작업 / 사용자 결정

1. **실제 HMD는 나중에:** 사용자가 이번에는 실기기 확인을 보류했다. 다음 Quest Link 착용 시 양안 한글·패널/화자 간격·컨트롤러 조작·재연결·높이 보정·편안함을 확인하고 `DeviceAcceptance.md`에 결과를 남긴다. 기존 체크박스를 자동으로 완료 처리하지 않았다.
2. **Mock HMD 패키지 제거는 미실행:** M 밖 `Packages/`·연결된 `Assets/XR/` 변경과 미리보기 코드 정리가 필요하므로 작업 범위 승인 후 진행한다. 백업의 과거 설정으로 덮어쓰지 않는다.
3. **APK/배포 빌드·기기 성능은 미검증:** 별도 승인 후 실제 대상 기기에서 확인한다. 현재 저장 빌드 목록에는 `Wait_Elevator`만 있다. 로비 시작 배포는 목록·시작 씬 정책 결정이 필요하다.
4. **팀 씬 통합은 별도:** 로비 정전·횡단보도는 준비 중 안내만 표시한다. GitHub Y 최신 `5a877c1` 결과물을 자동 병합하지 않았다. M 밖 팀 자산/씬의 통합 범위와 대상 씬을 먼저 합의한다.

최종적으로 Play/일시정지를 종료하고 원래 `Wait_Robby` 편집 씬으로 돌아간다. 검증 중 임시 백그라운드 실행 값·프레임 펌프를 복원하며, 원본 씬·프로젝트 설정은 저장하지 않는다. 작업 산출물은 `Assets/M/`에만 둔다.
