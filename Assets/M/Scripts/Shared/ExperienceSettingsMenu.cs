using TMPro;
using UnityEngine;

namespace ProjectWait.M.Shared
{
    public sealed class ExperienceSettingsMenu : MonoBehaviour
    {
        public GameObject mainCard,settingsCard;
        public UnityEngine.UI.Button open,back,defaults,calibrate;
        public UnityEngine.UI.Button[] decrease,increase;
        public TMP_Text[] values;
        public TMP_Text heightStatus;
        public bool IsOpen=>settingsCard&&settingsCard.activeSelf;
        readonly System.Collections.Generic.Dictionary<TMP_Text,float> baseSizes=new System.Collections.Generic.Dictionary<TMP_Text,float>();
        void Awake()
        {
            foreach(var text in GetComponentsInChildren<TMP_Text>(true))baseSizes[text]=text.fontSize;
            open.onClick.AddListener(Show);back.onClick.AddListener(Close);defaults.onClick.AddListener(ExperiencePreferences.Defaults);
            calibrate.onClick.AddListener(()=>{ExperienceSceneSettings.CalibrateHeight();if(heightStatus)heightStatus.text="잠시 가만히 있어 주세요.";});
            for(int i=0;i<decrease.Length;i++){int row=i;decrease[i].onClick.AddListener(()=>Change(row,-1));increase[i].onClick.AddListener(()=>Change(row,1));}
        }
        void OnEnable(){ExperiencePreferences.Changed+=Refresh;Refresh();}
        void OnDisable(){ExperiencePreferences.Changed-=Refresh;Close();}
        public void Show(){mainCard.SetActive(false);settingsCard.SetActive(true);Refresh();ExperienceSceneSettings.RequireFreshClick();}
        public void Close(){if(mainCard)mainCard.SetActive(true);if(settingsCard)settingsCard.SetActive(false);ExperienceSceneSettings.RequireFreshClick();}
        public void Change(int row,int direction)
        {
            var v=JsonUtility.FromJson<ExperiencePreferences.Values>(JsonUtility.ToJson(ExperiencePreferences.Current));
            switch(row){case 0:v.music+=direction*.05f;break;case 1:v.effects+=direction*.1f;break;case 2:v.text+=direction*.05f;break;case 3:v.dialogue+=direction*.05f;break;case 4:v.movement+=direction*.1f;break;case 5:v.haptics=direction>0;break;case 6:v.reducedShake=direction>0;break;}
            ExperiencePreferences.Apply(v);
        }
        void Refresh()
        {
            var v=ExperiencePreferences.Current;
            foreach(var item in baseSizes)if(item.Key)item.Key.fontSize=item.Value*v.text;
            string[] labels={Percent(v.music),Percent(v.effects),Percent(v.text),Percent(v.dialogue),ExperiencePreferences.MoveSpeed.ToString("0.00")+" m/s",v.haptics?"켜짐":"꺼짐",v.reducedShake?"켜짐":"꺼짐"};
            for(int i=0;i<values.Length&&i<labels.Length;i++)if(values[i])values[i].text=labels[i];
            if(heightStatus)heightStatus.text="공통 눈높이 · 엘리베이터 기준";
        }
        static string Percent(float value)=>Mathf.RoundToInt(value*100)+"%";
    }
}
