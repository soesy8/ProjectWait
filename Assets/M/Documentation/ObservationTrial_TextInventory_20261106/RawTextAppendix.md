# 씬 텍스트 원본 부록

주요 문구의 노출 시점·사용 여부는 `TextInventory.md`를 기준으로 읽습니다. 아래는 원문 누락을 막기 위한 원본 목록입니다.

## 1. 실행 중 씬의 TMP 전체 목록

총 172개. Lobby 단계의 단일 시점 스냅샷입니다. active=true여도 상위 알파가 0이거나 시야 밖이면 보이지 않습니다. 일시정지 UI가 열린 시점이므로 현재 보임 여부를 정상 진행의 사용 여부로 해석하면 안 됩니다.

| ID | Hierarchy 경로 | 원문 | active / enabled | 상위 CanvasGroup 알파 |
|---|---|---|---|---|
| TMP-001 | Stationary_XR_Origin/Camera Offset/Left Controller/Affordance Callouts Left/Affordance Callout Grab/Lazy Tooltip/Tooltip Canvas/Tooltip Text | Grab | False / True | Tooltip Canvas=1.0 |
| TMP-002 | Stationary_XR_Origin/Camera Offset/Left Controller/Affordance Callouts Left/Affordance Callout Click/Lazy Tooltip/Tooltip Canvas/Tooltip Text | UI Press | False / True | Tooltip Canvas=1.0 |
| TMP-003 | Stationary_XR_Origin/Camera Offset/Left Controller/Affordance Callouts Left/Affordance Callout Smooth/Lazy Tooltip/Tooltip Canvas/Tooltip Text | Move | False / True | Tooltip Canvas=1.0 |
| TMP-004 | Stationary_XR_Origin/Camera Offset/Right Controller/Affordance Callouts Right/Affordance Callout Grab/Lazy Tooltip/Tooltip Canvas/Tooltip Text | Grab | False / True | Tooltip Canvas=1.0 |
| TMP-005 | Stationary_XR_Origin/Camera Offset/Right Controller/Affordance Callouts Right/Affordance Callout Click/Lazy Tooltip/Tooltip Canvas/Tooltip Text | UI Press | False / True | Tooltip Canvas=1.0 |
| TMP-006 | Stationary_XR_Origin/Camera Offset/Right Controller/Affordance Callouts Right/Affordance Callout Blink/Lazy Tooltip/Tooltip Canvas/Tooltip Text | Blink | False / True | Tooltip Canvas=1.0 |
| TMP-007 | Stationary_XR_Origin/Camera Offset/Right Controller/Affordance Callouts Right/Affordance Callout Turn/Lazy Tooltip/Tooltip Canvas/Tooltip Text | Turn | False / True | Tooltip Canvas=1.0 |
| TMP-008 | Elevator_Safety_Lesson/Cabin/FloorNumber | 3 | False / True |  |
| TMP-009 | Elevator_Safety_Lesson/Cabin/FloorDigit | 5 | False / True |  |
| TMP-010 | Elevator_Safety_Lesson/Cabin/FloorDigit | 6 | False / True |  |
| TMP-011 | Elevator_Safety_Lesson/Cabin/FloorDigit | 3 | False / True |  |
| TMP-012 | Elevator_Safety_Lesson/Cabin/FloorDigit | 4 | False / True |  |
| TMP-013 | Elevator_Safety_Lesson/Cabin/FloorDigit | 1 | False / True |  |
| TMP-014 | Elevator_Safety_Lesson/Cabin/FloorDigit | 2 | False / True |  |
| TMP-015 | Elevator_Safety_Lesson/Cabin/DoorControlSymbol | &lt; &gt; | False / True |  |
| TMP-016 | Elevator_Safety_Lesson/Cabin/DoorControlSymbol | &gt; &lt; | False / True |  |
| TMP-017 | Elevator_Safety_Lesson/Cabin/DoorButtonLabel | 열림          닫힘 | False / True |  |
| TMP-018 | Elevator_Safety_Lesson/Cabin/IntercomTitle | 비상통화 | False / True |  |
| TMP-019 | Elevator_Safety_Lesson/Cabin/BellIcon | ☎ | False / True |  |
| TMP-020 | Elevator_Safety_Lesson/Cabin/LiftIdentityText | 햇살아파트 · M-01<br>정원 8명  &#124;  600 kg | False / True |  |
| TMP-021 | Elevator_Safety_Lesson/Cabin/AdEyebrow | 우리 동네 주말 소식 | False / True |  |
| TMP-022 | Elevator_Safety_Lesson/Cabin/AdTitle | 함께하는<br>농구 교실 | False / True |  |
| TMP-023 | Elevator_Safety_Lesson/Cabin/AdDate | 토요일 10시 · 야외 운동장 | False / True |  |
| TMP-024 | Elevator_Safety_Lesson/Cabin/FlyerText | 안전하게 함께<br>멈추면 비상통화<br>문에서 떨어져 기다리기 | False / True |  |
| TMP-025 | Elevator_Safety_Lesson/BasketballLobby/CallIndicator | ↑ | False / True |  |
| TMP-026 | Elevator_Safety_Lesson/BasketballLobby/CallUp | ↑ | False / True |  |
| TMP-027 | Elevator_Safety_Lesson/BasketballLobby/CallDown | ↓ | False / True |  |
| TMP-028 | Elevator_Safety_Lesson/BasketballLobby/LobbyFloor | 1F | False / True |  |
| TMP-029 | Elevator_Safety_Lesson/BasketballLobby/LobbyTitle | 햇살아파트 | False / True |  |
| TMP-030 | Elevator_Safety_Lesson/CinematicLetterbox/BottomLetterbox/CinematicSubtitle | （빈 문자열） | False / True |  |
| TMP-031 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/Chapter/StoryTitle | （빈 문자열） | False / True | Elevator_Screen_Thoughts=0.0 |
| TMP-032 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/Chapter/Objective | （빈 문자열） | False / True | Elevator_Screen_Thoughts=0.0 |
| TMP-033 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/TeamProgress | ○ 도움 요청   ○ 위치 전달   ○ 함께 대기 | False / True | Elevator_Screen_Thoughts=0.0 |
| TMP-034 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/Controls | WASD 이동  ·  마우스로 둘러보기  ·  중앙 조준 후 클릭  ·  Esc 일시정지 | False / True | Elevator_Screen_Thoughts=0.0 |
| TMP-035 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/PlayerThought/Thought | （빈 문자열） | False / True | PlayerThought=0.0; Elevator_Screen_Thoughts=0.0 |
| TMP-036 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/PlayerThought/ThoughtHeading | （빈 문자열） | False / True | PlayerThought=0.0; Elevator_Screen_Thoughts=0.0 |
| TMP-037 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/PlayerThought/CurrentThoughtBubble/Heading | （빈 문자열） | False / True | PlayerThought=0.0; Elevator_Screen_Thoughts=0.0 |
| TMP-038 | Elevator_Safety_Lesson/Elevator_Screen_Thoughts/PlayerThought/CurrentThoughtBubble/Thought | （빈 문자열） | False / True | PlayerThought=0.0; Elevator_Screen_Thoughts=0.0 |
| TMP-039 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/NotebookTitle | 우리 둘이 해냈어! | False / True |  |
| TMP-040 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/NotebookEyebrow | 함께 쓴 안전 기록 | False / True |  |
| TMP-041 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep0/StepLabel | 도움 요청 | False / True |  |
| TMP-042 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep0/DoneBadge/Done | 완료 | False / True |  |
| TMP-043 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep1/StepLabel | 위치 전달 | False / True |  |
| TMP-044 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep1/DoneBadge/Done | 완료 | False / True |  |
| TMP-045 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep2/StepLabel | 안전 대기 | False / True |  |
| TMP-046 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep2/DoneBadge/Done | 완료 | False / True |  |
| TMP-047 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/NotebookFooter | 도움을 요청하고,<br>안전한 곳에서 기다려요. | False / True |  |
| TMP-048 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep3/StepLabel | 안내 따르기 | False / True |  |
| TMP-049 | Elevator_Safety_Lesson/Completion_3D_Notebook/LeftLeaf/PageSurface/CompletedStep3/DoneBadge/Done | 완료 | False / True |  |
| TMP-050 | Elevator_Safety_Lesson/Completion_3D_Notebook/RightLeaf/PageSurface/PictureCaption/MemoryTitle | 함께 안전하게 기다렸어요. | False / True |  |
| TMP-051 | Elevator_Safety_Lesson/Completion_3D_Notebook/RightLeaf/PageSurface/MemoryDetail | 도움을 요청하고, 위치를 알리고,<br>친구와 함께 안내를 기다렸어요. | False / True |  |
| TMP-052 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor1/Label | 1 | True / True |  |
| TMP-053 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor1/Label | 1 | True / True |  |
| TMP-054 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor2/Label | 2 | True / True |  |
| TMP-055 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor2/Label | 2 | True / True |  |
| TMP-056 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor3/Label | 3 | True / True |  |
| TMP-057 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor3/Label | 3 | True / True |  |
| TMP-058 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor4/Label | 4 | True / True |  |
| TMP-059 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor4/Label | 4 | True / True |  |
| TMP-060 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor5/Label | 5 | True / True |  |
| TMP-061 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor5/Label | 5 | True / True |  |
| TMP-062 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor6/Label | 6 | True / True |  |
| TMP-063 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor6/Label | 6 | True / True |  |
| TMP-064 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor7/Label | 7 | True / True |  |
| TMP-065 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor7/Label | 7 | True / True |  |
| TMP-066 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor8/Label | 8 | True / True |  |
| TMP-067 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor8/Label | 8 | True / True |  |
| TMP-068 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor9/Label | 9 | True / True |  |
| TMP-069 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor9/Label | 9 | True / True |  |
| TMP-070 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor10/Label | 10 | True / True |  |
| TMP-071 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor10/Label | 10 | True / True |  |
| TMP-072 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor11/Label | 11 | True / True |  |
| TMP-073 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor11/Label | 11 | True / True |  |
| TMP-074 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontFloor12/Label | 12 | True / True |  |
| TMP-075 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideFloor12/Label | 12 | True / True |  |
| TMP-076 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontEmergency/Label | 비상 | True / True |  |
| TMP-077 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideEmergency/Label | 비상 | True / True |  |
| TMP-078 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_FrontEmergencyGuide/Label | 비상 호출 | True / True |  |
| TMP-079 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_SideEmergencyGuide/Label | 비상 호출 | True / True |  |
| TMP-080 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_DoorGuide0/Label | 열림 | True / True |  |
| TMP-081 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CallArrow0/Label | ▲ | True / True |  |
| TMP-082 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_DoorGuide1/Label | 닫힘 | True / True |  |
| TMP-083 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CallArrow1/Label | ▼ | True / True |  |
| TMP-084 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_LiftIdentity/Label | 한빛 생활체육센터 | True / True |  |
| TMP-085 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_LiftIdentity/Label | 본관 1호기 · HB-EL-01 | True / True |  |
| TMP-086 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_LiftIdentity/Label | 1000 kg / 13명 | True / True |  |
| TMP-087 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_LobbyDirectory/Label | 층별 안내 | True / True |  |
| TMP-088 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_LobbyDirectory/Label | 1F  로비 · 주출입구    2F  준비 중<br>3F  농구장 · 탈의실    4F  운동교실<br>5F  체력단련실    6–12F  문화 · 운영 | True / True |  |
| TMP-089 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CabinDirectory/Label | 층별 시설 안내 | True / True |  |
| TMP-090 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CabinDirectory/Label | 6–12F  문화 · 운영 | True / True |  |
| TMP-091 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CabinDirectory/Label | 5F  체력단련실 | True / True |  |
| TMP-092 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CabinDirectory/Label | 4F  운동교실 | True / True |  |
| TMP-093 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CabinDirectory/Label | 3F  농구장 · 탈의실 | True / True |  |
| TMP-094 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CabinDirectory/Label | 2F  준비 중 | True / True |  |
| TMP-095 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/1F_CabinDirectory/Label | 1F  로비 · 주출입구 | True / True |  |
| TMP-096 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor1/Label | 1 | True / True |  |
| TMP-097 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor1/Label | 1 | True / True |  |
| TMP-098 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor2/Label | 2 | True / True |  |
| TMP-099 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor2/Label | 2 | True / True |  |
| TMP-100 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor3/Label | 3 | True / True |  |
| TMP-101 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor3/Label | 3 | True / True |  |
| TMP-102 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor4/Label | 4 | True / True |  |
| TMP-103 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor4/Label | 4 | True / True |  |
| TMP-104 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor5/Label | 5 | True / True |  |
| TMP-105 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor5/Label | 5 | True / True |  |
| TMP-106 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor6/Label | 6 | True / True |  |
| TMP-107 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor6/Label | 6 | True / True |  |
| TMP-108 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor7/Label | 7 | True / True |  |
| TMP-109 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor7/Label | 7 | True / True |  |
| TMP-110 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor8/Label | 8 | True / True |  |
| TMP-111 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor8/Label | 8 | True / True |  |
| TMP-112 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor9/Label | 9 | True / True |  |
| TMP-113 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor9/Label | 9 | True / True |  |
| TMP-114 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor10/Label | 10 | True / True |  |
| TMP-115 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor10/Label | 10 | True / True |  |
| TMP-116 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor11/Label | 11 | True / True |  |
| TMP-117 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor11/Label | 11 | True / True |  |
| TMP-118 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontFloor12/Label | 12 | True / True |  |
| TMP-119 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideFloor12/Label | 12 | True / True |  |
| TMP-120 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontEmergency/Label | 비상 | True / True |  |
| TMP-121 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideEmergency/Label | 비상 | True / True |  |
| TMP-122 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_FrontEmergencyGuide/Label | 비상 호출 | True / True |  |
| TMP-123 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_SideEmergencyGuide/Label | 비상 호출 | True / True |  |
| TMP-124 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_DoorGuide0/Label | 열림 | True / True |  |
| TMP-125 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CallArrow0/Label | ▲ | True / True |  |
| TMP-126 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_DoorGuide1/Label | 닫힘 | True / True |  |
| TMP-127 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CallArrow1/Label | ▼ | True / True |  |
| TMP-128 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_LiftIdentity/Label | 한빛 생활체육센터 | True / True |  |
| TMP-129 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_LiftIdentity/Label | 본관 1호기 · HB-EL-01 | True / True |  |
| TMP-130 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_LiftIdentity/Label | 1000 kg / 13명 | True / True |  |
| TMP-131 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_LobbyDirectory/Label | 층별 안내 | True / True |  |
| TMP-132 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_LobbyDirectory/Label | 1F  로비 · 주출입구    2F  준비 중<br>3F  농구장 · 탈의실    4F  운동교실<br>5F  체력단련실    6–12F  문화 · 운영 | True / True |  |
| TMP-133 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CabinDirectory/Label | 층별 시설 안내 | True / True |  |
| TMP-134 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CabinDirectory/Label | 6–12F  문화 · 운영 | True / True |  |
| TMP-135 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CabinDirectory/Label | 5F  체력단련실 | True / True |  |
| TMP-136 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CabinDirectory/Label | 4F  운동교실 | True / True |  |
| TMP-137 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CabinDirectory/Label | 3F  농구장 · 탈의실 | True / True |  |
| TMP-138 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CabinDirectory/Label | 2F  준비 중 | True / True |  |
| TMP-139 | Elevator_Safety_Lesson/SportsCentreMap/ReadableSignage/3F_CabinDirectory/Label | 1F  로비 · 주출입구 | True / True |  |
| TMP-140 | Elevator_Safety_Lesson/ElevatorPauseCanvas/PauseCard/Heading | 일시 정지 | True / True |  |
| TMP-141 | Elevator_Safety_Lesson/ElevatorPauseCanvas/PauseCard/Resume/Label | 게임 재개 | True / True |  |
| TMP-142 | Elevator_Safety_Lesson/ElevatorPauseCanvas/PauseCard/Restart/Label | 처음부터 다시하기 | True / True |  |
| TMP-143 | Elevator_Safety_Lesson/ElevatorPauseCanvas/PauseCard/Title/Label | 타이틀로 돌아가기 | True / True |  |
| TMP-144 | Elevator_Safety_Lesson/ElevatorPauseCanvas/PauseCard/Quit/Label | 게임 종료하기 | True / True |  |
| TMP-145 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_Friend_Jump_Root/HintText | （빈 문자열） | False / True | Hint_Friend_Jump_Root=0.0 |
| TMP-146 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_FriendBasketball/HintText | （빈 문자열） | False / True | Hint_FriendBasketball=0.0 |
| TMP-147 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_FriendlyRescuer/HintText | （빈 문자열） | False / True | Hint_FriendlyRescuer=0.0 |
| TMP-148 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_Door_KeepClear/HintText | （빈 문자열） | False / True | Hint_3F_Door_KeepClear=0.0 |
| TMP-149 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_Side_Emergency_1.28m/HintText | （빈 문자열） | False / True | Hint_3F_Side_Emergency_1.28m=0.0 |
| TMP-150 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_Front_Emergency_2.15m/HintText | （빈 문자열） | False / True | Hint_3F_Front_Emergency_2.15m=0.0 |
| TMP-151 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_LiftIdentity_HB-EL-01/HintText | （빈 문자열） | False / True | Hint_3F_LiftIdentity_HB-EL-01=0.0 |
| TMP-152 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_FacilityDirectory_NoHighlight/HintText | （빈 문자열） | False / True | Hint_3F_FacilityDirectory_NoHighlight=0.0 |
| TMP-153 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_SafetyPoster/HintText | （빈 문자열） | False / True | Hint_3F_SafetyPoster=0.0 |
| TMP-154 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_Side_Floor01/HintText | （빈 문자열） | False / True | Hint_3F_Side_Floor01=0.0 |
| TMP-155 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_Front_Floor01/HintText | （빈 문자열） | False / True | Hint_3F_Front_Floor01=0.0 |
| TMP-156 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_Display_LastKnown3/HintText | （빈 문자열） | False / True | Hint_3F_Display_LastKnown3=0.0 |
| TMP-157 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_3F_Lobby_Call/HintText | （빈 문자열） | False / True | Hint_3F_Lobby_Call=0.0 |
| TMP-158 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_1F_MainExit/HintText | （빈 문자열） | False / True | Hint_1F_MainExit=0.0 |
| TMP-159 | Elevator_Safety_Lesson/Fixed_Object_Hints/Hint_Cabin_Optional119Phone/HintText | （빈 문자열） | False / True | Hint_Cabin_Optional119Phone=0.0 |
| TMP-160 | Elevator_Safety_Lesson/PosterScreenGuide/PointingInstruction/Instruction | 마우스로 둘러보고, 포스터의 원을<br>중앙 레이저로 1.5초 동안 가리켜요. | False / True | PointingInstruction=0.0 |
| TMP-161 | Elevator_World_Dialogue/StoryCard/Dialogue | 오늘 재밌었다!<br>내려가는 버튼 좀 눌러 줄래? | True / True | Dialogue=1.0; StoryCard=1.0; Elevator_World_Dialogue=0.0 |
| TMP-162 | Elevator_World_Dialogue/StoryCard/ReadingStatus | （빈 문자열） | False / True | StoryCard=1.0; Elevator_World_Dialogue=0.0 |
| TMP-163 | Elevator_World_Dialogue/StoryCard/SpeakerTab/Speaker | 친구 | True / True | Speaker=1.0; SpeakerTab=1.0; StoryCard=1.0; Elevator_World_Dialogue=0.0 |
| TMP-164 | Elevator_World_Dialogue/DeveloperPanel/DevTitle | 개발 도구 · F8 닫기 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-165 | Elevator_World_Dialogue/DeveloperPanel/Stage0/Label | 로비 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-166 | Elevator_World_Dialogue/DeveloperPanel/Stage1/Label | 공 받기 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-167 | Elevator_World_Dialogue/DeveloperPanel/Stage2/Label | 짧은 도입 연출 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-168 | Elevator_World_Dialogue/DeveloperPanel/Stage3/Label | 정지 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-169 | Elevator_World_Dialogue/DeveloperPanel/Stage4/Label | 통화 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-170 | Elevator_World_Dialogue/DeveloperPanel/Stage5/Label | 친구에게 말 걸기 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-171 | Elevator_World_Dialogue/DeveloperPanel/Stage6/Label | 구조 안내 | False / True | Elevator_World_Dialogue=0.0 |
| TMP-172 | Elevator_World_Dialogue/DeveloperPanel/NoResponse/Label | 비상통화 응답 / 무응답 | False / True | Elevator_World_Dialogue=0.0 |

## 2. 모델 원본에 포함된 문자 메시

TMP가 아니라 모델 형상에 포함된 글자입니다. 일부는 가독성용 TMP 표지와 겹치거나 복제되어 있습니다. 원본의 전체 이름·부모·문구를 보존합니다. 모델 원문이라는 이유만으로 현재 카메라에서 보인다고 단정하지 않습니다.

| ID | 모델 오브젝트 | 부모 / 컬렉션 | 원문 |
|---|---|---|---|
| MODEL-001 | 3F · Cabin · 식별 건물명 |  / 3F · 05 · 엘리베이터 내부 | 한빛 생활체육센터 |
| MODEL-002 | 3F · Cabin · 식별 정원 하중 |  / 3F · 05 · 엘리베이터 내부 | 1000 kg  /  13명 |
| MODEL-003 | 3F · Cabin · 식별 호기와 번호 |  / 3F · 05 · 엘리베이터 내부 | 본관 1호기  &#124;  HB-EL-01 |
| MODEL-004 | 3F · Cabin · 전면 01층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 1 |
| MODEL-005 | 3F · Cabin · 전면 02층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 2 |
| MODEL-006 | 3F · Cabin · 전면 03층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 3 |
| MODEL-007 | 3F · Cabin · 전면 04층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 4 |
| MODEL-008 | 3F · Cabin · 전면 05층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 5 |
| MODEL-009 | 3F · Cabin · 전면 06층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 6 |
| MODEL-010 | 3F · Cabin · 전면 07층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 7 |
| MODEL-011 | 3F · Cabin · 전면 08층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 8 |
| MODEL-012 | 3F · Cabin · 전면 09층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 9 |
| MODEL-013 | 3F · Cabin · 전면 10층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 10 |
| MODEL-014 | 3F · Cabin · 전면 11층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 11 |
| MODEL-015 | 3F · Cabin · 전면 12층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 12 |
| MODEL-016 | 3F · Cabin · 전면 비상 버튼 문자 |  / 3F · 05 · 엘리베이터 내부 | 비상 |
| MODEL-017 | 3F · Cabin · 전면 비상 호출 안내 |  / 3F · 05 · 엘리베이터 내부 | 비상 호출 |
| MODEL-018 | 3F · Cabin · 전면 층수 |  / 3F · 05 · 엘리베이터 내부 | 3 |
| MODEL-019 | 3F · Cabin · 측면 01층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 1 |
| MODEL-020 | 3F · Cabin · 측면 02층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 2 |
| MODEL-021 | 3F · Cabin · 측면 03층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 3 |
| MODEL-022 | 3F · Cabin · 측면 04층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 4 |
| MODEL-023 | 3F · Cabin · 측면 05층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 5 |
| MODEL-024 | 3F · Cabin · 측면 06층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 6 |
| MODEL-025 | 3F · Cabin · 측면 07층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 7 |
| MODEL-026 | 3F · Cabin · 측면 08층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 8 |
| MODEL-027 | 3F · Cabin · 측면 09층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 9 |
| MODEL-028 | 3F · Cabin · 측면 10층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 10 |
| MODEL-029 | 3F · Cabin · 측면 11층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 11 |
| MODEL-030 | 3F · Cabin · 측면 12층 숫자 |  / 3F · 05 · 엘리베이터 내부 | 12 |
| MODEL-031 | 3F · Cabin · 측면 개폐 안내 |  / 3F · 05 · 엘리베이터 내부 | 열림 |
| MODEL-032 | 3F · Cabin · 측면 개폐 안내.001 |  / 3F · 05 · 엘리베이터 내부 | 닫힘 |
| MODEL-033 | 3F · Cabin · 측면 비상 버튼 문자 |  / 3F · 05 · 엘리베이터 내부 | 비상 |
| MODEL-034 | 3F · Cabin · 측면 비상 호출 안내 |  / 3F · 05 · 엘리베이터 내부 | 비상 호출 |
| MODEL-035 | 3F · Lift 1 · 방향 |  / 3F · 04 · 엘리베이터 외장 | ▲ |
| MODEL-036 | 3F · Lift 1 · 층수 |  / 3F · 04 · 엘리베이터 외장 | 3 |
| MODEL-037 | 3F · Lift 1 · 호기 표기 |  / 3F · 04 · 엘리베이터 외장 | 01 |
| MODEL-038 | 3F · Lift 2 · 층수 |  / 3F · 04 · 엘리베이터 외장 | 5 |
| MODEL-039 | 3F · Lift 2 · 호기 표기 |  / 3F · 04 · 엘리베이터 외장 | 02 |
| MODEL-040 | 3F · 계단 하부 층 안내 |  / 3F · 07 · 오른쪽 올라가는 계단 | 4F ↑ |
| MODEL-041 | 3F · 계단실 층 안내 |  / 3F · 11 · 계단실 상부 / 난간 | 4F |
| MODEL-042 | 3F · 농구장 표지 |  / 32 · 3층 농구 연습장 | 농구 연습장 |
| MODEL-043 | 3F · 로비 월 그래픽 층수 |  / 3F · 09 · 가구 / 화분 / 안내 | 3F |
| MODEL-044 | 3F · 센터 이름 |  / 3F · 09 · 가구 / 화분 / 안내 | 한빛 생활체육센터 |
| MODEL-045 | 3F · 시설 안내 제목 |  / 3F · 05 · 엘리베이터 내부 | 층별 시설 안내 |
| MODEL-046 | 3F · 안내 시설 12–6F |  / 3F · 05 · 엘리베이터 내부 | 문화 · 운영 |
| MODEL-047 | 3F · 안내 시설 1F |  / 3F · 05 · 엘리베이터 내부 | 로비 · 주출입구 |
| MODEL-048 | 3F · 안내 시설 2F |  / 3F · 05 · 엘리베이터 내부 | 준비 중 |
| MODEL-049 | 3F · 안내 시설 3F |  / 3F · 05 · 엘리베이터 내부 | 농구장 · 탈의실 |
| MODEL-050 | 3F · 안내 시설 4F |  / 3F · 05 · 엘리베이터 내부 | 운동교실 |
| MODEL-051 | 3F · 안내 시설 5F |  / 3F · 05 · 엘리베이터 내부 | 체력단련실 |
| MODEL-052 | 3F · 안내 층 12–6F |  / 3F · 05 · 엘리베이터 내부 | 6–12F |
| MODEL-053 | 3F · 안내 층 1F |  / 3F · 05 · 엘리베이터 내부 | 1F |
| MODEL-054 | 3F · 안내 층 2F |  / 3F · 05 · 엘리베이터 내부 | 2F |
| MODEL-055 | 3F · 안내 층 3F |  / 3F · 05 · 엘리베이터 내부 | 3F |
| MODEL-056 | 3F · 안내 층 4F |  / 3F · 05 · 엘리베이터 내부 | 4F |
| MODEL-057 | 3F · 안내 층 5F |  / 3F · 05 · 엘리베이터 내부 | 5F |
| MODEL-058 | 3F · 안내판 내용 |  / 3F · 09 · 가구 / 화분 / 안내 | 1F  로비 · 주출입구<br>2F  준비 중<br>3F  농구장 · 탈의실<br>4F  운동교실<br>5F  체력단련실<br>6–12F  문화 · 운영 |
| MODEL-059 | 3F · 안내판 제목 |  / 3F · 09 · 가구 / 화분 / 안내 | 층별 안내 |
| MODEL-060 | 3F · 출입구 표기 |  / 3F · 08 · 뒤쪽 자동문 / 진입부 | 농구장 · 탈의실 |
| MODEL-061 | 3F · 층 번호 대형 |  / 3F · 04 · 엘리베이터 외장 | 3 |
| MODEL-062 | 3F · 탈의 보관 동선 |  / 31 · 3층 탈의 · 보관 복도 | 탈의실 · 보관함 |
| MODEL-063 | 3F · 호출 버튼 문자 |  / 3F · 04 · 엘리베이터 외장 | ▲ |
| MODEL-064 | 3F · 호출 버튼 문자.001 |  / 3F · 04 · 엘리베이터 외장 | ▼ |
| MODEL-065 | Cabin · 식별 건물명 |  / 05 · 엘리베이터 내부 | 한빛 생활체육센터 |
| MODEL-066 | Cabin · 식별 정원 하중 |  / 05 · 엘리베이터 내부 | 1000 kg  /  13명 |
| MODEL-067 | Cabin · 식별 호기와 번호 |  / 05 · 엘리베이터 내부 | 본관 1호기  &#124;  HB-EL-01 |
| MODEL-068 | Cabin · 전면 01층 숫자 |  / 05 · 엘리베이터 내부 | 1 |
| MODEL-069 | Cabin · 전면 02층 숫자 |  / 05 · 엘리베이터 내부 | 2 |
| MODEL-070 | Cabin · 전면 03층 숫자 |  / 05 · 엘리베이터 내부 | 3 |
| MODEL-071 | Cabin · 전면 04층 숫자 |  / 05 · 엘리베이터 내부 | 4 |
| MODEL-072 | Cabin · 전면 05층 숫자 |  / 05 · 엘리베이터 내부 | 5 |
| MODEL-073 | Cabin · 전면 06층 숫자 |  / 05 · 엘리베이터 내부 | 6 |
| MODEL-074 | Cabin · 전면 07층 숫자 |  / 05 · 엘리베이터 내부 | 7 |
| MODEL-075 | Cabin · 전면 08층 숫자 |  / 05 · 엘리베이터 내부 | 8 |
| MODEL-076 | Cabin · 전면 09층 숫자 |  / 05 · 엘리베이터 내부 | 9 |
| MODEL-077 | Cabin · 전면 10층 숫자 |  / 05 · 엘리베이터 내부 | 10 |
| MODEL-078 | Cabin · 전면 11층 숫자 |  / 05 · 엘리베이터 내부 | 11 |
| MODEL-079 | Cabin · 전면 12층 숫자 |  / 05 · 엘리베이터 내부 | 12 |
| MODEL-080 | Cabin · 전면 비상 버튼 문자 |  / 05 · 엘리베이터 내부 | 비상 |
| MODEL-081 | Cabin · 전면 비상 호출 안내 |  / 05 · 엘리베이터 내부 | 비상 호출 |
| MODEL-082 | Cabin · 전면 층수 |  / 05 · 엘리베이터 내부 | 1 |
| MODEL-083 | Cabin · 측면 01층 숫자 |  / 05 · 엘리베이터 내부 | 1 |
| MODEL-084 | Cabin · 측면 02층 숫자 |  / 05 · 엘리베이터 내부 | 2 |
| MODEL-085 | Cabin · 측면 03층 숫자 |  / 05 · 엘리베이터 내부 | 3 |
| MODEL-086 | Cabin · 측면 04층 숫자 |  / 05 · 엘리베이터 내부 | 4 |
| MODEL-087 | Cabin · 측면 05층 숫자 |  / 05 · 엘리베이터 내부 | 5 |
| MODEL-088 | Cabin · 측면 06층 숫자 |  / 05 · 엘리베이터 내부 | 6 |
| MODEL-089 | Cabin · 측면 07층 숫자 |  / 05 · 엘리베이터 내부 | 7 |
| MODEL-090 | Cabin · 측면 08층 숫자 |  / 05 · 엘리베이터 내부 | 8 |
| MODEL-091 | Cabin · 측면 09층 숫자 |  / 05 · 엘리베이터 내부 | 9 |
| MODEL-092 | Cabin · 측면 10층 숫자 |  / 05 · 엘리베이터 내부 | 10 |
| MODEL-093 | Cabin · 측면 11층 숫자 |  / 05 · 엘리베이터 내부 | 11 |
| MODEL-094 | Cabin · 측면 12층 숫자 |  / 05 · 엘리베이터 내부 | 12 |
| MODEL-095 | Cabin · 측면 개폐 안내 |  / 05 · 엘리베이터 내부 | 열림 |
| MODEL-096 | Cabin · 측면 개폐 안내.001 |  / 05 · 엘리베이터 내부 | 닫힘 |
| MODEL-097 | Cabin · 측면 비상 버튼 문자 |  / 05 · 엘리베이터 내부 | 비상 |
| MODEL-098 | Cabin · 측면 비상 호출 안내 |  / 05 · 엘리베이터 내부 | 비상 호출 |
| MODEL-099 | Lift 1 · 방향 |  / 04 · 엘리베이터 외장 | ▲ |
| MODEL-100 | Lift 1 · 층수 |  / 04 · 엘리베이터 외장 | 1 |
| MODEL-101 | Lift 1 · 호기 표기 |  / 04 · 엘리베이터 외장 | 01 |
| MODEL-102 | Lift 2 · 층수 |  / 04 · 엘리베이터 외장 | 5 |
| MODEL-103 | Lift 2 · 호기 표기 |  / 04 · 엘리베이터 외장 | 02 |
| MODEL-104 | 계단 하부 층 안내 |  / 07 · 오른쪽 올라가는 계단 | 2F ↑ |
| MODEL-105 | 계단실 층 안내 |  / 11 · 계단실 상부 / 난간 | 2F |
| MODEL-106 | 로비 월 그래픽 층수 |  / 09 · 가구 / 화분 / 안내 | 1F |
| MODEL-107 | 센터 이름 |  / 09 · 가구 / 화분 / 안내 | 한빛 생활체육센터 |
| MODEL-108 | 안내판 내용 |  / 09 · 가구 / 화분 / 안내 | 1F  로비 · 주출입구<br>2F  준비 중<br>3F  농구장 · 탈의실<br>4F  운동교실<br>5F  체력단련실<br>6–12F  문화 · 운영 |
| MODEL-109 | 안내판 제목 |  / 09 · 가구 / 화분 / 안내 | 층별 안내 |
| MODEL-110 | 출입구 표기 |  / 08 · 뒤쪽 자동문 / 진입부 | 출입구 |
| MODEL-111 | 층 번호 대형 |  / 04 · 엘리베이터 외장 | 1 |
| MODEL-112 | 호출 버튼 문자 |  / 04 · 엘리베이터 외장 | ▲ |
| MODEL-113 | 호출 버튼 문자.001 |  / 04 · 엘리베이터 외장 | ▼ |

## 3. 런타임 코드의 한글 문자열 원문

표시용 외에 내부 이력·판별 문자열·사용하지 않는 폴백도 포함합니다. 문자열 리터럴 단위이므로 동적 조합은 주요 목록에서 완성형을 확인합니다. 주석만 있는 줄은 제외합니다.

| ID | 소스 위치 | 문자열 리터럴 원문 |
|---|---|---|
| SRC-001 | ElevatorCinematicOverlay.cs:49 | 친구와 함께 엘리베이터에 타고 있어요. |
| SRC-002 | ElevatorCinematicOverlay.cs:49 | 구조요원과 함께 밖으로 나가고 있어요. |
| SRC-003 | ElevatorCinematicOverlay.cs:49 | 비상통화 연결 중… |
| SRC-004 | ElevatorCinematicOverlay.cs:50 | 친구 |
| SRC-005 | ElevatorCinematicOverlay.cs:51 | 친구가 뛰려고 해요. 안전 안내를 살펴보세요. |
| SRC-006 | ElevatorCinematicOverlay.cs:52 | 나  ·  여기서는 뛰면 위험해. 슛은 밖에서 하자. |
| SRC-007 | ElevatorCinematicOverlay.cs:53 | 친구 |
| SRC-008 | ElevatorCinematicOverlay.cs:53 | 엘리베이터 안에서는 뛰지 마세요. |
| SRC-009 | ElevatorCinematicOverlay.cs:54 | 친구 |
| SRC-010 | ElevatorCinematicOverlay.cs:57 | 나 |
| SRC-011 | ElevatorCinematicOverlay.cs:62 | 구조요원과 함께 계단으로 내려가고 있어요. |
| SRC-012 | ElevatorCinematicOverlay.cs:63 | 친구와 함께 기다리던 중… |
| SRC-013 | ElevatorCinematicOverlay.cs:64 | 친구와 함께 집으로 돌아가요. |
| SRC-014 | ElevatorDialogue.cs:7 | 오늘 재밌었다!<br>이제 1층으로 내려가자. |
| SRC-015 | ElevatorDialogue.cs:8 | 오늘 재밌었다!<br>내려가는 버튼 좀 눌러 줄래? |
| SRC-016 | ElevatorDialogue.cs:9 | 우리 1층으로 가자.<br>1층 버튼 좀 눌러 줄래? |
| SRC-017 | ElevatorDialogue.cs:10 | 고마워!<br>내 공 좀 잠깐 받아 줄래? |
| SRC-018 | ElevatorDialogue.cs:11 | 아까 그 슛 봤지? |
| SRC-019 | ElevatorDialogue.cs:12 | 이렇게 던졌잖아! |
| SRC-020 | ElevatorDialogue.cs:13 | 어? 왜 멈췄지? |
| SRC-021 | ElevatorDialogue.cs:14 | 엘리베이터가 멈췄어요.<br>친구랑 둘이 안에 있어요. |
| SRC-022 | ElevatorDialogue.cs:15 | 건물 이름과 승강기 번호를<br>알려 주세요. |
| SRC-023 | ElevatorDialogue.cs:16 | 문 옆 비상벨 아래에<br>건물 이름과 승강기 번호가<br>적혀 있어요. |
| SRC-024 | ElevatorDialogue.cs:17 | 네, 위치 확인했어요.<br>구조요원이 갈 테니<br>문에서 떨어져 기다려 주세요. |
| SRC-025 | ElevatorDialogue.cs:18 | 아직 좀 무서워…<br>다리가 떨리는데,<br>문에 잠깐 기대도 될까? |
| SRC-026 | ElevatorDialogue.cs:19 | 옆 벽으로 가서 같이 기다리자. |
| SRC-027 | ElevatorDialogue.cs:20 | 그래, 문에 잠깐 기대서 쉬어. |
| SRC-028 | ElevatorDialogue.cs:21 | 문에 기대면 위험해.<br>저쪽 벽으로 가서 기다리자. |
| SRC-029 | ElevatorDialogue.cs:22 | 응, 그쪽으로 갈게. |
| SRC-030 | ElevatorDialogue.cs:23 | 이제 좀 괜찮아졌어.<br>옆에 있어 줘서 고마워. |
| SRC-031 | ElevatorDialogue.cs:24 | 아직 좀 무서워…<br>옆에 있어 줄래? |
| SRC-032 | ElevatorDialogue.cs:25 | 응, 내가 옆에 있을게. 같이 기다리자. |
| SRC-033 | ElevatorDialogue.cs:26 | 구조대입니다.<br>문에서 떨어져 기다려 주세요. |
| SRC-034 | ElevatorDialogue.cs:27 | 문 앞 바닥을 확인 중이에요.<br>안전하게 나올 수 있게 도와드릴게요. |
| SRC-035 | ElevatorDialogue.cs:28 | 이제 나오셔도 괜찮아요.<br>저희 안내에 따라 천천히 나와 주세요. |
| SRC-036 | ElevatorDialogue.cs:29 | 엘리베이터는 점검해야 해요.<br>계단으로 1층까지 안내할게요. |
| SRC-037 | ElevatorDialogue.cs:30 | 휴, 이제 좀 안심이 된다.<br>우리 집에 가자. |
| SRC-038 | ElevatorDialogue.cs:31 | 같이 있어 줘서 고마워.<br>다음엔 코트에서 또 놀자! |
| SRC-039 | ElevatorDialogue.cs:32 | 문에 기대면 다칠 수 있어요.<br>문에서 떨어진 곳에서<br>기다리는 게 안전해요. |
| SRC-040 | ElevatorDialogue.cs:33 | 문을 억지로 열면 위험해요.<br>밖에 도움을 요청하고<br>구조요원을 기다려 주세요. |
| SRC-041 | ElevatorDialogue.cs:34 | 문에 기대거나<br>억지로 열지 마세요.<br>구조요원의 안내를 기다리세요. |
| SRC-042 | ElevatorDialogue.cs:36 | 119 · 모의 통화 |
| SRC-043 | ElevatorDialogue.cs:36 | 관리실 |
| SRC-044 | ElevatorDialogue.cs:37 | 119입니다. 무슨 일이세요? |
| SRC-045 | ElevatorDialogue.cs:37 | 관리실입니다. 무슨 일이세요? |
| SRC-046 | ElevatorDialogue.cs:44 | 에 있어요.<br>번호는  |
| SRC-047 | ElevatorDialogue.cs:44 | 입니다. |
| SRC-048 | ElevatorDialogue.cs:59 | 아래로 내려가는 엘리베이터를<br>부르는 버튼이에요. |
| SRC-049 | ElevatorDialogue.cs:61 | 층을 고르는 버튼이에요.<br>지금은 눌러도 움직이지 않아요.<br>비상통화로 도움을 요청하세요. |
| SRC-050 | ElevatorDialogue.cs:61 | 가려는 층을 고르는 버튼이에요.<br>옆쪽의 낮은 조작판에서도<br>같은 층을 누를 수 있어요. |
| SRC-051 | ElevatorDialogue.cs:62 | 친구가 건네주는 농구공이에요.<br>튀기지 말고 안고 있어 주세요. |
| SRC-052 | ElevatorDialogue.cs:65 | 연락했으니까<br>조금만 더 기다려 보자. |
| SRC-053 | ElevatorDialogue.cs:65 | 우리 괜찮겠지?<br>밖에 연락해 보자. |
| SRC-054 | ElevatorDialogue.cs:66 | 닫혀 있는 엘리베이터 문이에요.<br>직접 열지 말고<br>구조요원을 기다려 주세요. |
| SRC-055 | ElevatorDialogue.cs:66 | 엘리베이터 문이 닫혀 있어요.<br>억지로 열지 말고<br>비상통화로 도움을 요청하세요. |
| SRC-056 | ElevatorDialogue.cs:68 | 도움을 요청한 상태예요.<br>구조요원의 안내를 기다려 주세요. |
| SRC-057 | ElevatorDialogue.cs:68 | 비상벨을 누르면<br>관리실과 통화할 수 있어요. |
| SRC-058 | ElevatorDialogue.cs:69 | 높은 곳에 있는 비상벨이에요.<br>옆쪽 조작판의 낮은 비상벨을<br>이용할 수 있어요. |
| SRC-059 | ElevatorDialogue.cs:71 | 층 표시기에 3층이라고 나와요.<br>정확히 어디에 멈췄는지는<br>이 표시만으로 알 수 없어요. |
| SRC-060 | ElevatorDialogue.cs:72 | 1층 · 로비와 주출입구<br>3층 · 농구장과 탈의실<br>5층 · 체력단련실 |
| SRC-061 | ElevatorDialogue.cs:73 | 주말 농구 교실 안내예요. |
| SRC-062 | ElevatorDialogue.cs:74 | 밖으로 나오는 걸 도와줄<br>구조요원이에요. |
| SRC-063 | ElevatorDialogue.cs:75 | 119에 연락하는 휴대폰이에요.<br>이 체험에서는 실제로<br>전화가 걸리지는 않아요. |
| SRC-064 | ElevatorDialogue.cs:76 | 1층 출입구예요.<br>친구와 함께 나가세요. |
| SRC-065 | ElevatorDialogue.cs:77 | 주변을 살펴보세요. |
| SRC-066 | ElevatorDoorSafetyCutscene.cs:28 | 친구 |
| SRC-067 | ElevatorDoorSafetyCutscene.cs:31 | 나 |
| SRC-068 | ElevatorDoorSafetyCutscene.cs:34 | 나 |
| SRC-069 | ElevatorFlow.cs:72 | 한빛 생활체육센터 3층에서 농구를 마치고 1층으로 귀가 준비 |
| SRC-070 | ElevatorFlow.cs:75 | 친구의 농구공을 받아 안고 슛 이야기를 들음 |
| SRC-071 | ElevatorFlow.cs:86 | 구조원의 안내를 확인하고 함께 나옴 |
| SRC-072 | ElevatorFlow.cs:90 | 정지한 승강기를 다시 이용하지 않고 직원 안내로 계단을 이용함 |
| SRC-073 | ElevatorFlow.cs:92 | 1층 로비의 주출입구로 귀가 |
| SRC-074 | ElevatorFlow.cs:95 | 낮은 측면 |
| SRC-075 | ElevatorFlow.cs:98 |  패널에서 목적지 1층을 선택함 |
| SRC-076 | ElevatorFlow.cs:124 | 대처: 비상통화 장치를 찾아 도움 요청 |
| SRC-077 | ElevatorFlow.cs:129 | 대처: 휴대폰 119 모의 신고 |
| SRC-078 | ElevatorFlow.cs:135 | 확대한 식별 정보를 확인한 뒤 직접 위치 전달을 선택함 |
| SRC-079 | ElevatorFlow.cs:148 | 벽의 식별판을 직접 찾아 확대 정보로 확인함 · 아직 전달하지 않음 |
| SRC-080 | ElevatorFlow.cs:164 | 문에 기대도 된다고 친구에게 말함 |
| SRC-081 | ElevatorFlow.cs:166 | 문에서 떨어진 옆 벽에서 함께 기다리자고 말함 |
| SRC-082 | ElevatorFlow.cs:177 | 대처: 안내를 듣고 문에서 떨어져 대기 |
| SRC-083 | ElevatorFlow.cs:182 | 친구에게: 도움을 요청했으니 여기서 천천히 숨을 고르며 함께 기다리자. |
| SRC-084 | ElevatorFlow.cs:188 | 첫 시도 기록:  |
| SRC-085 | ElevatorFlow.cs:188 |  → Wait! 중단 후 재도전 |
| SRC-086 | ElevatorFlow.cs:224 | 도입 연출: 친구가 슛을 흉내 내다가 승강기가 멈춤 · 체험자의 선택 결과가 아님 |
| SRC-087 | ElevatorFlow.cs:231 | 친구가 만류를 듣지 않고 슛을 흉내 내며 점프함 · 체험자는 뛰지 않음 |
| SRC-088 | ElevatorFlow.cs:247 | 비상통화: 응답 없음 |
| SRC-089 | ElevatorFlow.cs:247 | 비상통화: 관리실 연결 |
| SRC-090 | ElevatorFlow.cs:279 | 친구가 문에서 떨어진 옆 벽으로 이동하여 안전하게 함께 기다림 |
| SRC-091 | ElevatorFlow.cs:301 | 엘리베이터가 덜컹하며 운행 정지 · 고장 대처 체험으로 진행 |
| SRC-092 | ElevatorFlow.cs:307 | 개발용 단계 이동: 앞 단계 생략 |
| SRC-093 | ElevatorLesson.cs:97 | 문을 억지로 열려고 함 |
| SRC-094 | ElevatorLesson.cs:98 | 스스로 빠져나가려고 함 |
| SRC-095 | ElevatorLesson.cs:140 | 전면 |
| SRC-096 | ElevatorRescueTeam.cs:95 | 소방 구조대원 |
| SRC-097 | ElevatorRescueTeam.cs:96 | 소방 구조대원 |
| SRC-098 | ElevatorRescueTeam.cs:97 | 승강기 담당자 |
| SRC-099 | ElevatorRescueTeam.cs:99 | 소방 구조대원 |
| SRC-100 | ElevatorScenarioDirector.cs:112 | 친구 |
| SRC-101 | ElevatorScenarioDirector.cs:113 | 친구 |
| SRC-102 | ElevatorScenarioDirector.cs:115 | 도움 요청 |
| SRC-103 | ElevatorScenarioDirector.cs:115 | 친구 |
| SRC-104 | ElevatorScenarioDirector.cs:116 | 비상통화 버튼으로<br>밖에 도움을 요청하세요. |
| SRC-105 | ElevatorScenarioDirector.cs:116 | 옆쪽 조작판에서<br>벨 표시가 있는 버튼을 누르세요. |
| SRC-106 | ElevatorScenarioDirector.cs:117 | 친구 |
| SRC-107 | ElevatorScenarioDirector.cs:118 | 구조요원 |
| SRC-108 | ElevatorScenarioDirector.cs:119 | 구조요원 |
| SRC-109 | ElevatorScenarioDirector.cs:120 | 친구 |
| SRC-110 | ElevatorScenarioDirector.cs:121 | 친구 |
| SRC-111 | ElevatorSpatialUI.cs:109 | 문 옆 비상벨 아래의<br>식별판을 눌러 확인하세요. |
| SRC-112 | ElevatorSpatialUI.cs:122 | 완료 |
| SRC-113 | ElevatorSpatialUI.cs:122 | 돌아보기 |
| SRC-114 | ElevatorSpatialUI.cs:125 | 함께 안전하게 나왔어요. |
| SRC-115 | ElevatorSpatialUI.cs:126 | 도움 요청 · 위치 전달<br>안전 대기 · 안내 이동 |
| SRC-116 | ElevatorSpatialUI.cs:173 | 눌러서 모두 보기 |
| SRC-117 | ElevatorSpatialUI.cs:173 | 눌러서 다음 글 보기 |
| SRC-118 | ElevatorSpatialUI.cs:222 | 요.  |
| SRC-119 | ElevatorSpatialUI.cs:222 | 요.<br> |
| SRC-120 | ElevatorSpatialUI.cs:271 | 01  도움을 요청했어요 |
| SRC-121 | ElevatorSpatialUI.cs:271 | 02  위치를 전달했어요 |
| SRC-122 | ElevatorSpatialUI.cs:271 | 03  문에서 떨어져 기다렸어요 |
| SRC-123 | ElevatorSpatialUI.cs:271 | 04  안내에 따라 이동했어요 |
| SRC-124 | ElevatorSpatialUI.cs:273 | 엘리베이터가 멈췄다고 알리고,<br>친구와 둘이 있다고 말했어요. |
| SRC-125 | ElevatorSpatialUI.cs:274 | 식별판에서 건물 이름과<br>승강기 번호를 찾아 알려 줬어요. |
| SRC-126 | ElevatorSpatialUI.cs:275 | 문에서 떨어진 옆 벽에서<br>친구와 함께 기다렸어요. |
| SRC-127 | ElevatorSpatialUI.cs:276 | 안전한지 확인한 뒤,<br>구조요원을 따라 나왔어요. |
| SRC-128 | ElevatorSpatialUI.cs:319 | 함께한 일 보기 |
| SRC-129 | ElevatorSpatialUI.cs:319 | 다시 해보기 |
| SRC-130 | ElevatorSpatialUI.cs:402 | WASD 이동 · 마우스 둘러보기<br>중앙 조준 후 클릭 · Esc 일시정지 |
| SRC-131 | ElevatorSpatialUI.cs:402 | WASD 이동  ·  마우스로 둘러보기  ·  중앙 조준 후 클릭  ·  Esc 일시정지 |
| SRC-132 | ElevatorSpatialUI.cs:546 | 승강기 정보를 찾았어요 |
| SRC-133 | ElevatorSpatialUI.cs:546 | ‘위치 알려주기’를 눌러<br>통화로 전달해 주세요. |
| SRC-134 | ElevatorSpatialUI.cs:547 | 119의 안내예요 |
| SRC-135 | ElevatorSpatialUI.cs:547 | 관리실의 안내예요 |
| SRC-136 | ElevatorSpatialUI.cs:547 | 안내를 다 읽었으면<br>카드의 버튼을 눌러 주세요. |
| SRC-137 | ElevatorSpatialUI.cs:551 | 엘리베이터가 멈췄어요 |
| SRC-138 | ElevatorSpatialUI.cs:551 | 비상통화 버튼을<br>눌러 주세요. |
| SRC-139 | ElevatorSpatialUI.cs:553 | 비상통화에 응답이 없어요 |
| SRC-140 | ElevatorSpatialUI.cs:553 | 휴대폰으로<br>119에 연락해 주세요. |
| SRC-141 | ElevatorSpatialUI.cs:555 | 어느 승강기인지 알려 주세요 |
| SRC-142 | ElevatorSpatialUI.cs:555 | 문 옆 식별판을 눌러<br>적힌 내용을 확인하세요. |
| SRC-143 | ElevatorSpatialUI.cs:557 | 친구가 문에 기대려고 해요 |
| SRC-144 | ElevatorSpatialUI.cs:557 | 친구의 이야기를<br>잠시 들어 주세요. |
| SRC-145 | ElevatorSpatialUI.cs:559 | 친구가 문에 기대려고 해요 |
| SRC-146 | ElevatorSpatialUI.cs:560 | 어떻게 대답할지<br>대화 카드에서 골라 주세요. |
| SRC-147 | ElevatorSpatialUI.cs:560 | 살펴본 뒤 친구에게<br>다시 말을 걸어 주세요. |
| SRC-148 | ElevatorSpatialUI.cs:560 | 친구를 눌러<br>말을 걸어 주세요. |
| SRC-149 | ElevatorSpatialUI.cs:562 | 친구가 문에서 떨어지고 있어요 |
| SRC-150 | ElevatorSpatialUI.cs:562 | 옆 벽으로 이동 중이에요.<br>잠시만 기다려 주세요. |
| SRC-151 | ElevatorSpatialUI.cs:564 | 문에서 떨어져 함께 기다려요 |
| SRC-152 | ElevatorSpatialUI.cs:564 | 구조요원이 올 때까지<br>여기서 함께 기다려요. |
| SRC-153 | ElevatorSpatialUI.cs:566 | 안전 확인이 끝났어요 |
| SRC-154 | ElevatorSpatialUI.cs:566 | 구조대가 도착했어요 |
| SRC-155 | ElevatorSpatialUI.cs:567 | 구조요원을 눌러<br>함께 밖으로 나가세요. |
| SRC-156 | ElevatorSpatialUI.cs:567 | 밖으로 나가도 안전한지<br>확인할 때까지 기다리세요. |
| SRC-157 | ElevatorSpatialUI.cs:569 | 구조요원과 함께 나가고 있어요 |
| SRC-158 | ElevatorSpatialUI.cs:569 | 안내를 따라<br>천천히 이동하세요. |
| SRC-159 | ElevatorStorybookUI.cs:171 | 친구 |
| SRC-160 | ElevatorStorybookUI.cs:172 | 관리실 |
| SRC-161 | ElevatorThoughtHeaderPolicy.cs:26 | 나의생각 |
| SRC-162 | ElevatorThoughtHeaderPolicy.cs:26 | 나의생각·힌트 |
| SRC-163 | ElevatorThoughtHeaderPolicy.cs:26 | 살펴보니 |
| SRC-164 | ElevatorView.cs:50 | 멈춰도 함께, 안전하게 |
| SRC-165 | ElevatorView.cs:55 | 마우스로 둘러보기 · 중앙을 맞추고 클릭 · R 다시 시작 |
| SRC-166 | ElevatorView.cs:63 | 개발 도구 · F8 닫기 |
| SRC-167 | ElevatorView.cs:64 | 로비 |
| SRC-168 | ElevatorView.cs:64 | 공 받기 |
| SRC-169 | ElevatorView.cs:64 | 슛 재현 |
| SRC-170 | ElevatorView.cs:64 | 정지 |
| SRC-171 | ElevatorView.cs:64 | 통화 |
| SRC-172 | ElevatorView.cs:64 | 함께 대기 |
| SRC-173 | ElevatorView.cs:64 | 구조 안내 |
| SRC-174 | ElevatorView.cs:66 | 비상통화 응답 / 무응답 |
| SRC-175 | ElevatorView.cs:73 | 멈춰도 함께, 안전하게 |
| SRC-176 | ElevatorView.cs:74 | 짧은 도입 연출 |
| SRC-177 | ElevatorView.cs:75 | 친구에게 말 걸기 |
| SRC-178 | ElevatorView.cs:146 | 살펴보기 |
| SRC-179 | ElevatorView.cs:163 | 친구 |
| SRC-180 | ElevatorView.cs:166 | 친구와 함께 타기 |
| SRC-181 | ElevatorView.cs:166 | 안내 |
| SRC-182 | ElevatorView.cs:166 | 친구와 함께 엘리베이터에 타고 있어요. |
| SRC-183 | ElevatorView.cs:167 | 구조요원을 따라 나가기 |
| SRC-184 | ElevatorView.cs:167 | 안내 |
| SRC-185 | ElevatorView.cs:167 | 구조요원과 함께 밖으로 나가고 있어요. |
| SRC-186 | ElevatorView.cs:169 | 엘리베이터 부르기 |
| SRC-187 | ElevatorView.cs:169 | 1   엘리베이터 타기 |
| SRC-188 | ElevatorView.cs:171 | 1층 버튼 누르기 |
| SRC-189 | ElevatorView.cs:171 | 1   1층 누르기 |
| SRC-190 | ElevatorView.cs:172 | 친구의 공 받기 |
| SRC-191 | ElevatorView.cs:172 | 1   공 받기 |
| SRC-192 | ElevatorView.cs:174 | 친구의 슛 이야기 |
| SRC-193 | ElevatorView.cs:176 | 친구에게 말 걸기 |
| SRC-194 | ElevatorView.cs:176 | 안내 |
| SRC-195 | ElevatorView.cs:176 | 친구가 뛰려고 해요.<br>주변의 안전 안내를 살펴보세요. |
| SRC-196 | ElevatorView.cs:178 | 친구에게 알려 주기 |
| SRC-197 | ElevatorView.cs:178 | 나 |
| SRC-198 | ElevatorView.cs:178 | 여기서는 뛰면 위험해.<br>슛은 밖에서 하자. |
| SRC-199 | ElevatorView.cs:180 | 1층으로 내려가는 중 |
| SRC-200 | ElevatorView.cs:180 | 알았어. 밖에서 하자! |
| SRC-201 | ElevatorView.cs:182 | 친구가 슛 동작을 흉내 내요 |
| SRC-202 | ElevatorView.cs:184 | 비상통화 버튼을<br>눌러 주세요. |
| SRC-203 | ElevatorView.cs:186 | 도움 요청 |
| SRC-204 | ElevatorView.cs:186 | 비상통화 버튼으로<br>밖에 도움을 요청하세요. |
| SRC-205 | ElevatorView.cs:186 | 옆쪽 조작판에서<br>벨 표시가 있는 버튼을 누르세요. |
| SRC-206 | ElevatorView.cs:189 | 비상통화 연결 중 |
| SRC-207 | ElevatorView.cs:189 | 안내 |
| SRC-208 | ElevatorView.cs:189 | 관리실에 연결 중입니다.<br>잠시만 기다려 주세요. |
| SRC-209 | ElevatorView.cs:191 | 휴대폰으로<br>119에 연락해 주세요. |
| SRC-210 | ElevatorView.cs:191 | 안내 |
| SRC-211 | ElevatorView.cs:191 | 비상통화가 연결되지 않아요.<br>휴대폰으로 119에 연락하세요.<br>실제 전화가 아닌 체험 통화예요. |
| SRC-212 | ElevatorView.cs:191 | 1   119에 연락하기 · 체험 |
| SRC-213 | ElevatorView.cs:193 | 무슨 일이 생겼는지 알리기 |
| SRC-214 | ElevatorView.cs:195 | 멈춘 상황과 인원 알리기 |
| SRC-215 | ElevatorView.cs:195 | 나 |
| SRC-216 | ElevatorView.cs:197 | 승강기 번호 확인하기 |
| SRC-217 | ElevatorView.cs:199 | 식별판 위치 확인하기 |
| SRC-218 | ElevatorView.cs:199 | 식별판 확인하기 |
| SRC-219 | ElevatorView.cs:201 | 안전 안내 확인하기 |
| SRC-220 | ElevatorView.cs:201 | 알겠어요 |
| SRC-221 | ElevatorView.cs:204 | 식별판을 눌러<br>내용을 확인하세요. |
| SRC-222 | ElevatorView.cs:204 | 식별판 찾기 |
| SRC-223 | ElevatorView.cs:204 | 문 옆 비상벨 아래의<br>식별판을 눌러 확인하세요. |
| SRC-224 | ElevatorView.cs:205 | 승강기 위치 알리기 |
| SRC-225 | ElevatorView.cs:205 | 승강기 식별판 |
| SRC-226 | ElevatorView.cs:205 | 위치 알려주기 |
| SRC-227 | ElevatorView.cs:205 | 다시 살펴보기 |
| SRC-228 | ElevatorView.cs:206 | 안전 안내 확인하기 |
| SRC-229 | ElevatorView.cs:206 | 알겠어요 |
| SRC-230 | ElevatorView.cs:208 | 친구를 눌러<br>말을 걸어 주세요. |
| SRC-231 | ElevatorView.cs:208 | 친구에게 말 걸기 |
| SRC-232 | ElevatorView.cs:209 | 친구가 문에 기대려고 해요.<br>친구를 눌러 말을 걸어 주세요. |
| SRC-233 | ElevatorView.cs:212 | 친구와 함께 기다리기 |
| SRC-234 | ElevatorView.cs:214 | 고마워. 이제 좀 안심이 돼. |
| SRC-235 | ElevatorView.cs:214 | 헤헤, 너랑 하니까 더 재밌었어. |
| SRC-236 | ElevatorView.cs:214 | 좋아! 다음엔 패스 연습도 하자. |
| SRC-237 | ElevatorView.cs:214 | 응, 나도 네 옆에 있을게. |
| SRC-238 | ElevatorView.cs:214 | 1   오늘 농구 재밌었지? |
| SRC-239 | ElevatorView.cs:214 | 1   다음에 또 같이 하자. |
| SRC-240 | ElevatorView.cs:214 | 1   조금만 더 같이 기다리자. |
| SRC-241 | ElevatorView.cs:216 | 친구와 함께 기다리기 |
| SRC-242 | ElevatorView.cs:216 | 친구 |
| SRC-243 | ElevatorView.cs:216 | 이제 여기서 기다리면 되겠지? |
| SRC-244 | ElevatorView.cs:218 | 기다릴 자리 다시 생각하기 |
| SRC-245 | ElevatorView.cs:218 | 잠깐! |
| SRC-246 | ElevatorView.cs:218 | 다시 살펴보기 |
| SRC-247 | ElevatorView.cs:220 | 구조요원 안내 기다리기 |
| SRC-248 | ElevatorView.cs:220 | 구조요원 |
| SRC-249 | ElevatorView.cs:220 | 1   구조요원 따라 나가기 |
| SRC-250 | ElevatorView.cs:222 | 계단으로 1층 내려가기 |
| SRC-251 | ElevatorView.cs:222 | 구조요원 |
| SRC-252 | ElevatorView.cs:222 | 1   계단으로 내려가기 |
| SRC-253 | ElevatorView.cs:223 | 계단으로 내려가는 중 |
| SRC-254 | ElevatorView.cs:223 | 안내 |
| SRC-255 | ElevatorView.cs:223 | 구조요원과 함께 계단으로 내려가고 있어요. |
| SRC-256 | ElevatorView.cs:225 | 친구와 밖으로 나가기 |
| SRC-257 | ElevatorView.cs:225 | 1   친구와 밖으로 나가기 |
| SRC-258 | ElevatorView.cs:226 | 친구와 집으로 |
| SRC-259 | ElevatorView.cs:226 | 안내 |
| SRC-260 | ElevatorView.cs:226 | 친구와 함께 집으로 돌아가요. |
| SRC-261 | ElevatorView.cs:228 | 함께 안전하게 나왔어요 |
| SRC-262 | ElevatorView.cs:228 | 친구 |
| SRC-263 | ElevatorView.cs:228 | 1   우리가 함께한 일 |
| SRC-264 | ElevatorView.cs:228 | 2   다시 해보기 |
| SRC-265 | ElevatorView.cs:231 | 현재 위치를 알리고 안내 듣기 |
| SRC-266 | ElevatorView.cs:231 | 문에서 떨어져 안전하게 기다리기 |
| SRC-267 | ElevatorView.cs:237 | 친구에게 할 말 |
| SRC-268 | ElevatorView.cs:237 | 살펴보기 |
| SRC-269 | ElevatorView.cs:244 | 조금 더 살펴보기 |
| SRC-270 | ElevatorView.cs:244 | 닫기 |
| SRC-271 | ElevatorView.cs:247 | &lt;color=#297A70&gt;● 도움 요청&lt;/color&gt; |
| SRC-272 | ElevatorView.cs:247 | ○ 도움 요청 |
| SRC-273 | ElevatorView.cs:247 | &lt;color=#297A70&gt;● 위치 전달&lt;/color&gt; |
| SRC-274 | ElevatorView.cs:247 | ○ 위치 전달 |
| SRC-275 | ElevatorView.cs:247 | &lt;color=#297A70&gt;● 함께 대기&lt;/color&gt; |
| SRC-276 | ElevatorView.cs:247 | ○ 함께 대기 |
| SRC-277 | ElevatorView.cs:283 | 살펴본 물체 |
| SRC-278 | ElevatorView.cs:288 | 안전 포스터 |
| SRC-279 | ElevatorView.cs:289 | 엘리베이터 안에서는<br>뛰거나 공을 튀기지 마세요. |
| SRC-280 | ElevatorView.cs:292 | 닫힌 출입문 |
| SRC-281 | ElevatorView.cs:295 | 친구 |
| SRC-282 | ElevatorView.cs:298 | 비상통화 장치 |
| SRC-283 | ElevatorView.cs:299 | 이 장치로 도움 요청하기 |
| SRC-284 | ElevatorView.cs:301 | 휴대폰 |
| SRC-285 | ElevatorView.cs:302 | 휴대폰으로 119 모의 신고 |
| SRC-286 | ElevatorView.cs:304 | 층 버튼 |
| SRC-287 | ElevatorView.cs:305 | 층 표시기 |
| SRC-288 | ElevatorView.cs:306 | 승강기 식별판 |
| SRC-289 | ElevatorView.cs:307 | 전면 비상벨 |
| SRC-290 | ElevatorView.cs:308 | 층별 시설 안내 |
| SRC-291 | ElevatorView.cs:309 | 농구 교실 안내 |
| SRC-292 | ElevatorView.cs:315 | 도움 요청 성공!  비상통화로 우리 상황을 알렸어요. |
| SRC-293 | ElevatorView.cs:315 | 위치 전달 성공!  벽의 표시에서 건물명과 승강기 번호를 찾았어요. |
| SRC-294 | ElevatorView.cs:315 | 함께 대기 성공!  놀란 친구를 안심시키고 안내를 기다렸어요. |
| SRC-295 | ElevatorView.cs:316 | 우리가 해낸 일 ·  |
