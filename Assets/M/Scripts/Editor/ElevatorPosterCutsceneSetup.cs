#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DwtD.TestScene;
using ProjectWait.M.Elevator;
using Object=UnityEngine.Object;

namespace ProjectWait.M.Editor
{
    public static class ElevatorPosterCutsceneSetup
    {
        static readonly string[] Scenes={"Assets/M/Scene/Elevator_Safety_Prototype.unity","Assets/M/Scene/Elevator_Safety_ObjectInteraction_Test.unity"};
        static T Ensure<T>(GameObject go) where T:Component
        {var existing=go.GetComponent<T>();return existing?existing:go.AddComponent<T>();}
        static RectTransform Rect(Transform parent,string name,Vector2 size)
        {
            var child=parent.Find(name) as RectTransform;
            if(!child){child=(RectTransform)new GameObject(name,typeof(RectTransform)).transform;child.SetParent(parent,false);}
            child.anchorMin=child.anchorMax=child.pivot=Vector2.one*.5f;
            child.sizeDelta=size;child.anchoredPosition=Vector2.zero;return child;
        }
        static void Configure(ElevatorLesson lesson)
        {
            if(!lesson.view.spatial||!lesson.presentation.space.overlay||!lesson.scenario)throw new Exception("Existing cinematic presentation missing");
            var poster=lesson.CollectObservations().Single(o=>o.name=="3F_SafetyPoster");
            var surface=poster.GetComponent<Collider>();
            if(!surface||!surface.enabled||!surface.gameObject.activeInHierarchy)throw new Exception("Safety poster collider unavailable");
            var guide=Ensure<ElevatorPosterCutscene>(lesson.gameObject);guide.lesson=lesson;lesson.posterCutscene=guide;
            var anchor=poster.transform.Find("PointingAnchor");
            if(!anchor){anchor=new GameObject("PointingAnchor").transform;anchor.SetParent(poster.transform,false);}
            anchor.position=surface.bounds.center+Vector3.right*.10f;
            guide.posterAnchor=anchor;guide.posterSurface=surface;
            guide.holdSeconds=guide.pulseSeconds=1.5f;guide.pulseGrowth=.20f;guide.targetRadius=.19f;

            var marker=Rect(lesson.transform,"SafetyPosterPointRing",new Vector2(128,128));
            var world=Ensure<Canvas>(marker.gameObject);world.renderMode=RenderMode.WorldSpace;world.worldCamera=lesson.playerCamera;world.sortingOrder=210;
            marker.position=anchor.position;marker.rotation=Quaternion.Euler(0,-90,0);marker.localScale=Vector3.one*.003125f;
            Ensure<CanvasRenderer>(marker.gameObject);
            var ring=Ensure<ElevatorPosterCueGraphic>(marker.gameObject);ring.ring=true;ring.progress=0;ring.raycastTarget=false;
            ring.material=lesson.scenario.worldOverlay;ring.color=Color.white;
            guide.worldCanvas=world;guide.worldRing=marker;guide.ringGraphic=ring;
            guide.ringGroup=Ensure<CanvasGroup>(marker.gameObject);guide.ringGroup.alpha=0;guide.ringGroup.interactable=guide.ringGroup.blocksRaycasts=false;

            var screen=Rect(lesson.transform,"PosterScreenGuide",new Vector2(1280,720));
            var canvas=Ensure<Canvas>(screen.gameObject);canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=210;
            var scaler=Ensure<UnityEngine.UI.CanvasScaler>(screen.gameObject);scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
            var arrow=Rect(screen,"PosterDirectionArrow",new Vector2(74,54));
            Ensure<CanvasRenderer>(arrow.gameObject);
            var arrowGraphic=Ensure<ElevatorPosterCueGraphic>(arrow.gameObject);arrowGraphic.ring=false;arrowGraphic.raycastTarget=false;arrowGraphic.color=Color.white;
            guide.arrowGroup=Ensure<CanvasGroup>(arrow.gameObject);guide.arrowGroup.alpha=0;guide.arrowGroup.interactable=guide.arrowGroup.blocksRaycasts=false;
            var panel=Rect(screen,"PointingInstruction",new Vector2(610,90));
            panel.anchorMin=panel.anchorMax=new Vector2(.5f,0);panel.anchoredPosition=new Vector2(0,140);
            var card=Ensure<CorridorBubbleGraphic>(panel.gameObject);card.color=new Color(1,.965f,.86f);card.raycastTarget=false;card.speechTail=false;card.thoughtCloud=false;
            guide.instructionGroup=Ensure<CanvasGroup>(panel.gameObject);guide.instructionGroup.alpha=0;guide.instructionGroup.interactable=guide.instructionGroup.blocksRaycasts=false;
            var label=Rect(panel,"Instruction",new Vector2(570,58));
            label.anchorMin=Vector2.zero;label.anchorMax=Vector2.one;label.offsetMin=new Vector2(18,10);label.offsetMax=new Vector2(-18,-10);
            var text=Ensure<TextMeshProUGUI>(label.gameObject);text.font=lesson.view.font;text.color=new Color(.28f,.19f,.14f);text.alignment=TextAlignmentOptions.Center;
            ElevatorTypography.Apply(text,22);
            text.text="마우스로 둘러보고, 포스터의 원을\n중앙 레이저로 1.5초 동안 가리켜요.";
            guide.screenCanvas=canvas;guide.arrow=arrow;guide.instructionPanel=panel;guide.instruction=text;
            world.gameObject.SetActive(false);canvas.gameObject.SetActive(false);
            lesson.settings.shotStorySeconds=lesson.settings.posterWarningSeconds=lesson.settings.friendReplySeconds=1.5f;
            lesson.settings.callQuestionSeconds=1.5f;lesson.settings.callReplySeconds=2;lesson.settings.callLocationGuideSeconds=2.5f;
            lesson.settings.reportDialogue="엘리베이터가 고장 나서 움직이지 않아요.\n친구랑 둘이 있어요.";
            EditorUtility.SetDirty(lesson.settings);EditorUtility.SetDirty(lesson);EditorUtility.SetDirty(guide);
        }
        [MenuItem("Tools/Wait/Elevator/Apply poster pointing cutscene to both scenes")]
        public static void ApplyMenu(){ApplyBoth();}
        public static object ApplyBoth()
        {
            if(Application.isPlaying)throw new Exception("Stop Play Mode before updating scenes");
            for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Preserve unsaved scene changes first");
            foreach(var path in Scenes)
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                Configure(Object.FindFirstObjectByType<ElevatorLesson>());
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            var settings=AssetDatabase.LoadAssetAtPath<ElevatorSettings>("Assets/M/Scene/ElevatorSettings.asset");
            AssetDatabase.SaveAssetIfDirty(settings);EditorSceneManager.OpenScene(Scenes[0],OpenSceneMode.Single);
            return new{scenes=Scenes,holdSeconds=1.5f,pulseSeconds=1.5f,pulseGrowth=.20f};
        }
    }
}
#endif
