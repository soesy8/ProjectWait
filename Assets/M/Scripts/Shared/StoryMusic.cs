using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ProjectWait.M.Shared
{
    // One decoded clip and one uninterrupted transport for lobby + both experiences.
    [DefaultExecutionOrder(-2500)]
    [RequireComponent(typeof(AudioSource))]
    public sealed class StoryMusic : MonoBehaviour
    {
        public TextAsset encryptedMusic;
        [Range(0,1)] public float volume=.25f;
        public static StoryMusic Current {get;private set;}
        public AudioSource Source {get;private set;}
        public bool Ready {get;private set;}
        public int PlayCount {get;private set;}
        readonly HashSet<UnityEngine.Object> pauseOwners=new HashSet<UnityEngine.Object>();
        AudioClip runtimeClip;
        bool held;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic(){Current=null;}
        void Awake()
        {
            if(Current&&Current!=this){Destroy(gameObject);return;}
            Current=this;DontDestroyOnLoad(gameObject);
            Source=GetComponent<AudioSource>();Source.playOnAwake=false;Source.loop=true;Source.spatialBlend=0;
            Source.volume=ExperiencePreferences.Current.music;Source.ignoreListenerPause=true;Source.priority=64;
            StartCoroutine(Load());
        }
        public static void SetPaused(UnityEngine.Object owner,bool pause)
        {
            if(!Current||!owner)return;
            if(pause)Current.pauseOwners.Add(owner);else Current.pauseOwners.Remove(owner);
            Current.ApplyPause();
        }
        void Update(){if(Current==this){Source.volume=ExperiencePreferences.Current.music;pauseOwners.RemoveWhere(owner=>!owner);ApplyPause();}}
        void ApplyPause()
        {
            bool pause=pauseOwners.Count>0;
            if(!Ready){held=pause;return;}
            if(pause==held)return;
            held=pause;if(held)Source.Pause();else Source.UnPause();
        }
        IEnumerator Load()
        {
            byte[] wave=null;int offset=0,samples=0,channels=0,rate=0;
            try
            {
                if(!encryptedMusic)throw new InvalidOperationException("Shared music data is not assigned.");
                wave=Decode(encryptedMusic.bytes);ReadWave(wave,out offset,out samples,out channels,out rate);
                runtimeClip=AudioClip.Create("Shared cheerful whistle",samples/channels,channels,rate,false);
            }
            catch(Exception error){Debug.LogWarning("공용 배경음을 준비하지 못했습니다: "+error.Message,this);if(wave!=null)Array.Clear(wave,0,wave.Length);yield break;}
            float[] batch=null;
            try
            {
                for(int decoded=0;decoded<samples;)
                {
                    int count=Mathf.Min(262144,samples-decoded);
                    if(batch==null||batch.Length!=count){if(batch!=null)Array.Clear(batch,0,batch.Length);batch=new float[count];}
                    for(int i=0;i<count;i++){int p=offset+(decoded+i)*3;int value=wave[p]|wave[p+1]<<8|wave[p+2]<<16;if((value&0x800000)!=0)value|=unchecked((int)0xFF000000);batch[i]=value/8388608f;}
                    runtimeClip.SetData(batch,decoded/channels);decoded+=count;yield return null;
                }
            }
            finally{Array.Clear(wave,0,wave.Length);if(batch!=null)Array.Clear(batch,0,batch.Length);}
            Source.clip=runtimeClip;Ready=true;Source.Play();PlayCount++;
            held=pauseOwners.Count>0;if(held)Source.Pause();
        }
        // Retain the supplied protected PCM format; never write decrypted music to disk.
        static byte[] Decode(byte[] packed)
        {
            byte[] key=Convert.FromBase64String("mVlp8j9exMeWUdgoQBneLZeK2sFuV7plXcJp0W/AMYc="),auth=Convert.FromBase64String("3lC8n0S0JHAULh+IYTFDdxMUmqDZaAeS6pToUQoQ/m0=");
            try
            {
                if(packed.Length<72||Encoding.ASCII.GetString(packed,0,8)!="PWMUS001")throw new CryptographicException("Invalid protected music format.");
                int end=packed.Length-32;
                using(var hmac=new HMACSHA256(auth)){byte[] signature=hmac.ComputeHash(packed,0,end);int difference=0;for(int i=0;i<32;i++)difference|=signature[i]^packed[end+i];Array.Clear(signature,0,signature.Length);if(difference!=0)throw new CryptographicException("Music authentication failed.");}
                using(var aes=Aes.Create()){aes.KeySize=256;aes.Mode=CipherMode.CBC;aes.Padding=PaddingMode.PKCS7;aes.Key=key;var iv=new byte[16];Buffer.BlockCopy(packed,8,iv,0,16);aes.IV=iv;using(var decryptor=aes.CreateDecryptor())return decryptor.TransformFinalBlock(packed,24,end-24);}
            }
            finally{Array.Clear(key,0,key.Length);Array.Clear(auth,0,auth.Length);Array.Clear(packed,0,packed.Length);}
        }
        static void ReadWave(byte[] wave,out int offset,out int samples,out int channels,out int rate)
        {
            offset=samples=channels=rate=0;int format=0,bits=0,length=0;
            if(wave.Length<44||Encoding.ASCII.GetString(wave,0,4)!="RIFF"||Encoding.ASCII.GetString(wave,8,4)!="WAVE")throw new InvalidOperationException("Invalid WAV.");
            for(int p=12;p+8<=wave.Length;){int size=BitConverter.ToInt32(wave,p+4);if(size<0||size>wave.Length-p-8)throw new InvalidOperationException("Invalid WAV chunk.");string tag=Encoding.ASCII.GetString(wave,p,4);if(tag=="fmt "&&size>=16){format=BitConverter.ToUInt16(wave,p+8);channels=BitConverter.ToUInt16(wave,p+10);rate=BitConverter.ToInt32(wave,p+12);bits=BitConverter.ToUInt16(wave,p+22);}else if(tag=="data"){offset=p+8;length=size;}p+=8+size+(size&1);}
            if(format!=1||bits!=24||channels!=2||rate!=48000||offset==0||length==0||length%6!=0)throw new InvalidOperationException("Expected 48 kHz, stereo, 24-bit PCM.");samples=length/3;
        }
        void OnDestroy()
        {
            if(Current!=this)return;
            Current=null;StopAllCoroutines();if(Source)Source.Stop();if(runtimeClip)Destroy(runtimeClip);
        }
    }
}
