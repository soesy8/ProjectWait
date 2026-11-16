using System;
using UnityEngine;

namespace ProjectWait.M.Shared
{
    // Elevator defaults, shared by the lobby and both lessons. Motion's base speed
    // deliberately comes from the slower crosswalk introduction, not its unsafe run.
    public static class ExperiencePreferences
    {
        public const float DefaultEyeHeight = 1.1064756f;
        public const float WalkingSpeed = 1.1f;
        const string Key = "ProjectWait.SharedComfort.v1";
        [Serializable] public sealed class Values
        {
            public float music = .25f, effects = 1, text = 1, dialogue = 1, movement = 1;
            public bool haptics = true, reducedShake;
        }
        public static Values Current { get; private set; } = new Values();
        public static event Action Changed;
        public static float EyeHeight => DefaultEyeHeight;
        public static float LookSensitivity => .1f;
        public static float MoveSpeed => WalkingSpeed * Current.movement;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            Changed = null; Current = new Values();
            try { if (PlayerPrefs.HasKey(Key)) Current = JsonUtility.FromJson<Values>(PlayerPrefs.GetString(Key)) ?? new Values(); }
            catch (ArgumentException) { Current = new Values(); }
            Clamp();
        }
        static float Finite(float value,float fallback) => float.IsNaN(value)||float.IsInfinity(value)?fallback:value;
        static void Clamp()
        {
            Current.music = Mathf.Clamp01(Finite(Current.music,.25f)); Current.effects = Mathf.Clamp01(Finite(Current.effects,1));
            Current.text = Mathf.Clamp(Finite(Current.text,1),.9f,1.25f);
            Current.dialogue = Mathf.Clamp(Finite(Current.dialogue,1),.75f,1.25f);
            Current.movement = Mathf.Clamp(Finite(Current.movement,1),.6f,1);
        }
        public static void Apply(Values value,bool save=true)
        {
            if(value==null)return;
            Current = JsonUtility.FromJson<Values>(JsonUtility.ToJson(value)); Clamp();
            if(save){PlayerPrefs.SetString(Key,JsonUtility.ToJson(Current));PlayerPrefs.Save();}
            Changed?.Invoke();
        }
        public static void Defaults() => Apply(new Values());
    }

    // A release edge and real-time guard, independent of the lesson's paused clock.
    public sealed class FreshPressGate
    {
        double until;
        public bool Ready { get; private set; } = true;
        public void Arm(double now){until=now+.3;Ready=false;}
        public void Observe(double now,bool held){if(!Ready&&!held&&now>=until)Ready=true;}
    }
}
