# ProjectWait CodexTemp 작업 정리 및 보관 안내

- 작성일: **2026-11-03**, 로컬 시간대 UTC+09:00.
- 프로젝트: `D:\project\ProjectWait`.
- 조사 폴더: `D:\project\ProjectWait\Assets\M\CodexTemp`.
- 문서: `E:\ProjectWait_CodexTemp_작업정리_20261103.md`.
- 목적: 임시 폴더를 압축하고 비우기 전에 작업 내용·변경 이력·검증 근거·복원 주의점을 한 문서에 보관한다.
- 이번 요청에서는 **이 MD만 저장**했다. 원본 변경·삭제·이동, ZIP 생성, Unity 씬 저장 및 테스트 재실행은 하지 않았다. 압축은 사용자가 별도로 진행한다.

> 검증 수치는 기존 파일에 기록된 당시의 실행 결과다. 이번에 Unity 테스트를 새로 통과했다는 뜻이 아니다. 중간 구현과 후속 변경을 구분했다. 이 문서는 원본 모델·이미지·코드를 대체하지 않으므로 폴더 압축본도 반드시 보관한다.

## 1. 전체 조사 결과

- 최초 조사 기준 **1,212개 파일 / 353,806,596바이트 / 약 337.42MiB**. 내부 `.meta` 포함, 폴더 자체의 사용량 제외.
- 최상위 작업 폴더 **35개**, 그중 빈 폴더 **5개**.
- 파일 수정 시각 범위: **2026-10-22 09:50:35 ~ 2026-11-03 14:28:28**. 복사 파일의 시각일 수도 있으므로 실제 작업 기간과 완전히 같다고 단정하지 않는다.
- 분야: 친구 캐릭터 제작/연동, Blender 승강기·체육센터 맵, Unity 안전교육 시나리오, 실루엣 강조·충돌, PC/XR 입력, 대화·생각 말풍선·UI, 스카이박스·텍스처 조사.
- 주요 파일: JSON 351개, PNG 277개, C# 249개, TXT 143개, `.meta` 62개, 로그 40개, Python 33개, Unity 씬 20개, MD 8개, Blender 8개와 `.blend1` 2개. 나머지는 자산·셰이더·PowerShell·머티리얼·CSV다.
- 임시 폴더 밖 C# **126개**에서 `CodexTemp` 문자열을 조사했다. 경로 참조는 **에디터 스크립트 3개**의 백업/결과 출력용뿐이었다.
- 임시 자산 및 부모 폴더의 **GUID 63개**를 임시 폴더 밖 직렬화 자산 **926개**와 비교했다. 해당 임시 자산을 가리키는 GUID 참조는 **0건**이었다. 모든 동적 경로·바이너리 의존성까지 보증하는 검사는 아니다.
- 문서 후반에 폴더별 통계 및 **원본 전체 파일의 절대 경로·크기·수정 시각·SHA-256**을 수록한다. 압축 해제본 대조용이다.

## 2. 마지막 작업 기록 기준의 동작과 변경 이력

### 실행과 진행

- 기본 씬: `D:\project\ProjectWait\Assets\M\Scene\Elevator_Safety_Prototype.unity`.
- 직접 오브젝트 선택 씬: `D:\project\ProjectWait\Assets\M\Scene\Elevator_Safety_ObjectInteraction_Test.unity`.
- 한빛 생활체육센터 3층에서 운동을 마친 뒤 1층으로 귀가하는 체험이다.
- 탑승 → 층 선택 → 공 받기/제안 → 고장 → 비상통화 → 상황 전달 → 식별판 확인 → 위치 전달 → 친구 안심 → 대기 → 직원 구조 → 하차/1층 이동 → 귀가/완료로 이어진다. 비상통화 무응답 시 휴대폰으로 119 **모의** 신고한다.

### 시점·입력·선택

- 대화/식별판마다 inside 관람 위치로 재배치하고 인물 쪽으로 강제 회전시키던 방식은 자유 시점 작업에서 제거했다. 탑승·하차·계단·귀가 경로 이동은 유지한다.
- 후속 연출 보완에서 **첫 탑승 3.4~4.7초에만 정면 180도 회전**을 한정 추가했다. 따라서 모든 구간에서 회전이 전혀 없다는 설명도 최종 이력과 다르다.
- 연출 중 둘러보기는 허용하지만 이동·클릭·숫자·R 재시작·F8 입력은 공통 잠금으로 차단한다. ESC/VR 메뉴의 일시정지는 예외다.
- 레터박스/퇴장 중 PC 둘러보기 감도는 평소의 10%. 물리적 VR 헤드 추적에는 감쇠를 넣지 않는다.
- 친구와 농구공은 독립 강조 대상이다. 2m 활성화/12m PC·UI 선택 제한은 제거했고 벽 가림은 유지했다. 거리 무관은 가림·레이 범위까지 무시한다는 뜻은 아니다.
- 기본 주황↔호버 파랑은 색상·선 두께·헤일로를 보간한다. 진행 오브젝트를 8초간 선택하지 않으면 1초 주기의 색상 힌트가 작동하며 연출/일시정지 중 멈춘다.
- 휴대폰은 친구 오른쪽 앞주머니에서 윗부분이 드러나고, 공은 원래 크기로 손 쪽으로 이동하며 약 1.1초 페이드 후 숨는다.

### 대화·생각 말풍선·전환

- 공간형 대화와 동화책 프레임을 유지하며 본문 35pt, 폭 580, 최소 높이 230, 줄 간격 9와 여백을 적용했다.
- 실제 TMP 폭으로 어절을 나누고 최대 3줄/페이지, 4줄 분량은 2+2로 분할한다. 첫 선택은 전체 표시, 이후 선택은 페이지/행동 진행이다. 동일 문장 갱신은 읽던 상태를 초기화하지 않는다.
- 최종 전환은 **alpha-only**다. 위치·크기·회전·배율 보간 실험은 후속 요청으로 제거했다. 한글 두 문장을 겹치지 않는 fade-through는 유지한다.
- 관찰 설명은 최초 빈자리 선택 후 시선/보행에 따라 계속 좌우로 이동하지 않는다. 레거시 월드 설명 패널 15개/씬은 비활성화했다.
- 최종 생각 말풍선은 **힌트/미션 복귀/관찰 모두 본문 전용**이다. ‘나의 생각’, ‘나의 생각 · 힌트’, ‘살펴보니…’와 생각 리본을 숨긴다. 관리실 화자 이름/리본과 일시정지 제목은 보존한다.
- 공용 프리팹 원본 대신 M 런타임 인스턴스에 정책을 적용했다.

### 오래된 자료와 충돌하는 지점

| 오래된 내용 | 후속 변경/해석 |
|---|---|
| 2026-10-22 대화/조사/선택 페이즈 분리안 | 당시에는 설계 인수인계, 구현 전이었다. 최종 구현 완료 기록이 아니다. |
| 2026-10-30 inside 재배치·시점 보정 | 2026-11-03 제거. 이후 최초 탑승 회전만 한정 추가. |
| 2026-10-30 모든 설명을 월드 공간에 배치 | 후속 화면형 설명 재사용/고정 배치 및 레거시 패널 비활성화와 함께 읽는다. |
| UI 위치·크기·배율 보간 | 후속 alpha-only에서 제거. |
| 미션의 ‘나의 생각’ 제목은 유지 | 마지막 제목 잔존 수정에서 힌트/미션까지 본문 전용으로 통일. |
| 과거 스카이박스 프리뷰 GUID | 정식 머티리얼은 현재 텍스처 GUID를 사용. 오래된 프리뷰를 복사하지 않는다. |

`D:\project\ProjectWait\Assets\M\Scene\README_엘리베이터.txt`는 수정일 2026-10-30으로 재배치/시점 보정 설명 등이 남아 있다. 2026-11-03 후속 문서와 현재 코드를 우선 대조한다.

## 3. 분야별 작업 내용과 근거

이 절의 절대 경로는 원래 위치다. 폴더를 비운 뒤에는 압축 해제한 대응 위치에서 읽는다. 오래된 적용 도구나 Before 사본으로 현재 구현을 덮어쓰지 않는다.

### 3.1 친구 캐릭터 추출·제작·연동·이관

**Blender 추출 — `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~`**

- 복도 씬의 Friend_Milo에서 메시·UV·노멀·재질·부모 계층·숨긴 부품을 추출하고 주머니 스티치를 커브로 변환했다.
- 메시 61개/삼각형 41,469개, 재열기 정점 오차 0m, 외부 텍스처 의존성 0 기록이다.
- 원래 뼈대가 없는 Transform 기반 캐릭터다. 완성 리깅 모델로 해석하지 않는다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\README_친구캐릭터.txt`, `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\Reopen_Validation.json`.

**신규 V2 — `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~`**

- 구조 레퍼런스 기반 큰 머리/주황 후드/청록 가방/반바지/운동화 모델과 기본 FK 뼈대를 제작했다. 1프레임 기본 자세, 24프레임 가방끈 잡기 포즈다.
- 재열기: 메시 103개, 뼈 17개, 기본 정점 73,230개/면 74,125개, 실패 없음.
- IK·손가락/표정 컨트롤·상용 애니메이션 스키닝 검수까지 완성한 모델은 아니다. Blender 셀 명암/Freestyle은 Unity URP의 근사 프리뷰다.
- 실제 `.blend`, `.blend1`, 방향별 렌더가 `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery`에 있다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\README_친구캐릭터.txt`, `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Reopen_Validation.json`.

**Unity 연동/검토 — `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~`, `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview`**

- FBX·프리팹·25개 머티리얼·17개 뼈·Idle/Walk/Fall 클립을 연결했다. 안전은 Idle/Reward, 위험은 Fall/UnsafeExplanation 경로를 확인했다.
- 당시 두 씬 Missing Script 0, Animator 각 1, 기존 캐릭터 중복 0, 임시 의존성 없음, 런타임 오류 0 기록이다.
- 이후 `D:\project\ProjectWait\Assets\M\CodexTemp\AssetMigration~`에서 42개 파일/10회 이동, 부산물 106개 보존을 기록했다. 과거 JSON의 M 경로는 현재 위치와 다를 수 있다.
- 현재 공용 친구 프리팹과 두 Test 씬 존재를 확인했다. 이번 작업에서 이관을 실행한 것은 아니다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Final_Validation.json`, `D:\project\ProjectWait\Assets\M\CodexTemp\AssetMigration~\Verification.json`.

### 3.2 초기 승강기 QA와 페이즈 설계

- 결과: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA`.
- 도구: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorTools~`.
- 초기 탑승/관찰/제안/고장/통화/무응답/119/대기/구조/완료 자료다. 런타임 41개, 시점·입력 23개 검사와 장면 캡처가 있다.
- 친구 눈높이 약 Y=1.1064756m, 마우스 시점·중앙 레이·선택지 고정·재시작/개발 메뉴 등을 검증했다.
- 페이즈 분리 인수인계는 **설계 방향 검토 완료, 구현 전**을 명시한다. 제안한 구조가 그대로 구현됐다고 단정하지 않는다.
- 2026-10-29 CanvasGroup 과거 예외를 조사했다. 당시 현재 상태에서는 재현되지 않았고 RequireComponent/생성 경로 보완 후 81개 검사 통과를 기록했다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\엘리베이터_페이즈_분리_작업_인수인계_20261022.md`, `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\CanvasGroupDiagnosis_20261029.txt`.

### 3.3 Blender 맵: 추출 → 실버 → 체육센터 → 3층

**추출 — `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~`**

- 실내·문·버튼·한글 안내·로비·친구/공·숨긴 구조원·카메라를 추출했다. 안내판 글리프와 내장 SDF 아틀라스를 보존했다.
- 재열기: 원본 135오브젝트, 37메시, 184,117정점, 317,890삼각형, 위치 오차 약 4.81e-7m, 외부 라이브러리 0.
- 스크립트·XR·상호작용·리깅을 Blender 기능으로 변환한 것이 아닌 정적 편집용 추출이다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\읽어주세요.txt`, `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Reopen_Validation.json`.

**실버 — `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~`**

- 왼쪽 창/오른쪽 계단, 19단 계단, 후면 자동문, 전면/낮은 측면 비상버튼, 12층 버튼, 한글 안내와 문 개폐를 검토했다.
- 재열기 모든 검사 true, 875오브젝트/794메시/24머티리얼/15컬렉션/5카메라/4애니메이션 오브젝트 기록이다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\Reopen_Validation.json`.

**체육센터 — `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~`**

- 사용자 원본을 보존하며 한빛 생활체육센터 콘셉트·1층/3층 표시·안전 포스터·식별정보·측면 패널 0.45m 이동을 정리했다.
- 완료 감사: 전달 11파일 보존, 재열기 29체크, 렌더 6개 완료, 필수 잔여 작업 없음.
- 포스터·프롬프트·시나리오·원본·렌더·전달 해시를 함께 보관한다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Completion_Audit_20261030.json`.

**3층 버전 — `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~`**

- 층간 3.96m/3층 7.92m, 1층 보존, 3층 농구장·탈의/보관 복도·안내·연결 계단 추가. 2층은 구조/계단 위주다.
- 오브젝트 2,080개, 3층 복제 750개, 주 승강기 3층/다른 승강기 5층, 주변 건물 9~32m 기록이다.
- Blender 단계 캐빈은 고정 모델이고 수직 운행·Unity 진행은 아직 없다는 설명이다. 후속 Unity 재구성과 구분한다.
- 모델 원본·사용자 최신 스냅샷·시나리오·7개 검토 이미지와 검증 도구가 있다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\읽어주세요_3층.txt`, `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Delivery_Validation.json`.

### 3.4 Unity 재구성과 초기 직접 상호작용

- 재구성: `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~`.
- Blender 내보내기 → 정식 Manifest/Geometry/머티리얼/프리팹 → 기본 씬 구성. 초기 대상 11개, 작업 전 씬 백업이 있다.
- 당시 런타임 65개/저장 씬 19개 통과, 계단 하강은 직원 안내 페이드 이동이다.
- 상호작용: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~`.
- PC/XR 선택·버튼 피드백·전체 실루엣·완료 노트·착지/일시정지를 보완했다. 32 + 시나리오 65 + 추가 7 + 저장 23개 검사 기록이다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\Completion.json`, `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Completion.json`.

### 3.5 고장 대처·직접 선택·호버·충돌·상시 강조

- 대처 개선: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~`.
  - 두 씬의 진행 방식을 구분하고 통화/상황 보고/식별판/위치 전달/친구 안심/구조 흐름을 보완했다.
  - 당황 표정은 원래 눈·입을 숨기고 검은 나선 눈/물결 입을 사용하며 피부·코·볼은 보존했다.
  - 원본 57개, 직접 선택 111개, 저장 47개 검사 기록이다.
- 호버/충돌: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~`.
  - 호버 설명·통화 다음 버튼·실제 PC 입력·캡슐 이동·문 프레임/경계/소품 충돌을 확인했다. 각 씬 설명 15개, 경계 11개, 소품 22개, 출입부 고정 메시 17개 기록이다.
  - 원본 94개, 직접 선택 100개, 저장 47개 검사 통과다.
- 상시 강조: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~`.
  - 농구장 뒤 경계를 2m 뒤인 Z=-8.4m로 옮겼다. 호버 중 큰 대화/선택은 유지하며 설명은 대화·버튼·얼굴을 피하게 했다.
  - 주황 코어 6.5px/파랑 코어 10.4px와 외부 글로우 기록이다.
  - 원본 118개, 직접 선택 112개, 저장/셰이더 32개 검사 통과다.
- 근거는 각 폴더의 `Completion.json`이다. 과거 월드 설명 UI 정책은 후속 화면형 설명/제목 제거 작업과 구분한다.

### 3.6 Render Graph 실루엣 및 설치 도구

- 렌더 개선: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~`.
- 여러 부품을 coverage 마스크로 묶고 반해상도 Jump Flood 거리 전파 후 원본 영역을 빼 외곽 띠/글로우를 합성한다. 벽 깊이와 대상별 마스크를 사용한다.
- 친구/직원은 실제 Renderer·SkinnedMeshRenderer, 정적 패널은 마스크용 geometry/anchor를 쓴다. 구형 hull/stencil/복제 표시 메시 경로를 정리했다.
- GPU 비교: 강조 픽셀 **31,466개 모두 바깥**, 원본 내부 0, 완전 가림 0. 부분 가림도 통과했다.
- 최종 기록: 원본 125개, 직접 선택 119개, 저장 34개, GPU 7개. PC 1920×1080/13개 대상 약 60fps는 당시 환경 결과이지 HMD 성능 보증이 아니다.
- 기존 XR UI Sprite 누락이 씬마다 8개 있었고 참조는 전후 동일했다. 모든 기존 누락이 해결됐다는 기록은 아니다.
- 설치 도구: `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~`.
- UI Toolkit 설치 버튼·씬 파이프라인 우선 탐지·M 복제·소스 보존·중복 방지·미저장 씬 거부·재열기 등 **23개** 검사 통과다.
- 설치 검증 씬: `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererToolValidation\InstallerValidation.unity`.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\RenderGraphReview.txt`, `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\PixelVerification.json`, `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\InstallerVerification.json`.

### 3.7 내부 시점 보정 도입 후 자유 시점으로 변경

- 구도 보정: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~`.
  - 대화 때 inside로 약 0.6초 이동하고 인물·대화·선택을 함께 보도록 정렬했다. XR은 페이드 때 정렬했다.
  - 원본 46개/직접 선택 42개 검사, 강조 대상 16개 설정 보존/Missing Script 0 기록이다.
- 자유 시점: `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~`.
  - 위 실내 재배치와 PC/XR 강제 회전을 제거하고 경로 이동은 유지했다. 연출 입력 잠금과 둘러보기를 분리했다.
  - 각 씬 125개 + 상호작용 회귀 각 32개 통과. 실제 Mouse/Keyboard 이벤트로 둘러보기 허용·이동/클릭/숫자/R/F8 차단·ESC/재개를 검사했다.
- 이후 최초 탑승 한정 회전/레터박스 감도는 다음 연출 보완에서 추가했다. 과거 구도 보정 도구를 무작정 재적용하지 않는다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\FinalSummary.json`, `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\README.md`.

### 3.8 선택·호버 분리 및 연출·지연 힌트

- 선택 수정: `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~`.
  - 친구/공 분리, 2m/12m 제한 제거, XR 레이 180m, 가림 유지, Lerp 색상 복귀, 공용 ThoughtCloud 내부 UI 재사용을 적용했다.
  - 각 씬 32개 통과. 실제 중앙 레이·원거리·벽 가림·공 전달·재시작·한글 overflow/중복 UI를 확인했다.
- 연출 보완: `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~`.
  - 첫 탑승 한정 회전·주머니 휴대폰·8초 힌트·공 페이드·비상벨 근처 관리실 대사·레터박스 감도 10%·관찰 제목 제거를 적용했다.
  - 각 씬 새 54 + 카메라 126 + 상호작용 32 = **총 424개** 통과다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\README.md`, `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\README.md`.

### 3.9 대화 가독성, UI 전환 실험과 alpha-only

- 가독성: `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~`.
  - 본문/폭/여백 확대, TMP 어절 줄바꿈, 최대 3줄 페이지, 읽기 버튼, 타이핑/페이지 유지, 불투명 크림 읽기 면과 설명 UI 회피를 구현했다.
  - 기본 220개/직접 선택 203개와 회귀를 합쳐 **847개** 통과다. 비율별 검사는 카메라 비율 변경이지 모든 기기 실제 캡처가 아니다.
- 위치/크기 전환 실험: `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~`.
  - 패널 약 0.22초 보간, 등장/퇴장 0.18초, 문장 0.16초 fade-through, 버튼 재사용/퇴장 입력 차단 등을 적용했다.
  - 각 씬 새 37개와 회귀를 포함해 **921개** 통과, 중간 프레임 캡처가 있다.
- 투명도만 유지: `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~`.
  - 위치·크기·회전·배율 보간을 제거하고 alpha-only/문장 페이드만 남겼다. 현재 코드에서도 이 정책을 확인했다.
  - 반복적인 좌우 배치/회전/확대를 제거하고 레거시 패널/제목 복제를 차단했다. 미션 제목은 이 단계까지 유지했으나 다음 작업에서 제거됐다.
  - 각 씬 새 41개와 회귀를 포함해 **929개** 통과다.
- 근거: 각 폴더 README 및 `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\FinalSummary.txt`, `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\FinalSummary.txt`.

### 3.10 생각 제목/리본 잔존 최종 수정

- 폴더: `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~`.
- 관찰 종료 후 기본 힌트로 돌아갈 때 공용 ThoughtCloud의 ‘나의 생각’/리본이 다시 켜지는 문제를 재현했다. 레거시 제목만 숨기는 것으로 충분하지 않았다.
- 생각/힌트/관찰 모두 본문 전용으로 통일했다. 같은 값 조기 반환·Layout·Tick·fallback·전환 복제 텍스트를 정리하고 다른 화자 리본·일시정지·본문은 보존한다.
- 새 정책: `D:\project\ProjectWait\Assets\M\Scripts\Elevator\ElevatorThoughtHeaderPolicy.cs`.
- 위 파일과 `D:\project\ProjectWait\Assets\M\Scripts\Elevator\ElevatorUITransitions.cs`를 현재 읽어 본문 전용/alpha-only를 교차 확인했다.
- 당시 기본 씬이 dirty여서 다른 씬을 열지 않고 **현재 씬의 Play Mode 복사본에서만** 실행했다. 두 저장 씬을 각각 재실행했다는 뜻이 아니다.
- 제목 144 + fade/skybox 41 + 가독성 220 + polish 54 + 카메라 126 + 상호작용 32 = **617개** 통과다.
- 당시 씬 2개·공용 프리팹·폰트의 전후 해시가 동일했고 사용자 dirty 상태를 보존했다는 기록이다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\README.md`, `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\FinalSummary.txt`, `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\FinalVerification.txt`.

### 3.11 스카이박스와 텍스처 조사

- 도구: `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxReview~`.
- 프리뷰: `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview`.
- 루트에 Day/City 생성·편집 프롬프트와 검증 JSON이 있다. 두 이미지 모두 1024×512의 2:1 파노라마다.
- City 좌우 첫/끝 열은 512행 모두 RGB 최대 차이 0, Day는 최대 차이 5 기록이다. 프리뷰 방향 렌더와 이음새 비교 이미지를 보관한다.
- 후속 정식 적용: `D:\project\ProjectWait\Assets\M\Arts\Textures\Skybox_Storybook_City_1024.png`, `D:\project\ProjectWait\Assets\M\Arts\Materials\Elevator\StorybookCitySkybox.mat`.
- 두 M 씬의 Skybox/Panoramic, 360도/모노, 노출 1/회전 0 적용 기록이다. 현재 머티리얼과 텍스처 메타의 GUID `5c3834bc0c4699f4e923bbb8e8ebf4a8` 일치를 확인했다.
- 프리뷰의 구 GUID `8f71bfbaaad501b4b93c26745b04b828`는 다르므로 정식 설정에 그대로 복사하지 않는다.
- 텍스처 도구: `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAuditTools~`.
- 2026-10-29 원본 134개/Unity 텍스처 139개 조사. 양변 2048 이상 원본 10개는 모두 M 밖이었다. **조사만 했으며 축소/임포트 설정 변경은 하지 않았다**.
- 한글 폰트 아틀라스는 글리프 좌표와 연결되어 픽셀만 줄이면 깨질 수 있다는 주의가 있다.
- 근거: `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.image-validation.json`, `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_20261029.txt`.

### 3.12 현재 비어 있는 작업 폴더

이름만으로 구현 완료를 추정하지 않는다. 압축 도구가 빈 폴더를 생략할 수 있다.

- `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorUIRedesign_20261029~`.
- `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorVRWorldUI_20261029~`.
- `D:\project\ProjectWait\Assets\M\CodexTemp\FrameRateLimit~`.
- `D:\project\ProjectWait\Assets\M\CodexTemp\SpatialUITools~`.
- `D:\project\ProjectWait\Assets\M\CodexTemp\TmpDisplayFix~`.

## 4. 기존 검증 결과 요약

서로 다른 시점의 반복 회귀를 포함하므로 **아래 행을 더해 고유 테스트 수로 제시하면 안 된다**. 전부 당시 기록이며 이번에 재실행한 결과가 아니다.

| 작업 | 당시 결과 | 범위/주의 |
|---|---:|---|
| 초기 승강기 | 런타임 41 / 입력 23 | 페이즈 설계 이전 |
| CanvasGroup 보완 | 81 | 과거 예외 조사/생성 경로 방어 |
| Unity 체육센터 재구성 | 런타임 65 / 저장 19 | 기본 씬 |
| 초기 직접 상호작용 | 32 + 65 + 7 + 저장 23 | 기본 씬 |
| 고장 대처 | 기본 57 / 직접 선택 111 / 저장 47 | 두 씬 |
| 호버·충돌 | 기본 94 / 직접 선택 100 / 저장 47 | 두 씬 |
| 상시 주황/호버 파랑 | 기본 118 / 직접 선택 112 / 저장·셰이더 32 | 두 씬 |
| 마스크 실루엣 | 기본 125 / 직접 선택 119 / 저장 34 / GPU 7 | 두 씬 및 GPU fixture |
| 강조 설치 도구 | 23 | 설치 검증 씬 |
| 내부 구도 보정 | 기본 46 / 직접 선택 42 | 후속 자유 시점에서 제거 |
| 선택·호버 수정 | 각 32 | 두 씬 |
| 자유 시점 | 각 125 + 상호작용 각 32 | 이후 첫 탑승 회전 예외 추가 |
| 연출·지연 힌트 | 합계 424 | 각 씬 54 + 126 + 32 |
| 가독성 | 합계 847 | 220/203 + 회귀 |
| 위치·크기 포함 전환 | 합계 921 | 이후 alpha-only로 변경 |
| alpha-only·스카이박스 | 합계 929 | 각 41 + 회귀 |
| 최종 제목/리본 수정 | 합계 617 | 당시 dirty 기본 씬에서만 실행 |

공통 미검증: 실물 HMD/양안/컨트롤러, 기기별 GPU·메모리·멀미/가독성, 실제 플레이어 빌드. 기존 OpenXR 장치 부재·오디오 fallback·메시 이름/기존 Sprite 참조 경고와 새 오류를 구분한다. ‘콘솔 오류 없음’이 모든 경고도 없었다는 뜻은 아니다.

## 5. 임시 폴더와 별도로 유지할 정식 자산

다음 경로는 현재 존재를 확인했다. CodexTemp와 함께 삭제하지 않는다.

| 구분 | 절대 경로 |
|---|---|
| 기본 씬 | `D:\project\ProjectWait\Assets\M\Scene\Elevator_Safety_Prototype.unity` |
| 직접 선택 씬 | `D:\project\ProjectWait\Assets\M\Scene\Elevator_Safety_ObjectInteraction_Test.unity` |
| 체험 설정 | `D:\project\ProjectWait\Assets\M\Scene\ElevatorSettings.asset` |
| M 런타임 | `D:\project\ProjectWait\Assets\M\Scripts\Elevator` |
| M 에디터 도구 | `D:\project\ProjectWait\Assets\M\Scripts\Editor` |
| 체육센터 프리팹 | `D:\project\ProjectWait\Assets\M\Prefabs\SportsCentre3F\SportsCentreMap.prefab` |
| 맵 Manifest | `D:\project\ProjectWait\Assets\M\Arts\Models\SportsCentre3F\SportsCentreManifest.json` |
| 맵 geometry | `D:\project\ProjectWait\Assets\M\Arts\Models\SportsCentre3F\SportsCentreGeometry.bytes` |
| 씬 파이프라인 | `D:\project\ProjectWait\Assets\M\Arts\Rendering\ElevatorScenePipeline.asset` |
| 외곽선 렌더러 | `D:\project\ProjectWait\Assets\M\Arts\Rendering\ElevatorOutlineRenderer.asset` |
| 공용 강조 렌더 자산 | `D:\project\ProjectWait\Assets\Shared Assets\Highlight\Rendering` |
| 공용 친구 | `D:\project\ProjectWait\Assets\Shared Assets\Models\Friend\Wait_Friend.prefab` |
| 이관된 애니메이션 씬 | `D:\project\ProjectWait\Assets\Test\Scenes\Friend_Animation_Test.unity` |
| 이관된 복도 친구 씬 | `D:\project\ProjectWait\Assets\Test\Scenes\WetCorridor_Friend_Test.unity` |
| 도시 텍스처 | `D:\project\ProjectWait\Assets\M\Arts\Textures\Skybox_Storybook_City_1024.png` |
| 정식 스카이박스 | `D:\project\ProjectWait\Assets\M\Arts\Materials\Elevator\StorybookCitySkybox.mat` |
| 생각 본문 정책 | `D:\project\ProjectWait\Assets\M\Scripts\Elevator\ElevatorThoughtHeaderPolicy.cs` |

과거 README의 강조 코드 위치는 공용화 등으로 현재와 다를 수 있다. 과거 경로에 새 사본을 만들기 전에 현재 씬 참조를 확인한다.

## 6. 기존 E 드라이브 전달본 현재 확인

다음 3파일만 존재/해시를 현재 읽기 전용 확인했다. 이번에 수정하거나 덮어쓰지 않았다. 과거 전달 JSON의 모든 외부 렌더/문서/캐릭터 파일을 확인한 것은 아니다.

| 파일 | 현재 크기(바이트) | 과거 전달 기록과 비교 |
|---|---:|---|
| `E:\ProjectWait\Blender\엘베 맵\엘베_맵_실버.blend` | 8,404,708 | 과거 8,390,499바이트/해시와 다름. 수정된 사용자 파일일 수 있으므로 오래된 사본으로 되돌리지 않는다. |
| `E:\ProjectWait\Blender\엘베 맵\엘베_맵_체육센터.blend` | 17,086,110 | 과거 전달 해시 일치 |
| `E:\ProjectWait\Blender\엘베 맵\엘베_맵_체육센터_3층.blend` | 17,619,218 | 과거 전달 해시 일치 |

현재 SHA-256:

```text
E:\ProjectWait\Blender\엘베 맵\엘베_맵_실버.blend
3FCACCC1913A7527D04FBFD7D70E7C8FED3D72B3FE3E73ABDCEB7707BF5C9078
E:\ProjectWait\Blender\엘베 맵\엘베_맵_체육센터.blend
418134A63D808206E23A5AF10F86ED40C89185FA66C5020285B73646AD057F37
E:\ProjectWait\Blender\엘베 맵\엘베_맵_체육센터_3층.blend
EDF4F0187485B3AA5CE09EC472323225D407A4C02A5EC088E2582485C82A7CE6
```

## 7. 비우기 전 의존성과 압축·복원 주의점

### 정적 조사 결론

- 외부 C# 126개 문자열 검색과 자산 926개/GUID 63개 비교에서 직접적인 런타임 임시 의존성은 발견하지 못했다.
- 최신 README 및 기존 저장 씬 검증도 런타임의 임시 폴더 비의존을 기록한다.
- 이번에는 Unity AssetDatabase 조회/씬 로드/컴파일/Play Mode를 하지 않았다. 정적 검색만으로 동적 경로 조합·바이너리 내부까지 보증하지는 않는다.
- 따라서 **삭제 전 압축 확인, 삭제 후 Unity 재확인**이 필요하다.

### 남은 에디터 출력 경로

| 파일 | 용도 |
|---|---|
| `D:\project\ProjectWait\Assets\M\Scripts\Editor\ElevatorCopingSetup.cs` | 임시 디렉터리 생성, 작업 전 씬 사본, 적용 JSON 출력 |
| `D:\project\ProjectWait\Assets\M\Scripts\Editor\ElevatorInteractionSetup.cs` | 임시 디렉터리 생성, 상호작용 전 씬 사본, 설정 JSON 출력 |
| `D:\project\ProjectWait\Assets\M\Scripts\Editor\ElevatorSportsCentreSetup.cs` | 임시 디렉터리 생성, 임포트/재구성 결과와 작업 전 씬 사본 출력 |

기존 임시 파일을 런타임에 읽는 용도로 확인된 경로가 아니다. 해당 도구를 실행하면 폴더가 재생성될 수 있다. 이번에는 코드 변경 없이 기록만 남긴다. 과거 구성 도구는 최신 변경과 충돌할 수 있으므로 단순 확인을 위해 실행하지 않는다.

### 보관 체크리스트

- [ ] 이 MD와 별도로 `D:\project\ProjectWait\Assets\M\CodexTemp` 전체를 압축한다. PNG/JSON/코드/`.blend`/`.blend1`/Before/OriginalScripts를 임의 누락하지 않는다.
- [ ] 숨김 파일·모든 `.meta`·한글/공백/기호·끝의 `~`와 폴더 구조를 보존한다.
- [ ] 빈 폴더 포함 여부를 확인한다. 최상위 빈 폴더는 3.12절 및 10절에 기록한다.
- [ ] 폴더 자체도 삭제한다면 **폴더 바깥의** `D:\project\ProjectWait\Assets\M\CodexTemp.meta`를 별도로 압축에 포함한다. 내부 1,212개 통계에는 이 부모 메타가 없다.
- [ ] 압축본을 프로젝트 밖 별도 위치에 시험 해제하고 파일 수·바이트·SHA-256을 11절과 대조한다. 이 문서가 압축 성공을 보증하지는 않는다.
- [ ] Unity의 저장하지 않은 사용자 변경을 먼저 확인한다. 정리를 이유로 임의 씬 저장/전환/되돌리기를 하지 않는다.
- [ ] **압축 확인 후에만** 원본을 비운다. 이번 요청에서는 원본을 비우지 않았다.
- [ ] 정리 후 Unity 컴파일과 두 M 씬의 참조/실행/선택/말풍선/강조/스카이박스를 확인한다.
- [ ] 복원 시 원래 경로와 `.meta`를 보존한다. 임시 Before 코드·씬을 정식 위치에 덮어쓰지 않는다.
- [ ] 도구 재실행 전 외부 전달 경로/씬 변경/저장 경로를 검토한다.

부모 폴더 메타의 현재 참고값:

```text
파일: D:\project\ProjectWait\Assets\M\CodexTemp.meta
SHA256: B414344540C63BC775C0B5FD5DF9656B195529386DAB9A0DCA2F51075AF7545E
GUID: 809d606b7ed6f784282fe2be4589023b
```

## 8. 보관 자료를 다시 찾는 순서

1. 최종 동작: 이 문서 2절 → 마지막 제목 수정 README → alpha-only README → 가독성/연출/자유 시점 README.
2. 시나리오/저장 상태: 각 작업 Summary/Completion/SavedValidation과 실제 검증 JSON. 마스크는 GPU 픽셀 비교까지 함께 본다.
3. Blender: 실제 `.blend`/내장 텍스처·폰트 설명/재열기 검증/전달 해시. 변경된 E 드라이브 실버 파일과 과거 사본을 구분한다.
4. PNG 277개는 포즈·맵 렌더·UI 단계/중간 페이드·마스크/가림 자료다. MD 안에 이미지를 넣지 않았으므로 압축본이 필요하다.
5. Failures/ConsoleFirst/RegressionInvestigation은 중간 실패일 수 있다. 마지막 결과와 시간순 비교한다.
6. Before/OriginalScripts는 특정 요청 직전 스냅샷이지 무조건 최신 복구본이 아니다.
7. Verify 스크립트는 Unity Pipeline `run_script`와 특정 씬/Play Mode를 전제할 수 있다. 일반 런타임 파일처럼 프로젝트에 복사하지 않는다.

## 9. 이번 문서화의 확인 범위와 한계

- 파일 목록·기존 MD 8개·주요 검증/요약 JSON·에디터 출력 코드·현재 일부 핵심 코드/머티리얼/메타를 읽어 종합했다.
- 환경 파일 값: Unity **6000.3.18f1**, URP **17.3.0**, Input System **1.19.0**, XR Interaction Toolkit **3.5.1**. 프로젝트 파일 확인값이며 외부 최신 버전 조사 결과가 아니다.
- 모든 과거 스크립트 실행/모든 로그 완독/PNG 시각 검수/모든 Blender 재열기를 새로 한 것은 아니다.
- 전체 원본 해시 목록을 생성하고 저장된 MD를 다시 읽어 필수 절·한글·목록 개수/크기를 확인한다. 원본 보존 여부도 대조한다.
- 프로젝트 코드·자산·설정을 변경하지 않았다. 사용자 확인에 따른 범위 예외는 **이 MD의 E 드라이브 저장뿐**이다. ZIP을 만들지 않았고 실물 HMD/플레이어 빌드는 검증하지 않았다.

<!-- AUTO_INVENTORY_START -->

## 10. 폴더별 보관 목록

생성 시각: 2026-11-03 14:49:32 +09:00. 총 1,212파일 / 353,806,596바이트 / 337.42MiB, 최상위 작업 폴더 35개.

| 최상위 폴더 절대 경로 | 파일 수 | 바이트 | MiB |
|---|---:|---:|---:|
| `D:\project\ProjectWait\Assets\M\CodexTemp\AssetMigration~` | 4 | 43,877 | 0.04 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~` | 11 | 5,336,943 | 5.09 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~` | 37 | 279,167 | 0.27 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~` | 58 | 3,424,733 | 3.27 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~` | 46 | 15,041,722 | 14.34 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~` | 44 | 7,794,597 | 7.43 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~` | 29 | 39,211,135 | 37.39 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~` | 64 | 9,587,810 | 9.14 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~` | 60 | 10,413,476 | 9.93 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~` | 48 | 6,762,515 | 6.45 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~` | 14 | 64,420,663 | 61.44 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~` | 76 | 14,102,670 | 13.45 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA` | 39 | 2,409,927 | 2.30 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~` | 49 | 32,360,807 | 30.86 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~` | 34 | 48,128,589 | 45.90 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorTools~` | 5 | 26,950 | 0.03 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorUIRedesign_20261029~` | 0 | 0 | 0.00 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorVRWorldUI_20261029~` | 0 | 0 | 0.00 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FrameRateLimit~` | 0 | 0 | 0.00 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~` | 12 | 61,652 | 0.06 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~` | 21 | 13,230,979 | 12.62 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview` | 34 | 1,422,628 | 1.36 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~` | 42 | 4,751,902 | 4.53 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~` | 84 | 12,460,049 | 11.88 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~` | 7 | 23,490 | 0.02 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererToolValidation` | 2 | 1,335,276 | 1.27 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview` | 12 | 1,698,804 | 1.62 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxReview~` | 2 | 13,544 | 0.01 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SpatialUITools~` | 0 | 0 | 0.00 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAuditTools~` | 1 | 2,068 | 0.00 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~` | 84 | 9,204,994 | 8.78 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TmpDisplayFix~` | 0 | 0 | 0.00 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~` | 114 | 26,496,173 | 25.27 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~` | 112 | 16,623,528 | 15.85 |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~` | 43 | 6,757,113 | 6.44 |
| `D:\project\ProjectWait\Assets\M\CodexTemp` 직접 파일 | 24 | 378,815 | 0.36 |

### 파일 형식 통계

| 확장자 | 파일 수 | 바이트 |
|---|---:|---:|
| `.json` | 351 | 58,172,785 |
| `.png` | 277 | 161,410,976 |
| `.cs` | 249 | 1,872,440 |
| `.txt` | 143 | 5,165,750 |
| `.meta` | 62 | 81,153 |
| `.log` | 40 | 549,190 |
| `.py` | 33 | 229,932 |
| `.unity` | 20 | 28,271,978 |
| `.blend` | 8 | 82,039,810 |
| `.md` | 8 | 41,621 |
| `.asset` | 7 | 4,359,085 |
| `.ps1` | 6 | 38,830 |
| `.shader` | 4 | 4,865 |
| `.blend1` | 2 | 11,565,460 |
| `.csv` | 1 | 1,617 |
| `.mat` | 1 | 1,104 |

### 파일이 없는 최상위 폴더

- `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorUIRedesign_20261029~`
- `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorVRWorldUI_20261029~`
- `D:\project\ProjectWait\Assets\M\CodexTemp\FrameRateLimit~`
- `D:\project\ProjectWait\Assets\M\CodexTemp\SpatialUITools~`
- `D:\project\ProjectWait\Assets\M\CodexTemp\TmpDisplayFix~`

## 11. 전체 파일 무결성 목록

각 행은 원래 절대 경로와 파일 바이트, UTC 수정 시각, SHA-256이다. 경로를 압축 해제한 보관 루트로 치환해 동일 파일을 대조한다. ZIP/복사 과정에서 수정 시각 표현이 달라져도 내용 동일성은 바이트와 SHA-256으로 확인한다. 부모 CodexTemp.meta는 7절에서 별도로 기록했다.

| 원본 파일 절대 경로 | 바이트 | 수정 시각 UTC | SHA-256 |
|---|---:|---|---|
| `D:\project\ProjectWait\Assets\M\CodexTemp\AssetMigration~\Migration_Result.json` | 191 | 2026-10-22T03:37:58.9106531Z | `C6AE299BAE09612145DAA3F3CC8E3828770EA278E7A15AF805C9A2A7E0DFF2A0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\AssetMigration~\MoveTestAssets.cs` | 11081 | 2026-10-22T03:37:30.6046063Z | `3A45E5DCF46B1ADAF15000F53BBC2EA42EB365EADF8BA6A6C642B1F61F4A023F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\AssetMigration~\Plan.json` | 32084 | 2026-10-22T03:37:58.0850261Z | `6E6BB4B81BB5CC510483393F4F0CE6B7F446A93E519985C8431F212FCC4FD392` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\AssetMigration~\Verification.json` | 521 | 2026-10-22T03:38:30.4288839Z | `23468AF42E4CBD9CBB2D37BAA9EE92D997A535142C8F2DE96E713AB9A752D702` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\build_friend_blend.py` | 16298 | 2026-10-22T00:52:43.3827303Z | `969598A9EC0F3DFDDFE0335FB66B4A4A2B00214652CA78F0F0737F92481B22D6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\Export_Validation.json` | 792 | 2026-10-22T00:52:46.5635093Z | `7F7A59113346AA875AD6E39EF2354CBBBDD1193D11D4EE1F4D818781B99855C8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\ExportFriend.cs` | 6102 | 2026-10-22T00:51:07.0026120Z | `15F0F2EF981F75726CF7708E41575BA4A97BEA29A062F221004B78F956169DD5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\Friend_Milo_Front.png` | 918297 | 2026-10-22T00:53:11.4380660Z | `5224A2C33BED14C6A594432FA8DF2B2D29B5B4AF7AE9725F84F502E46BBEE4BB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\Friend_Milo_Preview.png` | 934779 | 2026-10-22T00:53:10.7065080Z | `D723FF8FDE9E29D07A18E9531BACF4BED6D7794A637427D79C1BD3D668683680` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\Friend_Milo_Prototype.blend` | 331523 | 2026-10-22T00:52:46.5267514Z | `FDBE42D895AF147BDF40C5D4EBE9A60F898BCE5A6A01D88C7807BDBCB75A6904` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\Friend_Milo_Source.json` | 3121852 | 2026-10-22T00:51:10.6567266Z | `42E49D2E800972DBD788C0E242A25A8A28E7D2B3E654F42742720BB3D07136EE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\InspectFriend.cs` | 1429 | 2026-10-22T00:50:35.5156832Z | `EFD27EA4A3C6C6E4B0831691E9B440D875063F9C83CE3E40BBAD1D019967C8C6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\README_친구캐릭터.txt` | 1845 | 2026-10-22T00:52:46.4984215Z | `E9775D4CFEB7F5CE21C0D8E9974A2BA484B3EC174EB3EB9CB3FF129EB590DD34` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\Reopen_Validation.json` | 427 | 2026-10-22T00:53:20.4608715Z | `0C9CEF80962E8B692A06920AC057E27984569DA2063E409506C6AEA2D6991B92` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\BlenderExport~\verify_friend_blend.py` | 3599 | 2026-10-22T00:53:15.4327479Z | `A82CC533AC3E83CCBEF95A700AC9A75BA98803464500DC84EC29AFCE78672A90` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Baseline.txt` | 6918 | 2026-11-03T01:21:05.8583187Z | `891A63CA6C6C6F27A3A1E63925E394A3D5FD9544586524D9698E763DA8EB66BE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorCameraEffects.cs` | 4503 | 2026-10-30T09:18:17.9883423Z | `BC01C4E618CE5E5DD81C41BA01CF336B17FEEBB2F4B83A1EE2D45C26139E499D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorCinematicOverlay.cs` | 5673 | 2026-10-30T09:09:26.9658274Z | `9775BCBB0FF9098291C36027907F1417496034BD9D631A3611E8E301E2FF78A5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorDesktopControls.cs` | 13070 | 2026-11-03T00:46:47.8937558Z | `524C8EE8DBE6967E6F70626D3BB7EFB7B7F0D14AF62F758E0150E373AE17EF1B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorLesson.cs` | 11848 | 2026-11-02T23:44:24.8036495Z | `3C98A0BDEC9A54EA6CD645832D5B4982FA783EC24D3E09A2245E0BB2358E1235` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorObservation.cs` | 5804 | 2026-11-03T00:49:27.7413773Z | `91DF094B8EFF26F727B40897F6F6E472AE47020B9D4AC03830003816BC3147D1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorPlayerMovement.cs` | 2835 | 2026-10-30T09:09:26.9637075Z | `FA6B54434C0D8F49307298A247CE39162D11CFD964C43B1EE8D837D26688C016` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorScenarioDirector.cs` | 8356 | 2026-11-02T23:44:24.8046816Z | `F2ECF8E0D8A1488769D502F0F30774A91EBAD19A8EA5DA9473B51DBFEF33EF15` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Before\ElevatorSportsCentreMap.cs` | 12474 | 2026-10-30T09:13:00.8444049Z | `6489F8636A76384E0776F2614249D9936E4D1361B097FC2D9ED0D83FBC5E4FF8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\CodeReview.txt` | 3601 | 2026-11-03T01:23:07.8345656Z | `2C716A1C17A7FCD6A1659380BE505A849375D0623B1EBC34C3C454A0A8B60CB8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Compilation.json` | 429 | 2026-11-03T01:23:07.9850143Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\CompilationFinalStatus.json` | 440 | 2026-11-03T01:28:28.6442371Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\CompilationStatus.json` | 440 | 2026-11-03T01:24:36.5991743Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\CompletionCheck.txt` | 3830 | 2026-11-03T01:29:35.8060380Z | `B65458CC7CC2CF83E6F53744FD36CB40AE90B99DF53232E2DC54BBC6002BA2F5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\EditorBefore.json` | 591 | 2026-11-03T01:19:52.1827935Z | `F7790731B3C377ED5EA6CB883BCDBF048F07128340261AAB64857308E5ACB41E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\EditorFinal.json` | 591 | 2026-11-03T01:29:35.8823387Z | `168A0209935A6D255EFCD6928C68C902D414AC99DA6E29D52022121CBED43ADB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Elevator_Safety_ObjectInteraction_Test_Verification.json` | 13216 | 2026-11-03T01:27:13.1096884Z | `3E5465CB5396D16FADA2BF6BA32E324F8CE577E8D574D3418C4FA2208469DAF7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Elevator_Safety_Prototype_Verification.json` | 13203 | 2026-11-03T01:26:11.9869717Z | `C22B1C87CB2BFB4B540B22BF7AB9E78ABD0504F8FDA02A19916ABA7B1478EFB4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\FinalCodeReview.txt` | 22951 | 2026-11-03T01:28:00.2190458Z | `9827711F6AD17A0D2CEC741549674D60FFC6B4BCCDE481259B45B0C147AB21D2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\FinalConsoleErrors.json` | 51062 | 2026-11-03T01:28:32.9107668Z | `DF13FF4F097889FA2DFAE1BD5D31275A2A5A216118F6CE614BD8F211C7D33FB6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\FinalInputReview.txt` | 10681 | 2026-11-03T01:28:33.6572501Z | `B97C599067FC3DB597C67E75ADC4E3A25E8D02DBCE8B7284908297FB52CF7240` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Inspection.txt` | 16315 | 2026-11-03T01:19:52.0241738Z | `9B8396219CC0EE20AA716BA04F10D7C432889FC3080D4307DDC871BCB204282E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\LiveBefore.json` | 3167 | 2026-11-03T01:24:38.9191630Z | `BF087AACE7A99E420B1094B3A6E327BF1392B4CB54E721D7E393CE2BC083152E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\OpenTest.json` | 739 | 2026-11-03T01:26:45.0441973Z | `B3FD4C329E7FC6215C382C17C991C65CBDA07A4AA2FB1DD9BF8C331A6FF7D537` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\PlayPrototype.json` | 332 | 2026-11-03T01:24:39.7545629Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\PlayTest.json` | 332 | 2026-11-03T01:26:45.6546813Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\PrototypeInteractionRegression.json` | 4991 | 2026-11-03T01:26:43.0836907Z | `AC656D595AE31619B0C83BDBC94143566B11612CACD27B8D73875126F3BEBB70` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\PrototypeVerificationEnvelope.json` | 16874 | 2026-11-03T01:26:12.0138261Z | `4558F013F55C4511B923B05741F8209EB3EF54ACB1C2D1F84A93908562AC7F91` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\README.md` | 3195 | 2026-11-03T01:29:18.9654580Z | `6CFEE2789F8D9686BBF66C744AB3BE9AF8FD7D81804DF85CCE827791201A0241` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\RecompileFinal.json` | 429 | 2026-11-03T01:27:59.1424247Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\RestoredScene.json` | 729 | 2026-11-03T01:27:58.3740635Z | `789D76E49F6D4D966364B0A1D7B37F053CBED25E672C1F329A29EE562F6C0438` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\SavedSceneValidation.json` | 1278 | 2026-11-03T01:28:32.1646254Z | `05E6F4AFE4B8E58FAB6F74787A6FB06702D7B5B229DF2B945556B6134ED0C9E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\StopPrototype.json` | 331 | 2026-11-03T01:26:43.9202738Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\StopTest.json` | 331 | 2026-11-03T01:27:57.2278398Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\TestInteractionRegression.json` | 5004 | 2026-11-03T01:27:56.4429619Z | `3BCBA6B7471E92DEB26554CF75744FFBA908C9F0D9E328D8322AA72C0D8408A7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\TestVerificationEnvelope.json` | 16888 | 2026-11-03T01:27:13.1770488Z | `D97EF2D36B7697E0CCDD0BB7E54A7B1D2147CFA5F461CD764D300DA875E33E03` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\CameraFreedom_20261103~\Verify.cs` | 15716 | 2026-11-03T01:25:45.0327854Z | `00EE379A4B5906D9C251B3B9325EE47D5CA7FFBB9384AED452BEEBBBFD01B156` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Applied.json` | 723 | 2026-11-03T03:45:13.5426532Z | `B79BFB4D6FCF4F6C0C84EE57EA7B848B0E35B6065F56F75FEFA3AFC0AA19B9F8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Apply.cs` | 1127 | 2026-11-03T03:44:54.2515027Z | `373ABF9C19DE4C6CE1F23D4CE6D40277C83E8C95F665453304D6D10C6F3EA657` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Baseline.txt` | 13480 | 2026-11-03T03:33:54.6205056Z | `07A323C327CED5F5E0957B93BD10A15D45443F0F4A257A48695EA9A8307EB797` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorCinematicOverlay.cs` | 5645 | 2026-11-03T02:39:27.8346870Z | `D66064C7D4213037CF4FD28C8F699196893C9284B1085F1164D480A9866B83A3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorDesktopControls.cs` | 13887 | 2026-11-03T02:39:27.8356872Z | `FCCA293FB0C71605639A11E9E795A7D8D9F95314136C5FE724D87D9ADAD60EEB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorFlow.cs` | 12496 | 2026-10-30T05:54:06.6220687Z | `2561085596D72A06C96D8BBF66ED665A9B2CB67CA120465BF998EB728EEFBE8B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorScenarioDirector.cs` | 10838 | 2026-11-03T02:39:42.0419794Z | `AB5AD10366374F756EFACF180076564F74FD70A1319C1C8714C4EF58EC577A8A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorSettings.asset` | 2305 | 2026-10-30T07:22:26.5871123Z | `B06FE3137DF348F8F3D7278FC0CCD07E0B027057FA3E4F39C2016DBC05202688` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorSpatialUI.cs` | 18707 | 2026-11-02T23:44:24.8046816Z | `EEF3B4A3DA4419348C353DD761E2F54C7185C3D5E7CC4DF74D0BDAE93FBEF77D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorStorybookUI.cs` | 14294 | 2026-11-03T02:41:21.0055135Z | `DD5A28B1D13A6279C54285B4145AC3E6C8BED731F42E53064442E9EA4E13F430` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Before\ElevatorView.cs` | 22553 | 2026-10-30T05:54:06.6210727Z | `6E8C9BFE9E353DDA08108F3B9122C86D2A45D6F615F5D24E18AD0B5351021B20` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Capture.cs` | 2306 | 2026-11-03T03:42:04.1011546Z | `5C6B6C27CE96BD80C17F86C3C8E2276F7AB5BCE1643D46C41B70ECF743FE681C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\CaptureEnvelope.json` | 669 | 2026-11-03T03:52:05.0486312Z | `FF4B18F4BD3410D91DA2BADF313629490237DDAF43A52F36E887015C0B449885` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Compilation.json` | 429 | 2026-11-03T03:38:53.3051653Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\CompilationFinal.json` | 440 | 2026-11-03T03:53:42.9839378Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\CompilationStatus.json` | 440 | 2026-11-03T03:49:39.8265540Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\CompletionCheck.txt` | 1627 | 2026-11-03T03:54:28.0494974Z | `80A25E6574BCF987068092C1764164E1555776EA0F14B70C2A8E9011918104E2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\ConsoleFinal.json` | 909 | 2026-11-03T03:53:43.7044380Z | `2623C7DC4052B3AD2A31D0C361A13C2E9830B26A791F66CED2CEEC7B778CDBD7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\EditorBefore.json` | 591 | 2026-11-03T03:33:55.0451075Z | `0F48B05B1397FF973CF48696E04B8D88CE2128EF6DDCCF3CB15632B94394F382` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\EditorFinal.json` | 591 | 2026-11-03T03:54:28.1364961Z | `BBE3E39A2B8725B75290AE728A05922B6B23CC6078CE6EAC1983CB47592ADEFB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Elevator_Safety_ObjectInteraction_Test_01_LobbyReadable.png` | 707612 | 2026-11-03T03:50:01.3968862Z | `9C923A1A330B3D2D5D1BA8A216835169326E2E57DDE16BB494B3D51BA9DFADF5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Elevator_Safety_ObjectInteraction_Test_02_ManagerReadable.png` | 631854 | 2026-11-03T03:50:01.7141391Z | `F8F9552AE2271F69157C795CBB017F5CFDF4AD030CDCDF9D48A156FC7A8C8D7B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Elevator_Safety_ObjectInteraction_Test_Verification.json` | 29315 | 2026-11-03T03:50:01.7570951Z | `DCEABEA7681139A7B2523EE04C91A581C2920785BDD8FB6F1F6F585BEB6C8DB2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Elevator_Safety_Prototype_01_LobbyReadable.png` | 697817 | 2026-11-03T03:51:31.1387498Z | `4DDC7312CAD4BA1E58C79034B464FCC8EE22AF495528000880A2AE891C035A56` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Elevator_Safety_Prototype_02_ManagerReadable.png` | 631519 | 2026-11-03T03:51:31.3858562Z | `974FF3D7FE4FEFDB51533F948561CF3A9EDBFD18CF67491C879F80A351089DCD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Elevator_Safety_Prototype_Verification.json` | 30782 | 2026-11-03T03:51:31.4899931Z | `561474BAD20E295B00D245AA6562A2D83C6208F9176B1C7800E8B17249E0502C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\FinalReview.txt` | 15581 | 2026-11-03T03:52:16.2756451Z | `296C8083D8C7BE97068E44F2946280668F21C2D1E3322843F739CAEC7282AAB8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\FinalState.txt` | 4113 | 2026-11-03T03:53:44.4969868Z | `B9175E554D1D475D6F606F5B624F566DCB430CB12CE7423B9E0C85157786EEF0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\InitialReview.txt` | 16651 | 2026-11-03T03:38:54.9938639Z | `98EAC549DA8B8008532AA00A0AAA5E09FA42CE28988D8FF0F1A1D0D98724888F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\LobbySmall.png` | 197903 | 2026-11-03T03:52:04.9031798Z | `CBBC6214DEE165174A9D5DF5D3B7FD79D125A7BEAAAD2BC885DF28A76B32C424` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\ManagerSmall.png` | 160068 | 2026-11-03T03:52:05.0402156Z | `4EE7722C0D5321EBC610398F13B9FAD7095B7C3D1BCA03AB848544A38EA9C9F4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\OpenPrototype.json` | 729 | 2026-11-03T03:51:15.7826436Z | `0A3C6A18DC27487D0B15C41B863843AF62B7690BA29E2781EDCF8FEC76A2E482` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\OpenTest.json` | 739 | 2026-11-03T03:45:14.7070731Z | `E104A4AA15D5024DEDBE2A461F1EA11C4A180C08B8A84A4B5D0D998DE11D00D4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\PageReview.txt` | 6419 | 2026-11-03T03:41:20.4009014Z | `AA6BEF23F968E77C25CB434812E8E110CFEDBDC4671BC08D0F39656AB624CFBA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\PlayPrototype.json` | 332 | 2026-11-03T03:51:16.5136842Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\PlayTest.json` | 332 | 2026-11-03T03:49:40.5975304Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Preflight.txt` | 1178 | 2026-11-03T03:42:33.3619386Z | `BED012F85984FC80F04E162D8E0F599E7A04811F4EED46E6584BD4642ECB8400` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\PrototypeCameraRegression.json` | 17174 | 2026-11-03T03:51:59.6169999Z | `23FA22EB8960CFC0E439A178958FA12AF788FF797443B9F6BAE1A77811E5A2EF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\PrototypeEnvelope.json` | 38368 | 2026-11-03T03:51:31.6103355Z | `4AF0D3228CAAD11A66299693976F0117439A69B7481F2401C834C8E0E993A88A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\PrototypeInteractionRegression.json` | 4990 | 2026-11-03T03:52:04.0134655Z | `A5F5ED26BE8D972E4C0B37BD9A37569449231D8A0DADD68C16F2D5230418B29C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\PrototypePolishRegression.json` | 8083 | 2026-11-03T03:51:36.7566173Z | `13F0005ACDF3E1E92981337F9E1F2B6392E35E2CF8B05398D61171756AB58237` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\README.md` | 3706 | 2026-11-03T03:54:55.3487023Z | `F6FC97266296F1EE7FE1B325613E260E62FF5C0E9A9BB5FFEE4D018FFAE7CC2D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\RecompileFinal.json` | 429 | 2026-11-03T03:49:21.4088957Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\RegressionFailures.txt` | 1879 | 2026-11-03T03:46:51.3934549Z | `CBD3AB6822838CC03A415F0BBB12142D8E30AB74856DB01B028F4B2DB5082C0E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\RegressionInvestigation.txt` | 8699 | 2026-11-03T03:47:33.6457461Z | `694F09E426E3DC53AC2CED3D47104D73E50ACF18560768162EFC4D3C6A27C8BD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\SavedValidation.json` | 820 | 2026-11-03T03:53:42.2796779Z | `74514637D06F6E44AB646C33194917D5AC3F9F33C571EB3EF15C70CCC3923419` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\StatusBeforePlay.json` | 594 | 2026-11-03T03:43:28.2070762Z | `4AA1A6D6A4E97075B7B7D5B75F9DB8C6BAF93EDE86438CB5628DCA5F185942E9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\StopFinal.json` | 331 | 2026-11-03T03:53:40.1887832Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\StopForCompile.json` | 331 | 2026-11-03T03:42:31.4428306Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\StopForCopy.json` | 331 | 2026-11-03T03:45:12.3551882Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\StopReflow.json` | 331 | 2026-11-03T03:49:20.5667105Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\StopTest.json` | 331 | 2026-11-03T03:51:14.4527088Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\TestCameraRegression.json` | 17187 | 2026-11-03T03:50:18.7672640Z | `96D61666C10C5858AA41E9A2074623B18A9229FCFE38799B37820A42CA6CB21E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\TestEnvelope.json` | 36454 | 2026-11-03T03:50:01.8715246Z | `76EA261C8358FAD322AD2471693450D593E97B504318DE75FC82D9FE89AA2806` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\TestInteractionRegression.json` | 5004 | 2026-11-03T03:50:06.2947350Z | `3A1DE7705F543A89A58F0F7039971CCA7532FB6E533C2CB2294D113EBD4FC8CE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\TestPolishRegression.json` | 8096 | 2026-11-03T03:51:13.6322524Z | `7C6EBDDE951BC0ABD36326B96854C77B0D6E9539B418E2CF5151AD28322CEBDF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\ValidateSaved.cs` | 1584 | 2026-11-03T03:53:20.6863469Z | `39F6F0CFA1EB1DDC3CC7D90B3154E95B9D71D040FC2FF5DC6FC7DECDD39B3300` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\DialogueReadability_20261103~\Verify.cs` | 13010 | 2026-11-03T03:49:02.8071716Z | `A29D8725E85E014329FE4BD87D859341E48DC420EFBB74E9F129A6278FE23D48` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Apply.cs` | 802 | 2026-10-30T07:21:25.2511682Z | `FD08A82A25BE011134ADE085D6FC78366DAC7BA7557B23F52EB0F188BD8092DC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\Elevator_Safety_ObjectInteraction_Test.unity` | 1731110 | 2026-10-30T06:05:15.3364097Z | `619089213A05AE5D4954911A5DD466EEC5DC59C0ADBDCDE5E53D5216E19F168B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\Elevator_Safety_Prototype.unity` | 1731051 | 2026-10-30T06:05:14.6516130Z | `72E076FE26B15E187F0A60A1C1712B51DF52E2922393086CC547AB6EA6080EEE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorHoverRefinementSetup.cs` | 7055 | 2026-10-30T06:03:27.7278605Z | `B1ECC0CE1CD6FAA55F85BF41AEF939368624C0B3BF8C11FA0E8B9AF40603E01E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorObjectHint.cs` | 1289 | 2026-10-30T05:52:22.2287495Z | `AAA1076491DF0B2DCCB2B8410A8EE19221A571B9A62F8ABE19A0460A1B6D4E97` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorObservation.cs` | 1968 | 2026-10-30T05:52:22.2272432Z | `3C9A3B03A10B313E5C52F67E804D0FF5F1AB90C78A7B84AA7562C81184A40FF5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorScenarioDirector.cs` | 8886 | 2026-10-30T06:01:32.8117576Z | `59B0F054F8E17099174B2A692A839E7A1DD6A8E0ED262265D1796B3C81FB3F70` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorShapeHighlight.cs` | 909 | 2026-10-30T02:03:21.7477948Z | `CC660B136209788EFBED945C7970EE55FF4CB94C5509A3266577450A35F2C74A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorSkyHighlight.shader` | 1290 | 2026-10-30T02:29:05.2514943Z | `04142BF9E393F90C83CA7075A30AD4ADE636E8D979CDC290D77A1F8737CBC325` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorSkyHighlightMask.shader` | 1083 | 2026-10-30T02:35:15.6679524Z | `99EF9244C392B4E96E882F57C7E2CBB2A0E9AA87EC0164D9E905354D5271543B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\ElevatorSpatialUI.cs` | 17547 | 2026-10-30T06:02:06.0091409Z | `3FC4354DA9ABF263FB1B8D41D6F5B49AC593EA201D54B8831B715C5FCBFC7DD1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Before\README_엘리베이터.txt` | 6868 | 2026-10-30T06:16:13.1926694Z | `CCCA3F5FAE92102AF1F28611ADD512DF8C2CE7CF4628EDC31F766271D1D0677E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\BeforeLiveScene.unity` | 1736564 | 2026-10-30T07:04:44.9836317Z | `5A11B4970D9D433CE3C83C87B7C1B046941A1A05ADCE3C73CE593ECA8EDDE8D5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\BeforeLiveScene.unity.meta` | 155 | 2026-10-30T07:04:44.9856612Z | `439BDD6BCE396012EDCE0AD79FA718E353F5A9A20C3165CCDD56CCA744B69A69` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Completion.json` | 1435 | 2026-10-30T07:30:36.6983445Z | `2EE48279AA2579CB1213988B6A2ED2FD38AD25ED4DB17D7C0EBD7F0A9AF23998` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_01_LobbyFriend.png` | 570018 | 2026-10-30T07:24:15.4515729Z | `9C2455FD41F9D65E08C69A06C5DC9993224D981AB437B38CB8E520C6B1420E92` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_02_LobbyCall.png` | 541591 | 2026-10-30T07:24:15.8743586Z | `095011799C64B80659FF8C923EED9732DC1B8576989D251A06C9B39635D86D91` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_03_ProposalFriend.png` | 419153 | 2026-10-30T07:24:17.1928210Z | `40F1116E51EC31D8C131BE254805520F2826A5905789EFC20BBF51E94502C236` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_04_CabinFriend.png` | 419749 | 2026-10-30T07:24:17.7787275Z | `F9A65AF8B8E5DB41546C420840D6325909845BF3B2F3619BAE0532DD0EE28B3E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_05_Emergency.png` | 410170 | 2026-10-30T07:24:18.1822602Z | `C6834C915FEBC0ADD03B0D21049E174183F942018BE1D661B860D413A4DA753D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_06_Poster.png` | 489826 | 2026-10-30T07:24:18.5851714Z | `90D18E524FD7DB1D7745CF5C2B954D8C2EF1F278C12E71732CB05C981C891AA9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_07_Directory.png` | 564532 | 2026-10-30T07:24:18.9890629Z | `DF3EF2A6999A4A60A73C3BF3CDE852359C1F5AD214ADBD9CE1B2749B504E5DA5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_08_OperatorDescription.png` | 412819 | 2026-10-30T07:24:19.5934675Z | `BAD3AA0E3858A6BE2ABA18E107B6E8475CB85E98A7C0A80CACBDB31D3C5DEE4A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_09_Identity.png` | 565893 | 2026-10-30T07:24:20.3294105Z | `26EFEBB6E53E8D2666CF01463EA8673B281756AA720F05F62D057498F280BFA3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Guided_10_WaitingFriend.png` | 429299 | 2026-10-30T07:24:21.5355499Z | `7494C7405D899BEB0D418BE2276686C7470E669A39FD00768D79E0AF4EC0C566` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\GuidedEnvelope.json` | 14589 | 2026-10-30T07:24:22.4686705Z | `191101FC900995F41564CFAD09083FB167B9DCC8A0FD569440DE8B0387BFC97D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\GuidedVerification.json` | 12334 | 2026-10-30T07:24:21.8540474Z | `1F2AF0AE769F508D80FA130337F2A67E235850153D46D27B881D3AC721B774A1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\OpenOriginal.cs` | 224 | 2026-10-30T07:15:31.8993368Z | `019F44F38C14637D60BDB18E01527A339D87E4201466178C7772EA13D89DD490` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_01_LobbyFriend.png` | 570218 | 2026-10-30T07:27:06.1919711Z | `11EA717492E7E41C188BAC859CB07B0C90C9D5656547A247474089C8BBCC37D0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_02_LobbyCall.png` | 543317 | 2026-10-30T07:27:06.6182680Z | `115BE12F9966A303761CC4D414526B08E3D2D220DCB808D678E096E64E391FF3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_03_ProposalFriend.png` | 437322 | 2026-10-30T07:27:07.9328986Z | `FD42DBB0C0A82A474FD125FEEEC55A9DEC8F4B71992A358002BBE14AE521E5CB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_04_CabinFriend.png` | 428592 | 2026-10-30T07:27:08.5217676Z | `8B133C83ED3A58A46ED14BD4640C35BB4730A14933C06518519FDF51D3674EBC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_05_Emergency.png` | 419935 | 2026-10-30T07:27:08.9240490Z | `44DAA14395FB9A7ADF7AA7CC3C7A88E97656810F0F8D6104D11EF7E0F33580E5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_06_Poster.png` | 492577 | 2026-10-30T07:27:09.3285289Z | `2FF377BFB57168289062D81345E9E7402AF84B235D41DA12CA8EC120CF84BBC0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_07_Directory.png` | 565265 | 2026-10-30T07:27:09.7306078Z | `5420E039D68586009BEC758EAA676BC367E95CB4A6337FD49D5A96BAAF1CB112` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_08_OperatorDescription.png` | 416122 | 2026-10-30T07:27:10.3187541Z | `9BB70C090915F8D16B67A538ED5430A06E37B2ED3EB4832B40B4AC71080C7477` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_09_Identity.png` | 563884 | 2026-10-30T07:27:11.0238362Z | `AC673335025925AAF192FA2CBF3BCD1762381A036D350857813349F47D1CA598` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Original_10_WaitingFriend.png` | 442102 | 2026-10-30T07:27:12.2246680Z | `324D2F2AF1F5897D5B4E7EE6AB6DEEA0D6EF873866DBE9D3544D4D6AFE42FDB5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\OriginalBeforeStrengthIncrease.json` | 12775 | 2026-10-30T07:18:29.8060895Z | `919E9B94630DFE59A96B9B3FCFF3C9893262046BA049327918C50A8B81E010C1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\OriginalEnvelope.json` | 15314 | 2026-10-30T07:27:13.2978762Z | `9A3CAB1872422AFFBF26306307D5CC0AA9C5630284C18EB4F4C8A8D3305B959F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\OriginalVerification.json` | 12975 | 2026-10-30T07:27:12.5759221Z | `9F05B1DF7BD3176F3C40F9C44A085F5FE705DEB6FFF17A3D5DD4A8F1ACA91277` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\ProtectedSettings.json` | 367 | 2026-10-30T07:29:43.8146594Z | `A08C288A15DD83BB862964EF8C00113F0B5247AB8845213ADA9331A377429C83` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\SavedEnvelope.json` | 4858 | 2026-10-30T07:28:49.8858259Z | `27DF20999611F7358DFB90CAD5D20C4FBA88313C6754D4CD655A23E22F429DF5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\SavedValidation.json` | 3701 | 2026-10-30T07:28:49.2026242Z | `479FCC089F9585C9B654666DD4143C1F2774DEC1913AA13B91AAE649B34D810C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Validate.cs` | 4101 | 2026-10-30T07:20:12.8926123Z | `4968593E01FF1E903E8DF409B495670EB9973A04093C3F991A43927DD87EC7AF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorAmbientHints_20261030~\Verify.cs` | 10088 | 2026-10-30T07:20:12.8910955Z | `EDACA248794E3FC1424328D47612D01CC60CE871B31954E940EF25851AC9520C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\AfterFraming.json` | 450 | 2026-10-30T09:10:59.3351157Z | `19CF2BE0BE13ACEA921B37CA17034FBF13A86F7E1F4D36103E2ABBFDAA86DE1D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\AfterInterior.png` | 349835 | 2026-10-30T09:10:59.4018621Z | `4EA75EB2CBBB9581258EE901D0A8A88A34B67C086BD490E94E1C0C25C1EFED6A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\Elevator_Safety_ObjectInteraction_Test.unity` | 1338886 | 2026-10-30T08:24:15.0850762Z | `1033575B8A01EC767C4516B8175FEEDB5A32F79D36B41C98E1AF406C5F3F9383` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\Elevator_Safety_Prototype.unity` | 1334941 | 2026-10-30T08:24:13.9903510Z | `E888D4CAA726E727B0ECD78D678A93F3BFD79E411B5A0C6D197B90FE6F0896A4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorCameraEffects.cs` | 3329 | 2026-10-30T04:17:10.5710242Z | `763268ADA724F3E449FC290CBF13776B329F1928549D4BC39E03C929BF213639` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorCinematicOverlay.cs` | 5567 | 2026-10-30T03:48:20.0080295Z | `DC354F2F8060BA7D7B86CC6A35B6209EF2B73378E3ED37752AC0155A69DBA8B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorDesktopControls.cs` | 12534 | 2026-10-30T04:00:50.7547767Z | `F179990A7A4B7DB06FDE9C65378EAA075D91A8141B27BCF7BB4D3D8D37D203A1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorLesson.cs` | 11447 | 2026-10-30T05:54:49.1559745Z | `0F08CE7CDAC700A4B4B9889D1A1DD303E80FEF3E731936B8672CBD9842CBEF63` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorObjectHint.cs` | 4441 | 2026-10-30T07:11:18.9016745Z | `2FE73B98C991FA152C6E0D901A1AB7CF8CBF6F5060887C5718EC03D827CDCB6F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorPlayerMovement.cs` | 2826 | 2026-10-30T02:03:19.5406495Z | `7CB994A9F95290E2F6531659B9991661776D4C5CF4911FE033A4CD9A557BE044` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorSpatialUI.cs` | 17055 | 2026-10-30T06:58:41.7844748Z | `91DF2F705E1C1A38B1A86C27F1F46AE075EF1CC37B4A17E6483088749F798FFD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\ElevatorSportsCentreMap.cs` | 10917 | 2026-10-30T04:17:10.5700206Z | `E77A6A341CC1AD8AD4081F937EEBCD9E8F0D626E95C648F79CD375608B9226A2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Before\README_엘리베이터.txt` | 9439 | 2026-10-30T08:42:23.8388230Z | `F07BD624C069898D1B02B94EF8948BC0D93CB1B8346C4CB2612D8ACC6A8B7D8F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\BeforeEnvelope.json` | 1217 | 2026-10-30T09:07:22.0349761Z | `4F5C7FD6F648B5F5329FC9694E7D14EEAB096EB86FB01268CF7DF471C6CBE2E5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\BeforeFraming.json` | 462 | 2026-10-30T09:07:21.3756631Z | `AF00D4814635624937B1415D70F594AC2EB338E8BCE04DE3232FEDB2AF546BF1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\BeforeInterior.png` | 248643 | 2026-10-30T09:07:21.4354965Z | `32BB3F6F0C5673E02687AAA2EB1242DD5E03524A3BEB86DB660F64FBA99B9778` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\CliManifest.json` | 6153 | 2026-10-30T09:03:32.9354377Z | `1CEC2FD255632E77164F1811E47056B89357261609A82DFEC42D8E32A8898314` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\FinalConsole.json` | 80153 | 2026-10-30T09:25:00.3692299Z | `1A98F0F2FD36327C96B239387270D24D9CB4C77D53385589607D2B9955C73B59` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\FinalSummary.json` | 1372 | 2026-10-30T09:25:00.4514672Z | `89D07311B5AFD0329D1B5C0C65F2F2A3A9B0600A980A10F9FF277FAD0E819A58` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Guided_01_Jump.png` | 342274 | 2026-10-30T09:21:35.9074078Z | `83BC74265BD077550F6B7FA1A59E3E255633FBF8F634E7F60F4429482D465E4D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Guided_02_Identity.png` | 526844 | 2026-10-30T09:21:40.0477098Z | `00D29138E4671296312E1A1B3C7BB5F74AB8EABA0E302C566808FCBAB13259D9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Guided_03_Waiting.png` | 508279 | 2026-10-30T09:21:41.6072482Z | `A4F24A669BAACAD03A8788E276715A1F3FAEE4F746F5F3ADE42B2005FFF77290` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Guided_04_Aspect4by3.png` | 382641 | 2026-10-30T09:21:42.5609892Z | `1AC70AE9AC63B31C539EA526641D8B96945CC8F7626BA9411B342FD70ADAFD8D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Guided_05_Rescue.png` | 364083 | 2026-10-30T09:21:45.6486739Z | `A8F396795112CE6BB40C47D4F2820ED11B6180F3E24A5CD85D7DBC1AD018B357` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\GuidedEnvelope.json` | 6700 | 2026-10-30T09:21:46.3019118Z | `C0CEC98B23D5E35B9BB5FC844235DBC20C8ECE056D9A1B7CCA8622C4A1B8EE2A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\GuidedVerification.json` | 5400 | 2026-10-30T09:21:45.6736041Z | `CF3EE8BF5C4E83A41A661F3FFAA09FA665E0C599CC7DDBE7F194726B5DA3D162` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\InstallReference.ps1` | 20027 | 2026-10-30T09:03:07.8466000Z | `B7F8003180C35D5FD5300C919BF4855359F39D87E9CED1091A71B2E960BE8861` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Original_01_Jump.png` | 347948 | 2026-10-30T09:23:15.8410180Z | `617DF70A75253EC80E4DA745CFCFC0D1473E44492679DA5C69C690C563C2CA35` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Original_02_Identity.png` | 535444 | 2026-10-30T09:23:19.9821979Z | `7DDAD815AC239CA45118DF7A39511B49974D087BA6118DCD1633E6A64E95F829` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Original_03_Waiting.png` | 521251 | 2026-10-30T09:23:21.5384188Z | `52FD0A27637090C42B4D7A355231D94C3E6B317131EEBF84F3DACEA2052B75CA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Original_04_Aspect4by3.png` | 397653 | 2026-10-30T09:23:22.4934493Z | `9CD9E622A856D37BE2E9452410DF1BF41CFB9B005F4D4C79395BE3388094F901` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\Original_05_Rescue.png` | 364451 | 2026-10-30T09:23:25.5791055Z | `A4B487361E7669FC4BD6129409634BC53D56C5103D29F5F8ACA6B0F15EA2FFBE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\OriginalEnvelope.json` | 7474 | 2026-10-30T09:23:26.2587681Z | `4539FBB21866223AA716E5C54473A7BA00DE9F9F33CEAA2EE36168D86390A242` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\OriginalVerification.json` | 6117 | 2026-10-30T09:23:25.6144702Z | `7DB169EEEC3B4ED8CE9560C513E1FDD9806573875C551A3BF34D302B03CF6F4A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\OutlineSceneGuard.json` | 229 | 2026-10-30T09:24:11.0378260Z | `D9143CFA05F6A564FD67EDA89F5AF4719C31E09BC8DE5BED3A9F6DFB89B52FEF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\ProbeAfter.cs` | 1517 | 2026-10-30T09:10:44.1489882Z | `FC53D1244357398B1BB78160A15F326DE691679BF4D1B11A673C4190C69028C5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\ProbeFraming.cs` | 1521 | 2026-10-30T09:07:16.8377429Z | `5C3B52238CB9BC829BC80CECB00D3EFDF45231A5376A4C566876DCAD4A36D419` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\ProtectedAfter.json` | 2578 | 2026-10-30T09:25:00.4443081Z | `D0E0D9D6E4FDDD3D70F2C065C3F89BD7DC4E3ADBC3D4B98D04312620A3DD4C19` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\ProtectedBefore.json` | 1538 | 2026-10-30T09:03:04.6538724Z | `7E923ADDF9F4461237AD73AD1E06330894C3D6F72196C710BD14B8E95EBA379A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\SaveAndOpen.cs` | 1698 | 2026-10-30T09:21:07.5646691Z | `4703D5CF42041057B2B5C7785D7C72011F5083E20D2AF8166ED3FD519F1B51D8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\SavedEnvelope.json` | 1147 | 2026-10-30T09:22:59.3243869Z | `C9915126A203AA844C347FD6FFF31F8EE9FCB47B80695B2F7B483EA3E7E204A3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\SavedScenes.json` | 405 | 2026-10-30T09:22:58.6717426Z | `D51EBAC9078BFBA730BDC8B55B7A650DE140CE2E5BB5CCB5B1BCE3F7755B444C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\State.cs` | 567 | 2026-10-30T09:16:37.0398511Z | `D5AEA814E4A7A041617FA88D7D4CED709F9CF225338FA5AFE3923AB2380448A6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCinematicFraming_20261030~\VerifyFraming.cs` | 7144 | 2026-10-30T09:21:30.2315098Z | `26B5C381867284F25B759497DBEF067CF53D21F83293020276BCF42591B2D455` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\02_내부_draft.png` | 861276 | 2026-10-29T09:52:09.1324322Z | `82AA5748F03497ED33CEFB5EE62B3EEF3A1FCB6C51CB2145406A556D0FE768A4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\06_식별정보_포스터_draft.png` | 825398 | 2026-10-29T09:52:37.8684224Z | `E2606A557ED641B46D26E271B7C7A7E35385A70798D55E0168866FD2AA496D21` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Build.log` | 9240 | 2026-10-29T09:52:38.0313765Z | `B0FC9DB5A3873B590908E6201B3A033EFD8A4E76B3A776305189A9D20DC76810` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Build_Validation.json` | 3919 | 2026-10-29T09:51:39.9548266Z | `014D35317FEFBC3729627F74523A3532759D9643F2C39198EE5550756185ACFA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Completion_Audit_20261030.json` | 3413 | 2026-10-29T23:54:44.4594150Z | `EC23E0FC1626102B1F3561FAC3053959DEAB287A4203F9F858FA1792F5D86C34` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Delivery_Validation.json` | 2759 | 2026-10-29T09:58:06.8028733Z | `6726FEAC5C06A109ACF89827E16881BD840B587B12B151438D1BEB93AB49CC46` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\DestinationReopen.log` | 2546 | 2026-10-29T09:57:16.0480318Z | `7EFBCA45792188DEE6284CC2EBDD2E7A7228CAE4197FA66B0D418EFAE40119CD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\FinalRender.log` | 6914 | 2026-10-29T09:57:13.8679888Z | `0153C9CCCDEA3971FA2124EA14AFC1100D092DF26E8FC6066550A8586E800A67` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Inspect.log` | 69460 | 2026-10-29T09:40:27.3618214Z | `A97641DDC7FC8578DBB137E4190D6F89E010591F7E5A78ECAA632513F94D1711` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\inspect_latest.py` | 797 | 2026-10-29T09:40:24.2869926Z | `0A8A96CBBE6BCAE34A4F0FE33EA96722103429059CD5A2F0E8B3131CBCD3FD06` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Latest_Inspection.json` | 58538 | 2026-10-29T09:40:27.2253750Z | `74F3A31785F4B2B2151728FD380EDECF089A716025637023E46146CA7D018CF6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\render_sports_centre.py` | 2056 | 2026-10-29T09:53:01.2603262Z | `C209FD3E5671DFE960C314C76F1E0511E41A02BFDF21BA9B59C375E4CA3AD7AF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Reopen.log` | 2744 | 2026-10-29T09:53:04.2587132Z | `6256514922E91DB20616C982A84075675F01195B8F4A806E2D4915A2411E804F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Reopen_Validation.json` | 1306 | 2026-10-29T09:57:15.9168629Z | `DEF974F6238ACC7046734ABADC24F663B7647D244843FA18E759B9E7FCD22DDD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\revise_sports_centre.py` | 12245 | 2026-10-29T09:51:36.6951915Z | `460F8332D48A5AD451D8C48D2D22DD35DB6C7D8A03C55D2678393A5F76F1512B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\Source_Hash.json` | 198 | 2026-10-29T09:40:25.3636417Z | `90A3D0CA49D159B2BCF3207073495974F3D879FEC4C57A519ADFC164A7E43315` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\verify_sports_centre.py` | 4782 | 2026-10-29T09:53:00.5773351Z | `E44E20C37B7E217A191C6DE2C6C15FFB7E9153D59E1ABDFCA6AB0B32345AE3AB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\건물콘셉트_시나리오.txt` | 11051 | 2026-10-29T09:48:15.5821473Z | `D1C74A4A0B580D546F2E7F711FDAC14E4B39E2DA4F98317FD181D9561FBD8CE3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\엘리베이터_안전포스터.png` | 1563054 | 2026-10-29T09:41:40.8043559Z | `A3B52E362198AA278730D38ED727B8347E6E4F2A4ADB53609EA659A87FDF3FA8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\엘베_맵_체육센터.blend` | 17086110 | 2026-10-29T09:51:39.9272290Z | `418134A63D808206E23A5AF10F86ED40C89185FA66C5020285B73646AD057F37` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\원본_사용자최신.blend` | 8404708 | 2026-10-29T09:36:16.0000000Z | `3FCACCC1913A7527D04FBFD7D70E7C8FED3D72B3FE3E73ABDCEB7707BF5C9078` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\읽어주세요.txt` | 1632 | 2026-10-29T09:51:39.7774462Z | `82E5958A99D23ABBC78FF40103442265EA8CC3A26B8DD43A369BFCBC0AC0C573` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\체육센터_01_로비.png` | 1698342 | 2026-10-29T09:55:19.5055364Z | `311B5992685E6547C81643ACD55DE0CDEE7A0D227178477670F719A19832205D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\체육센터_02_내부.png` | 1667999 | 2026-10-29T09:53:56.5334205Z | `01EF956E583B575C4D8F8FB341B270326B5A42847FB047CE9612F5410BECCF62` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\체육센터_03_뒤쪽_자동문.png` | 1570997 | 2026-10-29T09:56:39.2592782Z | `B571C760C2E00323FBAEC9DA98B16ECC805EE55488422C400B3B3585F785D850` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\체육센터_04_전체배치.png` | 1682985 | 2026-10-29T09:57:13.7051150Z | `19F1FFBF42CA891E0C245E53151B5C3FD063A9708B9BE7174107C0164E9DF1CE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\체육센터_05_버튼패널.png` | 1657885 | 2026-10-29T09:55:57.8347849Z | `D57940CDC4DBDF71F23D7A410F66CE5C8649A3A9544429C7A3227DB7341B047C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\체육센터_06_식별정보_포스터.png` | 1996519 | 2026-10-29T09:54:39.9001127Z | `DE575CA6C787F247FDEC612071C157E70576A0190DCA1FF6D503E0A08370A54B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCommunitySports_20261029~\포스터_생성프롬프트.txt` | 2262 | 2026-10-29T09:41:56.0250528Z | `BB29070D886B0FFEED6F43A8494D160826560067D92424C3868B0C8C2E1FF157` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\01_DownCallAndFixedHint.png` | 517335 | 2026-10-30T04:33:49.6253907Z | `ADF76C6F76C9AFBB59F3FE629DF3109C74C8D626B2B8964C37A0C46771FBF584` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\02_BoardingWalk.png` | 374665 | 2026-10-30T04:33:51.9841360Z | `79D27B00EAD6C90C0A900D05DC93F4C7FD4E3740CCE29B4FDA190E5EC50782C8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\03_WorldUIThroughOccluder.png` | 70502 | 2026-10-30T04:33:58.7891217Z | `4761DDADCB6E053E1272B23C53F8E6E96DC143D2AF77CEC0015C6FD4AA02A407` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\04_FriendJump.png` | 299905 | 2026-10-30T04:34:00.1881274Z | `47758337D72026FCD094DF8425C1ACFF4CB0147776C8EAC9990A53B4A4E9D254` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\05_FaultAndPanicFace.png` | 310599 | 2026-10-30T04:34:00.5752113Z | `D5031CBEDD7E1CA4790B3E5837644CA909C4E10C49EDEACD2C03CD6C2CF02BC9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\06_FadedWaiting.png` | 27260 | 2026-10-30T04:34:02.5607960Z | `1889B0F9DF5A4FC19B4B922680AF5623BBAF464C658BA2DA16A95482CA21EFA0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\07_OriginalComplete.png` | 438681 | 2026-10-30T04:34:03.3176697Z | `B1E492633C980668DEDB055B9BBDF763FFFE2355FF9AF107DBDB2AB3585F3369` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\10_Test_ObjectGuidance.png` | 383452 | 2026-10-30T04:39:33.8594469Z | `02D96BA89B5C468545CAB5191015E1DEC05EBA1528A037DDA956B0061CD2F98C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\11_Test_BoardingGaze.png` | 325386 | 2026-10-30T04:39:36.7250893Z | `11131A0A51066A3686905C3A1F3219CD44D57781FAD2F7AF36894D9D811DFADF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\12_Test_ShotGaze.png` | 306291 | 2026-10-30T04:39:43.2723849Z | `D0E8A3497BB2A6E56D720F27D3BD679341C9B516BE46A1457B68D824405C6D85` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\13_Test_FaultGuidance.png` | 418341 | 2026-10-30T04:39:47.1831926Z | `E6B58EF5FD565A6A108DAED31A17511BD341F21AF7319A6F388F7D160B12640C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\14_Test_StaffRescue.png` | 543650 | 2026-10-30T04:40:01.6735527Z | `F6F6C27E2FA6504C223F27785BEBCF4A6B11668E897A97E485415099565A006E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\15_Test_Complete.png` | 429293 | 2026-10-30T04:40:18.3051781Z | `ED8F38B74A011B7167596D5A1E5C76CB64BA593A99D351639A1CF3003D4B5823` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\ApplyOriginal.cs` | 98 | 2026-10-30T03:55:43.1884944Z | `58E498204C9ADEC0455E1D10F7A479E3B02246CD33A3EF7E133C9F740C42BABF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\Elevator_Safety_Prototype.unity` | 1447650 | 2026-10-30T02:44:27.2376259Z | `71C4244C1B760947A1D4584B99936DDB56D9590C70CE264AB08577C89883039E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorBillboardCanvas.cs` | 459 | 2026-10-30T02:05:40.9271491Z | `C0EC274D1B208A75BC55D656C06F7BB2F0219F43AB3286805B8E5D361EA5F3ED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorCinematicOverlay.cs` | 5353 | 2026-10-30T02:04:27.6871291Z | `6366C3636366B966D865CA2B168FDF0F04129B6BD559E9FB65E07FD457664F17` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorDesktopControls.cs` | 12423 | 2026-10-30T02:04:24.6918338Z | `2E91293A0EFD249B7849939B01B7728F71DFA8DDC00F310C9CFBDEBD6E3C229F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorFlow.cs` | 11852 | 2026-10-30T00:46:12.9812563Z | `E08DE5FB10259CDD04FC74F9DC03C9EF54331C66AA6D43E0B4A53D8C75411718` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorLesson.cs` | 11197 | 2026-10-30T02:04:25.4450072Z | `A3B680621E594DB4362D1DCC306B3C6ECD2A195F3D4492EA0B3EB496247AE980` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorMemoryGraphic.cs` | 5878 | 2026-10-29T03:27:26.9490751Z | `680059D3114B62C42608474ED040EB5F4E6198FFCE006A62C61EC0ADF9A48814` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorObservation.cs` | 1711 | 2026-10-30T02:04:26.2068233Z | `4135CD4EF2220A486B96F05F840004DF9D05E6D323E7D9BE8522F50DA69D31DC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorPauseMenu.cs` | 3768 | 2026-10-30T02:03:20.2971217Z | `AC6D5BE96FFBE43EBB405A9937EE46D123F20E5BD910B919DDD2E9D53D851843` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorPlayerMovement.cs` | 2826 | 2026-10-30T02:03:19.5406495Z | `7CB994A9F95290E2F6531659B9991661776D4C5CF4911FE033A4CD9A557BE044` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorReticleGraphic.cs` | 1379 | 2026-10-30T02:29:04.3748243Z | `B73AD579E670B7B656225CAC546F9C397AD13FF6761D35BDDD764E3902057157` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorSceneRendering.cs` | 662 | 2026-10-30T02:03:22.4659959Z | `F913D93980ED974DF2D104ABE215836ADFAD8D9D5C438869757862BDA64EFB65` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorSettings.asset` | 2263 | 2026-10-30T00:59:00.7016895Z | `9096726E19D45ED904FB66FB7FC5AEDFB3B376B21192B905D4ACE344E0642A9F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorSettings.cs` | 1992 | 2026-10-30T00:37:07.7918697Z | `054AFF2CE130426C48C409FC94BF56EE11CE43BC0730249A2DA5C2ED9A4C1A43` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorShapeHighlight.cs` | 909 | 2026-10-30T02:03:21.7477948Z | `CC660B136209788EFBED945C7970EE55FF4CB94C5509A3266577450A35F2C74A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorSpacePresentation.cs` | 5150 | 2026-10-30T00:37:09.0874283Z | `BAAB80B44E48C6B4DB476AFBFF606E282628F84511470DE4ABEBFD1C3FB09309` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorSpatialUI.cs` | 16799 | 2026-10-30T02:04:28.3946374Z | `C5AFAD47203763603140B9567DAB99BD5425716B212A8DF948AA5805EFA23A3A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorSportsCentreMap.cs` | 10365 | 2026-10-30T02:05:41.6311105Z | `2445C91F8660192AF5DEABD658F4467F4D4C8BE47647B80D48FBC1AD440C06B1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorStoryPresentation.cs` | 12234 | 2026-10-30T02:04:26.9591818Z | `F81E7F0FB53115CCEDBF0D744EABEAECAFA246660BD2A19F0593C5CC19C2AF83` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\ElevatorView.cs` | 22268 | 2026-10-30T00:46:17.0604593Z | `105B7FE25E132914614A40FEEA995E5905CF718C67CA2F13304ABC20C978D0AB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Before\README_엘리베이터.txt` | 7394 | 2026-10-30T02:49:58.2597695Z | `2C1805DB7931E8BAF59A9E31AF8E6045268F90BF8843621FB298AB6FA0DD4C36` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\BeforeLiveScene.unity` | 1448178 | 2026-10-30T03:57:02.7748143Z | `1EAC86E7883948C97D5AC0CD9CFFD2BC04CAF32B6F555E827BFADB65E4E3D201` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\BeforeLiveScene.unity.meta` | 155 | 2026-10-30T03:57:02.7758549Z | `3A1609EDE6E266223556B7DB72D02CEBFDEC52309B48820783137077B15CA26F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\BeforeScene.json` | 2752 | 2026-10-30T03:47:33.7696302Z | `26FC442F6E34E47123818B6BC34F2D865A4D001E5ED9A22B5A791DB497D45208` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Completion.json` | 568 | 2026-10-30T04:44:46.8966064Z | `3955864E936087298C5F4B031375DB52198327C02C53E52D793F08EF5DE18C8D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\CopyTest.cs` | 119 | 2026-10-30T03:55:43.1884944Z | `F7C564040E05B4835331E294DFFF2332E1F074160F6C71E5BC9BB16B942A68E1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\ExpressionPreview.png` | 363565 | 2026-10-30T04:31:52.8311390Z | `3052C7C5EBD79BC48EC8A08EA882057FD64C2850BAFD9169CB40C7C69CE58C15` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\EyeGeometry.json` | 190 | 2026-10-30T03:53:15.4686499Z | `DE9099AFAECAF3AD39F45A32E73D0B148E7924255BA9AAB4DE97369BE01D14B1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\FaceParts.json` | 374 | 2026-10-30T04:31:27.7810643Z | `7D86A6E5460ED96A6CD0D236CFF815B0951303E6CE18B499A01BAA42903CE3DF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\FinalConsole.json` | 1386 | 2026-10-30T04:43:52.3334738Z | `FAE0AD47BC03DD441E089F024AE8C50986A009D5F0621669D9392CDB3197F816` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\FinishEditor.cs` | 1292 | 2026-10-30T04:42:58.1772281Z | `90B4262ECF3FFBA8E020F82BC38FE7A2DE0278637BC1930D0477474A3F47EA94` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\GuidedVerification.json` | 11518 | 2026-10-30T04:40:20.4151134Z | `71182E2A8AF10B34CD0FD0BA511D1C4575A9DA78CF049F2157975D9A1ABA3460` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\Inspect.cs` | 1195 | 2026-10-30T03:47:08.3613887Z | `B10A5C3DCE0F07F0060FD9A9ADE5E4D6F6BCA5E349B51737F68A3A8FF953ABF5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\InspectEyes.cs` | 1415 | 2026-10-30T03:52:50.5466882Z | `DA31E5BD9A06DADB757954AEF7A47204240AAA4E25B2ACA9DCB1C55912BCC1A8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\InspectFaceActual.cs` | 2738 | 2026-10-30T04:31:19.4681066Z | `E3B25B2D648F60D656D078BB3978676979AA0CB7E3C87B3EE6CC0DAEBA99A616` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\InspectFaceParts.cs` | 1826 | 2026-10-30T04:26:12.7372587Z | `572DFE606D82C76354B55F9E8F2D43BD727C5983D00E71B81139D2FD60640E3D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\InspectWorldUI.cs` | 937 | 2026-10-30T04:09:10.6462645Z | `B22FBE63156D6E15465546A38075520B32CF408D6A254A276D744567080241FA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\OpenAndApplyOriginal.cs` | 283 | 2026-10-30T04:27:00.2165521Z | `C3F400D48E81F7AF193C54179658454B7058EA5006CD58D1E09B0FFA7DDDDCC0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\OriginalApplied.json` | 170 | 2026-10-30T04:37:52.2685290Z | `793AB37A59456750617EE7B9E2BDFCBBAC45EB6E63A96277FF34E4BFF8626EFE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\OriginalVerification.json` | 5948 | 2026-10-30T04:34:03.6579009Z | `AEC65ABEC1595058A5DDD4993A6DEAF68FCCB2B7E3F9B42BC1343D63D36AC635` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\ProtectedAfter.json` | 427 | 2026-10-30T04:43:52.5576822Z | `E9B9E22AF46FAEF53B010FB4CA693959841B3F859A8C7771F12F90DB0EDD94ED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\ProtectedBefore.json` | 598 | 2026-10-30T03:56:05.3588874Z | `786EFC7CE8A3271FDA37851F87A197A585E0B6CFE81CBB56BCF27B755CD6EDDF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\RefreshExpressionPreview.cs` | 925 | 2026-10-30T04:31:46.6189069Z | `BCE58B3A00083F215AA45602AB71E9ACE0AFACFBEEFD304E948A39018144C695` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\RefreshTest.cs` | 992 | 2026-10-30T04:32:50.0205848Z | `0D568E85A9218629C14796F6041C56DEB919727318A2FC86F7B1504CC90AF097` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\SavedSceneValidation.json` | 4790 | 2026-10-30T04:43:50.8447965Z | `BFD99942B3E3240019863B3595B85E8E9F1658349614BA175577F98B7DCCCDFE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\TestBeforeFinalExpression.unity` | 1677463 | 2026-10-30T04:34:40.3300375Z | `D9C1853BBDC927E725F7A40B0127EB54BA63FB912667F1DF6CA5EAE720431F1E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\ValidateScenes.cs` | 5630 | 2026-10-30T04:39:26.5924016Z | `71DF341122288E2D1011A1D1B13FE7C978B99F3AC1864E61044B0B0647B55F6A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\VerifyGuided.cs` | 8744 | 2026-10-30T04:39:26.5913394Z | `88EAFAB8FF5A6C3C6003C9A7AC2D0E7FEF719E255E4CD7E0BC0D1D388BD3A604` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\VerifyOriginal.cs` | 11836 | 2026-10-30T04:22:51.8187212Z | `6E00D81F13BADD0BF3382E74DB7ED019A19980CCF55621111BB98A0B5FFD1AB9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorCoping_20261030~\WorldUIDiagnostics.json` | 1806 | 2026-10-30T04:09:36.2233902Z | `72AA90991F5B9F49EC5CD9BBD7B191B75B221735A331ED670AB0249229BFBB04` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Apply.cs` | 189 | 2026-10-30T05:54:49.1595678Z | `DBDD5E66B618CEACC3B3E72D2221F7E7660976AC66CAC2E9DD1A5CF5F8712D21` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\Elevator_Safety_ObjectInteraction_Test.unity` | 1677309 | 2026-10-30T04:43:06.8943179Z | `74E45967FD10F013CB5500483DA56A7B745C0BB4FCC32687AF342E6287AB75C0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\Elevator_Safety_Prototype.unity` | 1677292 | 2026-10-30T04:43:06.3132212Z | `76FA46DC7E4D70BA5F0EB0DC6948AE75A14938205665063E93820130AF4ADC40` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorBillboardCanvas.cs` | 459 | 2026-10-30T02:05:40.9271491Z | `C0EC274D1B208A75BC55D656C06F7BB2F0219F43AB3286805B8E5D361EA5F3ED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorButtonFeedback.cs` | 1575 | 2026-10-30T04:06:47.1204008Z | `659C8ADC63B13D8DE38B589C2C6C4AFB4162C2D78450B5D557D071B93FDA41E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorCameraEffects.cs` | 3329 | 2026-10-30T04:17:10.5710242Z | `763268ADA724F3E449FC290CBF13776B329F1928549D4BC39E03C929BF213639` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorCharacterPerformance.cs` | 4767 | 2026-10-30T04:32:50.0190716Z | `B3AFE606D75BDA19E37362564E683590E7554B729A841193594A5ECCF3D37F04` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorCinematicOverlay.cs` | 5567 | 2026-10-30T03:48:20.0080295Z | `DC354F2F8060BA7D7B86CC6A35B6209EF2B73378E3ED37752AC0155A69DBA8B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorCopingSetup.cs` | 17533 | 2026-10-30T04:45:08.1972937Z | `769726418D9259FC12D5A45E0A06DB54CEA22696A0637227EAF35B235E1B2ADE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorDesktopControls.cs` | 12534 | 2026-10-30T04:00:50.7547767Z | `F179990A7A4B7DB06FDE9C65378EAA075D91A8141B27BCF7BB4D3D8D37D203A1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorFlow.cs` | 12461 | 2026-10-30T03:48:05.0942037Z | `CD154AFC800294F5937D191AA2D13DD7C66D666AB494A7E7499D7F2CB08B7B1C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorLesson.cs` | 12372 | 2026-10-30T04:10:47.7182239Z | `614BFD758A6544C160C3D589D090A054B7C6107DADA5A31E551D6A04235CBF96` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorMemoryGraphic.cs` | 5878 | 2026-10-29T03:27:26.9490751Z | `680059D3114B62C42608474ED040EB5F4E6198FFCE006A62C61EC0ADF9A48814` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorObjectHint.cs` | 945 | 2026-10-30T04:06:47.1234075Z | `8F67D1113276621DDE41F852082FC7CFFDAEB1CC3EF630886561632A9AC35E3C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorObservation.cs` | 1989 | 2026-10-30T03:48:05.0952021Z | `1471B2A8D14A2E9EE773923E992DA9BE2EEF0A0A07A7A1C07774E9182212C64E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorPauseMenu.cs` | 3671 | 2026-10-30T04:10:47.7223686Z | `4315ADD529078E14651D9F53CABFE1868F6B3F4C0A888F4DB175F49DBC726322` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorPlayerMovement.cs` | 2826 | 2026-10-30T02:03:19.5406495Z | `7CB994A9F95290E2F6531659B9991661776D4C5CF4911FE033A4CD9A557BE044` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorReticleGraphic.cs` | 1379 | 2026-10-30T02:29:04.3748243Z | `B73AD579E670B7B656225CAC546F9C397AD13FF6761D35BDDD764E3902057157` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorScenarioDirector.cs` | 11582 | 2026-10-30T04:37:04.5762655Z | `5AE78B7E7FAA59CF0F104B5D421B40F985685A5C03FE01609DB5D6721FFD2F5E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorSceneRendering.cs` | 662 | 2026-10-30T02:03:22.4659959Z | `F913D93980ED974DF2D104ABE215836ADFAD8D9D5C438869757862BDA64EFB65` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorSettings.cs` | 1992 | 2026-10-30T00:37:07.7918697Z | `054AFF2CE130426C48C409FC94BF56EE11CE43BC0730249A2DA5C2ED9A4C1A43` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorShapeHighlight.cs` | 909 | 2026-10-30T02:03:21.7477948Z | `CC660B136209788EFBED945C7970EE55FF4CB94C5509A3266577450A35F2C74A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorSpacePresentation.cs` | 5150 | 2026-10-30T00:37:09.0874283Z | `BAAB80B44E48C6B4DB476AFBFF606E282628F84511470DE4ABEBFD1C3FB09309` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorSpatialUI.cs` | 16994 | 2026-10-30T04:10:47.7223686Z | `FF739B7E4F1D24F1401CEC5ADEDDC3B485937491763BC61E55F17048610A9E0D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorSportsCentreMap.cs` | 10917 | 2026-10-30T04:17:10.5700206Z | `E77A6A341CC1AD8AD4081F937EEBCD9E8F0D626E95C648F79CD375608B9226A2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorSportsCentreSetup.cs` | 21587 | 2026-10-30T02:14:25.5916580Z | `CF92033E2E23B166A5CC20FD31B433ABA395B40E925410C276BF9AE2346AAB22` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorStoryPresentation.cs` | 12221 | 2026-10-30T03:48:20.0059099Z | `2396AB8821B51100CFA442AFA8B4A8E3D7A7EF50F6CFBA5C269596BEFA43FD4F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\ElevatorView.cs` | 22631 | 2026-10-30T03:55:43.1874959Z | `B0681D03D30BB6875BC3B5F99B67852D3BE95DC91B579179E180DD8798D0ADF2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Before\README_엘리베이터.txt` | 7496 | 2026-10-30T04:42:58.1772281Z | `71910BF9EC9012CB15A8487011F541E676ED835D4180FAEF7A6E929F258E45CB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Completion.json` | 1024 | 2026-10-30T06:16:13.1936832Z | `A11C8C0F7C5BA080FE75F9953E07BF1CC489DC3E44D206F77FB346A50E8E869C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_01_FriendHover.png` | 535254 | 2026-10-30T06:06:22.6914878Z | `4C00744444176B09E0EC0103604273043DAFC802C71A10F14A2BC3797DE6E21A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_02_CallHover.png` | 492699 | 2026-10-30T06:06:23.2923580Z | `F335993753F4A912ED99CAFD686FD5E61820026FAAC3132E2068DF88E077EA44` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_03_PauseCentred.png` | 346571 | 2026-10-30T06:06:24.0434291Z | `5450C6E50F32243FBD27CCAEB8C096F5D4C999F4C6183AC9EE8ABCF2DD0A3BBF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_04_PosterDescription.png` | 432462 | 2026-10-30T06:06:39.5114462Z | `ED86555000AE9234FB561324991E20EF306DEED5B65B6E31D9414E4BDD30A6FE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_05_SidePanelDescription.png` | 334930 | 2026-10-30T06:06:40.0740966Z | `D1AEBD5F549EFF455753099782030431CC0E804F12ACC530E743AC462E515E76` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_06_OperatorNext.png` | 342911 | 2026-10-30T06:06:43.5836378Z | `40CBDDC1366C479C7F03E37E7DAB3A628689737881BEE41D52BD0FAC5A79BFB2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_07_ReassureFriend.png` | 435039 | 2026-10-30T06:06:45.6669503Z | `73ADA6586EB8A2EB7C191BD9A0A8FEB1A56B3EE0A24682CA1E4B7C6E684AF3F1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Guided_08_Completed.png` | 427806 | 2026-10-30T06:07:10.9303089Z | `0533559AF18D077F9AB328C0A9E75BBCCDD5887468175F4FF6CAC8C311FED4D2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\GuidedRunEnvelope.json` | 12491 | 2026-10-30T06:07:13.1674547Z | `816D027E6BA9ED5A2F22B939A86489B4F0647A22023495E7A7B1B02F3D73BEF2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\GuidedVerification.json` | 10381 | 2026-10-30T06:07:12.5243171Z | `29AC49FE058D507301F0B848616E7F95AAFE152409E4B74B354EBE7E81F54103` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Inspect.cs` | 946 | 2026-10-30T05:51:45.5709719Z | `F3CE7C719BE4CF9B2B91795ED92919EB2DA042636E8B89D1D186FF06576EA783` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\InspectWalls.cs` | 851 | 2026-10-30T06:01:00.3097250Z | `4C621B2F83EBF7B13C16193B607F416155017564DEB8B95551B97340B785755A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\OpenOriginal.cs` | 232 | 2026-10-30T06:08:08.8327727Z | `3CC85E5FB9151F6448019BED3858074ADDB98808E15EECA22FC0E0F7368BF3F3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_01_FriendHover.png` | 533475 | 2026-10-30T06:13:04.7974744Z | `2E9727F9826BC2A31D4BA050A510E8D71EF0DA80247F9B3CD05C0064954E1B3F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_02_CallHover.png` | 492063 | 2026-10-30T06:13:05.4032496Z | `95CBA3DCA210E036234A3B64EF34E581E61070F88B896D0384C90281C523284C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_03_PauseCentred.png` | 346628 | 2026-10-30T06:13:06.1527153Z | `AC0E38D78BEA960CB8D7D6A998D453CCF0C3AD2272644388524491A798EF2AA5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_04_PosterDescription.png` | 432915 | 2026-10-30T06:13:21.4905385Z | `0126D898AB6616C258A3C743E297631EDD552A57FF4F2613778835D1A1729FA6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_05_SidePanelDescription.png` | 334996 | 2026-10-30T06:13:22.0386268Z | `625C20D26A4088D9C03EBCEDE3803E0DA7A9D428FCE936252D36FDC99932E57B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_06_OperatorNext.png` | 342827 | 2026-10-30T06:13:25.5535960Z | `7717F1B622E4E0ABE09BBB8866F8AE198B70B1AE0D4EFE159985BA3A987844A0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_07_ReassureFriend.png` | 454776 | 2026-10-30T06:13:27.6420536Z | `6B4BCA85278C7A6BAA14ECB73D873D42CFEB67C4C8BD95C7A8D533A788ED5FB6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\Original_08_Completed.png` | 440035 | 2026-10-30T06:13:52.2322325Z | `17C29A8312310467F8E2A11DF6425FF205D87D3A08263A4BC25FCD44846EB83A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\OriginalInputAssumption.json` | 9122 | 2026-10-30T06:11:03.2333833Z | `C9311DBEB771E622F8FBD9BB2680E9880CBC61FD5BC68552FA4645A77211589A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\OriginalRunEnvelope.json` | 11840 | 2026-10-30T06:13:54.4472446Z | `9C4D9ECE36CF011ED8FA0966E92ECE00AAD97B36DF8D87F0158ECEF4B6B2B372` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\OriginalVerification.json` | 9815 | 2026-10-30T06:13:53.8418582Z | `52CE61CA08B7F24C87C9592E678F258075780E7E2DA7DC3A9AF47943F59A1F78` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\ProtectedSettings.json` | 367 | 2026-10-30T06:17:05.6552577Z | `A08C288A15DD83BB862964EF8C00113F0B5247AB8845213ADA9331A377429C83` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\SavedSceneValidation.json` | 5800 | 2026-10-30T06:15:17.8009314Z | `F53F68D8826B35994CBF2F0B228467CDFB70DE9290D3376B6B80EF814934DF8D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\SavedValidationEnvelope.json` | 7197 | 2026-10-30T06:15:18.3853942Z | `BA16384D161386AC9252AFB13564F923A447E303191AD2DB4D56244F53D8E38D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\ValidateSaved.cs` | 5914 | 2026-10-30T06:11:10.6949596Z | `D3F8FA781F76B013087A77B6341B51925C45D571A3C13A49CE47922BD28DBBE9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\VerifyHover.cs` | 14135 | 2026-10-30T06:12:49.3612954Z | `775BAA75CC0869C36D5BE545FA09D89B0682BAB5079D424A4D13D7F2877F2EF6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorHoverReview_20261030~\WallHierarchy.json` | 43758 | 2026-10-30T06:01:14.4282602Z | `DED3A47BACDC0B2E9B5CF6AFBA7D4A4BE2B778101319C60EE0E243E9E916A810` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\01_CentreRayAndGlow.png` | 486963 | 2026-10-30T02:33:17.6076666Z | `3147056A00F28AE5B4FE100EFFB90568B87842B3374E2D7EF63250AB90D43B0E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\02_PauseMenu.png` | 296430 | 2026-10-30T02:33:18.9372720Z | `F10D60ECA447EF39A97BE2D374203F8F2EDAF050CD8517C089067505D7C4042F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\03_HUDInFrontOfOccluder.png` | 81799 | 2026-10-30T02:33:19.9728406Z | `CFE73EBCB68E5FBC3E0E39278383176685BBEAB701233108D3A0A3284B7344E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\04_CharacterSilhouette.png` | 389415 | 2026-10-30T02:33:20.8425910Z | `A6ABAB926C8735C68395356FE149C64E6F88236E2361A5C6489FF848E47ABD27` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\05_Silhouette_Off.png` | 380457 | 2026-10-30T02:47:35.3499804Z | `E72A94511F17B807A1F8CDE7D19BDA31BCEF73A5482412D40376ADEF02BBD554` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\06_Silhouette_Only.png` | 412510 | 2026-10-30T02:47:35.7208869Z | `6A880CED030FCD41184D8CE2087A6A36C64FCECF02FF40EACBFC99FBC56062C3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Apply.cs` | 143 | 2026-10-30T02:10:08.5125825Z | `A254B59C1A8867186B6F7FD9D901874B289B3687E609F6AA84CB0C8A27660C5D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Before.json` | 8110 | 2026-10-30T02:00:48.3902135Z | `3BC04B69C9D3B4F801B303E86F691F5E3A41AF069FFCFFA9CEE0EF37AAD1E8F6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\BeforeInteraction.unity` | 1091016 | 2026-10-30T02:10:40.2404923Z | `99F3989AFBF224DF0CF4D3983DB93D1B4644A98383A5FFCF76DAF94ACD1FDE98` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\BeforeInteraction.unity.meta` | 155 | 2026-10-30T02:10:40.2426019Z | `13416AB46279D6180AF64E52669955A1E11104BE63D00E4717F23660497C6D87` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Completion.json` | 428 | 2026-10-30T02:49:58.2608174Z | `737C20FCE383C4EAFE6C6F4FA21C097FD3F9FB567AAF70C024678D685B72D217` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\FinalBehaviour.cs` | 3917 | 2026-10-30T02:42:04.2839472Z | `F2602D106338786EF141EB609D344C655514F5F93F2E6E97F7B1E34ED38DE749` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\FinalBehaviourVerification.json` | 724 | 2026-10-30T02:47:36.7490495Z | `94E2A2B373E0E8BF59D043F7ED5F47E2C00FE94A97341D2C180E8CA85F979A23` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Inspect.cs` | 3047 | 2026-10-30T02:00:18.4586724Z | `CCFCAD18EEA3FB679AD373AEAF131D6C0AC13B7AD0A7C592AC13C4E36BA66CC5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\InteractionVerification.json` | 3783 | 2026-10-30T02:33:23.7715729Z | `6FC3A014F8277372715A28FD49DF23C8D53D09347ADD29B24956D91CFDA4E9BC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\NotebookFacing.json` | 282 | 2026-10-30T02:47:36.7404951Z | `3F34EC6D2F51F3B69C73146085A5613F4A4D132E54B119B47EB80355A3C39D7E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorCinematicOverlay.cs` | 5306 | 2026-10-30T00:38:11.0027627Z | `72C97F0B74DCBAECE6F0B371E8F0229BF1C1B3C254916AB2A19B4AC18603F283` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorDesktopControls.cs` | 11855 | 2026-10-30T00:57:34.8660073Z | `DF84B49C414564A33516E50CC852B85F16A6EEE7B03A9D7A0A2388F2F141D0D5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorFlow.cs` | 11852 | 2026-10-30T00:46:12.9812563Z | `E08DE5FB10259CDD04FC74F9DC03C9EF54331C66AA6D43E0B4A53D8C75411718` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorLesson.cs` | 11036 | 2026-10-30T00:57:34.0515059Z | `BE577B390068B4D4E2DE869B38EA5F9DB98B3E6747CDD89D9BA2BCB03D1B5650` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorMemoryGraphic.cs` | 5878 | 2026-10-29T03:27:26.9490751Z | `680059D3114B62C42608474ED040EB5F4E6198FFCE006A62C61EC0ADF9A48814` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorObservation.cs` | 1705 | 2026-10-30T00:46:14.4603842Z | `E4B7AFA3DE1B1A2362DEA228424E920C8B5261946AED4F6D874D9B1B9B17BACB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorSettings.cs` | 1992 | 2026-10-30T00:37:07.7918697Z | `054AFF2CE130426C48C409FC94BF56EE11CE43BC0730249A2DA5C2ED9A4C1A43` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorSpacePresentation.cs` | 5150 | 2026-10-30T00:37:09.0874283Z | `BAAB80B44E48C6B4DB476AFBFF606E282628F84511470DE4ABEBFD1C3FB09309` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorSpatialUI.cs` | 16132 | 2026-10-30T00:52:34.2343050Z | `8BC9B585100DAA23A9923A3495CBB8F1CB498C887CAE9C60CFE8971F586543FB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorSportsCentreMap.cs` | 8584 | 2026-10-30T00:53:50.0476591Z | `7421D469A9517ADEFEEA98E7426EB658149EACBD8CA07C3A4695D0E2F4C2BDD7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorStoryPresentation.cs` | 12195 | 2026-10-30T00:45:25.2591288Z | `BAAC504156D28A39AD67716C1CD5388ED476F2A0400CD25EEE1407D1AC853ED8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\OriginalScripts\ElevatorView.cs` | 22268 | 2026-10-30T00:46:17.0604593Z | `105B7FE25E132914614A40FEEA995E5905CF718C67CA2F13304ABC20C978D0AB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Probe.cs` | 401 | 2026-10-30T02:27:48.6599893Z | `5EE42EC117B6E44ABC60C3C16BDE881B2F12299EC7E0A991866C57382B24D063` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\ProtectedAfter.json` | 685 | 2026-10-30T02:49:13.4669845Z | `CAA2995129327F796894378CC54D1BC03EACAD0150DF9C0FF0105DB25EF66586` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\ProtectedBefore.json` | 598 | 2026-10-30T02:14:26.9251248Z | `786EFC7CE8A3271FDA37851F87A197A585E0B6CFE81CBB56BCF27B755CD6EDDF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\01_Lobby.png` | 402547 | 2026-10-30T02:34:07.9657178Z | `B22E23BF3349E023B96DD8485A7F93B04AA85D40D881C4F8BFA157D0265F7D25` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\02_SidePanel_1F.png` | 314461 | 2026-10-30T02:34:16.8426521Z | `1AFEEC8E462DA5B0B75673DD6A013C20AE4E0D309C1BF6A28A2F60B90C2B24C7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\03_Judgment.png` | 464589 | 2026-10-30T02:34:18.1608398Z | `22271D0F63F8E2578655610C323790AA292C50C7EAAAEDDAB06617FAA8B5B428` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\04_WaitFeedback.png` | 452190 | 2026-10-30T02:34:19.5636906Z | `AD85E24F690394D130635751B905870ABD6479236E1DAAC3B3238882BD0E99FD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\05_PosterAndDirectory.png` | 430270 | 2026-10-30T02:34:26.8778135Z | `2527D8C165070D5A644D450C9E0F8BD8C0D2B3EDEAA5F223A0999D7EAC57F3FB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\06_Identity.png` | 458402 | 2026-10-30T02:34:29.2135759Z | `0FA6CBA0B82CF39F4CE06250E3AFF0F0A9A1A1B4A9E0580996327C1917F8B940` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\07_RescueAt3F.png` | 462166 | 2026-10-30T02:34:30.7832446Z | `304E6C229BBC6A0504CFD773493098A7C2119C22DC1E545EA80D3C43461FB7F2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\08_GroundFloor.png` | 420578 | 2026-10-30T02:34:32.1686177Z | `E70220A3B381F07A5FE3722679E3F5396AD13B7CFEE7D162DB0892C7D5C609CE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\09_Complete.png` | 42321 | 2026-10-30T02:34:39.7678513Z | `24D5DB7CEC13942E11C4766AAC0CB97A7A9154F9D21B15E1C131C65730A2C182` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Regression\RuntimeVerification.json` | 8351 | 2026-10-30T02:34:40.5415537Z | `C1E81BDE837B6AD194B68E9C0373979D2637311BC6F8F7F981631DDE3D934277` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\SceneValidation.json` | 2163 | 2026-10-30T02:49:12.4895381Z | `158B14CBECA6ABE565147442302F613B5DE4470708AD95D852E35F927F8FC915` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\SetGlowWidth.cs` | 456 | 2026-10-30T02:45:16.9213937Z | `2E83E8248BA6B29DF3413E44DB0067FC012F67BADDAD66830E098A9654D97DD3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Setup.json` | 287 | 2026-10-30T04:37:51.2126604Z | `709C52063842AC39D54CEFFCF48CAC208C8396D7CE620DF1F89B02087E6EEBAA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\ValidateSavedScene.cs` | 4572 | 2026-10-30T02:42:57.1972211Z | `1726E9EED8B0CC76E35235C156816B3899E5DD7C014EDCB7E068BD977EE81C60` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\Verify.cs` | 12156 | 2026-10-30T02:29:15.7434487Z | `D6112EBC57D98ACCCB8F2865D6674CAD37492BC74F914E2A30EFA6622F2EFE65` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\VerifyQuit.cs` | 346 | 2026-10-30T02:42:57.1982668Z | `E3A379C5B541385963546F5C1AC67B578EAFD8D1820B90B77168EE329F6AE9AF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorInteraction_20261030~\VerifyStoryRegression.cs` | 11844 | 2026-10-30T02:29:06.9665301Z | `2B8B569378A1D8B56D9C24555C605BCCE0F4BC09AB08D7EAFABF962B426D34EC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Build.log` | 27894 | 2026-10-29T07:15:30.1760482Z | `83961E079443D3D06AD8564026C4A9A3BDD58E861ED91DE9A530D300C0B8FBF3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\build_map.py` | 15925 | 2026-10-29T07:14:45.4665927Z | `56A2CB14B01CE81C1A572525F7C919E99978690307D3C83BAB2CB23DC6345133` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Build_Validation.json` | 309 | 2026-10-29T07:14:58.3504585Z | `4FE52288E30FD5B214052960268F981B19B0A9F0EE77D239586B0A71398AB81F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Elevator_Map_Source.json` | 52150871 | 2026-10-29T07:11:27.0904741Z | `2F302249731ED6A3A0D642706ACF9EA86A6CDEA5F9BAE8DECAA4DCDFEEE4ABCA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\ExportMap.cs` | 6848 | 2026-10-29T07:11:16.4086517Z | `FAC0EDDC9FD6DB0DE5CDE3F81FD4119A2952FE4ECCD5086D670B2360674F67C9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\InspectMap.cs` | 826 | 2026-10-29T07:10:20.8160927Z | `7D55C1C3AE716300A3B7EA1C8B707D670B3B31C4D31FACA80D236BB0BBF3A222` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Preview_내부.png` | 1054287 | 2026-10-29T07:15:29.5394008Z | `2A69E323DBBF409FD141C76F47F322573F55593B221D9EF63894F115003AC06D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Preview_로비.png` | 920898 | 2026-10-29T07:15:26.4727559Z | `60768D864B5E6F971446730A8614F3B69C8ABEB4B612B99ACC404C29CC524A36` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Reopen.log` | 1262 | 2026-10-29T07:15:25.5216629Z | `E27077FC5DB134AFF06E7250376CD08ED533803E648597C159010348589844BA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Reopen_Validation.json` | 469 | 2026-10-29T07:15:25.2460126Z | `64DEFA46ED86D20B48B281D85242687097D567511B4F0A9681061D6524777E5B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\Textures\atlas_0.png` | 687637 | 2026-10-29T07:11:21.0723384Z | `7B813EA1DF9252314A928A46B900A0337590473F121C6374396B7AD3F259A812` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\verify_map.py` | 2471 | 2026-10-29T07:14:46.4180453Z | `A74EA8074B6B75DF6BA1BB830118354D6055EA64E07A3744A938FE5A01719720` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\엘레베이터_맵.blend` | 9549327 | 2026-10-29T07:14:58.3472073Z | `34935BFB45C5FF92CFC406B87DDFE3BB801EEBBBAEA08AA2652317C1C6B7E6DF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMapBlender~\읽어주세요.txt` | 1639 | 2026-10-29T07:14:58.2718193Z | `B715C2786593A3DFE4FFD4972CA34234397305B8A55EE4C5703D157797751C98` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\Elevator_Safety_ObjectInteraction_Test.unity` | 1736870 | 2026-10-30T07:22:26.6684848Z | `5E0DEED543ADBC8EC70FE362B82CEF2E84D1E2D8AF855D943374DFF4F1960A2D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\Elevator_Safety_Prototype.unity` | 1732925 | 2026-10-30T07:22:25.5805696Z | `DEA23DF3FC195A81AE7E0059D42F4BB8B9CED87D7DBA85CE75C29CB5A5233C60` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorBasketballSetup.cs` | 20036 | 2026-10-29T01:10:41.7081784Z | `1F1690641DFD362A2E9B36BA63A731D7B0FA1F6AEDD1C8021D6E74D67361E28F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorButtonFeedback.cs` | 1575 | 2026-10-30T04:06:47.1204008Z | `659C8ADC63B13D8DE38B589C2C6C4AFB4162C2D78450B5D557D071B93FDA41E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorCharacterPerformance.cs` | 4767 | 2026-10-30T04:32:50.0190716Z | `B3AFE606D75BDA19E37362564E683590E7554B729A841193594A5ECCF3D37F04` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorCopingSetup.cs` | 17579 | 2026-10-30T05:54:49.1595678Z | `1C9516DAC8E9D9F50C5EA15A662F414387EC965B0176DD7D475CF639B644E8D1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorHoverRefinementSetup.cs` | 7442 | 2026-10-30T07:20:12.8910955Z | `6981A7320335EE3104B417E8A5A492EE9CDBA45A7FD9409B85079F2B9660AA71` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorInteractionSetup.cs` | 20522 | 2026-10-30T04:10:47.7213605Z | `C0E1E65ACE68D2C9D731EE7A51994DD056CCFFD91EEFBF132CA22AC0E5068434` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorObservation.cs` | 2024 | 2026-10-30T06:58:41.7821053Z | `331C5FF405D22FD032A12E5BEADA3DC01F0976B0A267BE305A7CCE4C3BC05BDE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorPrototypeBuilder.cs` | 18551 | 2026-10-22T04:11:17.1186002Z | `AE014E68FF5E7CA6FDD68E2D301639DA5C1F00CC16593E03653587135E1329D5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorScenePipeline.asset` | 4599 | 2026-10-30T04:37:50.0081367Z | `025532C15CF556CC6CCC6508C2AEFF4A33D46ECE9DE7058009572D06E879376A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorSceneRendering.cs` | 662 | 2026-10-30T02:03:22.4659959Z | `F913D93980ED974DF2D104ABE215836ADFAD8D9D5C438869757862BDA64EFB65` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorShapeHighlight.cs` | 3327 | 2026-10-30T07:20:12.8895916Z | `955E99EAF7A6B6633DEA17CA2B332D36007D0F4FED15248D79677080E7B5A6CA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorSkyHighlight.shader` | 1341 | 2026-10-30T06:58:41.7800310Z | `4EBB1EA448FCA98803A1A7E5D1EA75544107FA0CDC5076A166F321614F4A7DCD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorSkyHighlightMask.shader` | 1151 | 2026-10-30T06:58:41.7811058Z | `187BEC92E87D340BBBBA7374513ABBA51E5ACECFC31C74D594B2665724A7A2E9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorSpaceSetup.cs` | 23379 | 2026-10-29T03:38:06.8067984Z | `1C9A244C94863693CC00F7A73D8F6E72D1266BF4419564000C55575FB04AB1E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\ElevatorSportsCentreSetup.cs` | 21631 | 2026-10-30T06:03:27.7278605Z | `4DB8133D567FDD2A1D71821B913DDF521FF42017A74B1978598CA82DE4A30A31` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Before\README_엘리베이터.txt` | 7519 | 2026-10-30T07:30:36.6983445Z | `135411D44F5ECBED08ABCA19BEFCBCD9C6EBD4DBBFF208D947477A7DAA477F75` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Cleanup.cs` | 4263 | 2026-10-30T08:24:02.4050655Z | `03F4CC8FC37ED012119332F443537756EB3FA99E0C77D26A942FB20AC851BF21` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Cleanup.json` | 5236 | 2026-10-30T08:24:30.5521406Z | `F481A31BCD1EF6DDAAD192F15D346BC37996FE6D96942E7481A8C209269F6183` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\CleanupEnvelope.json` | 6368 | 2026-10-30T08:24:31.5599737Z | `5A674D2A0293BC48FCFED51A2E54D17C49890D6551400541ACEDA416BDEF11F7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\FinalConsole.json` | 86814 | 2026-10-30T08:42:30.5133031Z | `1B272F09F2DCCB14C85C37240CD70680FE511BE28C6C8D1824402FBE8773480E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\FinalSummary.json` | 1237 | 2026-10-30T08:42:23.8388230Z | `92DF7D43364C8C6DF33FE41AE238334A80FAD84FD283FE0A5703050766C98BE3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\FirstMaskOutline.png` | 554728 | 2026-10-30T08:08:48.9884542Z | `DF18421F6032767B7B4D6AF5A8544AE4FE681393E4C85570EEA21EE304C6C8D9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\FirstPlay.cs` | 1401 | 2026-10-30T08:08:31.1494425Z | `53236EDC2D4563278BF26E9D7A8803E05E257B4E3BD098C53C86821D2060EBB6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\FirstPlay.json` | 1927 | 2026-10-30T08:08:49.0848315Z | `CFEEF674B0110F4E542CC1C03D3D251127EA2D1AC0527BCC98A1975D29CFC405` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\FirstPlayEnvelope.json` | 2945 | 2026-10-30T08:08:49.7549570Z | `F9C7843FA1F1D73E3CDF552978BF43CA92BCF3E210B0E2A43D7EF28FB0FC5E00` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_01_LobbyFriend.png` | 552138 | 2026-10-30T08:39:15.7138717Z | `C570D9A65E15369634DFDB33A60CE2EE6A642DB5517365B49F6018A4FD3B58BA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_02_LobbyCall.png` | 539163 | 2026-10-30T08:39:17.8525551Z | `3B8B34814450E2D4F90B14C90ECDF7CBA4D856C1F4D0B8971F2E5F6B93EB0889` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_03_ProposalFriend.png` | 396128 | 2026-10-30T08:39:19.7436868Z | `ADC0282D2F3BAF514C33D19754D6D88080DB26F34B1BB6BCA63347E98FB7EDC9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_04_CabinFriend.png` | 395100 | 2026-10-30T08:39:20.3325752Z | `8B33E20BD6B80D3E1FBEC1B30D090566DA8E5D7756797F5C628EF941E8F2A6A3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_05_Emergency.png` | 391619 | 2026-10-30T08:39:20.7360899Z | `6C6A54EECE63FC14CF0F843D2B978E8F7EAD6102534268012798401EF35EADA3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_06_Poster.png` | 507878 | 2026-10-30T08:39:21.1781884Z | `575F1C1328FB3393E93E8F58F5F54F4A1C16F73F2353F780E6E2301773610894` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_07_Directory.png` | 551762 | 2026-10-30T08:39:21.5385536Z | `ECC3C4623A281CC95B08F948C6BE3C5E2BE08532A57B530D9B54E513CBA488D8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_08_OperatorDescription.png` | 394172 | 2026-10-30T08:39:22.1362432Z | `D31CA6A2A14C298C722B1A6FBE4EB7EBBC37F985A806B30AF0C26FAC05254022` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_09_Identity.png` | 590959 | 2026-10-30T08:39:22.8471890Z | `4C1C25C26F2EFA79F91C91CF6D4D20BC710B69B2123DB387355BC931CA79E534` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Guided_10_WaitingFriend.png` | 404986 | 2026-10-30T08:39:24.0334642Z | `016770E596A22F254468F838F00C1F44DC29F9EF6D9739462D4CFBDDAA69FE9A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\GuidedEnvelope.json` | 15331 | 2026-10-30T08:39:25.0182601Z | `172B29FA59688E9BCBBF1F3AE9805D8EB255348D83A048C81F5D6AD062A101C7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\GuidedPerformance.json` | 56 | 2026-10-30T08:39:17.4729462Z | `85512079FB8212AEA31ABF94630F2BDA9708E914A9169246E402F6069ED2AB6D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\GuidedVerification.json` | 12962 | 2026-10-30T08:39:24.3808149Z | `14B4F033D59E4413BB4F0A8664492C9A8B2CE53BB4B341E4E9DB3B3708A5B6B2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Inspect.cs` | 1101 | 2026-10-30T07:52:29.0395131Z | `D81E6E8B2D1167315EE0BEBADBC985A167ACB39B638FBAEFACBDD0A335FC2CA7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\InspectFeedback.cs` | 705 | 2026-10-30T08:36:47.6169466Z | `BE2E51073FAE499D657C331D977D16DBF5FD26A424116A80B006B2CE08967B84` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Migrate.cs` | 4769 | 2026-10-30T08:04:44.4646394Z | `66F05306C50CD2065CF6626E28563FE54F51CDAB35A6C7C66B3C78746FE89E0D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Migration.json` | 436 | 2026-10-30T08:08:30.1301566Z | `1D70A71C6E4D2D7B3C12A045A45D0EAEAF9101CDF2DDCA83845F89E90BE9D5C2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\OpenOriginal.cs` | 305 | 2026-10-30T08:30:34.5784716Z | `D160798FA4E2443F492FCA7ACA51754D51DCB34477C06A54F04BA0868C5150FD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_01_LobbyFriend.png` | 552556 | 2026-10-30T08:32:06.7572044Z | `F7493F59EB8F1102B11E61A0C294E63EEA2806FBC1F75A518768187AFCBC92DC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_02_LobbyCall.png` | 541348 | 2026-10-30T08:32:08.9166657Z | `F10717B3A8EBC05991FB49B8E012DACA48AC9B4E4CCA3C42E23F320DD4CFCCBC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_03_ProposalFriend.png` | 415057 | 2026-10-30T08:32:10.8256387Z | `48D57CD9C9A859FDC7AC91AD215639959DFB19F7542DF396CC53CBF12730D18B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_04_CabinFriend.png` | 406657 | 2026-10-30T08:32:11.4344749Z | `1FE5539C2A06439817ABC669C0E6B184D4768E35308D5A65C7B28CE9021FE5B0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_05_Emergency.png` | 402860 | 2026-10-30T08:32:11.8203712Z | `3A17E4F99FC47D65836DAB7C655E7054835DC13405A349C4B04305CF901747BA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_06_Poster.png` | 511494 | 2026-10-30T08:32:12.2294869Z | `20F88CDA28737F6D0EDB5F18E3B34B239FE719DD646074FDD167E87630F1A2E2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_07_Directory.png` | 552005 | 2026-10-30T08:32:12.6188246Z | `9C7C50415D2A8AC9D978E2867B78E2F683FBA4426DB5089EA8F3D21FD5ED31A1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_08_OperatorDescription.png` | 398586 | 2026-10-30T08:32:13.2040309Z | `E82FB76F5E9EB47078AEAF4DED90839128C1E33234F9CB07F39D4BD3F13BF10F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_09_Identity.png` | 590309 | 2026-10-30T08:32:13.8969117Z | `3380AC8E41FD3915BC5D98B36E1A738A4141FB107BCBDAF10814806CECECF1A7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Original_10_WaitingFriend.png` | 419121 | 2026-10-30T08:32:15.0836053Z | `BFF22C7D175B1D964B773DD8F27D06734210FBDC947F7EAA969A75AB3FC9428F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\OriginalEnvelope.json` | 16056 | 2026-10-30T08:32:16.1447453Z | `55CB7F90B212C9EC2A57A0211B1B5FFA1ABB9D427F1245A1CD55CB780F47A43D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\OriginalPerformance.json` | 56 | 2026-10-30T08:32:08.5332398Z | `086BA362081B0C6CB12C0D4034C2C6DB990E2145987C5A01D50BDE755DF2FB1C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\OriginalVerification.json` | 13603 | 2026-10-30T08:32:15.4133271Z | `D605439940654776FBB131FC9637B9A7A63E02BF6A3A2B518B2804B593E7A8B1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Pixel_01_CombinedWhiteMask.png` | 40387 | 2026-10-30T08:34:12.6543476Z | `EF0C86C12516F5C94E332192A47E90ECBC7A27335D86C282A003D62D131B7E7E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Pixel_02_OutsideOutline.png` | 56638 | 2026-10-30T08:34:12.9810024Z | `46112EBA0A0447262AD2ECB1A1EA465F750AAFBB7BF36E5B567C6C36B139C748` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Pixel_03_FullyOccluded.png` | 38667 | 2026-10-30T08:34:13.3483881Z | `2CEA9C91FB1584053F2D340D993EBA6E755FC14BFC44B6E33A2C551D13F625D6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Pixel_04_PartiallyOccluded.png` | 49499 | 2026-10-30T08:34:13.6734703Z | `54418D49F63369054E5C4FDCC0C130F7D020739482FD5DF8DDA3A5E92526A1A0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\PixelEnvelope.json` | 1837 | 2026-10-30T08:15:54.6752558Z | `9E24501EA96EB63698F66ED4D17489C57B98C245CC7C78AE69A877CC961AD8D1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\PixelEnvelopeFinal.json` | 1837 | 2026-10-30T08:34:15.9140214Z | `AAC72022F6E7B06EEAB3FED8714E26E190E892CF004A519F214C6FCDD8660A05` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\PixelVerification.json` | 1026 | 2026-10-30T08:34:15.2515124Z | `2455A5AE8AA5E38BE5AA8257B9359707E54AB6B9A73989EDC415168814FF4FD8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\Probe.cs` | 1423 | 2026-10-30T08:27:28.7340797Z | `402BF3D20A787B250F92D01A3B09665A95E6358C56123E8541494533AB01687E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\ProtectedAfter.json` | 1041 | 2026-10-30T08:42:23.8367036Z | `52EA80F3180BA6A7BF3BED572A66F51FC6A274F4572F93B56F7BB256540693EF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\ProtectedBefore.json` | 617 | 2026-10-30T08:04:44.4641217Z | `0345A8DE26AB1D5E01D0E59A2B1705FAEC8C00CC4707836B35764BA197195821` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\RenderGraphReview.txt` | 3208 | 2026-10-30T08:42:23.8377050Z | `40163ABF9B76669953E3D91ED501AB00CF8345FB25FC257638851B33DE4F2649` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\SavedEnvelope.json` | 7066 | 2026-10-30T08:39:01.0361101Z | `50145294C31634F40225A8B69E43A6A0B533E8F44E4666F54C5D1E8DF05B1F9C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\SavedSceneValidation.json` | 5768 | 2026-10-30T08:39:00.3674950Z | `E951EF1E5BAE8D577204E2C7FB6D800CF5C46A7CBFB51A4AF1C4BC654C12E4E5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\ShaderCheck.cs` | 450 | 2026-10-30T08:07:31.7975125Z | `563EC791B740C126D4199193E970F5840C22F759ABD439993463640B0B72E586` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\UnchangedSpriteReferences.json` | 243 | 2026-10-30T08:40:39.6168584Z | `7CE08B6046382856A15E5E14EA7F40B809F257F3C24E1CA331561EBCE387DC77` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\ValidateSaved.cs` | 5428 | 2026-10-30T08:38:55.7122166Z | `4DB95CB53E1AC084C80A9A4657F35E973662AB2A8A05F6354C02E88F9694CFBF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\VerifyPixels.cs` | 6217 | 2026-10-30T08:15:46.4813056Z | `8170E972739E57926CF4751C92AB5CAA75F694FEEA0AC25F77D1759FDFD4A26A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorMaskOutline_20261030~\VerifyScene.cs` | 11221 | 2026-10-30T08:30:34.5784716Z | `2AEF27B9D8416A9CFB5402CF96C70F134AECFD11108DD7DEFC115FAD9710B346` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA.meta` | 172 | 2026-10-22T03:50:24.4600770Z | `B08056725B87D79F5B4A837A2D53CA933A1DC6D953A97B398C67CA409508F6D1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\01_Observation.png` | 167542 | 2026-10-22T03:59:17.2534898Z | `A4B43F62A9D1BAFB43D73FB5E16BE8F514F6E53F10CD3092FCFB90739F92D1A5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\01_Observation.png.meta` | 3009 | 2026-10-22T03:59:17.3499279Z | `355945933AC25BF59A0730B9AE22DAEECFCC390A1268EA077B0665EA21E866B7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\02_Proposal.png` | 176367 | 2026-10-22T03:59:18.4204620Z | `E823FC00980D8C4C5CEA74E119559F5AF8EB61996673E8383C933739D8653A77` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\02_Proposal.png.meta` | 3009 | 2026-10-22T03:59:18.5140510Z | `9390BD4FA848742CB0D988BA4F22748CB47BA2762060D6FC6C6A955839712ACE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\03_Stopped.png` | 191005 | 2026-10-22T03:59:19.5911462Z | `C5AC97B04CF15B6CB6078BC76482D162973EEDC53569F67FA7C634DADD6FBD40` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\03_Stopped.png.meta` | 3009 | 2026-10-22T03:59:19.6831231Z | `6A78746A506C0CB831625F334449EF76B4C159FD32E123485CBCC924A80B5D33` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\04_Wait_Retry.png` | 174795 | 2026-10-22T03:59:28.1705908Z | `56652076C70DB690364F6589C7FE98E388BF17C2DC4690FFEC10C85E38C35406` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\04_Wait_Retry.png.meta` | 3009 | 2026-10-22T03:59:28.2564652Z | `DDE44AD242672C52718F0622337F629D81CF1B40C5B2AB9F249E569A9C90E051` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\05_Rescue_Wait.png` | 171478 | 2026-10-22T03:59:29.3319021Z | `D1E686C1739461C5CDE5B8AE2E26F18B74A9E5570BB6484A7EC00DA1C91A3491` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\05_Rescue_Wait.png.meta` | 3009 | 2026-10-22T03:59:29.4217711Z | `E6DE8E6986058C75C986C46A08470900A1C2D4BDA9AA36180EEBACAB57B07D51` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\06_Complete_History.png` | 129749 | 2026-10-22T04:02:47.2186658Z | `95E0989FE15507B4C74F8EF49DA6BFC4095B38F386AD8DB753A655225B6B8599` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\06_Complete_History.png.meta` | 3009 | 2026-10-22T03:59:30.5484285Z | `DE279DC895BE9891F562F2343FFD0AE5FE54818938F7DEFC04311D1EB03604F2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\07_No_Response.png` | 184732 | 2026-10-22T03:59:42.0593113Z | `4ACD730079D8D7D65843895970CB026B18C7247798597B627A44DB833C0455C2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\07_No_Response.png.meta` | 3009 | 2026-10-22T03:59:42.1541422Z | `9A3A543A447149FFADD5D0E8B257CE7649DA8E1806365AA6C0C102485D353835` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\08_119_Call.png` | 185424 | 2026-10-22T03:59:43.1781900Z | `5C3419FBF9BC2A64F0005F40D96A5793E361C1EC692B8B06986A767B35EB826A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\08_119_Call.png.meta` | 3009 | 2026-10-22T03:59:43.2670526Z | `1BF5D0C0124BC3F780946D742F738D1926DE2C35A77B8763460EC2A3C62B2147` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\AssetValidation.json` | 2702 | 2026-10-22T04:18:11.6747920Z | `CA940AB4E0F9A1FA213EAA7A0EA59E664AEC671A93C28D90DB39CE7FC9760DD0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\AssetValidation.json.meta` | 158 | 2026-10-22T04:01:56.4458916Z | `3AC7B584AD3196E06E5ACF43F99E0832CE6FFC1C2AC8ADB62538BCE539576302` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\CanvasGroupDiagnosis_20261029.txt` | 2193 | 2026-10-29T04:50:05.5752111Z | `F0EAA83AC4BD8AA8CD706B8C17D5DD0DC6A3B1EE273A67F2C2A33F5E65A01386` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\CanvasGroupDiagnosis_20261029.txt.meta` | 158 | 2026-10-29T04:58:25.9262161Z | `D7C06E30FBB56676533BB3F6D33664217381139F24BD8C7CEDEAFDFDCC65C559` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\CanvasGroupVerification_20261029.json` | 7402 | 2026-10-29T04:49:06.1255667Z | `755A02C438C100124E83AE1663EA381A9D7CEEC15F4CE478BBFEBC049930CD6A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\CanvasGroupVerification_20261029.json.meta` | 158 | 2026-10-29T04:58:25.9205128Z | `ACCE72AAAECDE083F0B725241C621C56D7BECEEEC53CCAAF292BB5E4C6926DC1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\InputVerification.json` | 2436 | 2026-10-22T04:16:59.2660585Z | `0619266B2AA142BCF3818274D08BE92CFB991C071FDFC274284753E53BDDFF5E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\InputVerification.json.meta` | 158 | 2026-10-22T03:58:29.0349604Z | `66427F7515A12D22E965E7E4397781A733D4A49C1C735C6B0DCE9D24C3B1B868` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation.meta` | 172 | 2026-10-22T04:11:52.9032214Z | `F73435DDBFF083ADA6498E29DC12EBCD94F61FCBCF735AB6B6E999680BA10A5D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\01_Observation.png` | 168655 | 2026-10-22T04:16:36.1280861Z | `6DFF7081208A7A3D07DE4D28C3063D159097624E56AEEE5211859035134E89CA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\01_Observation.png.meta` | 3009 | 2026-10-22T04:11:52.9704772Z | `6F4BF09237CDB0C5D1368C56E4DE9216BA89E568C488512D931C757AA9845995` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\02_Choices.png` | 192239 | 2026-10-22T04:16:37.3710654Z | `9AD24B4864B4E4EAB598E0E9D93690E07999AA48E20A4A82A0773207F29A0E89` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\02_Choices.png.meta` | 3009 | 2026-10-22T04:11:54.1431757Z | `D0491AFEBF6E5E2BC9C648F3F6037429F91F8BE6795ADE1D58CC43B48715F40B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\03_Stopped.png` | 224111 | 2026-10-22T04:16:38.5901224Z | `F5502A1FE3136C9F16C3557FA949D5B8B55E86B3406E7D5E35EB809D81180C7C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\03_Stopped.png.meta` | 3009 | 2026-10-22T04:12:23.8499171Z | `0503D16A19AF5F394D57151923D8CDE9EEE4CD4DC472FD739C24407AC6364857` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\04_Call.png` | 213743 | 2026-10-22T04:16:47.4918998Z | `85EBCEC589FF8655BC3FDC63EE71B04FAF68FAAD947D3FBFC51446E868AE8E4E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\04_Call.png.meta` | 3009 | 2026-10-22T04:16:47.5845325Z | `948F21B8E9D980F71EF9A639F042074000E2C15C3528CD99CA03A77CD59A6FBE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\05_Complete.png` | 153245 | 2026-10-22T04:18:09.5563913Z | `E8067DCEC49EFF8FA88AAC26EE2F70EB64FDFF807DD912336ACBFD3C672BC372` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\Presentation\05_Complete.png.meta` | 3009 | 2026-10-22T04:16:48.7519543Z | `F6600265B6ABB7C1314248768764CABA8832FD7ADD034441927C04A6FE2C599A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\RuntimeVerification.json` | 6312 | 2026-10-22T04:15:18.5373091Z | `516245600A6A1891F40C1F4BBB2921A24D3BD895356E05C3A60106ED92B67910` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\RuntimeVerification.json.meta` | 158 | 2026-10-22T03:54:53.3034046Z | `44E72DAA8BD6BD3F429FA8242177C47E876C4D636C3C849103B761C84D807046` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\엘리베이터_페이즈_분리_작업_인수인계_20261022.md` | 15560 | 2026-10-22T04:43:28.3678902Z | `C58598DA27920746632F2E9BFA38A1D6540BD88AD26F8F34A8F68D96362425A3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorQA\엘리베이터_페이즈_분리_작업_인수인계_20261022.md.meta` | 158 | 2026-10-29T04:36:50.5922957Z | `283CA546682CA7C9282BA1E4B8464C000AD5091167C1B1C61E1ED3D1152D33BE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\01_로비.png` | 1697945 | 2026-10-29T09:24:23.3071703Z | `9D984E341FD14EB637480F2F2D74482CBA980A556023BB51C9DF6B22AE25B37D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\01_로비_draft.png` | 776509 | 2026-10-29T08:56:42.6528229Z | `B57961690B28C88AC39E566527A7C4F759807C37A693D004BAD662801F11A376` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\01_로비_refined.png` | 876669 | 2026-10-29T09:00:26.7711604Z | `F9ED5864EF8ED58557FB75CDB72B10E23C41187EDE69564686646B4B56CC6782` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\02_내부.png` | 1570509 | 2026-10-29T09:23:04.1404965Z | `2D05A2FCC58E502785941C010982C1152E477097F6402548A1B14734ADE5E161` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\02_내부_draft.png` | 822281 | 2026-10-29T08:57:11.4511428Z | `AEF4CB20B0B84D7823074D704E831560A0D015A07F24FCF079CA0DD661DF4480` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\02_내부_참고반영_draft.png` | 813436 | 2026-10-29T09:20:52.4652522Z | `55DC96D211DDF443BCC8F92088C2BE242505C510E232A43DD4E4E3A5AA659F58` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\03_뒤쪽_자동문.png` | 1573621 | 2026-10-29T09:04:10.8092352Z | `1FAEA02B94ED6E3507FF0DACCB5BA7D527E7C7E5406BCFFF520EFC8FB23EA8DB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\03_뒤쪽_자동문_draft.png` | 769899 | 2026-10-29T08:57:40.5058950Z | `41ABC1B41ACFEE7D69A63870A4C84576FABE36766F82C4C0AE0B332FAF311B4D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\04_전체배치.png` | 1684950 | 2026-10-29T09:24:57.8817845Z | `80260FC2D0B14A29ABDC94527C2B3A724C81B2B250E35EAE5D0D5E53C5B01019` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\04_전체배치_draft.png` | 877815 | 2026-10-29T09:00:55.0596233Z | `673AA8EB8DB626BE5CCAE7A44473F3AD49432AC26C8C4A624085858D75A87E4B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\05_버튼패널_높이비교.png` | 1596840 | 2026-10-29T09:23:42.8921673Z | `BF3D0CE7D1349A3FC460209DA61EC3023505BE93E986AA5F648B4C9BE38E0549` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\05_버튼패널_높이비교_draft.png` | 752795 | 2026-10-29T09:11:47.2049375Z | `0ABA3145C44895A4C901882CA9B57712EF029421BA4F3D0145E126EB6DBEBDF3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\05_버튼패널_높이비교_draft_v3.png` | 790719 | 2026-10-29T09:16:19.0312299Z | `1B4AF8E4C65F9D2413D3D8697783F0D0AB2F812059275ABDEEF9BAED15131FCA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\05_패널_참고반영_draft.png` | 825333 | 2026-10-29T09:21:21.5681220Z | `9BB488111E42D37F04ADBAB57599BD0D14EAEC7DDCBB14FE32D90E62FF02872A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\Build.log` | 6232 | 2026-10-29T08:57:40.7082557Z | `23172B26E3D461183777DFF70692736D93116B5CAFA82B4BF0778260921356E7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\build_silver_map.py` | 34591 | 2026-10-29T08:55:33.6091438Z | `7D63292FF9B4A714E40AFEEADF0CE60C2C327D1A9E918B15F23F17E48C400A68` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\Build_Validation.json` | 384 | 2026-10-29T08:56:11.2958437Z | `26E825E983541D3C50AED6E089D80224740B0EC787470CFB86E3230CFB95F8A4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\ControlsRevision.log` | 2032 | 2026-10-29T09:11:47.3583645Z | `8D38FFFB95D5BEF7C33A5CCEA54CDEFED6FE0820956BD05E192F597C22808559` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\ControlsVerify.log` | 2730 | 2026-10-29T09:11:51.6887196Z | `2856FDEBDDB1249ADA55763EF06DF7044615E73EF9AA6157AF88CD7F2D6E80E7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\Delivery_Validation.json` | 1477 | 2026-10-29T09:25:35.3563983Z | `48DE2E309FFFAEADF57B24A1EAE69216323E80D77217BE0B27C18DB93B14C32B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\DeliveryRender.log` | 4628 | 2026-10-29T09:19:41.2345331Z | `A2C54857260BD114C2E827037A4C9FB97CC64BA3EE1DD026DFA632A52DC00873` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\DestinationReopen.log` | 2758 | 2026-10-29T09:24:43.6157399Z | `3F1952C9BAC5FF636FA78F023B7F0F8F405A1652470EBECAD3F53C1B20D05160` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\FinalRender.log` | 4992 | 2026-10-29T09:04:45.8232652Z | `DD96CDC1E4D6A3206059C9C732B35B36ECB4F54871C606BADF9DB8E1ED8BD51F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\FinalReopen.log` | 2932 | 2026-10-29T09:22:52.4005898Z | `8BA0DD9ACCF1142E52E218699E5101A970B5D903FCB7AB637CB3481C714B8426` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\FinalVerify.log` | 3836 | 2026-10-29T09:05:46.8070520Z | `9F0A9F179260F7630A66621DFB69EF488795052B8EC86123D58C0ED5E8FF371C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\finish_cabin.py` | 2586 | 2026-10-29T09:04:31.3522700Z | `62BF568C6E7BB013115A69393A4333D5BE2A9816B7865CBE41180937BAA88787` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\FinishCabin.log` | 3964 | 2026-10-29T09:06:46.9697703Z | `62032B53B8101414B5AA695FA3F707401044689D8D045F2F815AC66067FA4F05` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\flatten_buttons_and_render.py` | 3756 | 2026-10-29T09:22:22.1480631Z | `C474054C42E4FE24D924CF9511BD9C98979428F1D6B5837B2BD94661A80FF7A2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\match_control_reference.py` | 5504 | 2026-10-29T09:20:16.9791904Z | `3E3A73E68F6D161981123428C13701421CDD21FA4602946E7851A9B5020CDBE4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\raise_and_expand_controls.py` | 7738 | 2026-10-29T09:15:43.1649077Z | `2CF35F6ABA42E7695BEA6635E4F07BC6B0932DA4B1A89BDCBFDD71EADA1F78E0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\RaisedControls.log` | 2034 | 2026-10-29T09:16:19.2195496Z | `5355CA139D42EEDD5B3509DE59BE1AC98C595B3CE05447C16A05FDCCECA8B6A7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\RaisedControlsVerify.log` | 2914 | 2026-10-29T09:16:33.9936921Z | `D5F4B971F212FC18015439711DF5E3A400B9C418BD6DDE4BE4F2098849A708EC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\RayDiagnosis.log` | 3936 | 2026-10-29T09:06:50.1108711Z | `F7A535E7A19D9D61F1F02D2C58C4CF470D00609CC861997154C92CEAA59EF789` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\ReferenceControls.log` | 2948 | 2026-10-29T09:21:21.7236360Z | `8674963186482B8B3433A14D13F74E993308A4BB03D330A80706695E3055DEEC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\ReferenceControlsVerify.log` | 2932 | 2026-10-29T09:20:55.7305493Z | `8A2B0DAF668E9ECB5C2591AE824108CB3BFA192EBF85A251C2D485EF3C78BE66` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\Refine.log` | 2882 | 2026-10-29T09:00:55.2155390Z | `8B155EC5F696FD5D2BE071A4A975C90821B03EC952632DB26ABBC1BB1336EA0E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\refine_silver_map.py` | 5010 | 2026-10-29T08:59:49.5144453Z | `084EFC42F63392BB96F3C0BE9AB66944D31403E43D198D83B6F3E9E636116573` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\render_delivery.py` | 1656 | 2026-10-29T09:17:05.9346329Z | `ACE05441F75CE58BCC202953724752BF7BE681E9CE66A1B742EFEABD064F42CC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\render_final.py` | 4254 | 2026-10-29T09:02:06.7023446Z | `BECD38BD80C8814396702758EFEB3E042AAFEB46ACC97D07713B9E627539B979` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\Reopen.log` | 2376 | 2026-10-29T08:56:40.2856940Z | `7D47E491FAA99FE67770ED6C2A556C32D1202D271AF5195AC656C08407CC1306` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\Reopen_Validation.json` | 1544 | 2026-10-29T09:24:43.4734528Z | `BB5790089C59565E5DC6ADD2EEBE0FF18A0312873859800B698B777DD9EE60FB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\revise_controls.py` | 10091 | 2026-10-29T09:11:09.3967359Z | `09815394098114DB1E8F74AAEC0E891D8F6A952BEFC5ACA6F1DEB8AF30C4825D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\seal_cabin.py` | 351 | 2026-10-29T09:06:34.4972040Z | `BEE87BC60701AEE58F9907F5C1833C5899EBCCA0E1A5477B71B2A757B904F501` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\SealVerify.log` | 4224 | 2026-10-29T09:06:37.8904941Z | `85BEE14D26AEB70ABD3285DE6ABD7AFE53941A09F35740D30C7C53400FAF54EF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\ShallowButtonsFinal.log` | 5052 | 2026-10-29T09:24:58.0349872Z | `9E68A05B6A52CC2EEF363C369E21A4783C1EC78F2240FCD4C9F17F191B048843` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\verify_silver_map.py` | 4921 | 2026-10-29T09:20:52.6907464Z | `78E5C2628BBC80E741EED3EA02B17D3938018EBD923667C4E53D8B5BA0162030` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\엘베_맵_실버.blend` | 8390499 | 2026-10-29T09:22:25.0047866Z | `542BE00DD0BC879995EF29BF036C5E65A0E9EF1E1F87CC4FBC52EEBB5DCD700D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\엘베_맵_실버.blend1` | 8391110 | 2026-10-29T09:20:23.1882039Z | `391FFC894B0F70ED9E1656E09BE37DDB9C25235AE51E0FF237EDE8ECE866C6DE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSilverMap_20261029~\읽어주세요.txt` | 2612 | 2026-10-29T09:22:24.8912529Z | `B470DBF21FE6162957BB5BC308F5360E41DAEAC30CF0E7DC95DB2A6562AF38F6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\3층_01_로비.png` | 1709967 | 2026-10-30T00:21:01.5917644Z | `38FEB09073468CAE9899BEDC3EB22269A396723B6B24B7CE2A54754EA0580448` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\3층_02_내부.png` | 1668695 | 2026-10-30T00:14:20.9955682Z | `A07A790B50C6661B98A06B86C93B72A1F13F9A62C174D5F5D5DDC6CEF92D7202` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\3층_03_층별시설안내.png` | 1594768 | 2026-10-30T00:15:00.6883174Z | `D2899C690519170BA42C007438327EF22F8F53AFC5722400CEA5FF9647E8BC80` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\3층_04_농구장.png` | 1504929 | 2026-10-30T00:15:42.1894725Z | `0F75F30301DDD0FBC6EB7F8C48843A3DDB830B8F67748D0F0F0799A5D62C136D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\3층_05_건물외관.png` | 1479088 | 2026-10-30T00:21:37.9412375Z | `187B30FFF620D57F1C73F561F756EE8A4781432BEB025AA43027543B56A4D2DB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\3층_06_층구성단면.png` | 1663494 | 2026-10-30T00:22:13.2215202Z | `F8984A4811399B961DA97DB0BE11C0CED73AE978A452C9F0D3F86AB85B70979F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\3층_07_창밖높이.png` | 1489539 | 2026-10-30T00:20:19.6407460Z | `23FF58492CD4E5E6A732B61E9A75B0D8B5B894E851F4AD6C9EDB6530ACE4518B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Build.log` | 7476 | 2026-10-30T00:11:40.5005001Z | `A232ECCEFA5EAC846FDAF2AFAE6E5CE2B147F71105F92258291BF1F28EE48248` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\build_three_floors.py` | 23748 | 2026-10-30T00:07:28.2973344Z | `9302A077B870BAB479136BEEEB3747D1F9A0DDAFB2BF602991F68C931B36BE67` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Build_Validation.json` | 879 | 2026-10-30T00:23:00.1938103Z | `4E466306901BBF72C2A4058899AB080212A0EAF30930043BBE253C09CBC6D3D0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Delivery_Validation.json` | 2437 | 2026-10-30T00:23:58.1923536Z | `7B515A89FC3E6DC629D6F48206131522D361B1C2DBDED165764A282930D4964E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\DestinationReopen.log` | 3970 | 2026-10-30T00:24:01.4283543Z | `B14054EA362F5D46B8EEA9C5D8F16D5BC31F6B7DA4D91547566716178AF492A8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Draft_3F_Court.png` | 571674 | 2026-10-30T00:11:40.2747176Z | `36B762B83383334913237E0795F9493B43723E0BA2A6C0D806C4DDA5A2CD12E8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Draft_3F_Directory.png` | 618842 | 2026-10-30T00:11:11.5104755Z | `FA9095CA2501E2503B45625F257A7544A3AFB6548E9C1900BF6D7A8258191F7C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Draft_3F_Lobby.png` | 664843 | 2026-10-30T00:10:42.9171090Z | `6DAA88CD9F56EC9908909AE02F9B35E392AB21CB0234A308ABF604D6F2F70425` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\final_metadata.py` | 1340 | 2026-10-30T00:22:57.0167852Z | `E81320F01E1172FD47803D9442034F737309F73162E40A26B24CC58F1AAC6847` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\FinalizeVerify.log` | 4658 | 2026-10-30T00:12:42.6896489Z | `941224ED4DDEE7CD0CFAA487EFAEDBE00285E545C1EAEE0871014B55B242E97B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\FinalRender.log` | 7740 | 2026-10-30T00:17:35.0872496Z | `9C7923B2D327C629F085710D197E070413ADF8E860F1D0CB58D9FC33B2F8EDB8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\FinalVerify.log` | 4140 | 2026-10-30T00:20:40.4902328Z | `55869912C3A80AAC9DB6C12819264250205E5C6F7FB9F01139783562425C85C5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\finish_three_floors.py` | 2367 | 2026-10-30T00:12:38.5513403Z | `EE1A2D9BE5FE33E1106AAEC076E51D70A4339733C3919FE5B25633DFE750E1A7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Inspect.log` | 468 | 2026-10-30T00:03:20.0516649Z | `FABE8650D4EAF824C22A3AF5A55CFF20719A9F0E9B9570333FD5B06CCBC85B7C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\inspect_source.py` | 626 | 2026-10-30T00:03:17.0421144Z | `CA9B1219DF99D62AA2C9AAE4B5F4445656A82CF93E481BEBF9AACCD381187C01` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Metadata.log` | 864 | 2026-10-30T00:23:00.3653787Z | `B416FD665FA475AB5F9E454238DF968B7D832A439C9C2A6FD9B0C41FCEF9AD35` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\polish_review_views.py` | 3762 | 2026-10-30T00:19:32.9362847Z | `542A93E9ABF1446775893E58FFB2B0AFD3F16F58F0D4AC83EB6BE8FCA160FE68` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\render_three_floors.py` | 2438 | 2026-10-30T00:10:30.8331718Z | `C5AF2DE4DBAE92FCDA409F531F3EF50BAAD5B215F00B845A051A8EC8AF3DA83C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Reopen_Validation.json` | 2418 | 2026-10-30T00:24:01.2561952Z | `C42816437598DE6C28E1E98363629711BBEC239D9BB76FE4AD3F67914B5BBC2F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\ReviewViews.log` | 5108 | 2026-10-30T00:22:13.3927681Z | `029C7BF5F56BCABAA31762BCF7888A89C497C6DE068A5FE84CBF440D33E63CB8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Source_Hash.json` | 204 | 2026-10-30T00:03:18.0926524Z | `D94682D74DC005C609B5D7811B31C891E868714384565B106BF079EF62F7BC76` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Source_Inspection.json` | 368577 | 2026-10-30T00:03:19.9082556Z | `D9AABF64C50A4DB5E6D4B00C776C5F159336F4B4061F38A3D81737FA2A9EFD1D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\Source_Latest.blend` | 17086110 | 2026-10-29T09:51:40.0000000Z | `418134A63D808206E23A5AF10F86ED40C89185FA66C5020285B73646AD057F37` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\verify_three_floors.py` | 6626 | 2026-10-30T00:12:39.1942648Z | `0BDFD328006707A079749E9F0D0911834DBFC36CB607927830B89DAC438170EB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\시나리오_3층에서_귀가.txt` | 6244 | 2026-10-30T00:07:28.9394964Z | `122BD6BF62CD155FEDC82A8BF90DD9692D8CDDC64C30450AA7F45A2B82315C28` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\엘베_맵_체육센터_3층.blend` | 17619218 | 2026-10-30T00:23:00.1887027Z | `EDF4F0187485B3AA5CE09EC472323225D407A4C02A5EC088E2582485C82A7CE6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorSports3F_20261030~\읽어주세요_3층.txt` | 1332 | 2026-10-30T00:12:42.1218781Z | `04B5614D4832FEB7C997258E2D08C1E0FEC8A11EC00529A0DFABBDA2326F78AB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorTools~\InspectPresentation.cs` | 1710 | 2026-10-22T04:08:11.7147788Z | `C829FCB27D5D59382466C5B2BA4EBF4130494D2F928DAB23580CC854645F80FD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorTools~\ValidateAssets.cs` | 2617 | 2026-10-22T04:00:28.6090502Z | `804FE406B9A141385B5A4CE797CD862537DACB308BD9DF035C70794EE72E7FED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorTools~\VerifyCanvasGroups.cs` | 7664 | 2026-10-29T04:48:25.9093799Z | `C27524D8E93F3944B748F3D41BD86BE9536D5C89EA0D7993149089C042C37FA1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorTools~\VerifyElevator.cs` | 8065 | 2026-10-22T03:58:37.1653570Z | `A2C29150971F93EE4211CE7DD4D35C7307280AD64C5BA544E2FCB7D2E0A30FD5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ElevatorTools~\VerifyInput.cs` | 6894 | 2026-10-22T04:16:05.9260188Z | `375637073BD172269A0B4AEB210016D2BAFE30480A5B014CE6AF99DDFDB3A1C2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\Build_Validation.json` | 361 | 2026-10-22T03:25:24.2146339Z | `F1DD845933B7B2C2040264918C67BFF655BCC21F1D43170693217A836154C70C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\BuildFriendIntegration.cs` | 22075 | 2026-10-22T03:29:48.7071144Z | `333FAAE732A2999428E3A083E3C62054ADDCF80F07F05EE16DDE3E61AFF88893` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\export.log` | 2662 | 2026-10-22T03:15:54.5725100Z | `879D8ABA3538222F32FDF6FD9A8975A9BD09EFB8E5EBAB9500F9FEF13C84B0FF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\export_edited_friend.py` | 4224 | 2026-10-22T03:15:50.6810813Z | `C4A880508992D4BD8AA417AB7C16E6CD14D3B00D26288E3EBD27203D88A272AB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\Export_Validation.json` | 13284 | 2026-10-22T03:15:54.4402959Z | `F5F5A911A0306F193ED9196B296C002BD1481C24B73FEBCDD7AC969C7D1A2D7C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\FinalAssetCheck.cs` | 2002 | 2026-10-22T03:31:23.4532522Z | `0722E664D0A93AD343EE7E401D6A9D58DB7C31AAA40C31EEABDF371E0A6CE28D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\FinalizeFriend.cs` | 2010 | 2026-10-22T03:29:49.4188276Z | `54257145FCF6125197EF975D20584181E36E248AA302525EAB217B7C21A1B4F1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\FixPreview.cs` | 1532 | 2026-10-22T03:23:15.4657815Z | `9A6090CE1FEFD40E89943CDC8D1554DC5E53460CFC2D5D29B5B231F249C43EE4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\InspectUnity.cs` | 1343 | 2026-10-22T03:17:12.2200038Z | `5F11AE59CCA4E7E19E9EC3C29293CC483E02082AA680EA71B83571CB2C479A58` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\ReviewFriend.cs` | 5271 | 2026-10-22T03:24:54.0027691Z | `948213C6D12F14AD7CAA55A7B82E0D0D0AF159CF10B270E14881DF7D89CDD542` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\RuntimeFriendReview.cs` | 2080 | 2026-10-22T03:27:41.4031245Z | `F6491C4884CA1032C1B342CEA892973F6469D2CE5EE5FA507D16D0A6BBDAB542` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendIntegration~\source_inspect.log` | 4808 | 2026-10-22T03:15:32.3007675Z | `D401999120F2FFF67E99E9C4ADE568CC8A050B980056379E6209C42EC1CC0759` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\build.log` | 101380 | 2026-10-22T02:09:14.0246247Z | `9131810C3628358402A7330A7564000EB2DF4537DD6B3C9F838F3F6A6FAE7D75` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\build_friend_v2.py` | 32259 | 2026-10-22T02:08:14.9454319Z | `2E4EBF7C99D96F572465D8F8FD373D16DB61744A3B211F00867197C54D244E78` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Build_Validation.json` | 276 | 2026-10-22T02:08:22.1415772Z | `C7E0F9E2731D2D8EA25B6DDF82FA62EC00542E525AC4FA6A488B9868ACBAC133` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\check_leg_lines.py` | 611 | 2026-10-22T02:13:29.0304894Z | `4C18CF161E23D6EC39104206161074613CC8242E0A085D2469088B0246CE6411` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\README_친구캐릭터.txt` | 2134 | 2026-10-22T02:10:19.3686961Z | `000A4BFC7C4D4B9E41980AF25DF43B584BEC85DC3EEF1B1F2104C8F84B003DB1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\Wait_Friend_Back.png` | 1144708 | 2026-10-22T02:15:20.7343838Z | `2B6EE5D567797B0F29A37149A93F66A949EC764B025B7CD1E63BA04BA9AD06DA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\Wait_Friend_Back_ThreeQuarter.png` | 1144016 | 2026-10-22T02:15:28.3896029Z | `2CA3413B92D21F72D72262ABF76CDB37B9E65E928FECA294992297112E3932E0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\Wait_Friend_Front.png` | 1076395 | 2026-10-22T02:15:04.9215110Z | `B6C32357EB13CC12A6EE600ED18089C263AFD6B24B0DDF9A7F9EE81B6FF650F8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\Wait_Friend_Hero.png` | 1104046 | 2026-10-22T02:14:56.2149566Z | `A9C855988F66B0DD867A2AAC6B80F284CBE419AE374A893B37D90DB0F5838F30` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\Wait_Friend_ReferenceV2.blend` | 3572315 | 2026-10-22T02:14:24.8707757Z | `8A16A424A265214BE34D6FB551869A7A749BBCBD7D1693600CCD90073FF384D4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\Wait_Friend_ReferenceV2.blend1` | 3174350 | 2026-10-22T02:05:37.3271001Z | `8A31EA935D2175C5E6BE5239B5D737235E879B2CE9C9F2F475BD99624BB97C6F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Delivery\Wait_Friend_Side.png` | 1093341 | 2026-10-22T02:15:12.9774911Z | `825D2ECFFBA06C589BDEDBD26E0EEDC937BC47FA163DCEC0EDEFFDE9DEB58B03` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\finish.log` | 99032 | 2026-10-22T02:11:21.4630874Z | `90699584CBBCF97288C25976FA66756C9212803AF3A510A321EA853823A2605C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\finish_and_review.py` | 4847 | 2026-10-22T02:10:13.1984184Z | `12B266DF5C5E6D214413BA438D0B5D7A43ECA086D0649FAEC47EAB6F6D4ADB9D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\lines_check.log` | 19698 | 2026-10-22T02:14:04.5362062Z | `41D862FEE79239D6C3C24A4B60C791E16FB14D4022B177AFFB8202A400B543A0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\polish.log` | 98980 | 2026-10-22T02:15:28.6209674Z | `3F1160D1C23C8036B1D745A310DD54FCDBF57DBFE13AA42DA4733386D758467F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\polish_outlines.py` | 1273 | 2026-10-22T02:14:22.0409211Z | `E287ECBE9AECED0DB3593CD446CBE4EB063EC92F8F13A056556C5D2DAFD85E13` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Reopen_Validation.json` | 1557 | 2026-10-22T02:11:08.0503843Z | `CA41236EC2DDECDBABBD10635AEB55B657C24FDA04B55F8DC0317AEC4BA8A80D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\Test_LegOutlinesOff.png` | 551868 | 2026-10-22T02:14:04.3311466Z | `108F5056587773AD73E9A0420240D22EC998B55431E6DDCC92D47AB25B1AAB95` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\verify.log` | 4744 | 2026-10-22T02:11:08.2221975Z | `C8362EA9C670B0EC08EE3FA5ED52E2825B74D9ED63AA47DD9FC509079EDD2A6C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReferenceV2~\verify_friend_v2.py` | 3149 | 2026-10-22T02:11:01.4861320Z | `40B754E82F42BFD881DE480471E44D33EC760D52EA3EF15737AEC471A610DCE6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview.meta` | 172 | 2026-10-22T03:25:19.3872436Z | `41D7AB0DB3E83EBF039D9B23F5EBC41A71FE6DCC7FB69AC1A906FC9193F0FEF8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Fall_0.55.png` | 118360 | 2026-10-22T03:25:25.8418266Z | `A63C725BDE476507B46E70BF539D1E32ECA7D43D6C1C68A59C3E67E41BF16D71` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Fall_0.55.png.meta` | 3009 | 2026-10-22T03:25:19.5576746Z | `114FB9EEFE16A0F863C987664C8AF3E0D8105F7A3E85CC07C95F7E2B33096736` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Fall_1.40.png` | 111999 | 2026-10-22T03:25:25.8892289Z | `158F0BA02998A5518DF517AB0323313ACBF5ED8534CCEBD5E38B3BE90386F8F2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Fall_1.40.png.meta` | 3009 | 2026-10-22T03:25:19.5251055Z | `B3530538763078373C7889CA302A3FE3ACBBFA3C3ADD4328A90237355538C204` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Final_Validation.json` | 563 | 2026-10-22T03:31:28.1121209Z | `EA509BA3A4C140966C813C50D28CC3DFA95AE6AACC3F8C765DE967E6CDDF212A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Final_Validation.json.meta` | 158 | 2026-10-22T03:31:47.0431895Z | `F407E9204BB7DFFB2CBF63CEBB335C0D96B4842407F51DA53C85240054A3F7B2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Idle_0.20.png` | 112881 | 2026-10-22T03:25:25.7046745Z | `95F840296117ABE656C7CD8352AAD2D90C0E1389F9426F62B39CD1A1EA9B0654` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Idle_0.20.png.meta` | 3009 | 2026-10-22T03:25:19.6215196Z | `01693DCC41D79987AE1B07A2135B57F08342D969E6F724DF9217D7BADA2A494C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Preview_Fall.png` | 138359 | 2026-10-22T03:30:37.8006920Z | `DFF2E2A8EC628D4DE00A44AA707085BDD2EC51D8AE4F41B16F6DE63E0078EA3B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Preview_Fall.png.meta` | 3009 | 2026-10-22T03:30:37.8662653Z | `12B3C16237CC2268D5113758169030DC8D7FE0E610F99F6C1EAAFEFA18840AC3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Preview_Walk.png` | 139152 | 2026-10-22T03:30:22.4984086Z | `D4EA2B7C100256EB1CE89FF9EA668AC63B06A5864B9786A71036BB472B7F654C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Preview_Walk.png.meta` | 3009 | 2026-10-22T03:30:22.5757302Z | `7F0889A0887D93E23480BAE98CF0CC5C025AE8DF4685ACCE87CC843609054916` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_Preview.json` | 119 | 2026-10-22T03:30:37.2347937Z | `EEC9BDC6104FB95F471474FFE2745FB24B8D54FD6778BF294C119F965E6F8130` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_Preview.json.meta` | 158 | 2026-10-22T03:30:22.6082005Z | `F52A1D2A5F160400A82677CF602177ACC2989631AE6218ABDCAE9631ED078E13` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_Preview.png` | 134408 | 2026-10-22T03:26:21.0009174Z | `C030B00E561B09C61632DFD4FC8665F20318E404C5EFCC4359A84E0C57909687` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_Preview.png.meta` | 3009 | 2026-10-22T03:26:21.1116645Z | `DBF24077ED8173E730BF4C50A0777D064ACEA4D2FE853BB4C28B9F9E65FD2E7E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_Reward.json` | 117 | 2026-10-22T03:29:10.1261587Z | `F94D8EA3871F12A13F1C8D1380A4C357EC78F07E729BF47B212FB233E6D19DD1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_Reward.json.meta` | 158 | 2026-10-22T03:30:22.6021168Z | `CA17DBDD5819F53C81657F2060FF40BF01133945A38BB1460E26215D2E17DDFF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_SafeMotion.json` | 124 | 2026-10-22T03:29:07.2340850Z | `7CD736594C9541C330BEDAB09573E9C552CE91CDECBC5F415CD8A1A5880501AF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_SafeMotion.json.meta` | 158 | 2026-10-22T03:30:22.6321173Z | `E6ED9F80C11D8BB953E0D705509F3E6B4ED786E42AC03358BBA7FC5CA9881CE5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_UnsafeExplanation.json` | 131 | 2026-10-22T03:28:49.4986271Z | `64D7BAF70E83CFA9A4A621AEF2A226F9D54DC27B957EA91FFEF073E922C4279E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_UnsafeExplanation.json.meta` | 158 | 2026-10-22T03:28:50.1505817Z | `D25587A3C0409B701A3F46AA8FC4BE820F5B42793000611687D75C550C0836CA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_UnsafeMotion.json` | 124 | 2026-10-22T03:28:46.9868254Z | `8F7673D138ACA8763D3C172FE01E596244A02C55DC7E0DEE201C69227BF3C9C4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Runtime_UnsafeMotion.json.meta` | 158 | 2026-10-22T03:28:47.7277591Z | `DEF4FF4AB0A8D174A55BB95983333E32FB293E9FCC7178A63E549CFC00E061FC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Story_Fall.png` | 197213 | 2026-10-22T03:28:50.0676072Z | `B4204CD495D7A092A81E6B65439D20CC4D373BDB9521736B5BF5E03DC1F07BF6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Story_Fall.png.meta` | 3009 | 2026-10-22T03:28:50.1358639Z | `38284DE5FC8EE1EF78350F39A04C90BAD7CFB25D467A3BE1094E9814CF53898D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Story_Walk.png` | 213633 | 2026-10-22T03:28:47.6362074Z | `B98C5B94468E9D0F5E6C14594F6E09D1592A9EFF471DE5D25834196562B4A787` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Story_Walk.png.meta` | 3009 | 2026-10-22T03:28:47.7082350Z | `196DC37EE889F1A14E63A9665A960E50046014DBB560F6B7FE49E9C35B0F716B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Validation.json` | 1011 | 2026-10-22T03:25:25.8937548Z | `108848D38AA1A331DEF551844B6321FFE8DCFE78B262A02451A5A7BFD122BE3B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Validation.json.meta` | 158 | 2026-10-22T03:25:19.6516997Z | `D6A0FA95CF1ED9E758ED7267ABB325E6F644163ADCFABFC65227864C622210F7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Walk_0.18.png` | 111619 | 2026-10-22T03:25:25.7528855Z | `8D9D824DBCBF9A884B3F6BB184BAD991ABF497708619C0125542B1766DF40867` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Walk_0.18.png.meta` | 3009 | 2026-10-22T03:25:19.5880244Z | `0CE1BE500E320171DDBEA91C042629878DCCFA8A9DD43C25650C2CDC71EEB426` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Walk_0.54.png` | 111619 | 2026-10-22T03:25:25.7966534Z | `8D9D824DBCBF9A884B3F6BB184BAD991ABF497708619C0125542B1766DF40867` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\FriendReview\Walk_0.54.png.meta` | 3009 | 2026-10-22T03:25:19.4887597Z | `79D299FCEDF092A5956614CC2F916846E4DBC1E7E961EF9B5450CF13F644F951` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Applied.json` | 911 | 2026-11-03T00:48:24.6351205Z | `CF2D3298B62FDB62105F8CB4B5C0CC5DEA3C511669B3ADFA1F1028F4066C019F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\AppliedFinal.json` | 911 | 2026-11-03T00:51:24.0536846Z | `3D7D15E2DAAD21BF1EB3B65CA33A6FA4BC6479CA5A3FA120E1D37A5266B57DAF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\AppliedVerified.json` | 910 | 2026-11-03T00:56:54.2514196Z | `0DE534E0308DDF27DD9E5BEB9A316CB8AD15134835ED373FA2346963F4D4DE78` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Apply.cs` | 2488 | 2026-11-03T00:48:08.3776585Z | `DB8D6362AAAF20AFE6565C2DA6E4EAADF165497408AA8976FA9E0B57BD5B0841` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Changes.txt` | 22057 | 2026-11-03T00:48:47.1945766Z | `A64C701D020C579491BD43AD98D66244F38E81BAD47735660B1A36754AF5F248` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Compilation.json` | 288 | 2026-11-03T00:48:00.9584689Z | `3972D63F4C91A962518EAAEB51DE391501D2412F3DF767AF79CDB635BEC8747E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\CompilationFinal.json` | 288 | 2026-11-03T00:50:08.2392890Z | `3972D63F4C91A962518EAAEB51DE391501D2412F3DF767AF79CDB635BEC8747E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\CompletionCheck.txt` | 2700 | 2026-11-03T00:58:19.9423520Z | `3FE2C43E02B52E6D06C466CCCD59B0430C19E91851AE0EE4BEADB7657BD0B4FE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\ConsoleErrors.json` | 51063 | 2026-11-03T00:54:50.2202449Z | `E739D74741AE2872661E6DB791D58847EE54A877FB9CBD8BBA404135B02D7AFB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\ConsoleFinal.json` | 3877 | 2026-11-03T00:57:23.8101905Z | `4A5D3598CF0E2B204C8ECA216CB91F19A5BD8A9DF86F51337CED78D676A9C9FE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\ConsoleHelp.json` | 9783 | 2026-11-03T00:48:47.3251086Z | `EC93B5F26A36303BF49FEB2353AF4551BA468F9D91DFA543B9ABDAF6A233CF47` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Elevator_Safety_ObjectInteraction_Test_BallHover.png` | 646341 | 2026-11-03T05:08:11.1996504Z | `96C8ADA80B90AAA1558A9B8F2E74F76070F84EDB5397B912FE5561CB17F8F475` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Elevator_Safety_ObjectInteraction_Test_ThoughtAndHover.png` | 597694 | 2026-11-03T05:08:10.3863449Z | `A4027BC045DD74D1D8FDB9DA158FC54132E70C01C84BE29F34D3C0946B85C6F6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Elevator_Safety_ObjectInteraction_Test_Verification.json` | 3565 | 2026-11-03T05:08:11.4407074Z | `116DE12B98C1022D70C6E49070EDE2BAA2F746FE7D43C04AFDC308343418FDA5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Elevator_Safety_Prototype_BallHover.png` | 650971 | 2026-11-03T05:26:36.4395435Z | `28FE890251F74C9DB26C3D3C63ADFD11DE80FA4013DE7EC4FD181393DB34E389` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Elevator_Safety_Prototype_ThoughtAndHover.png` | 603081 | 2026-11-03T05:26:35.6126365Z | `83B49B464E0548A0FB882D5EC5A16E358CC476B48510AEFE34AA150055D604C7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Elevator_Safety_Prototype_Verification.json` | 3552 | 2026-11-03T05:26:36.6802167Z | `82BC706916DD0378213C2EA3970ED5CB9610DE7DDCD6262530CEF04EBA14FA1D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\FinalDiffReview.txt` | 2102496 | 2026-11-03T00:55:27.8106982Z | `AD337D0AF6C6036F2AF5EB2356579FFE1AB6D8424F9780B5653F915CF60B4BAC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\FinalGitReview.txt` | 3273 | 2026-11-03T00:54:51.1984440Z | `8DED719A4305039C064F739208D2879C942005F19A6B4A8A8F9B5B0798E37606` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\FinalStatus.txt` | 1631 | 2026-11-03T00:57:25.0720953Z | `5A4C71F6D27B5F09C9324B04DCDA74AA1E2A47428C544AC16F8C039F33C7D58D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Inspect.cs` | 1716 | 2026-11-03T00:47:26.1361725Z | `BF41D6301C16DD3F1635D7568D4D08058170719DEF85FC685C2A2E47D7F0C311` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Inspection.json` | 3221 | 2026-11-03T00:47:38.0095120Z | `ACFD4599E49D5339CF23CF36AB7383C9D16D6ED3910B0FA83424201DB3A46B27` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\OpenScene.cs` | 861 | 2026-11-03T00:53:38.6159393Z | `50579E26A93C294FB24819DCE90198BE83DCF8E2BE13F673AB84F47F127F05F5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\OpenTest.json` | 310 | 2026-11-03T00:53:11.8692391Z | `EB42E19D64644D3F434D7B7AA436163A32E011AE9353C0B0820B73065328E8A4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\OpenTestFinal.json` | 741 | 2026-11-03T00:53:54.0180667Z | `148581D40CA448AA1F9185D95606DEE4FD58D4F72E0A47B32629FF4478EE4F7B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Play.json` | 332 | 2026-11-03T00:51:24.7851220Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\PlayTest.json` | 332 | 2026-11-03T00:53:12.6429330Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\PlayTestFinal.json` | 332 | 2026-11-03T00:53:54.8359824Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\PlayVerificationEnvelope.json` | 4705 | 2026-11-03T00:51:41.2213475Z | `5EC4D2304152A2E2EFA009A503441F1E1BCAD574C4ABDE4CB5E121F5C7DA8D5B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\PlayVerificationFinal.json` | 4991 | 2026-11-03T00:52:35.7815267Z | `0CB48A41E2D9177C081C813950441229FA265BC0EBA8CA04D7CFEEE51F05B963` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\PreservedGeometry.txt` | 234 | 2026-11-03T00:56:51.7833291Z | `6EF9FC2D3FEAAC18CF771DB80060EA77BABD71B43632AD1A8F1F5EF24A1CB5B0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\README.md` | 2717 | 2026-11-03T00:58:04.6790920Z | `84B8CAFE1744B5C60F64A0B848D157859801450D0C415AF4C562B1F16C0271B5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\RestoredScene.json` | 729 | 2026-11-03T00:54:49.5238104Z | `F4BB0B210443369021A3FC5859D4808266DB9E2B156B679F183C279C47EEE066` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\SavedValidation.json` | 1277 | 2026-11-03T00:56:56.4587761Z | `657208641B966599195DD0C699AEA3E0D3651838AAF75AA92421FD51D27E8032` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\SceneDiffFinal.txt` | 1968 | 2026-11-03T00:56:57.1496200Z | `401BBD24264B60C4D928B79B76A2F446DFCF3D5E45D29B054B455F3AC34DA3E0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Status.json` | 591 | 2026-11-03T00:48:19.5677666Z | `4BC58A427578A823BA68B54160F23C60E247EF7CAF0A97784ACC74E169299501` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\StopBeforeTest.json` | 331 | 2026-11-03T00:53:51.5521523Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\StopPrototype.json` | 331 | 2026-11-03T00:52:36.5095134Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\StopTest.json` | 331 | 2026-11-03T00:54:48.3618705Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\TestVerificationEnvelope.json` | 5005 | 2026-11-03T00:54:21.1534428Z | `5D7ED0FDDAF51055C701A51C6F9AF578F1B3F77DDBED9FCD88EEA43DC18638EE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\ValidateSaved.cs` | 2123 | 2026-11-03T00:55:52.6450017Z | `D2AE30408E81F6F18BF8C738406833B5A3BF8924EB0F30E0FFE9CB3A9AD05BC6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionFix_20261103~\Verify.cs` | 10844 | 2026-11-03T00:52:20.0067605Z | `AC4D7B9FCFDFBB0B9268BA8EC474B9809BC484A26D696C2A30A4683D3503B546` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Applied.json` | 913 | 2026-11-03T02:51:34.9536940Z | `460E6793E912EE71F98B825DC8544B446DEDD8DCF542D0257EF8A1AEEDA02EDC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Apply.cs` | 3586 | 2026-11-03T02:51:15.2815036Z | `5EB0045AB3FE322B3CADB09087B950B36F90FDD3ABD64B4CB853E8C11682B8BB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Baseline.txt` | 12279 | 2026-11-03T02:34:25.2568892Z | `EE53F8DB9CF6748F24C53B8B3CBA1904DED80846BC6FE1F4E61EB679174D58E9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\Elevator_Safety_ObjectInteraction_Test.unity` | 1398516 | 2026-11-03T00:56:53.6389637Z | `0F6FF860C348E42367370D5A725E0885FB6851B094CFC16D7AFADD3EC9C43BC2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\Elevator_Safety_Prototype.unity` | 1394586 | 2026-11-03T00:56:52.9762819Z | `61348E8A1F68C95C34D0AB742E38F794ED2B35954AFD2E2CA4DEE72700551E39` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorCharacterPerformance.cs` | 4735 | 2026-10-30T08:21:22.8301525Z | `0B399583487A88C2BC6F035F52F3A5CBFE6D9C3581586599480EB325B3110A05` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorCinematicOverlay.cs` | 5567 | 2026-11-03T01:21:47.9006536Z | `DC354F2F8060BA7D7B86CC6A35B6209EF2B73378E3ED37752AC0155A69DBA8B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorDesktopControls.cs` | 13350 | 2026-11-03T01:22:45.6411980Z | `048F1DA950AA1F337B718F22C3B5EB2FD817DCF9C227D33E22F6B491F6C9CCEA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorFlow.cs` | 12496 | 2026-10-30T05:54:06.6220687Z | `2561085596D72A06C96D8BBF66ED665A9B2CB67CA120465BF998EB728EEFBE8B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorLesson.cs` | 11719 | 2026-11-03T01:21:47.8996557Z | `1D30C379FCD1430C908188A779ABA08FB4BFCF341CC5957011FDAC02CEA6FB94` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorObservation.cs` | 6072 | 2026-11-03T01:22:45.6411980Z | `8C1DC4EEB77AF296D8A44CEEEF7BB05C6441158650365A0AAF0393F2298C88E8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorScenarioDirector.cs` | 8226 | 2026-11-03T01:21:47.8996557Z | `62150230AF142705E4E4B5C1C1FBC51CC6F5753959EC9820AD29317EBCB9E6BF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorSpatialUI.cs` | 18707 | 2026-11-02T23:44:24.8046816Z | `EEF3B4A3DA4419348C353DD761E2F54C7185C3D5E7CC4DF74D0BDAE93FBEF77D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorSportsCentreMap.cs` | 11109 | 2026-11-03T01:21:47.8986492Z | `851692621B9103413D24E5B7DFD083353AE63CFF5AA41EC2C847C51D995951C9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorStorybookUI.cs` | 14073 | 2026-11-03T00:47:11.0149399Z | `2E6118B1A34DB19903476CEA56055FA3C68EB256C6FE3891C9FF74F59F8D86E7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Before\ElevatorStoryPresentation.cs` | 12531 | 2026-10-30T05:54:06.6235754Z | `26AA15983142A1915EA0BE083C94239866C90CCC99D306C6443C3D008A175D09` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\CameraRegression.cs` | 16034 | 2026-11-03T02:53:44.8908399Z | `41FDC83A32E631A0A1A1FC9B7BBAAA550F6B84782E241BD7BB84E5736D571C7E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\CaptureReview.cs` | 2382 | 2026-11-03T02:53:44.8913449Z | `4FDB8F13E24CDCA7630C219A0804E6E4ABE1A2AE8D516D9F8E892B285F9738E2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\CaptureReview.json` | 679 | 2026-11-03T02:54:13.1901220Z | `057A82CF4962043144B7DC8D68CD97397710AC2FE69EBAB7AD9E55AE2A0614DC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Compilation.json` | 429 | 2026-11-03T02:40:46.8207290Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\CompilationFinal.json` | 440 | 2026-11-03T02:56:09.3393083Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\CompilationStatus.json` | 440 | 2026-11-03T02:41:58.0228457Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\CompletionCheck.txt` | 3173 | 2026-11-03T02:57:38.1072842Z | `4DE66A41B107392B24D2545FE3028848001083CCF12ECECE09D439A3027F006D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\ConsoleFinal.json` | 907 | 2026-11-03T02:56:10.0215807Z | `3D63028248D1CE783D26DD49C3C2A303D0D75B0DFC597A3FA7291CBFDBC8B538` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\EditorBefore.json` | 591 | 2026-11-03T02:34:25.4678567Z | `C1C64BD727DF04A66A868A033448A413E865801577D63972A8D52B47C7403A19` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\EditorFinal.json` | 591 | 2026-11-03T02:57:38.2512516Z | `A23365790B96F9F4C5F6B8CDA99331382EA82325ED7105B126603CE3B261AF6C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_01_PocketCloseup.png` | 466228 | 2026-11-03T03:51:10.5130241Z | `700640A6F3206D5BCE8ADADBE8C5C446E31338D53308B986AABFFA4093ED2CD3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_02_IntercomGuidance.png` | 216011 | 2026-11-03T03:51:11.0509655Z | `594C4C4754BCC0B2CD639C774E4BC734763456EAF07E49E2DD6F63A774451E4E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_03_ManagerAtBell.png` | 674027 | 2026-11-03T03:51:11.3146066Z | `8D44959CF543809AE732A34AEE4E0DF728047A6E4A354A00633B0827A0B86355` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_04_DescriptionOnly.png` | 650964 | 2026-11-03T03:51:11.4463292Z | `7C3F0B1A50389B52A8EA8E7E9E4C025A239B5406BAED28AFF0F76A2097EB0D5D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_05_CatchStart.png` | 455033 | 2026-11-03T03:51:11.5720542Z | `E2C1D7728406806A790F03FAFC6F6046A5A84722F23A1996BDFAC23DB2376BB8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_06_CatchFade.png` | 493354 | 2026-11-03T03:51:12.1595089Z | `1D1279A73388F4C1A3A8EA5AC97393F749D6FF4465140849F7C6B78083B98627` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_07_CatchFinished.png` | 490669 | 2026-11-03T03:51:13.0044485Z | `E74E9139260225404C455D129865849E0AF14F68D4316E9E856FBC7F88801244` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_CameraRegression.json` | 13468 | 2026-11-03T05:08:07.0879705Z | `CFD967D2BF54D6C31DEEA5F419F386F88517E1D991AB5A218897F1340BEA905D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_ObjectInteraction_Test_Verification.json` | 6126 | 2026-11-03T03:51:13.6145112Z | `C7466EEB885F9019A76FBFBBD965A6EC2C2370B91F8AB5375C2AAAE0FEC00372` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_01_PocketCloseup.png` | 547911 | 2026-11-03T04:27:32.2593325Z | `11D711D274B7A38FFD2E1340F81E91209ADB8296EEFEC697AD245D1988B676DB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_02_IntercomGuidance.png` | 836148 | 2026-11-03T04:27:32.8784754Z | `0F3898305D2378B9D791411875DEC2C23D5DD91054E5A1DA4E63A2F515069F81` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_03_ManagerAtBell.png` | 697247 | 2026-11-03T04:27:33.1343852Z | `A11B19013563A62A403CBF5367679F22AC3E8AAF603DA2952D012411085521E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_04_DescriptionOnly.png` | 650876 | 2026-11-03T03:51:34.5406923Z | `EB8703E65DECB4BCD30F5ADE9783A506815DC6D436E4EC95A921E151E4689521` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_05_CatchStart.png` | 681532 | 2026-11-03T03:51:34.6818056Z | `D82BC0970B2B8DD4B8B607B4FA24207903066DA2F5E5FB2FC54534D277F6266C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_06_CatchFade.png` | 721025 | 2026-11-03T03:51:35.2850207Z | `FEB6DB42A0D3EF6848DFAC64F0BED243464815B5D2DF5FF66C259A577C1804BC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_07_CatchFinished.png` | 714075 | 2026-11-03T03:51:36.1312678Z | `88BE29C52552DC49A36C072E92C45B48E9D00E5C18278167DA504FFD08D92357` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_CameraRegression.json` | 13455 | 2026-11-03T05:26:32.3317913Z | `B4369F1497FFA9F9E8AFFC9A628B33094B75EF307A422459B3341E68966FB867` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Elevator_Safety_Prototype_Verification.json` | 4982 | 2026-11-03T04:27:33.1799862Z | `FE37359266EFE65F6B99B32B1763CC030ED9FC6B937436B7080486FEB0447E21` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\FinalReview.txt` | 7622 | 2026-11-03T02:56:11.7584577Z | `5F9043FCF1A535C88A018068E69A444AD74F51973D0B24C50E074CFE16ABD4B7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\FitPhone.cs` | 2706 | 2026-11-03T02:50:15.9664484Z | `044F9AE77AE5F73ECFEE8D6B96B7599166C7F371BC7C09467B50834A6BB76F4C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\FocusedContext.txt` | 14518 | 2026-11-03T02:37:55.4093073Z | `D7B13FE14DB1CD332845116F815505A377FE4998162D8829746042036AE3A01A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Inspect.cs` | 1947 | 2026-11-03T02:35:53.2622252Z | `B5641B64C46C4B0A295B5DBE148D1AF6987BF0FA4599710E7BDC1792C92D3F89` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Inspection.json` | 13179 | 2026-11-03T02:36:07.3074068Z | `64C6AC4BAE5CD606283EC4C146F92C13683100F4FA48F17DDB5FF2017DA9B6F9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\IntercomAndDescriptionReviewSmall.png` | 162457 | 2026-11-03T02:54:13.1775044Z | `DE7AF478AD186377D0317F3ACDFE641A5C377ABB75E2C03830049483BD7A0078` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\InterimReview.txt` | 5749 | 2026-11-03T02:48:39.7186987Z | `EE0CBF426E2A1998B6892CD3C9314A5D757A2F20D5B93EEA81B03E5092CD648A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\LatestEnvelopeSummary.txt` | 1970 | 2026-11-03T02:50:41.6191872Z | `E6C800342997F8A5070936F8C4C4F62AF5A180B121CF88E0E348D51FD28243B4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\OpenTest.json` | 739 | 2026-11-03T02:52:31.9908101Z | `4208D7CA5F8F942E36121B1BA3382521CD9C3F2B090826A1490E8F1B9E44EDF1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PlayPrototype.json` | 332 | 2026-11-03T02:51:35.6917323Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PlayPrototypeFinal.json` | 332 | 2026-11-03T02:54:56.5945236Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PlayTest.json` | 332 | 2026-11-03T02:52:32.7211574Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketFit.json` | 1295 | 2026-11-03T02:50:40.8272581Z | `2B9E59EAF86964D72C893E4F441F6E6F5FB98CCE889869162F9E83EBC9A8E86B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketFit.png` | 567070 | 2026-11-03T02:50:40.6369988Z | `F7A902DDEF95F335AF277859361A10D2249363B02041B1758FB62CB511A6CF06` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketFitSmall.png` | 184259 | 2026-11-03T02:50:40.8154478Z | `70E64B824E37CED3A9FAF8E34F47D3F5E2D5314AB9D83E0CA818379E3B168C99` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketHits.txt` | 739 | 2026-11-03T02:45:58.5834758Z | `E909F38EBD38FCBDEADF0D18EEF394521E4D1E4AC96D869E88C1C733588C7022` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketMaterials.txt` | 0 | 2026-11-03T02:45:58.5870374Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketProbe.cs` | 808 | 2026-11-03T02:40:32.8570124Z | `DD6AF56EF961873B3614D11A293F0AE6676DA0F406637191761CA72F2AAC41FA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketProbe.json` | 2417 | 2026-11-03T02:40:47.9012225Z | `A0C8ED61A3C3BF0FC4425DA7AEF52E890812A1F66D91916F3BA195C28D29392B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PocketReviewSmall.png` | 116321 | 2026-11-03T02:54:12.9340235Z | `1BEFBF373B030D5A68A3900608727E994077DEEA04FB03D35657BF266BFABA5C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PrototypeCameraRegressionEnvelope.json` | 17175 | 2026-11-03T02:55:20.2086946Z | `E4C29CC28F623F82C752293A48F9E2417EBCAB0C08E5BED9D5698DDBE8A11182` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PrototypeEnvelope.json` | 8084 | 2026-11-03T02:51:54.7249603Z | `46D6DCB41153B7092A0C947C7E3AC2FC7162BAF3B8D2C3BC508E6BDB45871D72` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PrototypeInteractionRegression.json` | 4991 | 2026-11-03T02:55:25.0714017Z | `14BCA95EFF2533B19C2F7076EABD07071A9FAB93FAB9C37B1D4BC2DA5DCBDF74` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\PrototypeSummary.txt` | 1544 | 2026-11-03T02:47:33.9435158Z | `BE611D893307F5C2D0079C9E9066FDC751224699BB9D62D9E6677005DA31CDD7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\README.md` | 3745 | 2026-11-03T02:57:21.7451027Z | `1A7BA1F8A73C5D1856D83CDE059BF07ACDD980B2B0044581073690E945367ECB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Recompile.json` | 429 | 2026-11-03T02:41:34.8799196Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\RestorePrototype.json` | 729 | 2026-11-03T02:54:56.0156890Z | `3B90363AD9623C8C1793BAD778AAB97C186FF1778CBCF31DA1D2C585AB8AC80E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\SavedValidation.json` | 1249 | 2026-11-03T02:56:08.6518625Z | `78AE446612860F3B0D7EE129B0D4890E8640D1FA87348D6724B5361001E1798C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\StatusNow.json` | 593 | 2026-11-03T02:46:54.2029883Z | `AF5C8432631CFACFDA7EFB22213EFD5C46796D6FE9AC8B919DE4A899C09004DF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\StopFinal.json` | 331 | 2026-11-03T02:56:06.4229687Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\StopForPocket.json` | 331 | 2026-11-03T02:46:23.9093631Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\StopForPocketFinal.json` | 331 | 2026-11-03T02:51:31.6198385Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\StopPrototype.json` | 331 | 2026-11-03T02:52:30.7956815Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\StopTest.json` | 331 | 2026-11-03T02:54:54.3376249Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\TestCameraRegressionEnvelope.json` | 17187 | 2026-11-03T02:54:12.1058320Z | `0A32EA70C8BDA8B0D9FF6BFC163DD5609742E7A60B356C7282ECB7D1BCFD4E45` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\TestEnvelope.json` | 8097 | 2026-11-03T02:52:54.2687006Z | `16759D7B57472F9264E2B96DDCE4A612035D41E83EED8402DEE536BA903171FC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\TestInteractionRegression.json` | 5004 | 2026-11-03T02:54:53.2501587Z | `AAFD1C7E001D08FC1A44FBCB4693B69FB8F9EB5335B308FB8FACB7F632C91A62` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\TestSummary.txt` | 17 | 2026-11-03T02:53:06.3558861Z | `834A7ABED553E3CD3394DE11D69DC4D6EC353E7DE8A0DF4E8A1A8761CF9C5484` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\ValidateSaved.cs` | 1887 | 2026-11-03T02:55:51.4233677Z | `6A7353B254B3C46B602FC39083B0A8A3EE2FEB6C380B16247187AFE10F2B15FB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\InteractionPolish_20261103~\Verify.cs` | 15609 | 2026-11-03T02:49:22.4648016Z | `A8325D95AE9E269F595399A2425603448FC31D0C613154D160DBE3C1FB62A7BB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\Before.json` | 1242 | 2026-10-30T09:38:14.3894676Z | `73373911C50B47B6641A4482E82CCA1ED1C890928691FF0F72485C3D48269555` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\CliManifest.json` | 6153 | 2026-10-30T09:38:18.3798603Z | `1CEC2FD255632E77164F1811E47056B89357261609A82DFEC42D8E32A8898314` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\Deliver.cs` | 859 | 2026-10-30T09:48:41.6335529Z | `CF967F975A8CDAEB8E8FFE840EEB92D6613DCE4738021CD8A1DCB4743F30381A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\InstallerEnvelope.json` | 3725 | 2026-10-30T09:45:43.9570917Z | `818809E3D51070A94ED9E10461E13C09DD5C7A8E3018FE98442056D74767D5FF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\InstallerVerification.json` | 2680 | 2026-10-30T09:45:43.3548481Z | `045B9EC5998922B6A1AD854D4D1A18E3F4D12A707C8C2659EED30B7F0FA69E84` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\VerifyInstaller.cs` | 6973 | 2026-10-30T09:48:06.4336401Z | `1BC038824E4011F45A645446FFA15F06B77ACE8DDC7F5568816973E67A1830A3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererTool_20261030~\VerifyPlay.cs` | 1858 | 2026-10-30T09:45:43.9894084Z | `173F6559D6364B7CCB9098A5325891AEE149295BB60B6AC7EE7DBB1A8B0C6BBA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererToolValidation.meta` | 172 | 2026-10-30T09:42:57.3163193Z | `090A035161E5C2A88FD988208D9124B4645E9E3562DD2634EF9F15B474B64D65` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererToolValidation\InstallerValidation.unity` | 1335121 | 2026-10-30T09:45:42.0278680Z | `3A2BE03E3BC50D54B27C64AC7CE9C361AB778B763D3D21D3DA93A03ED389B5DA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\OutlineRendererToolValidation\InstallerValidation.unity.meta` | 155 | 2026-10-30T09:42:57.7553755Z | `B552D6F56664798B4B817F959DB567C00D681EC881ECF7D53519BBC0B77EF84D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.image-validation.json` | 539 | 2026-10-22T01:02:26.3373982Z | `9034E2D94128F56DB414C775EA815E31F628F5832310E422DE866CA444722C16` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.image-validation.json.meta` | 158 | 2026-10-22T01:04:49.1958283Z | `943868E976E32B2B28D0207AB7549A02B02F4976C811785A7E05FD420EF257DB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.prompt.txt` | 3762 | 2026-10-22T00:59:10.1109883Z | `DD6559ACF9F26C962AC84FD34071EDDC4F5442A98AD2BE9F67AC1BEE14768E6F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.prompt.txt.meta` | 158 | 2026-10-22T01:04:49.2262482Z | `5D98A8157C846B2DF140D36431C4201A54B1C546D5176EE724080CAC500FFCCA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.seam-edit.prompt.txt` | 1455 | 2026-10-22T01:00:18.7965078Z | `7E666169AB01545BB9710708C2786472749550138C7842170F39095ADC3B3586` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.seam-edit.prompt.txt.meta` | 158 | 2026-10-22T01:04:49.1649683Z | `0148366232E0C6782B7C84FEFACCA8E32E0D88AF198E9677CBC038489E256D8A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.taller-edit.prompt.txt` | 2945 | 2026-10-22T01:02:43.9315072Z | `9FB42985FE0540443407F4856113B7C88B53BB3472DC7574F17331F2262DE51E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_City_1024.taller-edit.prompt.txt.meta` | 158 | 2026-10-22T01:04:49.2174353Z | `F9A5B11CB9164B904137625164FB89BDC326F0D2CBB0CAB586DD70F6131287F7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_Day_1024.image-validation.json` | 292 | 2026-10-22T00:55:23.7529590Z | `5514E5B59AD0D5BF47E50A04C279A16B22F07CE00AE5BDC4AA9380D2B7BECE3D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_Day_1024.image-validation.json.meta` | 158 | 2026-10-22T01:04:49.1717521Z | `D11102EA5B58302F7154997846AE7FA6CDB19EF54E6DE31683FCA9F82FDB8BF2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_Day_1024.prompt.txt` | 4631 | 2026-10-22T00:55:11.3090123Z | `50FCD5666C1C0928FBE13C5F57CE91546BFB69568064EFF80F29F9C5C257631C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\Skybox_Storybook_Day_1024.prompt.txt.meta` | 158 | 2026-10-22T01:04:49.1685373Z | `027EEC0206D72068601D038B991F6B4642BC0C9B8C6C0CDC2AB3D2DA2B1B4ED9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview.meta` | 172 | 2026-10-22T00:58:18.2690251Z | `E1D71F199FB49ECDEB2B9917B3D23CCAAE87AD7F534F9FB41CE7310593151929` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\Preview.unity` | 7217 | 2026-10-22T01:02:29.0589683Z | `AB7F860F79FC135AE21008A10FC2CCB740C8490CC2BFE0C610F1FE86D696B3A2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\Preview.unity.meta` | 155 | 2026-10-22T00:58:18.3447277Z | `E38EFD4FAE893A8BCFAF7BC8B20E30D9460E65975B02FD70343736ED600190EB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\PreviewSkybox.mat` | 1104 | 2026-10-22T01:02:29.0351575Z | `BC8B7AF4CCD2C2D45E9AC8D91BE7135F2524C60B3C8BF28153F535D717E31E32` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\PreviewSkybox.mat.meta` | 188 | 2026-10-22T00:58:18.2853009Z | `CEFA2C5584EA77E015B303FFEF0ECAEC94D6BA8A4E88B765D2B4E473830F8B10` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\SeamValidation.json` | 356956 | 2026-10-22T01:02:07.9321212Z | `BA1A1ADAA88C5A61AFF664658D92BD4D23C8EED7DE223211457A5571A5A218B0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\SeamValidation.json.meta` | 158 | 2026-10-22T01:04:49.2100486Z | `BE32BC5106AF262D82E16BA8814EAC4E659379225AF8FB772882CC3132B84546` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\Skybox_Views.png` | 1167198 | 2026-10-22T01:02:29.2223617Z | `11EE24C4AA8DF6DCD8516C1E56DB951C07AA1177E2BD88E421C8ECA25F829A43` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\Skybox_Views.png.meta` | 3009 | 2026-10-22T01:04:49.0875344Z | `D641E730C5218F9DCE47F11C9E6B6CACF15E518E45E6AA8C3C6995E762600793` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\Validation.json` | 769 | 2026-10-22T01:02:29.2291850Z | `315B5F321E00B1A1BDD6B4ABF9E274CBFA9B0C0C41AB11994F88641BAA10EA8B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\Validation.json.meta` | 158 | 2026-10-22T01:04:49.1830954Z | `53B923D965AF8A3272A5ABE1D040D18AF4F4035B3BB2EF6A1A1BF20E4DB52ED9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\WrapSeam_4x.png` | 158883 | 2026-10-22T01:02:04.5434783Z | `06B00856DFE28FB044433BD9F2AD19E29E3B36C65B83B3B40AED43EB783B6715` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxCityReview\WrapSeam_4x.png.meta` | 3009 | 2026-10-22T01:04:49.1267837Z | `DA3559A84E2D9F23FC03F574B9D0E191A60C00FF1BB44363204B1B1A2D4E8A25` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxReview~\FinalizePanorama.ps1` | 6345 | 2026-10-22T01:02:02.3102732Z | `548BC2C871A52D94A9B49D79A737DB2A8966CC18FE92B8BCA5D74EB822EAB1CE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\SkyboxReview~\ReviewSkybox.cs` | 7199 | 2026-10-22T00:59:55.7569499Z | `4328E0E19C90AE1C0AEF35B54358A0A7086FB000E3E5DFE1F7C052C5692A1799` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_20261029.csv` | 1617 | 2026-10-29T04:43:01.5417890Z | `1792B9A6CF2FBC7CFF036D5FF3C5AE399ED643F5C50826E0A62F1DCFA66AA019` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_20261029.csv.meta` | 158 | 2026-10-29T04:47:43.0812973Z | `FB0DF107827FC1D413A4E8C73D642BD0FA6B17A07EEAB2968D571A09D16A7AB8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_20261029.json` | 292134 | 2026-10-29T04:43:01.5008348Z | `59F3E183D4B3CDB41412F074A0F433C7181FA825239F103976256A937B2090E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_20261029.json.meta` | 158 | 2026-10-29T04:47:43.0791692Z | `0A7D27706CFE54AE4FBB3C8C5DD276DC8243B0BB15A33760781359E7234EBBDD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_20261029.txt` | 2757 | 2026-10-29T04:43:01.5839862Z | `FDE3A6193CCB13FE1E06E7018714960421FA457898695F3C65C252A8BEE44261` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_20261029.txt.meta` | 158 | 2026-10-29T04:47:43.0715574Z | `C55E54AE40293993281D7280FBEB7DD5FA0A864AD736AB26C183B489BE0DA5D4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_Unity_20261029.json` | 66415 | 2026-10-29T04:42:23.8890576Z | `80BBF2DAD249644C7DC1342BBBF579591B7EFCF3BD7F1D664CA2EF54DCEB393F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAudit_Unity_20261029.json.meta` | 158 | 2026-10-29T04:47:43.0942025Z | `3FCBE2297A84E5CD9509188034C70012B3734F0E8844AABAA28F7CE7710B1E76` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\TextureAuditTools~\ScanImportedTextures.cs` | 2068 | 2026-10-29T04:42:21.8295215Z | `D7F929361CC5AA92758E2CF289EBAF5EFC90976667E0946D97088AC524F2AD1B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\AfterReproduceStop.json` | 315 | 2026-11-03T05:22:35.1439637Z | `E3B030AEEBC8B3DA71F9E04D2B0A4DE68F9A5CBEC759CA9CEEB931415373ACD1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Baseline.txt` | 15650 | 2026-11-03T05:19:55.7463090Z | `61E70107F676A0AC7A811898E42DE93D57F116E7220745B7F4D2B07E60E2E64C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Before\ElevatorScenarioDirector.cs` | 10668 | 2026-11-03T05:00:58.0691464Z | `848DE1800D89417B18014B78F1BD0D51CE1E6ADCC1A4F639AC4600135C131E56` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Before\ElevatorSpatialUI.cs` | 31649 | 2026-11-03T05:05:38.1445948Z | `B975BAC36F75C03DBA0C00608DFB34F0962705FCED4749F1110EB7EDC01D4148` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Before\ElevatorStorybookUI.cs` | 15959 | 2026-11-03T05:00:58.0686336Z | `945C2297663D1B574EBC374D76EAE2D3AAAA1256CAE5F72BFE563D4A5FA68B2C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Before\ElevatorUITransitions.cs` | 10023 | 2026-11-03T05:02:14.6796141Z | `178576C28353E9C6FF6D16F159195DBEAE95087CBC86A04D4A4427FCBDF22473` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Before\ElevatorView.cs` | 23083 | 2026-11-03T04:23:09.0213478Z | `E9C647E08918C029B21432F920FE97DBDE88B156322AB7CCED3EBFAE01108B99` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Before\MFont.asset` | 27293 | 2026-11-03T05:12:13.9385855Z | `F05CF21F6D52601486C1F91166BD1CB002E221CB5AC2AEFAFA6FFEF6670264A1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Before\SharedFont.asset` | 27293 | 2026-11-03T05:13:04.7656466Z | `7F5775124BED050C4E35FCD124D46E2C97FE4680BC258B4F319A1A7A03C4BFA3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\BeforePlay.json` | 316 | 2026-11-03T05:22:20.9484117Z | `44A6AA59987374434A9F16E837E1CE97C6ABE8256B0DE2ACF5F76840A49E705C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Cabin_ThoughtBodyOnly.png` | 730627 | 2026-11-03T05:26:11.2045945Z | `5F36533CACEEC575B292DE8F6209B8B7CCC765117A4E492B2077B9ABA0A921B1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Camera.json` | 16638 | 2026-11-03T05:26:32.9325085Z | `5BC2E44F7174DA1356BE7AD17C3131828007DF7AE1432759ED081589FA3928BF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\CodeReview.txt` | 11145 | 2026-11-03T05:26:12.4814534Z | `A8BEB5EC87DDFC92C74787E5B91681345D6AE2FC1B6460AC4768B3F516AABBD5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\CompilationFinal.json` | 419 | 2026-11-03T05:26:41.3675109Z | `0AA18ECB77F774630293DBD8E75D9798C7DAC42185B44FFDFEB6688D49552DA3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\CompilationStatus.json` | 440 | 2026-11-03T05:25:13.7485428Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Compile.json` | 429 | 2026-11-03T05:23:59.4635655Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\CompletionCheck.txt` | 4014 | 2026-11-03T05:27:14.1992201Z | `5F70FB1B0D48250CAEB39E901617592253B5A59321F5EFD54455E912FFB693E8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ConsoleFinal.json` | 869 | 2026-11-03T05:26:42.0508493Z | `0EBDB098D265A75A83524CA69A2E40956CCD8CE08E3BB196A56458B0D7A7287B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\EditorBefore.json` | 591 | 2026-11-03T05:19:55.9938322Z | `1D8905D0AB1520E4BFF039A9C3AF61D1DCCEEC5643721A2FC3D18C083178DD38` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\EditorFinal.json` | 567 | 2026-11-03T05:26:42.8409914Z | `B8D370FD76B86AAA503E0BF7BF0B64E8056AC07D2C6AE5AE582194365AD69A86` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_01_LobbyReadable.png` | 697834 | 2026-11-03T05:26:14.2346370Z | `15C1A1388F19A4F6FE2D7602F8A26684EE20E65E502EC56B323C646F348A441E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_01_OpacityFade.png` | 606357 | 2026-11-03T05:26:12.5929677Z | `4C03DAA9057EB6C7AD921986A7424ECF5C648F5AC988111A27E7C7D9FF68FA3B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_01_PocketCloseup.png` | 602690 | 2026-11-03T05:26:16.5476366Z | `FE120D908641DEA90C684EAEE7F817DDCA95D5B40189F884B1F29B5C4A70B21E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_02_IntercomGuidance.png` | 778200 | 2026-11-03T05:26:17.1043409Z | `C4FC902C35E07C454329AB871D200E30325D389EC7B6AEF512CAED0A63408590` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_02_ManagerReadable.png` | 643850 | 2026-11-03T05:26:14.4979877Z | `7E569405D327C5E08F21367FB3FC069194D7DCDEAA717D4B40D65854E3004F15` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_02_StableDescription.png` | 612875 | 2026-11-03T05:26:12.7095416Z | `C5CDE5C47698E8708EE6AFF08D4242A5E05E6FD4EC322E74ADB5E3D07AF8DFA4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_03_ManagerAtBell.png` | 627202 | 2026-11-03T05:26:17.3396776Z | `6236C64D837730E8B6AFB6BA1F43A970CAB2A86CFF5452A4B833CB7570D4C9AF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_03_PanoramaInScene.png` | 699459 | 2026-11-03T05:26:12.8219248Z | `344960FBFFB3C98ECF63E653F96DB135DE0C5A6C19C4E139B9358E7B2128E167` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_04_DescriptionOnly.png` | 578732 | 2026-11-03T05:26:17.5502977Z | `6EC74BBC817D3A9457BF84D5CE8B19C95FD8E3F75817BC3F2E7C5930EAE8CB43` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_05_CatchStart.png` | 638936 | 2026-11-03T05:26:17.6900634Z | `2792A340C5E1228E1F9A957D54C26FC94C5A530F6C6357B6250A559E93CBD901` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_06_CatchFade.png` | 720986 | 2026-11-03T05:26:18.3114259Z | `BED169FEC79EAC1C00B29A17E4FE88128AD396B3CC1CA1E7A8C5A3F6102FE63B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_07_CatchFinished.png` | 713877 | 2026-11-03T05:26:19.1265729Z | `7EB1CC6C50B97628CFA02463DB3A8947114ADC16B0F258CAAD0B31334C910E91` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_FadeSkyboxVerification.json` | 4989 | 2026-11-03T05:26:12.8629414Z | `A6E5D2EB751B281AD1230B9FFDA2B74D84479082D060045AC3DA0C56DCC6894F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Elevator_Safety_Prototype_Verification.json` | 6117 | 2026-11-03T05:26:19.7579783Z | `590463080D9327226E1ED3C4D661BE4FC582CC7948EFD6B3E4EA73E8FA24F176` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorScenarioDirector.context.txt` | 10668 | 2026-11-03T05:00:58.0691464Z | `848DE1800D89417B18014B78F1BD0D51CE1E6ADCC1A4F639AC4600135C131E56` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorSpatialUI.context.txt` | 31649 | 2026-11-03T05:05:38.1445948Z | `B975BAC36F75C03DBA0C00608DFB34F0962705FCED4749F1110EB7EDC01D4148` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorSpatialUI.final.txt` | 31814 | 2026-11-03T05:23:29.5853066Z | `3BECAEBFA8338EE31A29C6D23B1FC1987C4A04F4EF84213AB350CF952423A839` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorStorybookUI.context.txt` | 15959 | 2026-11-03T05:00:58.0686336Z | `945C2297663D1B574EBC374D76EAE2D3AAAA1256CAE5F72BFE563D4A5FA68B2C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorStorybookUI.final.txt` | 14572 | 2026-11-03T05:23:29.5853066Z | `5A8A45633E4E2EE8A72D36BE6114437901D425DD411525B8216BDBAEF46C071A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorThoughtHeaderPolicy.context.txt` | 1349 | 2026-11-03T05:23:29.5843069Z | `BB2298E0350F590C538F56EA6D76FC3B9A2005EF99952107435A178FC8E07341` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorThoughtHeaderPolicy.final.txt` | 1349 | 2026-11-03T05:23:29.5843069Z | `BB2298E0350F590C538F56EA6D76FC3B9A2005EF99952107435A178FC8E07341` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorUITransitions.context.txt` | 10023 | 2026-11-03T05:02:14.6796141Z | `178576C28353E9C6FF6D16F159195DBEAE95087CBC86A04D4A4427FCBDF22473` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorUITransitions.final.txt` | 10715 | 2026-11-03T05:23:29.5863035Z | `1D0DC55723B20734162DFD78F3202921F4958711C86E11EA2B49BECE709A0D2E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ElevatorView.context.txt` | 23083 | 2026-11-03T04:23:09.0213478Z | `E9C647E08918C029B21432F920FE97DBDE88B156322AB7CCED3EBFAE01108B99` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Fade.json` | 6465 | 2026-11-03T05:26:13.4218438Z | `A705200DC6C32BCED8F3751A24B9140B67F856CC19E9251B656A528B607917CF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\FadeRegression.cs` | 9724 | 2026-11-03T05:24:00.0811528Z | `FC23732B6DB2CD98B169271E77DCFA7D69E4F6F9E367D8305F14BC46647EDF73` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\FinalSummary.txt` | 239 | 2026-11-03T05:26:42.8927311Z | `B6C385D057619EC70D8A7F1F58859AEA3C55DF0744D6D6182C4F67F542522617` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\FinalVerification.txt` | 1586 | 2026-11-03T05:28:28.6418693Z | `E061900C49D6D642F9EDAB6F934C93ED2B886A8A2A172746F275489D199B417F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Header.json` | 27252 | 2026-11-03T05:26:11.9823478Z | `B1D62922C3B0079EDCC0E4CD4B969B1F316843D57782D11DCE43E7FFFE46521E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Inspect.cs` | 2623 | 2026-11-03T05:20:43.7023857Z | `9091D7A33C983A04C1982EB4F772B845EC645675B3279B5F158AB65708E2AB6D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\InspectionAfterTests.json` | 2781 | 2026-11-03T05:26:40.6658921Z | `9293E580DA640A58E643A1946F843F9B6801E781A5A4F6576ECAB027D3C21ED2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\InspectionAtTestStart.json` | 2782 | 2026-11-03T05:25:56.8548912Z | `F3AD10D0193ED586A6445AE0FAF843EE71921BCF5B102E685C8C2A87F1A62142` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\InspectionEnvelope.json` | 2875 | 2026-11-03T05:20:59.7556081Z | `9283BF6AB1789CA8901219B929B575B756685E56AF9ED13A19EA1D931083C3EF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Interaction.json` | 4830 | 2026-11-03T05:26:37.3723460Z | `A0BC7A54DC32846FDAD73E4D6D1FF6B97A75595DE88735A3FD11642C53906B12` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\PlayCurrent.json` | 316 | 2026-11-03T05:25:57.5342386Z | `44A6AA59987374434A9F16E837E1CE97C6ABE8256B0DE2ACF5F76840A49E705C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Polish.json` | 7858 | 2026-11-03T05:26:20.4162079Z | `DCDD9A1FA0BC0B3C849547FBAED626A0B3B9D6BA223541213601A4D5916E406B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\PolishRegression.cs` | 15614 | 2026-11-03T05:24:47.1741006Z | `630A629C43BE06139F80C68DC77E4D4DF521C3DBDCC33426747B887DAA13F90E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Progress.txt` | 882 | 2026-11-03T05:26:43.0827415Z | `8CD41D230ED7AF6BF3B4EE2A4D4346810F9A9605227E2D78895AEFA014DAC372` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ProtectedHashesBefore.json` | 1432 | 2026-11-03T05:20:20.5884184Z | `CAA840A9FEEC697A532CAC7E531EF3C9849A6051B583816D66926181F2985F50` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Readability.json` | 37022 | 2026-11-03T05:26:15.2570542Z | `8DB3C006976B91CDEA41325826618EA09EC0AE17E7B738260C71DCB909B0A5D2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReadabilityRegression.cs` | 13059 | 2026-11-03T05:24:00.0785627Z | `687C573C77FC6C4939BC1079D44BCACDC16714C38402A528BD976CDB547247B2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\README.md` | 4825 | 2026-11-03T05:28:05.4208544Z | `2768BF9AF4B921F1E9073C8AF6D11E154F6B3CEB07999856063793E9FF7C8B30` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReadOnlyInspection.json` | 1847 | 2026-11-03T05:26:39.9927015Z | `0C6192629016C78D0166846C423118F1C5D0E2223462FCB19D97A5891871AA8A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReadOnlyInspectionBefore.json` | 1847 | 2026-11-03T05:20:59.7182630Z | `0C6192629016C78D0166846C423118F1C5D0E2223462FCB19D97A5891871AA8A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Reproduce.cs` | 1961 | 2026-11-03T05:22:04.6127917Z | `099287BAA2FD2F4F302A2CCF2D6C6DE896389615EEAC070FFBC757F0A4F0F2B7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReproducedBefore.json` | 270 | 2026-11-03T05:22:33.0769782Z | `6366C08685DEDE0A163840321D79441FE88B9183127F2C3EED128BC5D405A9B4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReproduceDone.txt` | 10 | 2026-11-03T05:22:35.1548253Z | `43F57FFB991888352F5131634217AF283D4279618AFE30963D7A7C97AD80E9E2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReproduceError.txt` | 0 | 2026-11-03T05:22:18.8318715Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReproduceOutput.txt` | 0 | 2026-11-03T05:22:18.8308676Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReproducePid.txt` | 10 | 2026-11-03T05:22:18.8850712Z | `F0EFE92718B1211E8C419A37AAE38A40555CF7CDE08E5D31E3CC76487126290A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ReproduceStatus.json` | 569 | 2026-11-03T05:22:28.9061218Z | `FC193DD8E6E857D9C6B295E0DEB08F2E7849AFC13B13721ED1F67AAE3DBE14AE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Reproduction.json` | 932 | 2026-11-03T05:22:33.7054366Z | `FE61D0B81458978335A2B90730EF4F60C6BC94980A5F959F6133ED4AFD570089` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\RunCurrent.ps1` | 2881 | 2026-11-03T05:25:31.7445687Z | `DBE3D7696B0A084200560D78F7643156B501B8346EE6175A4E9EDBC3F5DC0CD2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\RunnerDone.txt` | 10 | 2026-11-03T05:26:42.8937777Z | `43F57FFB991888352F5131634217AF283D4279618AFE30963D7A7C97AD80E9E2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\RunnerError.txt` | 0 | 2026-11-03T05:25:51.6596769Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\RunnerOutput.txt` | 0 | 2026-11-03T05:25:51.6565640Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\RunnerPid.txt` | 10 | 2026-11-03T05:25:51.7004122Z | `472173FACFAF6119093DFED733A6F37DBDE0A754C49235E7AE55DAD4987A8425` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\RunnerStatus.json` | 567 | 2026-11-03T05:26:39.7643686Z | `CD9F911280A24C79842466486FBE064DD6DC865ADB71A9BBAE585682AB20BCB4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\RunReproduce.ps1` | 1416 | 2026-11-03T05:22:04.6127917Z | `46B8EB8C40297A1204D2653DA8AB0219B7E9947938DDC7F7B9687A75664DE971` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\StopCurrent.json` | 315 | 2026-11-03T05:26:38.2603099Z | `E3B030AEEBC8B3DA71F9E04D2B0A4DE68F9A5CBEC759CA9CEEB931415373ACD1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\ThoughtHeaderVerification.json` | 22581 | 2026-11-03T05:26:11.2562681Z | `40AEA99FAE602FC0389FEE55A1676CC631C071EACBFC5BE360D3D3A7B508F4AF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\TitlePaths.txt` | 4114 | 2026-11-03T05:20:20.6206442Z | `30A4BE67E0EA9A2749B9B7C6698D9B38170EC7DB51A909C9DDF81B0ADA8CE132` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\TitleRegressionExpectations.txt` | 888 | 2026-11-03T05:21:15.5090600Z | `D57A59BE8C4C1460069C6A698925EA0A8A15670833C1C39AE3E85C7711865134` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\ThoughtHeaderFix_20261103~\Verify.cs` | 11328 | 2026-11-03T05:24:47.1751011Z | `394E74DD520DC56EB66CBBFEBE0142A49B76565549A1EB8F619168CB4B3B87BD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\AppliedSkybox.json` | 1040 | 2026-11-03T05:06:31.4280332Z | `8C254168FCC7D40D1060F5977BC4EF4D88EC4F1BB3026E5104BC2A8C6118665F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ApplySkybox.cs` | 3426 | 2026-11-03T05:05:38.1459825Z | `82FC38A29BD714A3A0A26EEFD214A331B0F848CB83186118359F3ACD9A88035B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\AutoAssetReview.txt` | 138 | 2026-11-03T05:06:04.7182576Z | `FD51898FCBF2C1E9337B59FA200115A89B1802A5CB8D8D0E1A860CE7ED0F54E9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Baseline.txt` | 8696 | 2026-11-03T04:56:19.9461074Z | `B8B8E8B30C4DC139BC45152655C3BDFE64688FDDFF4B013A46E3DEB283A4BB0D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\Elevator_Safety_ObjectInteraction_Test.unity` | 1399109 | 2026-11-03T02:51:34.3190883Z | `B413F293C524DA0E0694D3E23A046ADBA2DB65926BEC1A12662A9B395B38D781` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\Elevator_Safety_Prototype.unity` | 1395179 | 2026-11-03T02:51:33.6336402Z | `FD03C722FA7FCBC8B1601CBFAA9E9BA7D57042DDE42E0B005314DFCC14AE25A9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\ElevatorSmoothButtonFeedback.cs` | 2012 | 2026-11-03T04:31:01.3668586Z | `21CAEFCFA709738585A2A76E6BE422CBC1BAB7B63AD5D8D02A9E81418947998A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\ElevatorSpatialUI.cs` | 30064 | 2026-11-03T04:28:30.0315474Z | `21FFB38C88C22369000532DA2E340994A096FFAAD91FC897A5A382613C29D1D2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\ElevatorStorybookUI.cs` | 15530 | 2026-11-03T04:22:47.5629101Z | `D24D3BADC75E2373B128D9915E368ED67C57DFF7EC8215F5C11E768F13B3FF83` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\ElevatorUITransitions.cs` | 13754 | 2026-11-03T04:35:23.6829738Z | `E006AB4556B7DE6A22AE442F049AB226C4051B276A20BB36E72EAB1497B3B80B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\ElevatorView.cs` | 23083 | 2026-11-03T04:23:09.0213478Z | `E9C647E08918C029B21432F920FE97DBDE88B156322AB7CCED3EBFAE01108B99` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\GeneratedMFontCache.asset` | 2153611 | 2026-11-03T05:04:22.0760843Z | `22A50EA207C3EFF025DB698E1DABC8D6DF07E3AFA89393D169E10EC18C2D530F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Before\GeneratedSharedFontCache.asset` | 2141721 | 2026-11-03T05:04:23.0105586Z | `CF6CE6A850D79C48A2FC1B6DFC1EBEA5A2F35D38C3EE1C3750016F6E2679D5A2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\CacheReview.txt` | 2099841 | 2026-11-03T05:10:30.7335266Z | `3C5BB087FAC51621477984BC4672F7952885CCFEB547ED4C02EE2EDC3BBD56C9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\CaptureSkybox.cs` | 2132 | 2026-11-03T05:10:00.2124058Z | `31504772F2F400FB8F44328749C2DB0F28C1A3F5E4BF4AECE9912A0AF7B15C7B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\CleanupReview.txt` | 13639 | 2026-11-03T05:11:05.0098409Z | `9C4B6255875E393F21457560523158EA092AFDD37AE6C7FAE7210B271CEBCD97` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\CompilationFinal.json` | 419 | 2026-11-03T05:08:18.3635178Z | `0AA18ECB77F774630293DBD8E75D9798C7DAC42185B44FFDFEB6688D49552DA3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\CompilationStatus.json` | 441 | 2026-11-03T05:04:15.4354051Z | `34B5C5A538A835003686BDF3224A242E13838E5F55364F68EEDF76C1CB9F0B74` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Compile.json` | 429 | 2026-11-03T05:06:03.9114085Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\CompletionCheck.txt` | 3262 | 2026-11-03T05:12:14.2877454Z | `138323B9E5F86C2BF60EB0BBE03DD741AE50CD0EED524899F1E7A86FE2B369B5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ConsoleAfterCapture.json` | 909 | 2026-11-03T05:11:05.0619614Z | `7C6B1E37B51460800BF37B6B8FA00ED2D8D6B0042035A0B2ED004AC2E2874FCA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ConsoleFinal.json` | 869 | 2026-11-03T05:08:19.0159170Z | `AD24385B58CE0F1E408CB22DCD0EED6F047F3828AF36DA08D2EA7C616C763842` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\EditorBefore.json` | 591 | 2026-11-03T04:56:20.1390961Z | `848149AD8E538E6DB56BB6BA1C6AFB4E4901DB4F7112D08EC71624A70881322D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\EditorCompletion.json` | 591 | 2026-11-03T05:13:34.9995832Z | `735C922F1C88A40B910BA99907851EF83831E442487504D99482D05DEF24BEFA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\EditorFinal.json` | 567 | 2026-11-03T05:08:19.7241968Z | `D8037EA1455150DB71EEA8860353E3B112723A58C62F44BF9B378A530B65C38E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_01_LobbyReadable.png` | 707918 | 2026-11-03T05:07:49.3247741Z | `C3E574CB8507C374F7F7251C8EB7944A4B5F87B47474746E26CE51797008169F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_01_OpacityFade.png` | 574976 | 2026-11-03T05:07:47.7640713Z | `284341E30E15A87B98885E31804F260B782B4F33FBE034EAFE42D5B60CDD54F8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_01_PocketCloseup.png` | 509778 | 2026-11-03T05:07:51.4812498Z | `DEB16F2795756CC6A30B3C672C697F09DC94A3872E231FBA047E370F986DE1C5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_02_IntercomGuidance.png` | 772388 | 2026-11-03T05:07:52.0546391Z | `2810F78145337C93EE27C027275B2A8B19E02EA3BAFC063622AD2BF484CB7627` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_02_ManagerReadable.png` | 643525 | 2026-11-03T05:07:49.5939958Z | `3CBB4907BB4C9C27FD2EAF86A22BE3E29F55A767B2C532AB0F9D1FB480F84C85` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_02_StableDescription.png` | 588151 | 2026-11-03T05:07:47.8757224Z | `DF204F1B0ECFCBE84B74525BAE10E74644E3BAD2E59AA0BE216F6A1C3B8BC445` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_03_ManagerAtBell.png` | 660344 | 2026-11-03T05:07:52.3140836Z | `B167A6F1544C1BCC1FEA5D04BBC339800E424CFBB3BF973ADB9B843F4E4FFEB8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_03_PanoramaInScene.png` | 699495 | 2026-11-03T05:07:47.9895162Z | `CDBF9B7A06F085A67F6E41C60E099FE6E788EBE520C863AF3D17626518E83928` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_04_DescriptionOnly.png` | 600822 | 2026-11-03T05:07:52.4312196Z | `87026DB9F6209B282A65B4AC95CDD09B1F8F2C375CCA3104247180E58FC9EA08` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_05_CatchStart.png` | 520001 | 2026-11-03T05:07:52.5693032Z | `D578C02F578AA39A4F5E1120262F83CD21073BE49E2625E7EC9596242E44F8CD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_06_CatchFade.png` | 494013 | 2026-11-03T05:07:53.1356563Z | `5AE91A82E59E3F0FE6FE100B544257EA1765355238083873E40C59A9984EC2D1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_07_CatchFinished.png` | 491414 | 2026-11-03T05:07:53.9867013Z | `B69ACB0CB4F137D9605C0278BA350C687B577B0E2CA154DD74A3C93CEC58145D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_FadeSkyboxVerification.json` | 5002 | 2026-11-03T05:07:48.0372552Z | `AC1CA813AFE209F06B6FD744BCB0275C4BE83B08257FC96CAD5619B84B9E2C82` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_ObjectInteraction_Test_Verification.json` | 6126 | 2026-11-03T05:07:54.6071419Z | `C7466EEB885F9019A76FBFBBD965A6EC2C2370B91F8AB5375C2AAAE0FEC00372` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_01_LobbyReadable.png` | 696690 | 2026-11-03T05:07:05.2526385Z | `1A6E193859283A1CB5DDCB3A2C6D5CC1D801CC7D4A465F73D7BC7DE114287CD4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_01_OpacityFade.png` | 586411 | 2026-11-03T05:07:02.8577287Z | `C09E4F46D50E086C1ED0113003E773E69746FFFA849601BBF0EF3045063E2C09` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_01_PocketCloseup.png` | 602690 | 2026-11-03T05:07:07.6558179Z | `FE120D908641DEA90C684EAEE7F817DDCA95D5B40189F884B1F29B5C4A70B21E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_02_IntercomGuidance.png` | 778525 | 2026-11-03T05:07:08.2457000Z | `F8A10320E8A197B0F261737B550F2C951DAF4B93ED8DB3937AE66DCA81FB37B7` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_02_ManagerReadable.png` | 644136 | 2026-11-03T05:07:05.5226782Z | `32694795C04E48994BBEE3D8F71D5AD687385F1E9A26E80678DB5663CB953B58` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_02_StableDescription.png` | 612905 | 2026-11-03T05:07:03.2201329Z | `2F83A86FB9642CDA655F731625BBD2ABB50A5134DE5774FB324D95EB15F5C9A0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_03_ManagerAtBell.png` | 660344 | 2026-11-03T05:07:08.4860076Z | `C58E147E528676557A384D39DDABBB7F1E8504D6F9768C3D2A2C700D3358EFD9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_03_PanoramaInScene.png` | 699389 | 2026-11-03T05:07:03.5700359Z | `C7F691C8B5FBDB1A16EAD19F0EA3791FA15ED1E87E7DF024CF3C224EDA01E02F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_04_DescriptionOnly.png` | 618800 | 2026-11-03T05:07:08.6454582Z | `17DC4C975F066711546C48030621723642A259262BE65C210CB19F7B57D032B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_05_CatchStart.png` | 634533 | 2026-11-03T05:07:08.7536327Z | `C2C11DF834387E553B55F4B6A53C5CCC35501F6CA5D8CBF57D6CEFEB15165D5B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_06_CatchFade.png` | 720977 | 2026-11-03T05:07:09.3492155Z | `8ABC4845245B95E8910898E59B283A3C5C28B4DC5F6AE56B9E82A306BF2B8F30` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_07_CatchFinished.png` | 713869 | 2026-11-03T05:07:10.2048613Z | `5545DE0512D7C5C94DC77AAA53985AE2921F2A84C4C3624308DB0676B6757383` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_FadeSkyboxVerification.json` | 4989 | 2026-11-03T05:07:03.6083070Z | `A6E5D2EB751B281AD1230B9FFDA2B74D84479082D060045AC3DA0C56DCC6894F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Elevator_Safety_Prototype_Verification.json` | 6113 | 2026-11-03T05:07:10.8276687Z | `AE97F493AC3572CF2A673434FE5362D671257A325149AA89F9EBF4B9E2CE897B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorScenarioDirector.context.txt` | 10668 | 2026-11-03T05:00:58.0691464Z | `848DE1800D89417B18014B78F1BD0D51CE1E6ADCC1A4F639AC4600135C131E56` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorScenarioDirector.final.txt` | 10668 | 2026-11-03T05:00:58.0691464Z | `848DE1800D89417B18014B78F1BD0D51CE1E6ADCC1A4F639AC4600135C131E56` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorSmoothButtonFeedback.context.txt` | 2012 | 2026-11-03T04:31:01.3668586Z | `21CAEFCFA709738585A2A76E6BE422CBC1BAB7B63AD5D8D02A9E81418947998A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorSmoothButtonFeedback.final.txt` | 734 | 2026-11-03T05:02:41.5289316Z | `9115A9685CA69422ADF0CB0FAF4C3F637C517A30B5F29C049A931D168F027586` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorSpatialUI.context.txt` | 30064 | 2026-11-03T04:28:30.0315474Z | `21FFB38C88C22369000532DA2E340994A096FFAAD91FC897A5A382613C29D1D2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorSpatialUI.final.txt` | 31649 | 2026-11-03T05:05:38.1445948Z | `B975BAC36F75C03DBA0C00608DFB34F0962705FCED4749F1110EB7EDC01D4148` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorStorybookUI.context.txt` | 15530 | 2026-11-03T04:22:47.5629101Z | `D24D3BADC75E2373B128D9915E368ED67C57DFF7EC8215F5C11E768F13B3FF83` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorStorybookUI.final.txt` | 15959 | 2026-11-03T05:00:58.0686336Z | `945C2297663D1B574EBC374D76EAE2D3AAAA1256CAE5F72BFE563D4A5FA68B2C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorUITransitions.context.txt` | 13754 | 2026-11-03T04:35:23.6829738Z | `E006AB4556B7DE6A22AE442F049AB226C4051B276A20BB36E72EAB1497B3B80B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorUITransitions.final.txt` | 10023 | 2026-11-03T05:02:14.6796141Z | `178576C28353E9C6FF6D16F159195DBEAE95087CBC86A04D4A4427FCBDF22473` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ElevatorView.context.txt` | 23083 | 2026-11-03T04:23:09.0213478Z | `E9C647E08918C029B21432F920FE97DBDE88B156322AB7CCED3EBFAE01108B99` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\FinalCleanup.txt` | 706 | 2026-11-03T05:13:04.9779145Z | `133B07B74096D9D78C0FDCB62485019B8437D96345554385E6E7C491A473322A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\FinalCodeCheck.txt` | 2630 | 2026-11-03T05:13:34.8435956Z | `565C7D40B2E26F88AA7F75C42E8768F82609F27EFE99438DB39483C392C92BCE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\FinalState.txt` | 4346 | 2026-11-03T05:09:23.9243847Z | `A65C86172857C99D0FA377223AB0F71649EFD403C7DBFFE339FF59BA807A07C8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\FinalSummary.txt` | 458 | 2026-11-03T05:08:19.9564389Z | `531D32B543CB164C6E4D9BFB1A0A326D778DEDC6AF4B157C6084224ED37F5FBD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\FocusedContext.txt` | 6266 | 2026-11-03T04:57:30.4408397Z | `B280B768EBC0CC45947245CBA4F8B2A427223DA85BF1CBF59EE559C74FA96389` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Inspect.cs` | 2259 | 2026-11-03T04:58:50.3009809Z | `59380B99778553E2D38718018D067B66E3D1F1DB0EA208EB6B3E7C6DE9140FF1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\InspectEnvelope.json` | 3243 | 2026-11-03T04:59:07.3676538Z | `1F03D788BA144C360167B492D7522C8711C216F81F49FF9F30F403C918702B80` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Inspection.json` | 2037 | 2026-11-03T04:59:07.3375176Z | `435226FC469DEA591089EA68C6485D391A1C9768CEE06FF8AAAEC201EC759A42` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\LocalSkyboxShaders.txt` | 0 | 2026-11-03T05:01:20.1762889Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PolishRegression.cs` | 15604 | 2026-11-03T05:03:00.4260713Z | `0915F84CFB3E50B9C5849A77E77052F39BA457C94EA859F7ADA79299A6769D61` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Progress.txt` | 1720 | 2026-11-03T05:08:19.8861263Z | `84BEDEF7A264AEC71945CA3D08882FDBD549078A4D75BB7311780BB80A28D8C4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PrototypeCamera.json` | 16638 | 2026-11-03T05:07:24.5871799Z | `35CD6742CB671871863C7DFF2F3DC8CA3A19FED1F82B17405BC25C14D4B3DFEB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PrototypeEnvelope.json` | 6446 | 2026-11-03T05:07:04.1822419Z | `9365E09A0FCCD7B5867169641C6B7FC8232C94F30B1127705F4829FC4AEEA654` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PrototypeInteraction.json` | 4830 | 2026-11-03T05:07:29.0555925Z | `DDB65FE6FCA7997CFF59C35A5A330A75E5C22C49C6831867517F736A211A1E73` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PrototypeOpen.json` | 703 | 2026-11-03T05:06:47.9878129Z | `89541A9D916A644C8E2AF1DA3AD0FB4614DA47F6D809E6329FA70299410D9566` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PrototypePlay.json` | 316 | 2026-11-03T05:06:48.7321820Z | `44A6AA59987374434A9F16E837E1CE97C6ABE8256B0DE2ACF5F76840A49E705C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PrototypePolish.json` | 7850 | 2026-11-03T05:07:11.3980334Z | `EA29E76DE20B606C127F0229EC07A32E34ED7447E456D00A25E95EE1DC406560` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\PrototypeReadability.json` | 37019 | 2026-11-03T05:07:06.1983885Z | `14EEF55A478A2F3F120E4BE7F13E4E9AF7D4CAB6BC96EFFEEA5633A70C44112C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ReadabilityRegression.cs` | 13055 | 2026-11-03T05:03:45.2706294Z | `8F119418479011B64E01204D3D9CBDC87AD49467BD8D3DE89501601B30850FBC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\README.md` | 3970 | 2026-11-03T05:09:00.5790677Z | `14BCF271B3179ACB99DC844F764FFC32BDBE622450B6D908A2E8CE2E25ADF689` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RecentFiles.txt` | 1009 | 2026-11-03T05:07:15.1665620Z | `33054D02798C1C511E14127149C2792C2353C8FBE73DB70478A8E190E0E8B5F4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RestorePrototype.json` | 703 | 2026-11-03T05:08:15.7767561Z | `B9194D41C3591957EE00EC1E3C5B9CF11CBA6314EB7B0530EEAD0E8D7095B2D9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RunFinal.ps1` | 4058 | 2026-11-03T05:03:45.2706294Z | `360C37F0516FF3F76E5D8FB81DA87BC1CBC45DE4600B276C9FF472AE35B1C50D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RunnerDone.txt` | 10 | 2026-11-03T05:08:19.9579459Z | `43F57FFB991888352F5131634217AF283D4279618AFE30963D7A7C97AD80E9E2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RunnerError.txt` | 0 | 2026-11-03T05:06:42.9451907Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RunnerOutput.txt` | 0 | 2026-11-03T05:06:42.9436847Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RunnerPid.txt` | 9 | 2026-11-03T05:06:42.9859095Z | `AFFB709DF1BEB8D47ED0EF2E50FF0BD60E64CD8EBF93C78B17E1D59507E49171` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RunnerStatus.json` | 567 | 2026-11-03T05:08:14.4794461Z | `F4F0F295422CFA50FB89309153CD46F361DE7D610E1758628B3E5517C4BB6C27` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\RunnerStop.json` | 315 | 2026-11-03T05:08:12.9259165Z | `E3B030AEEBC8B3DA71F9E04D2B0A4DE68F9A5CBEC759CA9CEEB931415373ACD1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\SavedValidation.json` | 783 | 2026-11-03T05:08:17.7212914Z | `0920A615635EF2A2530F4A0E20BAB77FFAFC1342A6EBD23C4C51EEF1E94C7246` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\SavedValidationFinal.json` | 813 | 2026-11-03T05:12:15.6809962Z | `C4BCC0D44C72B324C5A2ABC4B65692EBBC2C1B5B57949A24577761CA02AE7BBA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\SceneChanges.txt` | 17702 | 2026-11-03T05:04:58.4682045Z | `C2EF33B6CC5A645A7219AC413BB78239ED989FE6EA392287C0A152A1AFFF3500` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\SharedFontCacheReview.txt` | 458 | 2026-11-03T05:13:04.7332405Z | `F55AB042570060C18D0FFC751B274B089C0B72BDD0603ABB31601D8E2D6FB564` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Skybox_0.png` | 350650 | 2026-11-03T05:10:29.3164608Z | `CC58C31C36B4DEB6357BB3EF2C329E180E77539DB128A980B9D927D1A683CB29` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Skybox_180.png` | 349633 | 2026-11-03T05:10:29.3766517Z | `E99055A4AE2B5AA0A165FFCF221ADB7B5127E0CBF99AF268C92FAEF85A17CA02` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Skybox_270.png` | 361009 | 2026-11-03T05:10:29.4092853Z | `9EC4618EBF21F134CADC01BB9C3530208204B20879CBDFEEB5FD985CA91FE7E3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Skybox_90.png` | 298118 | 2026-11-03T05:10:29.3451475Z | `80D95D5C1A1F43DABD6AC7E874BD4FA1A19E8A268575844E5C5837EB76792393` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Skybox_ThroughWindow.png` | 158482 | 2026-11-03T05:10:29.4351595Z | `8F8F05C5FD303D8DB511748B5BBF86EB129845610170FC9FE6825AB6C85930CF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\SkyboxRender.json` | 885 | 2026-11-03T05:10:29.8510118Z | `55329B951E48804E2ED64A2B60DE3711A4257672727DD7327FCAFDEA212E8727` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\SkyGeometry.txt` | 7398 | 2026-11-03T04:59:08.1203501Z | `E1B99E8EB2EB1545F85E187960064E349C153D1DCC775EABD1E8FB278C5319D4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\StopForCompile.json` | 331 | 2026-11-03T05:06:02.4119893Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\TestCamera.json` | 16651 | 2026-11-03T05:08:07.6539179Z | `CFC7AED5DC2748C7F0745A216AA236B664EECC0B0C5EC4DEF18A13C8358EBB11` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\TestEnvelope.json` | 6459 | 2026-11-03T05:07:48.6308454Z | `589AF3C86F79142AF21AE15228E70E947C7CFB238C45B276EE41DDEA171B380E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\TestInteraction.json` | 4844 | 2026-11-03T05:08:12.1209477Z | `F4F9434F85475FDD5B025C328912472EED9DA84826519DAAE29E219006B2BAD6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\TestOpen.json` | 713 | 2026-11-03T05:07:33.0347657Z | `40A04D65008873F0B38C53C7227FF10D3332309F0AC37E215CE469BEC53AD57F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\TestPlay.json` | 316 | 2026-11-03T05:07:33.9342954Z | `44A6AA59987374434A9F16E837E1CE97C6ABE8256B0DE2ACF5F76840A49E705C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\TestPolish.json` | 7863 | 2026-11-03T05:07:55.1802415Z | `EFCDE00B5714667EBA4F4C8424A22D573092C85A3D4D473FA2C0A84F5399C498` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\TestReadability.json` | 35211 | 2026-11-03T05:07:50.2505481Z | `AF840B8C657F7B1374AFF07F3F18A59362CA1FEF63B9B986AF388986DA572649` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\ValidateSaved.cs` | 1846 | 2026-11-03T05:04:01.3697310Z | `8292FE551E6599E0427BAFDB1DBF30C2C4FDB57DC34C06049CA1DAABBD5092F1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UIFadeSkybox_20261103~\Verify.cs` | 9720 | 2026-11-03T05:03:27.5933690Z | `AEEA2A332F439D43D7A5C56F62EFCF05A3A93F0F32775028E92BAE1D0DF527CB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Before\ElevatorDesktopControls.cs` | 13887 | 2026-11-03T02:39:27.8356872Z | `FCCA293FB0C71605639A11E9E795A7D8D9F95314136C5FE724D87D9ADAD60EEB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Before\ElevatorFlow.cs` | 12496 | 2026-10-30T05:54:06.6220687Z | `2561085596D72A06C96D8BBF66ED665A9B2CB67CA120465BF998EB728EEFBE8B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Before\ElevatorScenarioDirector.cs` | 10615 | 2026-11-03T03:37:57.3118554Z | `5C51851CD023528C7E702EE2F4B79D91267455E69C8A3F724EFBE0A6359AEA51` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Before\ElevatorSpatialUI.cs` | 28569 | 2026-11-03T03:49:02.8071716Z | `958A55C6D2FD2C2DF5B553710775C3612E59259765ACDCF49FD84AFE837F5459` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Before\ElevatorStorybookUI.cs` | 15374 | 2026-11-03T03:42:58.2172926Z | `443FFCDD2A0A113B84D3DF2DD77EAC2FCA71579AE7EFEBF43576D5FC144ACFEC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Before\ElevatorView.cs` | 22140 | 2026-11-03T03:48:36.9744744Z | `592C75B2A51C708DCCD62FA45CCCE44F942D04D369EAE520289E975126C83965` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Capture.cs` | 2504 | 2026-11-03T04:34:26.9180453Z | `C1212409A54ED4A35975FD491EEE86D557C530C01C88997D7B6C9543E05320DC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\CaptureEnvelope.json` | 1615 | 2026-11-03T04:36:30.2109214Z | `5CED583B11C4B2FC97BF7337C7E6EB1616D8032F2C585BC777548C31588301D8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Changes.txt` | 14054 | 2026-11-03T04:31:27.9805023Z | `AF79E9F1A7508542A45709741E8C6B167427D16709821E466CB20D196F07B4E5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\CodeFinalReview.txt` | 113 | 2026-11-03T04:35:53.2637714Z | `F22D5A1106297F135151465E7A8064DC9884241F1707A0E7DB1B3E0E126355A9` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\CompilationFinal.json` | 419 | 2026-11-03T04:45:39.2551016Z | `0AA18ECB77F774630293DBD8E75D9798C7DAC42185B44FFDFEB6688D49552DA3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\CompilationStatus.json` | 440 | 2026-11-03T04:36:05.6421462Z | `346C62BF056F2807A83EF242F92C72768FCA8130F0360897163FEF5B6337A5B8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Compile.json` | 429 | 2026-11-03T04:35:52.3995535Z | `93B92A62F9ACA68D47727A61608C482C102AFD0B493C8E401CA8D51394C6651A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\CompletionCheck.txt` | 1695 | 2026-11-03T04:46:09.7286278Z | `393B775E08E27994A490C24332F49AF352D32E7750BEEFF76A08338629CF43EE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\CompletionFiles.txt` | 285 | 2026-11-03T04:45:40.7185958Z | `92A2542D01D9C7E57DB17D9A41599A4084382A8D7E70DDAA793A3A06341A05AC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ConsoleFinal.json` | 867 | 2026-11-03T04:45:39.9601094Z | `2D31C1B2F6401B336C63AE67C3AD208C0E238ECE533D3C9C81B0381F70049905` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ConsoleFirst.json` | 908 | 2026-11-03T04:25:08.7041688Z | `108DB26E586D8E94BB8487BB6BFD168ADD5E4FE5907AB8E1BC89C60D101F6262` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\EditorBefore.json` | 591 | 2026-11-03T04:16:51.0705449Z | `AB7DA7913F6EFD3BEB010C85238C4003F6572C79E27472F4B9AD37E8EA33B7D4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\EditorConnectionCheck.txt` | 8470 | 2026-11-03T04:38:42.7445328Z | `97F50DD0F4ED868E99D9553EE7035C155E9242F1CC5914448938F96EAA622AA0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\EditorFinal.json` | 567 | 2026-11-03T04:45:40.5833125Z | `3EC931F7F312090ED8CFC9930BE9E5B78999C6BA765D58CC512AF4D42C4D0B54` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_01_LobbyReadable.png` | 717052 | 2026-11-03T04:45:10.2096277Z | `EA24DF233E627B1E09C52D176B3546349BC0E7A288E774E857D037569AD4419B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_01_PocketCloseup.png` | 467306 | 2026-11-03T04:45:12.5097975Z | `6778E8E9626BA64D7AE9F87FD96EC3494FD508BF460E42F543ACEE23C474AD53` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_02_IntercomGuidance.png` | 739248 | 2026-11-03T04:45:13.0663101Z | `3AA651CD1A0F864393898C2015F49F90EF5D1BF13DC1661987FC6A7E783DE326` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_02_ManagerReadable.png` | 641512 | 2026-11-03T04:45:10.4631044Z | `AA059EC7840E248ECBA54EDEFB32C84F5B85BDAB728D8F078F7E524521E0AFED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_03_ManagerAtBell.png` | 688627 | 2026-11-03T04:45:13.3653445Z | `3BDC6F4503F50F15B58CC9B67E856D6A2A1EE84EC24758755EA317A805554D21` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_04_DescriptionOnly.png` | 661352 | 2026-11-03T04:45:13.4595441Z | `D1EBC8A7561195A07D8C4A4754722DB142A83AD986AF22018AC357EA7D7B1C3E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_05_CatchStart.png` | 582207 | 2026-11-03T04:45:13.5790792Z | `57B9E29B55C256AB8A4D9429292A062C7A05A5AF1AE04A33757B81E0F258FC4E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_06_CatchFade.png` | 494680 | 2026-11-03T04:45:14.1697645Z | `35CE44B61D74A027108A6529F076D9D652E94DD36A063201A8EEFD0ED20B0F2F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_07_CatchFinished.png` | 491503 | 2026-11-03T04:45:15.0168845Z | `DBC88B9DADF5C25F1BD84B332127C3C7EC1CC3F849057E609012FEB360A82938` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_SmoothVerification.json` | 4205 | 2026-11-03T04:45:08.7050303Z | `E925C8C7B5DE2ED5B8B48CB8384ECC56478D4E2BA8AC97B636DC508DB8527F35` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_ObjectInteraction_Test_Verification.json` | 6126 | 2026-11-03T04:45:15.6503964Z | `C7466EEB885F9019A76FBFBBD965A6EC2C2370B91F8AB5375C2AAAE0FEC00372` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_01_LobbyReadable.png` | 706432 | 2026-11-03T04:44:29.3884997Z | `236639B0DB96AA78438F19D15E37E79271045AA3C9BD0460D798B7B63E08319F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_01_PocketCloseup.png` | 546827 | 2026-11-03T04:44:31.7463182Z | `D7EED4A93335B5D8A0727638DCDC5C87B18E5EEBEAD976E9A42DEB53E39A8442` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_02_IntercomGuidance.png` | 722176 | 2026-11-03T04:44:32.3100573Z | `4A88666D6048CB63D14A2B3D7EDDD3F8DA0D2239D5FD74F956561F205D568A57` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_02_ManagerReadable.png` | 641373 | 2026-11-03T04:44:29.6676221Z | `87C52DC99900BFC80D9248CE3B6C97B9772819C5D99270FAB131B54DB3D88193` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_03_ManagerAtBell.png` | 682595 | 2026-11-03T04:44:32.5606696Z | `0C015D581753A61B0A6A3C18F3CA36CDAEF057B62B6DB01EB956215BA7FE6468` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_04_DescriptionOnly.png` | 671885 | 2026-11-03T04:44:32.6804873Z | `BF8108FA3706ECF94E6A8630F746B5129B447027F09843CEB6D27C7023BF5DCD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_05_CatchStart.png` | 727151 | 2026-11-03T04:44:32.8277379Z | `CFAB9AF2AB82438FCCA40AF5137570A118912E72E66A5990F29A6C26C69C24F6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_06_CatchFade.png` | 721243 | 2026-11-03T04:44:33.4286665Z | `9C0AE271E1E9FDA18FB6A0EE317B07BD1A3F04CB6807735DB1F48FB8D0D65DF0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_07_CatchFinished.png` | 714075 | 2026-11-03T04:44:34.2729398Z | `88BE29C52552DC49A36C072E92C45B48E9D00E5C18278167DA504FFD08D92357` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_SmoothVerification.json` | 4192 | 2026-11-03T04:44:27.9524881Z | `A97E3A8D6DD0A92B7206AEF327BF0E69617BFA127C7AFB2127BD5804B7B4CDED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Elevator_Safety_Prototype_Verification.json` | 6113 | 2026-11-03T04:44:34.8878960Z | `AE97F493AC3572CF2A673434FE5362D671257A325149AA89F9EBF4B9E2CE897B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorDesktopControls.context.txt` | 13887 | 2026-11-03T02:39:27.8356872Z | `FCCA293FB0C71605639A11E9E795A7D8D9F95314136C5FE724D87D9ADAD60EEB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorFlow.context.txt` | 12496 | 2026-10-30T05:54:06.6220687Z | `2561085596D72A06C96D8BBF66ED665A9B2CB67CA120465BF998EB728EEFBE8B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorScenarioDirector.context.txt` | 10615 | 2026-11-03T03:37:57.3118554Z | `5C51851CD023528C7E702EE2F4B79D91267455E69C8A3F724EFBE0A6359AEA51` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorSmoothButtonFeedback.final.txt` | 2012 | 2026-11-03T04:31:01.3668586Z | `21CAEFCFA709738585A2A76E6BE422CBC1BAB7B63AD5D8D02A9E81418947998A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorSpatialUI.context.txt` | 28569 | 2026-11-03T03:49:02.8071716Z | `958A55C6D2FD2C2DF5B553710775C3612E59259765ACDCF49FD84AFE837F5459` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorSpatialUI.final.txt` | 30064 | 2026-11-03T04:28:30.0315474Z | `21FFB38C88C22369000532DA2E340994A096FFAAD91FC897A5A382613C29D1D2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorStorybookUI.context.txt` | 15374 | 2026-11-03T03:42:58.2172926Z | `443FFCDD2A0A113B84D3DF2DD77EAC2FCA71579AE7EFEBF43576D5FC144ACFEC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorStorybookUI.final.txt` | 15530 | 2026-11-03T04:22:47.5629101Z | `D24D3BADC75E2373B128D9915E368ED67C57DFF7EC8215F5C11E768F13B3FF83` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorUITransitions.final.txt` | 13754 | 2026-11-03T04:35:23.6829738Z | `E006AB4556B7DE6A22AE442F049AB226C4051B276A20BB36E72EAB1497B3B80B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorView.context.txt` | 22140 | 2026-11-03T03:48:36.9744744Z | `592C75B2A51C708DCCD62FA45CCCE44F942D04D369EAE520289E975126C83965` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ElevatorView.final.txt` | 23083 | 2026-11-03T04:23:09.0213478Z | `E9C647E08918C029B21432F920FE97DBDE88B156322AB7CCED3EBFAE01108B99` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Failures.txt` | 925 | 2026-11-03T04:26:55.4489300Z | `8B12BBFC973606E777ED18ED79C212D1D85ABF7D7EC4E4B47EA89D3750D1E5C8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\FinalState.txt` | 3327 | 2026-11-03T04:41:43.7505876Z | `A9E47D12BC305EA8853DC0ADF08B85A5FFCEBF370F11D26DFD32C7680A88BB02` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\FinalSummary.txt` | 458 | 2026-11-03T04:45:40.6325172Z | `68AF2780AEE3D3AE27BBA048F776494A04D424D7060F81F150007A1FBEC42A12` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\GitBefore.txt` | 2430 | 2026-11-03T04:16:50.9864211Z | `0B2CB8A74DDE5648FCA327CC3140BA36CF117F71CD4EC1C10E9625515A4638CF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\NestedInstructions.txt` | 0 | 2026-11-03T04:16:50.9930849Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\OpenPrototype.json` | 729 | 2026-11-03T04:34:40.8728554Z | `851CC0FAABF2BBC2E2CA624E1D873F6A61C3E4AA53CC385013757DD3B2D0FCCF` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\OpenTest.json` | 741 | 2026-11-03T04:31:53.2880343Z | `F05118C61FEFC86C9BC48FF8544F5A2FF149D86E4B9F6F1EAC44054E08087273` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\OpenTestFinal.json` | 739 | 2026-11-03T04:39:41.6296835Z | `FECDA22F8C30E64CB998D3DF5A433B8777816D4F3A4818411B936213BFD661FD` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PlayPrototype.json` | 332 | 2026-11-03T04:36:06.4413101Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PlayTest.json` | 332 | 2026-11-03T04:31:53.9256552Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PlayTestFinal.json` | 332 | 2026-11-03T04:38:20.0072580Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PlayTestVerified.json` | 332 | 2026-11-03T04:39:59.1844737Z | `37B767439EAAFCC4CCFA27D16B7FC7D0C311159273518A269F2F7C55594DC796` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PolishFailure.txt` | 816 | 2026-11-03T04:28:01.3303730Z | `B776ECE4E805ED867170BA036711D7586B80387F977798935273DB51D5C85E3F` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PolishRegression.cs` | 15600 | 2026-11-03T04:28:58.7460626Z | `EB2799BA3665C3A19ED6AFAAA4E13D49629D90A7960AFF4B935474286724A052` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Preflight.txt` | 390 | 2026-11-03T04:23:34.4146252Z | `9BD47CCAC223B296739152BDC5A0C15052B9701F6F1F6C13598AFD41FC052BED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypeCamera.json` | 16638 | 2026-11-03T04:44:47.9932940Z | `CFCB6F7DFA405352B2A8A709D37ED0D05C7B59CECD101D8224853781A527F461` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypeEnvelope.json` | 5565 | 2026-11-03T04:44:28.5433202Z | `31CCE88A480AA0965EB432589E1C61DF8067227AD3FEBE4F4687A41EFF3DFD63` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypeInteraction.json` | 4830 | 2026-11-03T04:44:52.4125213Z | `A29DB3D729A3AEA28C0BAB42DFFA9332D01D0F13CC0E130F07D32CDCD707C8FA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypeOpen.json` | 703 | 2026-11-03T04:44:14.8395638Z | `5B9654FC10F11C7821C049577CF22374DF1CFCF897AC437BA7BCCEF58FB5E859` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypePlay.json` | 316 | 2026-11-03T04:44:15.5619817Z | `44A6AA59987374434A9F16E837E1CE97C6ABE8256B0DE2ACF5F76840A49E705C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypePolish.json` | 7846 | 2026-11-03T04:44:35.4600173Z | `5C405EBE1C019EC15C9DF00A05D383B3F1FC67A9D17D3D7731E4D26500EA483A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypeReadability.json` | 37014 | 2026-11-03T04:44:30.3653652Z | `647F73E6961B6B5087D42401BADA9A2BB6F03F3761598AB37B8265B7FDEF7782` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\PrototypeSummary.txt` | 217 | 2026-11-03T04:30:22.0516130Z | `A92B296F3497EB4B0AD3CBBC37FF08C63C3B4E108431EEF458B8E2EAC31252BC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ReadabilityRegression.cs` | 13051 | 2026-11-03T04:25:53.8893085Z | `6D6DA3D5EA0DE112983F7804393882EA6F2EB64CA87B99D45E052EAE368E73A3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\README.md` | 3903 | 2026-11-03T04:36:42.2935020Z | `76B15709DC85FB539114B8FD9D1A43CCA665D11388932F470B0A55D5B0EFB0AA` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RecentFiles.txt` | 1553 | 2026-11-03T04:45:09.5664862Z | `A445027C2E92F276A7106DB786CF840E9952BD02A35E6786FC9615A138AFB07B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\ReferenceReview.txt` | 7684 | 2026-11-03T04:33:21.3253981Z | `ECB4A2BF0EF3785D4A8B99D3725973600DACB40E73D16AEB464BA4F2ACB01F0E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RestorePrototype.json` | 703 | 2026-11-03T04:45:36.7746217Z | `8638B2496CA23A44C494D1C6B5C440943235B5C2794FCAB4210C3D224F3BEC1E` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunFinal.ps1` | 4103 | 2026-11-03T04:43:49.1897252Z | `DB4EB4E70ECE97BA23F853EE87E5DA2EB8C0879B354DA0CA0C5ED7B9947AB105` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunnerDone.txt` | 10 | 2026-11-03T04:45:40.6338579Z | `43F57FFB991888352F5131634217AF283D4279618AFE30963D7A7C97AD80E9E2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunnerError.txt` | 0 | 2026-11-03T04:44:01.5519666Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunnerOutput.txt` | 0 | 2026-11-03T04:44:01.5519666Z | `E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunnerPid.txt` | 10 | 2026-11-03T04:44:01.5989026Z | `1A1BD29EFF6132F8E69B04C6D22DB31B37116581C9AC365DECD99C022C06B8FC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunnerProcess.txt` | 115 | 2026-11-03T04:45:09.5996854Z | `0FC6CFD43899E884F60C9DF95B1ABBE6B9F966D7CC45700F9E876A604FBB3D5D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunnerStatus.json` | 567 | 2026-11-03T04:45:35.5724716Z | `9B2684A15BA6E290B915CC983A60BF4C01631D29325677846593FDF28EDC8869` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\RunnerStop.json` | 315 | 2026-11-03T04:45:34.0492629Z | `E3B030AEEBC8B3DA71F9E04D2B0A4DE68F9A5CBEC759CA9CEEB931415373ACD1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\SavedValidation.json` | 790 | 2026-11-03T04:45:38.6306875Z | `58B651EA931DB27DCACDEBAF4A53EAD2A7F303FF786A8C765EEEC2DDBB5D62B6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Smooth_01_Appear.png` | 728447 | 2026-11-03T04:36:29.5133634Z | `270622D95E697D0A17F23E852EB23AB64A1528C7FF7E549594CBB5378104CDBC` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Smooth_02_Appear.png` | 721490 | 2026-11-03T04:36:29.6303861Z | `C3135F5F7D6DCEE7ABEF4369CFB1C8C40A6C4727940FBB837C041272037B5E55` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Smooth_03_Appear.png` | 730418 | 2026-11-03T04:36:29.7722364Z | `FD7F8A4DA6F20F025C48BBF539EFE2D0B0DBB63B64C9F7313AFB44150C63462A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Smooth_04_Change.png` | 742751 | 2026-11-03T04:36:29.9032584Z | `A648701E211DB2AFD2E37B4C3CA23D747FC8CAA6C3DA986C78D7D9CC9003111C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Smooth_05_Change.png` | 754613 | 2026-11-03T04:36:30.0365318Z | `60512A8F8CCE42AA17AB5A997C604AB283C9766B2B5419E033746ABD2F18585D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Smooth_06_Change.png` | 761395 | 2026-11-03T04:36:30.1573540Z | `BDFE81CC8C96ADEC5F74484BBC562B8BFCC50C46B9D3FF4CF5897DD8114BBA48` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\StatusRetry.json` | 593 | 2026-11-03T04:38:42.8888184Z | `D05210B9C796F60D2DFE0E5D685928F7C4A26DE0961A50496E14F85E32E2A584` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\StatusTestFinal.json` | 262 | 2026-11-03T04:38:24.7985673Z | `D9F2090121F13581B235BAAD957B9C7358A06F3250C5030FE612B3928E2D3477` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\StopBeforeTestFinal.json` | 331 | 2026-11-03T04:39:27.5675574Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\StopForCompile.json` | 331 | 2026-11-03T04:35:51.5305102Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\StopPrototype.json` | 331 | 2026-11-03T04:37:13.0673830Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\StopTest.json` | 331 | 2026-11-03T04:34:39.7454117Z | `B5E45902FC7B4099AF3215052E93A76A2FC888A672FADACEC73348315E104195` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestCamera.json` | 16651 | 2026-11-03T04:45:28.7713219Z | `A30344D12F96082186A8A44242A6B471BDC88D4F8B57D42FDF2D7FC83CA77733` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestEnvelope.json` | 5578 | 2026-11-03T04:45:09.3717534Z | `1ED4352BCB4DE74C5ECC4E6A3F507C3FC2207814B9255E48EB62B59680BF5E72` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestInteraction.json` | 4843 | 2026-11-03T04:45:33.2046985Z | `A3B2EE481BB31916E1AC3878BCBA9CF7F663EE440E5A4C63B2B1108CB18FA185` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestOpen.json` | 713 | 2026-11-03T04:44:56.0189825Z | `AF9322A975911F2469B4177DF675F1FB89609ABCB5F72FA35414C5300CF6E856` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestPlay.json` | 316 | 2026-11-03T04:44:56.6855254Z | `44A6AA59987374434A9F16E837E1CE97C6ABE8256B0DE2ACF5F76840A49E705C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestPolish.json` | 7859 | 2026-11-03T04:45:16.2333133Z | `4E3DE7EE62B7030C46E913B604BAAFA56A51DD0AE6E51D84B5841D659250FB13` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestReadability.json` | 35207 | 2026-11-03T04:45:11.1895837Z | `1279E04EAF339DD7364535A4563569FEC1DDC80B322176D44469CA57B290438A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\TestSummary.txt` | 192 | 2026-11-03T04:33:12.4968564Z | `1EB32B1C1E06CE511209D9BD747214952DDEA2499965AD18592F93677D12EE0C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Verify.cs` | 10326 | 2026-11-03T04:35:23.6829738Z | `BDCE44E6EFCC54AFE233C5F58532DDBC4D8CF920AFAC51675D2E43387D9AC3E5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UISmooth_20261103~\Warnings.json` | 5692 | 2026-11-03T04:33:21.9240773Z | `D2B4EE87B965AED5F3D81301F7D016E780DE09C49DA681647DBA07B9133EC456` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\01_Lobby.png` | 527547 | 2026-10-30T01:00:10.3973034Z | `FF0382A5272041BF432EE9208B86C4A0984881F1CD2A1FA4AFEC54FC31F194A8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\02_SidePanel_1F.png` | 378418 | 2026-10-30T01:00:19.2882325Z | `40D5FAFA9DCC7B0E24E8BAF57BA59261E369ACA90503ED49942C6D47D9966F88` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\03_Judgment.png` | 607378 | 2026-10-30T01:00:20.6106884Z | `0455DF627006F3C02026747ADADD0AD6B1996CA99B12680100C064854473948B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\04_WaitFeedback.png` | 617912 | 2026-10-30T01:00:21.9923696Z | `59D8D64A3B2B3E8A0EF694D6EC7F2BE6F976C196F9BDAA27FB47499E72AD0115` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\05_PosterAndDirectory.png` | 696735 | 2026-10-30T01:00:29.3681003Z | `4B89934E84F0260AB032B9FCF98592D8C9170FEC2A8107011B63B11E33E31379` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\06_Identity.png` | 718533 | 2026-10-30T01:00:31.7302201Z | `716545BE16186133CFCBB7D2C4FCE0B910D9AA00CF75A0A462B3A845A4BFBF3A` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\07_RescueAt3F.png` | 655167 | 2026-10-30T01:00:33.3252844Z | `AD80EAD264CB86A288CF94F08966507DA2A68754EA30DFA9FABBE8996AB1AF33` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\08_GroundFloor.png` | 455409 | 2026-10-30T01:00:34.7134675Z | `56B4D7FC9E9B942666350DACA649707CE071A2A4752D891F855C49587D282CC8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\09_Complete.png` | 406040 | 2026-10-30T01:00:42.3158785Z | `8C3306A6A528A0D626A3B32460FEE02220F2952E70A995CDAD985812E67D898C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\10_Court.png` | 182310 | 2026-10-30T01:07:11.0228710Z | `C66AFA9DDBEA4D7904F94DA3F721BA8BF64A1878F57A7F306CBABE5D773B1FD4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\11_Building.png` | 145970 | 2026-10-30T01:07:11.0698755Z | `83F9FF9BC57F7FC128D7B560DF3E0F489E5A260A848D6753F099B8F0FA05ECA6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\12_ThirdFloorWindow.png` | 238268 | 2026-10-30T01:07:11.1235741Z | `AB21D317D7852A1259AC9533B4B12CC1889C17DF55913F2AFDA9581AFFEEA2AE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\BeforeSportsCentre.unity` | 980995 | 2026-10-30T00:41:35.4184824Z | `9D05C152090C6DA3B2FD68C8C9CA5CA3ADAB2268B51318A5AD591800C0E1C0A6` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\BeforeSportsCentre.unity.meta` | 155 | 2026-10-30T00:41:35.4194813Z | `376F65A70458B14342BA08BDA91DA9C5B547F36C7E77934442E958C43CD68C43` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\Completion.json` | 332 | 2026-10-30T01:08:32.9736266Z | `C58E7EB1F694C015231F4F582478249CFF951058C2D06C1F0E15456ACE3B05D1` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\export_blender_map.py` | 4031 | 2026-10-30T00:39:14.1983142Z | `2D803EB4E538C44868845C3E79756ED87346372848D608591B6BAEE1E2C3D034` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\FinishScene.cs` | 5523 | 2026-10-30T01:04:12.8564451Z | `37CDE48C107DFAE0FF7B404A516CCD856854356200059BF6217AF933C5CA950C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\FrameProof.cs` | 661 | 2026-10-30T00:43:47.0500889Z | `5D0DF096A61F72EF0C48828AF694E82671483096DF3B07628122A68BC32471E0` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\FrameProof.json` | 108 | 2026-10-30T00:43:53.5319955Z | `7FBC0E74E2254289954D87500CAAB9332EA17B5E4E261563659909600D4ECBED` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\Import.cs` | 69 | 2026-10-30T00:41:07.2837749Z | `4DB81276B216D33E98F5525D4C5EC46F6F54412B7B9BA73D6AC370921F0B2658` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\ImportReport.json` | 429 | 2026-10-30T00:41:17.4404154Z | `9321D0843DDA40E2E1A09C23F05AB76BFB77B16CACECA3585C4BC08F00BFB4E4` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\InputProbe.cs` | 1481 | 2026-10-30T00:49:38.3571432Z | `B309D50903EF2405354A35B4C91F7D61FCF27023426E7D0DB049CA910AFFE3F3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\InspectScene.cs` | 1423 | 2026-10-30T00:35:15.9629590Z | `AE3FBF7B6753F446840479C6C71A1174E6133994773BBDA9D8494B272F5BE9B5` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalSceneReadme.txt` | 7000 | 2026-10-29T03:36:15.4648284Z | `541D3FB0ED802DA54C53FC3EA0442E6992F2A87E2AAB651DCF906898C1EC66D2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorCinematicOverlay.cs` | 4685 | 2026-10-29T03:17:09.3768042Z | `9A97A88480F70568704FE8D84EDA42C2992E120CA126C209934875F1910DB59D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorDesktopControls.cs` | 11392 | 2026-10-29T03:12:23.3118215Z | `E26EC4CFF6AF9C6169345F2D471ECD87B14E2D1ACBB16FA117CA84CB1713734C` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorFlow.cs` | 9778 | 2026-10-29T02:52:08.8237612Z | `AFA8F92FA4FF014075471B449EAEA45CE3D7B10A1B5BD0BEB93F57AB2EFBA010` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorLesson.cs` | 9171 | 2026-10-29T03:06:08.5923449Z | `2327B00D7A068CE09EE54A8B7AC3B83EB01AE1C7D15BAADB15B1EE8A424CC847` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorMemoryGraphic.cs` | 5878 | 2026-10-29T03:27:26.9490751Z | `680059D3114B62C42608474ED040EB5F4E6198FFCE006A62C61EC0ADF9A48814` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorObservation.cs` | 1644 | 2026-10-29T03:06:07.9179016Z | `D224E608D0EC5C2033B04E3A8012144FE37071C95B0F57158CA322505C583262` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorSettings.cs` | 1970 | 2026-10-29T02:52:08.1113835Z | `87843293346066EAF9D4F1C3CB7789A6CAB76F5358EFA6456BD96AAC4F7CCA03` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorSpacePresentation.cs` | 4992 | 2026-10-29T03:43:15.1525784Z | `0FB0B842329D596CFBF2626126630126D75F6C2DAE66C41F4B23254BEA23DBC2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorSpatialUI.cs` | 15158 | 2026-10-29T04:47:31.0177623Z | `C5E2F1971A80067A90EFFC505800C4E5AD98F0EFC74A3C9B90FA277658B8A6FB` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorStoryPresentation.cs` | 12103 | 2026-10-29T03:43:15.9352634Z | `3A3BF43E70828B263AFBD9BD60F94287435FF68EEA8C3C8B4BF46835FB7429E8` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\OriginalScripts\ElevatorView.cs` | 20028 | 2026-10-29T04:47:30.2901352Z | `7155D32DB4AC665A6EFC5BAC062713A0766E58483E8054B63F05ED79FE87107D` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\Rebuild.cs` | 113 | 2026-10-30T00:41:07.9812493Z | `3DCA02A8A8AE05453738ECC173433FB63D329505D3E0CB2D8834DF21FE57A8CE` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\Reconstruction.json` | 377 | 2026-10-30T00:59:00.9492776Z | `52ECEA172994A6DCDF57DF000FAD4EEBC68030DF44B7CA867488A1E838442247` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\ReviewMap.cs` | 2027 | 2026-10-30T01:07:07.5435484Z | `B8710465965B52B49D484540E2DDCEF2ED3C3785D385E345CC492318CCDC863B` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\RuntimeConsole.json` | 2662 | 2026-10-30T01:02:03.3616408Z | `2E292311D6BA7C64F14B98D7269ED35ECFF54FD32474AFC0BB8A61E3ACEF5F10` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\RuntimeStatus.cs` | 438 | 2026-10-30T00:43:45.9891334Z | `61AE98318FE9E3074139D4A86CE323AE6D3241359EE1EAED9058819B403B97C2` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\RuntimeVerification.json` | 8351 | 2026-10-30T01:00:43.0893740Z | `E5E4DEEBD266E95E0F560858FB75698A7921B598372314659F4725395C0A97F3` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\SceneValidation.json` | 2589 | 2026-10-30T01:05:08.4764749Z | `34AFFAC0EF6600DE165DF83FA83EF3F5E51B4AF5B30D91D7D668E6C9F6453B40` |
| `D:\project\ProjectWait\Assets\M\CodexTemp\UnitySportsCentreRebuild~\VerifySportsCentre.cs` | 11863 | 2026-10-30T00:55:09.0213837Z | `6D8F6C97D01F9FF66F3AA241289B7C139F3D5AA8091932CCBFEAA8B4BC3C031D` |

<!-- AUTO_INVENTORY_END -->

## 12. 문서 저장 후 최종 확인

- 전체 목록 1,212개 파일의 크기 및 SHA-256을 원본과 재대조: 불일치 0개.
- 원본 전체 크기: 353,806,596바이트. 원본 삭제 및 압축 생성 없음.
