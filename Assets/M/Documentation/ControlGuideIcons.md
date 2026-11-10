# 조작 안내 아이콘 제작 목록

## 조사 범위와 결론

기존 프로젝트 파일은 읽기 전용으로 조사했다. 실제 빌드 등록 씬은 `Assets/M/Scene/Elevator_Obsevation_Choice_Test.unity`이며 `ProjectSettings/EditorBuildSettings.asset:7-10`에서 확인했다. README, 현재 ObservationTrial 런타임, 실제 씬의 활성 컴포넌트와 입력 액션 참조를 함께 대조했다. 비교용 DialoguePreview 씬은 존재하지만 빌드 대상이 아니다.

**제작 목록: VR 7종 + PC 6종 + 튜토리얼 3종 = 16종.** 아래 목록과 원본 사양을 먼저 확정하고 제작한다. 기기 버튼 이름은 프로젝트의 현재 바인딩 기준이며 모든 VR 기종에서 B라는 이름을 쓰는 것은 아니다.

| 파일 ID | 안내 의미 | 입력 또는 사용 맥락 | 분류 |
|---|---|---|---|
| `vr_look_around` | 머리로 둘러보기 | HMD 위치·회전 추적 | 기본 |
| `vr_move_left_stick` | 왼손 스틱 이동 | `<XRController>{LeftHand}/primary2DAxis` | 기본 |
| `vr_point_ray` | 레이로 가리키기 | 양손 Near-Far Interactor | 기본 |
| `vr_select_trigger` | 검지 트리거 선택 | 양손 TriggerButton, 물체 Select 및 UI Press | 기본 |
| `vr_pause_left_menu` | 왼손 메뉴 | 왼손 menuButton, 메뉴 열기/닫기 | 기본 |
| `vr_pause_right_b` | 오른손 B | 오른손 secondaryButton, 메뉴 열기/닫기 | 기본 |
| `vr_height_calibrate` | 눈높이 맞추기 | 메뉴에서 선택하는 VR 보정 기능, 전용 버튼 없음 | 보조 |
| `pc_look_mouse` | 마우스로 둘러보기 | 마우스 delta, 우클릭 조건 없음 | 기본 |
| `pc_select_left_click` | 중앙 조준 후 좌클릭 | 화면 중앙 레이 + leftButton, 메뉴에서는 자유 포인터 | 기본 |
| `pc_move_wasd` | WASD 이동 | W/A/S/D, 시점의 수평 방향 기준 | 기본 |
| `pc_choice_1_4` | 숫자로 선택지 고르기 | 현재 표시된 UI 선택지에만 1/2/3/4 적용 | 보조 |
| `pc_pause_esc` | Esc 메뉴 | 일시정지 및 계속하기 | 기본 |
| `pc_restart_r` | R 재시작 | 일시정지·연출 중에는 제한 | 보조 |
| `tutorial_call_lift` | 엘리베이터 호출 | CallLift 단계의 대상 표시 | 튜토리얼 |
| `tutorial_choose_floor_1` | 1층 버튼 선택 | ChooseFloor 단계의 대상 표시 | 튜토리얼 |
| `tutorial_receive_ball` | 선택해서 공 받기 | ReceiveBall 단계의 대상 표시, 그립 잡기 아님 | 튜토리얼 |

## 소스 근거

기준 경로는 `Assets/M/Scripts/ObservationTrial/`이다. 개별 아이콘의 자세한 경로·줄 범위·설명은 `Assets/M/Arts/Textures/ControlGuide/Source~/icon_spec.json`에 보관한다.

- `ElevatorPlayerMovement.cs:28-35,85-98`: 왼손 스틱, WASD, 시점 기준 이동.
- `ElevatorDesktopControls.cs:52-61,89-117,189-205,235-240`: 숫자 선택지, R, 좌클릭, 마우스 시점, 중앙 조준과 메뉴 포인터 차이.
- `ElevatorPauseMenu.cs:24-26,60-70`: Esc / 왼손 메뉴 / 오른손 secondaryButton, VR 높이 보정.
- `ElevatorTutorialPreview.cs:74-80,181-190`: 호출·1층·공 받기 단계와 레이/클릭 안내. 작업 중 감지된 최신 소스에서는 1층 선택과 공 받기의 순서가 자유이며, 두 동작이 모두 남아 있으면 `BoardingActions` 안내를 사용한다. 개별 아이콘은 고정 순서를 표시하지 않는다.
- `ElevatorObservation.cs:46-58`: Select 이벤트로 선택, 누른 채 단계가 바뀌는 것을 새 선택으로 처리하지 않음.
- 실제 씬 `:27475-27496,32053-32074`: 양손 Select 입력 액션 참조. `:27588-27609,32166-32187`: 양손 UI Press 참조.
- `Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/XRI Default Input Actions.inputactions:1007-1011,2188-2192`: 양손 TriggerButton binding.
- 실제 씬 `:36012-36019,36059-36066`: 스냅/연속 회전 provider 비활성. `:29314-29321`: 점프 비활성. `:49158-49165`: 텔레포트 provider 비활성.

## 제외 및 재사용

- F8·단계 건너뛰기는 개발자 전용이므로 제외한다.
- 점프, 달리기, 사용자 텔레포트, 오른손 스틱 회전, E 상호작용, 우클릭 시점, 시선 고정 선택을 새 조작처럼 그리지 않는다.
- **공 받기는 트리거 또는 클릭으로 선택하는 상호작용이다.** 그립·손 추적 제스처·물리적인 잡기 조작을 요구하는 그림으로 표현하지 않는다.
- 양손 근접 UI Poke는 구현된 보조 경로이나 현재 안내는 레이+트리거를 우선하므로 이번 기본 세트에서 제외한다.
- 대화 넘기기, 기록 열기/닫기, 계속하기, 종료는 별도 입력 키가 없으므로 기존 선택 아이콘을 재사용한다.
- 1–4 선택지 키는 모든 키를 동일 색상으로 그려 정답을 암시하지 않는다. 튜토리얼의 층 버튼은 키보드 단축키와 구분되는 패널 그림으로 표현한다.

## 아트와 출력 사양

- 따뜻한 동화책 카툰 스타일. 둥근 형태, 진한 갈색 외곽선, 크림색 기기, 청록·민트 보조면, 주황색 조작부.
- UI 기준색: Ink `#473024`, Paper `#FFF6DB`, Teal `#297A70`, Mint `#C9E3C2`. 강조색은 후드 기준 `#FF8C2D`.
- 3단계의 분명한 명암, 얕은 베벨, 넓은 그라데이션·블러·금속 광택 없음.
- 아이콘은 256×256 벡터 좌표계, 512×512 RGBA 투명 PNG. 각 원본 SVG는 독립 파일이며 외부 이미지·폰트에 의존하지 않는 벡터 도형이다.
- 파일명과 아이콘 내부에는 한글 문장을 굽지 않는다. 한글은 UI의 TMP 텍스트로 붙이고, 파일별 권장 문구는 사양 JSON에 제공한다. L/R, B, Esc, R, WASD, 숫자는 벡터 도형으로 포함한다.
- 주 그림은 64px 이상 권장. 입력 설명이 중요한 VR 컨트롤러/1–4 키는 80–96px 이상이 적합하며, 48px는 비교용으로만 제공한다. 실제 HMD 양안 가독성은 별도 확인한다.
- 기본 사용 배경은 프로젝트의 종이색·밝은 민트 안내 카드다. 청록색 또는 어두운 배경에서는 같은 청록색의 가는 방향선이 약해질 수 있으므로 종이색 배지를 뒤에 둔다. 배경 비교 미리보기는 이런 제한까지 확인하기 위한 자료다.

## 저장 위치와 사용 범위

- 사용 PNG 및 Sprite `.meta`: `Assets/M/Arts/Textures/ControlGuide/`
- SVG 원본·사양: 같은 폴더의 `Source~/` — Unity에서 원본 작업 폴더를 가져오지 않도록 `~` 접미사 사용.
- 제작 도구·미리보기·검증 결과: `Assets/M/CodexTemp/ControlGuideIcons~/`
- 기존 씬·런타임 코드·아트·프로젝트 설정을 수정하지 않는다. 이번 산출물은 **사용할 수 있는 신규 자산 세트**이며, 기존 `ElevatorVRSelectGraphic`의 런타임 교체나 씬 적용은 포함하지 않는다.

## Unity 사용법

PNG는 Sprite (2D and UI) / Single / Full Rect / 중심 Pivot / PPU 100 / Bilinear / Clamp / sRGB / 알파 투명 / Mipmap Off / 비압축 설정의 신규 `.meta`와 함께 제공한다. 기존 자산 GUID는 재사용하지 않는다.

1. PNG를 `UnityEngine.UI.Image`의 Source Image로 지정한다.
2. `Preserve Aspect`를 켜고 Image 색상은 흰색으로 둔다. 안내용이면 `Raycast Target`은 끈다.
3. 조작 아이콘 옆에 사양의 `instruction` 문구를 TMP 텍스트로 붙인다.
4. `vr_pause_right_b`는 B 표기 장치용이다. 다른 장치에서는 실제 secondary 버튼 이름과 일치하는지 먼저 확인한다.
5. PNG는 색상 완성본이므로 `Image.color`를 청록 등으로 다시 칠하지 않는다. 비활성 상태는 전체 알파로 표현한다.

## 검증 범위

최종 검증 결과는 `Assets/M/CodexTemp/ControlGuideIcons~/verification.json`에 기록한다. 목록과 파일 대응, SVG 파싱·외부 의존성, PNG 크기·알파·경계 여백, 신규 GUID 중복, import 설정, 작은 크기 비교 미리보기, 작업 전 기존 파일 해시 보존을 검사한다.

작업 중 기존 런타임·검증·폰트 파일에 동시 변경이 감지되었다. 제작 도구는 신규 ControlGuide 자산·메타 및 자기 작업 폴더에만 쓰도록 제한하고, 기존 파일에 쓴 기록이 없는지 따로 검사한다. 기존 파일 해시 비교는 이 동시 변경을 숨기거나 통과로 표시하지 않으며, 해당 파일을 되돌리거나 덮어쓰지 않는다. 최종 목록은 변경된 튜토리얼 소스와 다시 대조했다.

기존 씬에 적용하지 않았으므로 실제 씬 배치·UI 런타임 교체·실제 HMD 양안 가독성·기기 성능은 이번 자산 제작의 검증에 포함하지 않는다. Unity import 실검증을 별도로 수행했는지는 검증 JSON을 기준으로 판단한다.
