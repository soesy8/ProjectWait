using ProjectWait.M.Shared;
using UnityEngine;

namespace ProjectWait.M.Elevator.DialoguePreview
{
    public sealed class ElevatorUIFocusWindow : DialogueFocusWindow
    {
        public ElevatorSpatialUI owner;
        void Start(){Refresh();}

        public void Refresh()
        {
            if(owner&&owner.view)Bind(owner.view.playerCamera,owner.view.dialoguePanel,owner.view.dialogueText,owner.speaker);
        }

        public void Advance(float delta)
        {
            if(owner&&owner.view)
                UpdateFocus(owner.view.dialoguePanel.gameObject.activeInHierarchy&&!owner.view.lesson.CinematicBlocking&&
                    owner.view.lesson.Flow?.Step!=ElevatorStep.Complete,delta);
        }
    }
}
