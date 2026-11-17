# Quest 2 Link 연결 점검 — 2026-11-12

## 최종 결과: 실제 플레이 확인

- 사용자가 헤드셋에서 프로젝트가 작동하며 실제 플레이까지 진행했다고 확인함.
- Unity 로그에서도 OpenXR 세션이 `XR_SESSION_STATE_FOCUSED`에 도달한 기록을 확인함.
- 마지막 확인 시 Unity는 Play 종료 상태이며, 현재 씬은 `Assets/M/Scene/Wait_Robby.unity`임. 다시 테스트하려고 임의로 재실행하지 않음.
- 진단용으로 실행했던 Oculus Mirror 창은 닫음. Meta Link 앱은 유지함.

## 확인 및 처리

- 실행 중인 프로젝트: `D:\project\ProjectWait`, Unity `6000.3.18f1`.
- 현재 씬: `Assets/M/Scene/Wait_Robby.unity`.
- Meta Horizon Link 앱 실행 및 기존 로그인 상태 확인. 계정은 변경하지 않음.
- Windows OpenXR 활성 런타임: Meta Horizon의 `oculus_openxr_64.json`.
- Oculus Virtual Audio Device의 장치 시작 오류(Code 10)를 발견하고, 관리자 승인 후 해당 장치만 재시작함. 이후 장치 상태 `OK`, 오류 코드 `0` 확인.
- 프로젝트의 Standalone OpenXR Loader 및 자동 XR 초기화 설정 확인.
- Oculus Touch Controller Profile 활성화, Mock Runtime 비활성화 확인.
- 로비 플레이어는 실제 XR 기기가 활성화되면 스테레오 카메라·머리 추적·컨트롤러 입력으로 전환하는 기존 구현을 사용함.
- 초기 Unity Play 테스트에서는 `xrGetSystem`이 `XR_ERROR_FORM_FACTOR_UNAVAILABLE`을 반환했음. 이후 사용자가 헤드셋에서 Link를 실행한 뒤 이 단계는 통과함.
- PC Link에서 헤드셋의 정상 사용 가능 상태와 배터리 27%를 확인함.
- 사용자가 헤드셋의 알 수 없는 출처 경고를 알려줌. PC Meta Link의 설정 > 일반에서 해당 항목이 꺼져 있는 것을 확인하고, Unity 실행을 위해 공식 확인 창의 허가 버튼을 눌러 활성화함. 저장 후 켜진 상태를 다시 캡처해 확인함.
- 허용 설정 적용 후 Unity Play를 다시 실행했고, 사용자가 정상 동작과 플레이를 확인함.
- 기존 소스, 씬, Packages, ProjectSettings, Assets/XR 파일은 직접 수정하지 않음. 진단 캡처·보고서와 진단용 네이티브 UI 도구만 이 폴더에 저장함.

## 다시 실행할 때

1. Meta Link 앱을 실행하고 헤드셋에서 해당 PC의 Quest Link를 실행.
2. Unity 프로젝트에서 Play를 실행.

ADB 연결은 여전히 `unauthorized`여서 배터리 수치를 읽을 수 없었음. 이 USB 디버깅 권한은 기본 Quest Link 실행의 필수 조건은 아님.

PC 전체 재부팅, 드라이버 삭제·재설치, 계정 변경, 공장 초기화, Windows 보안 설정 변경은 수행하지 않음. PC Meta Link의 알 수 없는 출처 허용만 개발용 Unity 실행을 위해 켬.
