프로젝트 카툰 UI — 2026-11-02

팀 공유용 프리팹: Assets/Shared Assets/UI/Prefabs/
공유본은 이미지·폰트·셰이더를 Shared Assets/UI 안에서 참조합니다.
쉬운 사용법: Assets/Shared Assets/사용방법_UI와강조.txt
통합 예제: Assets/Shared Assets/Examples/SharedAssets_Showcase.unity

적용한 게임 씬
- Assets/M/Scene/Elevator_Safety_Prototype.unity
- Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity

두 씬의 대화, 화자 이름표, 관찰 메모, 선택·다음 버튼, 일시정지 메뉴와 완료 기록에 적용했습니다.
게임 캐릭터와 어울리도록 갈색 외곽선, 크림·청록·주황 색면, 단순한 셀 명암을 사용합니다.
종이·천의 세밀한 질감을 사용했던 앞선 버전은 Art 폴더에 보관하며 현재 씬은 Art/Cartoon 이미지를 참조합니다.

현재 사용하는 이미지
Assets/M/UI/Storybook/Art/Cartoon/
DialogueFrame, ThoughtCloud, ButtonTeal, ButtonCream, SpeakerRibbon,
ThoughtRibbon, ObservationNote, FriendPortrait, SpeechTail — 투명 PNG 9종.
그림은 내장 이미지 생성 도구로 새로 생성했습니다.
프레임과 버튼은 Sprite의 9-slice 테두리를 지정해 크기가 달라져도 모서리를 유지합니다.
말풍선 꼬리·초상·이름표는 별도 이미지입니다.

재사용 프리팹
Assets/M/UI/Storybook/Prefabs/
DialogueCard, IntercomDialogueCard, ThoughtCloud, ObservationCard, ChoiceCard,
MissionCard, CompletionCard, PausePanel, NextButton — World Space Canvas를 포함한 9종.
프리팹 자체는 화면 디자인 템플릿입니다. 별도 씬에서 사용할 경우 카메라, EventSystem과 버튼 동작을 연결하세요.
현재 엘리베이터 씬은 기존 시나리오의 진행·선택·일시정지 동작에 이미 연결되어 있습니다.

미리보기
Assets/M/UI/Storybook/Scenes/StorybookUI_Showcase.unity
이 씬을 열어 Play하면 UI 구성 요소를 한눈에 비교할 수 있습니다.

편집 방법
- 문구는 TextMeshPro 텍스트이며 이미지에 포함하지 않았습니다.
- 기존 대사와 화자·선택지는 ElevatorView와 시나리오 설정을 그대로 사용합니다.
- ElevatorSpatialUI의 Storybook 참조가 이미지 테마를 연결합니다.
- ElevatorStorybookUI의 Sprite 필드에서 배경 이미지 세트를 바꿀 수 있습니다.
- StorybookButtonFeedback은 마우스와 XR 포인터의 가리킴·누름에 맞춰 버튼을 올리거나 누릅니다.
- 관찰 메모는 화면 오른쪽에 표시하고 대화·선택지와 구분합니다.

작업 자료
Review/UIDesign_20261102/Cartoon_Concept.png — 카툰 방향 시안
Review/UIDesign_20261102/CartoonPrompts.txt — 내장 이미지 생성 프롬프트
Review/UIDesign_20261102/Cartoon_*.png — Unity 실행 화면
Review/UIDesign_20261102/verify-cartoon-*.json — UI 클릭·배치 검사 기록

XR용 Canvas와 포인터 구성은 유지했습니다. 실제 HMD 착용 검증과 기기 빌드는 별도입니다.
