# Quest Link 실기기 실행 준비 — 2026-11-13

## 결과

- Unity 6000.3.18f1의 현재 `Assets/M/Scene/Wait_Robby.unity`에서 Play 실행.
- 사용자가 헤드셋을 착용하고 Quest Link를 실행한 뒤 재시도하여 실제 Oculus OpenXR 세션 시작에 성공했다.
- Unity 로그 12:24:23.918: `XR_SESSION_STATE_VISIBLE->XR_SESSION_STATE_FOCUSED`, session=2.
- 런타임 `Oculus 1.208.0`, 폼 팩터 `XR_FORM_FACTOR_HEAD_MOUNTED_DISPLAY` 확인.
- Meta 연결 정보에서 Quest 2와 좌/우 컨트롤러가 모두 `connected / operable / active`로 확인됐다.
- 재시도 캡처에서 Play 중 로비가 `Left Eye`로 출력되는 것을 확인했다. 사용자가 테스트할 수 있도록 Play를 유지했다.

## 초기 시도 및 구분

- 첫 Play(12:21:45)는 `xrGetSystem: XR_ERROR_FORM_FACTOR_UNAVAILABLE`로 실패하여 PC 모드로 실행됐다. PC 모드 Play를 중지한 뒤, 헤드셋 내부에서 Link를 실행한 사용자의 확인을 받고 재시도했다.
- USB 연결 캐시의 `connected`만으로 OpenXR 사용 가능 여부를 단정할 수 없다. 실제 Play 초기화와 세션 상태를 별도로 확인했다.
- ADB는 `unauthorized`였다. APK 설치·기기 쉘 디버깅에는 USB 디버깅 허용이 필요하지만 이번 PC Link 실행에는 필요하지 않았다.
- 실제 버튼 입력·엘리베이터 체험 진행·착용 상태의 UI 선명도와 피로도·전체 플레이 완료는 이번 연결 준비 점검에서 검증하지 않았다.
- 재시도 화면에 SRP에서의 `Camera.stereoTargetEye` 관련 경고가 표시됐다. 해당 경고를 없애기 위한 코드나 렌더링 설정 변경은 하지 않았다.

## 변경 범위

- 게임 코드·씬·XR 설정·패키지·플랫폼 설정을 직접 변경하거나 저장하지 않았다. APK 빌드·설치도 하지 않았다.
- 이 폴더에만 네이티브 Unity 창 조작 도구와 캡처·이 기록을 추가했다. Unity 네이티브 메뉴는 Play 상태 체크가 신뢰할 수 없어 캡처의 Play/Stop 버튼 상태를 확인한 다음 입력했다.
- 기존 사용자 변경은 보존했다. `AGENTS.md`의 로컬 Git 제외도 확인했다.

## 캡처

- `play-status.png`: 첫 실행의 PC 폴백.
- `stopped-before-retry.png`: 재시도 직전 Play 중지.
- `retry-play.png`: 실제 OpenXR 실행 후 로비의 왼쪽 눈 출력.
