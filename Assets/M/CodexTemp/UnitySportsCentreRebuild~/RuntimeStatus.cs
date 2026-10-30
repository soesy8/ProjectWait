var l=UnityEngine.Object.FindFirstObjectByType<ProjectWait.M.Elevator.ElevatorLesson>();
var c=l.playerCamera.transform;
return Newtonsoft.Json.JsonConvert.SerializeObject(new{playing=UnityEngine.Application.isPlaying,frame=UnityEngine.Time.frameCount,state=l.Snapshot(),camera=new[]{c.position.x,c.position.y,c.position.z},heading=c.eulerAngles.y,choices=l.view.ChoiceButtons.Count,thought=l.Thought,dialogue=l.view.dialogueText.text});
