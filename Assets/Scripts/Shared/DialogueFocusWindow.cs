using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWait.M.Shared
{
    // Both eyes see the same soft window in the world-space paper. Text and buttons stay legible.
    [DisallowMultipleComponent]
    public class DialogueFocusWindow : MonoBehaviour
    {
        public Shader windowShader;
        [Range(3,25)] public float horizontalDegrees=12,verticalDegrees=10;
        [Range(.5f,3)] public float radiusScale=2;
        public Vector2 RadiusTangents=>new Vector2(Mathf.Tan(horizontalDegrees*Mathf.Deg2Rad),Mathf.Tan(verticalDegrees*Mathf.Deg2Rad))*radiusScale;
        [Range(0,1)] public float centreOpacity=.12f;
        [Min(.01f)] public float fadeSeconds=.16f;
        public float Amount {get;private set;}
        public int BackgroundCount=>images.Count;
        readonly List<Image> scratch=new List<Image>();
        readonly Dictionary<Image,Material> images=new Dictionary<Image,Material>();
        readonly Dictionary<TMP_Text,Material> textSources=new Dictionary<TMP_Text,Material>();
        readonly List<Material> textMaterials=new List<Material>();
        Material windowMaterial;
        Camera viewer;
        public static bool IsFocusMaterial(Material material)=>material&&material.shader&&material.shader.name=="ProjectWait/M/Observation UI Focus Window";

        public void Bind(Camera eyes, RectTransform panel, TMP_Text body, TMP_Text speaker)
        {
            if(!Application.isPlaying||!eyes||!panel||!windowShader)return;
            viewer=eyes;
            if(!windowMaterial)windowMaterial=new Material(windowShader){name="Observation background focus (runtime)",hideFlags=HideFlags.DontSave};
            scratch.Clear();panel.GetComponentsInChildren(true,scratch);
            foreach(var image in scratch)
            {
                if(!image||!image.enabled||image.GetComponentInParent<Button>(true))continue;
                if(!images.ContainsKey(image))images.Add(image,image.material);
                image.material=windowMaterial;
            }
            ProtectText(body);ProtectText(speaker);
        }

        void ProtectText(TMP_Text text)
        {
            if(!text||!text.fontSharedMaterial||textMaterials.Contains(text.fontSharedMaterial)||!text.fontSharedMaterial.HasProperty("_OutlineWidth"))return;
            if(!textSources.ContainsKey(text))textSources.Add(text,text.fontSharedMaterial);
            var material=new Material(text.fontSharedMaterial){name="Observation readable ink (runtime)",hideFlags=HideFlags.DontSave};
            // A paper-coloured glyph edge keeps brown text legible over dark objects.
            material.SetColor("_OutlineColor",new Color(1,.965f,.86f));material.SetFloat("_OutlineWidth",.08f);
            textMaterials.Add(material);text.fontSharedMaterial=material;text.UpdateMeshPadding();
        }

        bool Hit(RectTransform rect,Ray ray)
        {
            if(!new Plane(rect.forward,rect.position).Raycast(ray,out float distance)||distance<=0)return false;
            // Include the soft edge, even if the centre ray falls just outside the card.
            var radius=RadiusTangents;
            var margin=new Vector2(distance*radius.x/Mathf.Max(.0001f,rect.lossyScale.x),distance*radius.y/Mathf.Max(.0001f,rect.lossyScale.y));
            var area=Rect.MinMaxRect(rect.rect.xMin-margin.x,rect.rect.yMin-margin.y,rect.rect.xMax+margin.x,rect.rect.yMax+margin.y);
            return area.Contains((Vector2)rect.InverseTransformPoint(ray.GetPoint(distance)));
        }

        public void UpdateFocus(bool visible,float delta)
        {
            if(!viewer||!windowMaterial||(!visible&&Amount==0))return;
            var eyes=viewer.transform;
            bool focused=false;
            if(visible)
            {
                var ray=new Ray(eyes.position,eyes.forward);
                foreach(var item in images)if(item.Key&&item.Key.isActiveAndEnabled&&Hit(item.Key.rectTransform,ray)){focused=true;break;}
            }
            float blend=1-Mathf.Exp(-Mathf.Max(0,delta)*5/Mathf.Max(.01f,fadeSeconds));
            Amount=Mathf.Lerp(Amount,focused?1:0,blend);
            if(Mathf.Abs(Amount-(focused?1:0))<.001f)Amount=focused?1:0;
            windowMaterial.SetVector("_FocusOrigin",eyes.position);
            windowMaterial.SetVector("_FocusForward",eyes.forward);
            windowMaterial.SetVector("_FocusRight",eyes.right);
            windowMaterial.SetVector("_FocusUp",eyes.up);
            var radius=RadiusTangents;
            windowMaterial.SetVector("_FocusRadius",new Vector4(radius.x,radius.y,0,0));
            windowMaterial.SetFloat("_FocusStrength",Amount);
            windowMaterial.SetFloat("_FocusOpacity",centreOpacity);
        }

        // Mirrors the shader for the optional full-card reader's raycast filter.
        public float OpacityAt(Vector3 worldPoint)
        {
            if(!viewer)return 1;
            var p=viewer.transform.InverseTransformPoint(worldPoint);
            if(p.z<=.001f)return 1;
            var radius=RadiusTangents;
            var ellipse=new Vector2(p.x/(p.z*radius.x),p.y/(p.z*radius.y));
            float feather=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,1,ellipse.magnitude));
            return Mathf.Lerp(1,Mathf.Lerp(centreOpacity,1,feather),Amount);
        }

        public void ResetWindow(){Amount=0;if(windowMaterial)windowMaterial.SetFloat("_FocusStrength",0);}
        protected void OnDisable(){ResetWindow();}
        protected void OnDestroy()
        {
            foreach(var item in images)if(item.Key&&item.Key.material==windowMaterial)item.Key.material=item.Value;
            foreach(var item in textSources)if(item.Key&&textMaterials.Contains(item.Key.fontSharedMaterial))item.Key.fontSharedMaterial=item.Value;
            if(windowMaterial)Destroy(windowMaterial);
            foreach(var material in textMaterials)if(material)Destroy(material);
        }
    }
}
