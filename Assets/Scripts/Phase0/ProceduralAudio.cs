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
    // Linear chirp: phase = integral of ω(t) = 2π*(f0*t + (f1-f0)*t²/(2*T))
    public static AudioClip PaddlePop() => Synth("PaddlePop", 0.14f, i =>
    {
        float t    = (float)i / Rate;
        const float dur = 0.14f, f0 = 1400f, f1 = 220f;
        float phase = MathF.PI * 2f * (f0 * t + (f1 - f0) * t * t / (2f * dur));
        return MathF.Sin(phase) * MathF.Exp(-t * 22f);
    });

    // Bright ping — far (front) wall, high and sharp so it reads as "distant"
    public static AudioClip FrontWallPing() => Synth("FrontWallPing", 0.9f, i =>
    {
        float t = (float)i / Rate;
        return MathF.Sin(MathF.PI * 2f * 1200f * t) * MathF.Exp(-t * 120f);
    });

    // Hollow thock — near (back) wall, low and woody so it reads as "behind you"
    public static AudioClip BackWallThock() => Synth("BackWallThock", 0.9f, i =>
    {
        float t = (float)i / Rate;
        return MathF.Sin(MathF.PI * 2f * 320f * t) * MathF.Exp(-t * 40f);
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
