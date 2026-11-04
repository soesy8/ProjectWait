구조팀 적용 완료 — 2026-11-04
원본·직접 상호작용 씬에 A 소방 구조대원과 B 승강기 전문 구조요원을 함께 연결했습니다.
A 안내 → B 승강장 확인 → 두 요원의 퇴장 안내 → 친구와 동행 → 1층 → 귀가/재시작.
PC Play Mode: 두 씬 각각 31개 검사 통과. 저장·참조·셰이더·스크립트 검사 42개 통과. 총 104개.
물리 HMD 착용 및 실제 기기 빌드는 미검증입니다.
최종 캡처: RescueTeam_Final.png. 원본 씬은 편집 모드·변경 사항 없음 상태로 돌려놓았습니다.
새 구현·자산·임시 출력은 Assets/M에 있습니다. 기존 XR 설정 사용자 변경은 보존했습니다.
콘솔 메모: CLI beta.16의 상태 조회가 현재 Pipeline에 없는 'editor status' 명령을 보내는 도구 호환성 오류 기록이 있습니다.
이 메시지는 구조 연출 C# 런타임 예외가 아닙니다. XR 오디오 대체·아이트래킹 기기 미연결·친구 표정 메시 이름 경고도 기록에 있습니다.
관련 메시지 원문은 Console.json / ConsoleErrors.json에 있습니다. 공용 패키지와 프로젝트 설정은 변경하지 않았습니다.
재구성 도구: Assets/M/Scripts/Editor/ElevatorRescueSetup.cs.
런타임 역할과 포즈: Assets/M/Scripts/Elevator/ElevatorRescueTeam.cs.