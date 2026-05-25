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
    public static AudioClip WhiteNoise(float duration = 2f)
    {
        int     n    = Mathf.CeilToInt(Rate * duration);
        float[] data = new float[n];
        float   prev = 0f;

        for (int i = 0; i < n; i++)
        {
            prev   = Mathf.Lerp(prev, UnityEngine.Random.Range(-1f, 1f), 0.04f);
            data[i] = prev;
        }

        var clip = AudioClip.Create("WhiteNoise", n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Pink noise (1/f) — more natural than white noise, softer high-frequency content.
    // Uses Paul Kellet's economical filter on white noise.
    public static AudioClip PinkNoise(float duration = 2f)
    {
        int     n    = Mathf.CeilToInt(Rate * duration);
        float[] data = new float[n];
        var     rng  = new System.Random(42); // seeded so the loop is deterministic
        float b0=0, b1=0, b2=0, b3=0, b4=0, b5=0;

        for (int i = 0; i < n; i++)
        {
            float w = (float)(rng.NextDouble() * 2.0 - 1.0);
            b0 =  0.99886f * b0 + w * 0.0555179f;
            b1 =  0.99332f * b1 + w * 0.0750759f;
            b2 =  0.96900f * b2 + w * 0.1538520f;
            b3 =  0.86650f * b3 + w * 0.3104856f;
            b4 =  0.55000f * b4 + w * 0.5329522f;
            b5 = -0.76160f * b5 - w * 0.0168980f;
            data[i] = Mathf.Clamp((b0 + b1 + b2 + b3 + b4 + b5 + w * 0.5362f) * 0.11f, -1f, 1f);
        }

        var clip = AudioClip.Create("PinkNoise", n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Pure sine at 220 Hz — 2 s × 220 Hz = 440 complete cycles, loops without clicks.
    // BallAudio pitch-shifts this at runtime so the tone tracks ball speed.
    public static AudioClip PureSine(float duration = 2f, float frequency = 220f)
    {
        int     n    = Mathf.CeilToInt(Rate * duration);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t  = (float)i / Rate;
            data[i]  = MathF.Sin(MathF.PI * 2f * frequency * t);
        }

        var clip = AudioClip.Create("PureSine", n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Kept for back-compat; identical to WhiteNoise.
    public static AudioClip Whoosh(float duration = 2f) => WhiteNoise(duration);

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
