using UnityEngine;

namespace ProjectWait.M.Elevator
{
    [CreateAssetMenu(menuName = "Wait/Elevator lesson settings")]
    public sealed class ElevatorSettings : ScriptableObject
    {
        [Header("No player time limit; only the friend performs the jump")]
        [Min(.1f)] public float friendReplySeconds = 1.2f;
        [Min(.1f)] public float jumpSeconds = .85f;
        [Min(.1f)] public float afterJumpSeconds = 1.1f;
        [Min(.1f)] public float connectionSeconds = 3;
        [Min(1)] public float firstHintSeconds = 20;
        [Min(1)] public float secondHintSeconds = 40;
        [Min(1)] public float rescueWaitSeconds = 12;
        [Min(1)] public float rescueSubtitleSeconds = 4;
        [Min(5)] public float boardingSeconds = 6.2f;
        [Min(3)] public float exitSeconds = 4.6f;
        public string building = "햇살아파트";
        public string liftId = "교육용 M-01";
        [TextArea] public string lobbyDialogue = "아까 내 슛 봤어?\n처음으로 그렇게 멀리서 넣었어!";
        [TextArea] public string handoffDialogue = "공 좀 잠깐 맡아 줘.\n내가 어떻게 넣었냐면…";
        [TextArea] public string proposal = "이렇게 뛰어서!";
        [TextArea] public string stoppedDialogue = "어? 왜 멈췄지?";
        [TextArea] public string reportDialogue = "엘리베이터가 멈췄어요. 친구랑 둘이 있어요.";
        [TextArea] public string waitDialogue = "문은 건드리지 말고 같이 기다리자.";
        [TextArea] public string doorWarning = "문을 억지로 열거나 혼자 나오려고 하지 않아요. 문에서 떨어져 도움을 요청하고 안내를 기다려요.";
        [TextArea] public string safetyNotice = "안전하게 함께 타요\n\n안에서 뛰지 않아요.\n문에 기대지 않아요.\n\n멈추면 비상통화로 도움 요청.\n연결이 안 되면 휴대폰으로 119.\n문을 건드리지 않고 안내를 기다려요.";
    }
}
