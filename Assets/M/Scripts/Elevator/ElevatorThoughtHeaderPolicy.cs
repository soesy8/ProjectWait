using TMPro;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ProjectWait.M.Elevator
{
    // Thought, mission hints and observations all use the same body-only bubble.
    // Limit cleanup to that bubble: speaker tabs and the pause heading are unrelated.
    public static class ElevatorThoughtHeaderPolicy
    {
        sealed class HeaderCache
        {
            public HeaderCache(){}
            public int hierarchyCount=-1;
            public readonly List<Transform> descendants=new List<Transform>();
            public readonly List<TMP_Text> texts=new List<TMP_Text>();
            public readonly List<GameObject> objects=new List<GameObject>();
        }
        static readonly ConditionalWeakTable<RectTransform,HeaderCache> caches=new ConditionalWeakTable<RectTransform,HeaderCache>();
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
            var cache=caches.GetOrCreateValue(panel);
            if(cache.hierarchyCount!=panel.hierarchyCount)
            {
                cache.descendants.Clear();cache.texts.Clear();cache.objects.Clear();
                panel.GetComponentsInChildren(true,cache.descendants);
                foreach(var child in cache.descendants)
                {
                    if(child==panel)continue;
                    var text=child.GetComponent<TMP_Text>();
                    if(child.name!="ThoughtRibbon"&&!IsHeader(text))continue;
                    cache.objects.Add(child.gameObject);if(text)cache.texts.Add(text);
                }
                cache.hierarchyCount=panel.hierarchyCount;
            }
            foreach(var text in cache.texts)if(text){if(text.text!="")text.text="";text.raycastTarget=false;}
            foreach(var child in cache.objects)if(child&&child.activeSelf)child.SetActive(false);
        }
    }
}
