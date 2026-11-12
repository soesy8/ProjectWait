# Original bubble pop

`BubblePop_Original.wav` is newly synthesised, not downloaded/extracted reference audio.

- 48,000Hz mono, 16-bit PCM, 150ms, peak ~0.76; tapered endpoints, negligible DC.
- Current revision **V2** replaces the initially rejected V1. It uses an original rounded pressure oscillator with a rapid **rising** phase curve (early-body fit ~451→848Hz, ~5.7ms time constant), a stronger first 8–10ms, and a short wet tail. Extra bell-like/non-integer oscillators from V1 were removed. The smooth amplitude curve is fitted to measured 4ms RMS features, not copied reference PCM.
- Reference: https://www.youtube.com/watch?v=Ge2zHoXskC8 — normal browser playback was numerically analysed with Web Audio. Approximately 60ms main RMS region and ~750–900Hz dominant DFT bins guided the synthesis. No reference samples are included, and the video's title is not treated as a licence grant.
- Unity importer: mono, preloaded PCM / Decompress On Load, no background loading or streaming. Runtime pitch variation is per pooled voice, not a global pitch change.
- V1 generation: `Assets/M/CodexTemp/LobbyPopFade~/GenerateAudio.cs`. **Current V2 revision:** `ReviseAudio.cs` / `audio-revision-v2.json`; the latter records phase fitting, envelope error and spectral-centroid comparison. Reference analysis: `reference-analysis.json`. Runtime does not depend on these temporary files. Numerical similarity is not a substitute for user listening approval.
