using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectWait.M.TitleScene
{
    // Bounded polyphony: no GameObject/AudioSource allocation per pop and no shared
    // pitch changes that would retune already-playing voices.
    sealed class LobbyDropletPopAudio : IDisposable
    {
        const int VoiceCount = 6;
        readonly LobbyDissolveTest owner;
        readonly GameObject root;
        readonly AudioSource[] voices = new AudioSource[VoiceCount];
        uint random;
        float countdown;
        bool disposed;

        public int PlayedCount { get; private set; }
        public float LastPitch { get; private set; } = 1;
        public int ActiveVoices
        {
            get { int n = 0; foreach (var voice in voices) if (voice && voice.isPlaying) n++; return n; }
        }

        public LobbyDropletPopAudio(LobbyDissolveTest owner)
        {
            this.owner = owner;
            random = (uint)owner.GetInstanceID() ^ 0xA341316Cu;
            if (random == 0) random = 1;
            root = new GameObject("Lobby bubble pop voices (runtime)") { hideFlags = HideFlags.HideAndDontSave };
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            for (int i = 0; i < voices.Length; i++)
            {
                var voice = root.AddComponent<AudioSource>();
                voice.playOnAwake = false; voice.loop = false; voice.spatialBlend = 0;
                voice.dopplerLevel = 0; voice.reverbZoneMix = 0; voice.priority = 180;
                voices[i] = voice;
            }
        }

        public void Begin()
        {
            foreach (var voice in voices) voice.Stop();
            countdown = .035f;
        }

        public void Tick(float seconds, bool transitioning, float progress)
        {
            if (disposed) return;
            if (!owner.playDropletPops || !owner.dropletPopClip || owner.popVolume <= 0)
            {
                foreach (var voice in voices) if (voice.isPlaying) voice.Stop();
                return;
            }
            // A naturally decaying tail may finish after the endpoint, but no new
            // events are scheduled while idle. Never burst to catch up skipped time.
            if (!transitioning || seconds <= 0) return;
            countdown -= seconds;
            if (countdown > 0) return;
            countdown = Mathf.Lerp(owner.popIntervalRange.x, owner.popIntervalRange.y, Next01());
            AudioSource available = null;
            foreach (var voice in voices) if (!voice.isPlaying) { available = voice; break; }
            if (!available) return; // Do not steal/cut an active voice.
            float lo = Mathf.Clamp(owner.popPitchRange.x, .5f, 2), hi = Mathf.Clamp(owner.popPitchRange.y, lo, 2);
            float pitch = Mathf.Lerp(lo, hi, Next01());
            for (int retry = 0; retry < 4 && hi - lo > .10f && Mathf.Abs(pitch - LastPitch) < .055f; retry++)
                pitch = Mathf.Lerp(lo, hi, Next01());
            if (hi - lo > .10f && Mathf.Abs(pitch - LastPitch) < .055f)
                pitch = LastPitch < (lo + hi) * .5f ? hi : lo;
            available.clip = owner.dropletPopClip;
            available.pitch = pitch;
            available.panStereo = Mathf.Lerp(-.12f, .12f, Next01());
            available.volume = Mathf.Clamp01(owner.popVolume) * Mathf.Lerp(.76f, 1, Next01()) * Mathf.Lerp(.75f, 1, Mathf.Sin(progress * Mathf.PI));
            available.Play();
            LastPitch = pitch; PlayedCount++;
        }

        float Next01()
        {
            random ^= random << 13; random ^= random >> 17; random ^= random << 5;
            return (random & 0xFFFFFF) / 16777216f;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            foreach (var voice in voices) if (voice) voice.Stop();
            if (Application.isPlaying) Object.Destroy(root); else Object.DestroyImmediate(root);
        }
    }
}
