using UnityEngine;

// Drives two AudioSources on the ball:
//   bounceSource — one-shot click/thud/pop on each collision
//   whooshSource — looping noise whose pitch + volume track ball speed + distance
//
// Both sources use spatialBlend = 1 so Unity (or Steam Audio spatializer) apply
// full HRTF. Steam Audio is configured globally via Project Settings > Audio >
// Spatializer Plugin; no per-source component changes are needed for Phase 0.
[RequireComponent(typeof(AudioSource))]
public class BallAudio : MonoBehaviour
{
    [Header("Bounce clips — assigned by Phase0Bootstrap")]
    public AudioClip wallHit;
    public AudioClip floorHit;
    public AudioClip paddleHit;
    public AudioClip frontWallHit;
    public AudioClip backWallHit;

    [Header("Whoosh envelope")]
    [Range(0f, 1f)] public float whooshMinVolume    = 0.08f;  // always audible
    [Range(0f, 1f)] public float whooshMaxVolume    = 0.35f;
    public float whooshPitchMin    = 0.6f;
    public float whooshPitchMax    = 2.0f;
    public float whooshVolumeScale = 1f;
    public bool  whooshPitchModulate = true; // false for custom audio files — keeps pitch at 1×

    AudioSource bounceSource;
    AudioSource whooshSource;
    BallController ball;
    Transform      listener;

    void Awake()
    {
        ball     = GetComponent<BallController>();
        listener = Camera.main.transform;

        bounceSource = GetComponent<AudioSource>();
        Configure3D(bounceSource, loop: false);

        whooshSource = gameObject.AddComponent<AudioSource>();
        Configure3D(whooshSource, loop: true);

        ball.OnBallHit += HandleHit;
    }

    void OnDestroy() => ball.OnBallHit -= HandleHit;

    void Update()
    {
        float speedT = Mathf.InverseLerp(ball.minSpeed, ball.maxSpeed, ball.Speed);
        float distT  = Mathf.Clamp01(1f - Vector3.Distance(transform.position, listener.position) / 12f);

        whooshSource.volume = Mathf.Clamp01(Mathf.Lerp(whooshMinVolume, whooshMaxVolume, speedT) * distT * whooshVolumeScale);
        whooshSource.pitch  = whooshPitchModulate ? Mathf.Lerp(whooshPitchMin, whooshPitchMax, speedT) : 1f;
    }

    void HandleHit(SurfaceType.Kind surface, Vector3 _)
    {
        AudioClip clip = surface switch
        {
            SurfaceType.Kind.FloorCeiling => floorHit,
            SurfaceType.Kind.Paddle       => paddleHit,
            SurfaceType.Kind.WallFront    => frontWallHit,
            SurfaceType.Kind.WallBack     => backWallHit,
            _                             => wallHit,
        };
        if (clip != null)
            bounceSource.PlayOneShot(clip, surface == SurfaceType.Kind.Paddle ? 1.5f : 1.0f);
    }

    public void StartWhoosh(AudioClip clip, bool pitchModulate = true)
    {
        whooshPitchModulate       = pitchModulate;
        whooshSource.dopplerLevel = pitchModulate ? 0.5f : 0f;
        whooshSource.clip         = clip;
        whooshSource.Play();
    }

    static void Configure3D(AudioSource src, bool loop)
    {
        src.spatialBlend  = 1f;                          // full 3D
        src.rolloffMode   = AudioRolloffMode.Logarithmic;
        src.minDistance   = 0.3f;
        src.maxDistance   = 12f;
        src.dopplerLevel  = 0.5f;
        src.loop          = loop;
        src.playOnAwake   = false;
    }
}
