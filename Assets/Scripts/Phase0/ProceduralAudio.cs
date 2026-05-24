using System;
using UnityEngine;

// Generates placeholder AudioClips in code so the scene runs and HRTF
// positioning can be validated before any real sound assets exist.
// All clips are mono — the AudioSource spatializer handles panning.
public static class ProceduralAudio
{
    const int Rate = 44100;

    // Sharp transient — primary localization cue on every wall bounce
    public static AudioClip WallClick() => Synth("WallClick", 0.06f, i =>
    {
        float t = (float)i / Rate;
        return MathF.Sin(MathF.PI * 2f * 900f * t) * MathF.Exp(-t * 80f);
    });

    // Low thud — perceptually distinct from wall click for floor/ceiling
    public static AudioClip FloorThud() => Synth("FloorThud", 0.10f, i =>
    {
        float t = (float)i / Rate;
        return MathF.Sin(MathF.PI * 2f * 180f * t) * MathF.Exp(-t * 35f);
    });

    // Pitched pop with downward glide — most important to distinguish (paddle)
    public static AudioClip PaddlePop() => Synth("PaddlePop", 0.07f, i =>
    {
        float t   = (float)i / Rate;
        float freq = 1400f + (500f - 1400f) * (t / 0.07f); // 1400→500 Hz
        return MathF.Sin(MathF.PI * 2f * freq * t) * MathF.Exp(-t * 45f);
    });

    // Low-pass filtered noise for looping ball-in-flight whoosh.
    // BallAudio modulates pitch and volume so this just needs a plausible loop.
    public static AudioClip Whoosh(float duration = 2f)
    {
        int     n    = Mathf.CeilToInt(Rate * duration);
        float[] data = new float[n];
        float   prev = 0f;

        for (int i = 0; i < n; i++)
        {
            prev   = Mathf.Lerp(prev, UnityEngine.Random.Range(-1f, 1f), 0.04f);
            prev = UnityEngine.Random.Range(-1f, 1f);
            float env = MathF.Sin(MathF.PI * i / n); // fade in/out to avoid click at loop point
            data[i] = prev; // * env * 0.5f;
        }

        var clip = AudioClip.Create("Whoosh", n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip Synth(string clipName, float duration, Func<int, float> sample)
    {
        int     n    = Mathf.CeilToInt(Rate * duration);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
            data[i] = Mathf.Clamp(sample(i), -1f, 1f);

        var clip = AudioClip.Create(clipName, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
