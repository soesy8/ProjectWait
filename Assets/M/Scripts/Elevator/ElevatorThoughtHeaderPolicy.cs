using TMPro;
using UnityEngine;

namespace ProjectWait.M.Elevator
{
    // Thought, mission hints and observations all use the same body-only bubble.
    // Limit cleanup to that bubble: speaker tabs and the pause heading are unrelated.
    public static class ElevatorThoughtHeaderPolicy
    {
        public static bool IsTitle(string value)
        {
            if(string.IsNullOrEmpty(value))return false;
            string title=value.Replace(" ","").Replace("\t","").Replace("\r","").Replace("\n","").TrimEnd('.', '…');
            return title=="나의생각"||title=="나의생각·힌트"||title=="살펴보니";
        }

        public static bool IsHeader(TMP_Text text)
        {
            return text&&(text.name=="Heading"||text.name=="ThoughtHeading"||IsTitle(text.text));
        }

        public static void Hide(RectTransform panel)
        {
            if(!panel)return;
            foreach(var child in panel.GetComponentsInChildren<Transform>(true))
            {
                if(child==panel)continue;
                var text=child.GetComponent<TMP_Text>();
                if(child.name!="ThoughtRibbon"&&!IsHeader(text))continue;
                if(text){text.text="";text.raycastTarget=false;}
                child.gameObject.SetActive(false);
            }
        }
    }
}