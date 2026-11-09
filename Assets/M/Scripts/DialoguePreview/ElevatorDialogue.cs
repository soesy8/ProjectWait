namespace ProjectWait.M.Elevator.DialoguePreview
{
    // Spoken lines shared by world cards and cinematic subtitles. Character speech
    // never describes clicking/selecting; those instructions belong to the HUD.
    public static class ElevatorDialogue
    {
        public const string Lobby="이제 1층으로 내려가자.";
        public const string CallLift="내려가는 버튼 눌러 줄래?";
        public const string ChooseFloor="1층 버튼 눌러 줄래?";
        public const string Handoff="고마워!\n공 좀 잠깐 받아 줄래?";
        public const string ShotStory="아까 그 슛 봤지?";
        public const string ShotGesture="이렇게 던졌잖아!";
        public const string Stopped="어? 왜 멈췄지?";
        public const string SituationReport="엘리베이터가 멈췄어요.\n친구랑 둘이 안에 있어요.";
        public const string LocationQuestion="건물 이름과 엘리베이터 번호를\n알려 주세요.";
        public const string IdentityGuide="문 옆에 건물 이름과\n엘리베이터 번호가 적힌\n안내판이 있어요.";
        public const string IdentityPanelHint="문 옆 조작판의\n비상벨 아래를 살펴보세요.";
        public const string SafetyAdvice="네, 위치 확인했어요.\n구조요원이 갈 테니\n문에서 떨어져 기다려 주세요.";
        public const string FriendQuestion="아직 좀 무서워…\n문에 잠깐 기대도 될까?";
        public const string SafeReply="문에 기대면 위험해.\n옆 벽에서 같이 기다리자.";
        public const string UnsafeReply="문에 기대서 잠깐 쉬자.";
        public const string DoorWarning=SafeReply;
        public const string FriendMoving="응, 그쪽으로 갈게.";
        public const string FriendSettled="이제 좀 괜찮아졌어.\n옆에 있어 줘서 고마워.";
        public const string FriendWaiting="아직 좀 무서워…\n옆에 있어 줄래?";
        public const string Reassure="응, 내가 옆에 있을게. 같이 기다리자.";
        public const string RescueArrival="구조대입니다.\n문에서 떨어져 기다려 주세요.";
        public const string RescueChecking="문 앞 바닥을 확인 중이에요.\n안전하게 나올 수 있게 도와드릴게요.";
        public const string RescueReady="이제 천천히 나와 주세요.\n저희가 도와드릴게요.";
        public const string Stairs="엘리베이터는 점검해야 해요.\n계단으로 1층까지 안내할게요.";
        public const string GroundFloor="휴, 이제 좀 안심이 된다.\n우리 집에 가자.";
        public const string Complete="같이 있어 줘서 고마워.\n다음엔 코트에서 또 놀자!";
        public const string RetryDoor="문에 기대면 다칠 수 있어요.\n문에서 떨어져 기다려 주세요.";
        public const string RetryExit="문을 억지로 열면 위험해요.\n밖에 도움을 요청하고\n구조요원을 기다려 주세요.";
        public const string SafetyNotice="안에서는 뛰지 마세요.\n문에 기대지 마세요.\n문을 억지로 열지 마세요.\n구조요원의 안내를 기다리세요.";

        public static string Caller(bool phone)=>phone?"119 · 모의 통화":"관리실";
        public static string Greeting(bool phone)=>phone?"119입니다. 무슨 일이세요?":"관리실입니다. 무슨 일이세요?";
        public static string LocationReport(ElevatorSettings settings)
        {
            // The plate contains a descriptive prefix and an identifier. Read the
            // identifier naturally rather than attaching a location particle to it.
            string id=settings.liftId??"";int separator=id.LastIndexOf('·');
            if(separator>=0)id=id.Substring(separator+1).Trim();
            return settings.building+"예요.\n엘리베이터 번호는 "+id+"이에요.";
        }
        public static string IdentitySearchHint(int level)=>level>=2?"문 옆 조작판의\n비상벨 아래를 살펴보세요.":
            level==1?"문 옆 조작판을\n살펴보세요.":"건물 이름과 번호가 적힌\n안내판을 찾아보세요.";
        public static string HelpHint(int level)=>level>=2?"옆쪽 조작판의\n벨 표시 버튼을 누르세요.":
            level==1?"비상통화 버튼을\n찾아보세요.":"밖에 도움을\n요청해 보세요.";
        public static string Caption(string speaker,string line)=>speaker+"  ·  "+line.Replace("\n"," ");
        public static string WithoutShortcut(string label)
        {
            // Remove only the UI shortcut prefix, never the actual "1층" or "119".
            return label.Length>=4&&label[0]>='1'&&label[0]<='4'&&label[1]==' '&&label[2]==' '&&label[3]==' '
                ?label.Substring(4).TrimStart():label;
        }

        public static string DescribeObject(ElevatorLesson lesson,ElevatorClue clue)
        {
            var f=lesson.Flow;
            switch(clue)
            {
                case ElevatorClue.LobbyCall:return "아래로 내려가는 엘리베이터를\n부르는 버튼이에요.";
                case ElevatorClue.FloorButtons:case ElevatorClue.FrontFloorButtons:
                    return f.Step==ElevatorStep.Stopped?"층을 고르는 버튼이에요.\n지금은 눌러도 움직이지 않아요.\n비상통화로 도움을 요청하세요.":"가려는 층을 고르는 버튼이에요.\n옆쪽의 낮은 조작판에서도\n같은 층을 누를 수 있어요.";
                case ElevatorClue.Ball:return "친구가 건네주는 농구공이에요.\n튀기지 말고 안고 있어 주세요.";
                case ElevatorClue.Friend:
                    return f.Step==ElevatorStep.DoorPosterPointing?FriendQuestion:
                        f.Step==ElevatorStep.Boarding?Handoff:f.HelpRequested?"연락했으니까\n조금만 더 기다려 보자.":"우리 괜찮겠지?\n밖에 연락해 보자.";
                case ElevatorClue.Door:return f.HelpRequested?"닫혀 있는 엘리베이터 문이에요.\n직접 열지 말고\n구조요원을 기다려 주세요.":"엘리베이터 문이 닫혀 있어요.\n억지로 열지 말고\n비상통화로 도움을 요청하세요.";
                case ElevatorClue.Notice:return SafetyNotice;
                case ElevatorClue.Intercom:return f.SearchingIdentity?IdentityPanelHint:f.HelpRequested?"도움을 요청한 상태예요.\n구조요원의 안내를 기다려 주세요.":"비상벨을 누르면\n관리실과 통화할 수 있어요.";
                case ElevatorClue.FrontIntercom:return "높은 곳에 있는 비상벨이에요.\n옆쪽 조작판의 낮은 비상벨을\n이용할 수 있어요.";
                case ElevatorClue.Identity:return lesson.settings.building+"\n"+lesson.settings.liftId;
                case ElevatorClue.Display:return "층 표시기에 3층이라고 나와요.\n정확히 어디에 멈췄는지는\n이 표시만으로 알 수 없어요.";
                case ElevatorClue.FacilityGuide:return "1층 · 로비와 주출입구\n3층 · 농구장과 탈의실\n5층 · 체력단련실";
                case ElevatorClue.Advertisement:return "주말 농구 교실 안내예요.";
                case ElevatorClue.Rescuer:return "밖으로 나오는 걸 도와줄\n구조요원이에요.";
                case ElevatorClue.Phone:return "119에 연락하는 휴대폰이에요.\n이 체험에서는 실제로\n전화가 걸리지는 않아요.";
                case ElevatorClue.GroundExit:return "1층 출입구예요.\n친구와 함께 나가세요.";
                default:return "주변을 살펴보세요.";
            }
        }
    }
}
