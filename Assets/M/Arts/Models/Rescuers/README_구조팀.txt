Wait! 구조팀 — Unity 적용본 / 2026-11-04

Rescuer_A_Firefighter.prefab: 소방 구조대원.
Rescuer_B_LiftSpecialist.prefab: 승강기 전문 구조요원.
최신 Blender 원본의 비율을 사용했습니다. B 몸 좌우 83%, A/B 몸 앞뒤 112%/108% 수정본입니다.
B 추가 수정: 직전 원본 대비 전체 키 89%, 머리·이목구비·머리카락·안전모 96%입니다.
B 최종 키는 1.4785m이며, 머리는 세 축에 동일한 비율을 적용했습니다.
몸의 좌우 폭·앞뒤 두께는 유지하고 원본 메시와 뼈대의 높이를 수정했습니다.
원본 Blender 파일은 E:\ProjectWait\Blender\구조요원에 보존되어 있습니다.

Sources/: Unity 임포트용 FBX와 색상·외곽선·부위 이름 매니페스트.
Generated/: 통합 스킨 메시. 뼈대 18개와 색상·명암·외곽선 속성이 들어 있습니다.
머티리얼: Assets/M/Arts/Materials/Rescuers/RescueTeamPalette.mat.
셰이더: Assets/M/Arts/Shaders/ElevatorPaletteToon.shader.
런타임은 임시 폴더의 파일에 의존하지 않습니다.

각 모델의 여러 메시를 스킨 메시 1개와 머티리얼 슬롯 1개로 묶었습니다.
고밀도 작업복 표면만 단순화했습니다. 얼굴과 머리의 실루엣은 유지했습니다.
A: 184,177 삼각형 / B: 188,656 삼각형. 콜라이더는 선택용이며 플레이어와의 충돌은 연출 중 제외됩니다.

두 M 전용 승강기 씬의 RescueTeam에 두 프리팹이 배치되어 있습니다.
ElevatorStoryPresentation.rescuer는 구조팀 컨테이너이고 rescueTeam 필드가 역할과 포즈를 제어합니다.
공통 승강기 시나리오·통화·문 상태·입력 잠금을 유지하며 두 요원 모두 동일한 선택 강조 색상을 사용합니다.
퇴장 안내 후 두 요원 중 누구를 선택해도 같은 동행 흐름으로 진행합니다.

재구성 도구: Tools/Wait/Elevator/Integrate both rescue characters.
B 원본 재적용: Tools/Wait/Elevator/Update specialist from original model.
씬의 B Transform 배율은 1입니다. 새 뼈대에 맞춰 동작 목표와 선택 콜라이더를 조정했습니다.
기존 메시 자산은 GUID를 유지하며 정점 버퍼를 재구성해 다시 적용합니다.
PC 검증 기록: Assets/M/CodexTemp/RescueIntegration_20261104~/.
B 축소 검증 기록: Assets/M/CodexTemp/RescueBHeight_20261104~/.
두 씬 재생 검사 각 32개, 저장·참조 검사 42개, 원본 대조 검사 14개 통과.
물리 HMD와 실제 기기 빌드는 검증하지 않았습니다.


[2026-11-05 최적화·임시 자료 정리]
최적화 수치와 최신 검증: Assets/M/Documentation/OptimizationAndTempCleanup_20261105.md
과거 CodexTemp 기록은 Assets/M/Documentation/TempCleanupArchive_20261105.md로 정리했습니다.
이 문서의 이전 CodexTemp 경로는 과거 기록이며 정리 후 삭제되었습니다.
