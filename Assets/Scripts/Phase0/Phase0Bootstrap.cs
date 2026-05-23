using UnityEngine;

// Phase 0 — Audio Localization PoC
//
// SETUP (one time):
//   1. Unity Hub → New Project → Unity 6 LTS → 3D Core
//   2. Copy this repo's Assets/, Packages/, ProjectSettings/ into the project root
//   3. Package Manager will install Steam Audio from the git URL in manifest.json
//      If the git install fails: download the .unitypackage from
//      https://github.com/ValveSoftware/steam-audio/releases and import manually
//   4. Project Settings → Audio → Spatializer Plugin → "Steam Audio Spatializer"
//      (the AudioManager.asset in this repo pre-sets this but Unity may override)
//   5. File → New Scene (empty), add an empty GameObject, attach this script, Play
//
// VALIDATION:
//   Wear headphones. Close your eyes. Try to predict where the ball is before
//   each click. If you can track it reliably from the click alone — Phase 0 passes.

public class Phase0Bootstrap : MonoBehaviour
{
    [Header("Arena dimensions (metres)")]
    public float arenaWidth  = 6f;
    public float arenaHeight = 4f;
    public float arenaDepth  = 6f;

    [Header("Debug")]
    public bool debugVisuals = true;

    void Awake()
    {
        SetupCamera();
        ArenaBuilder.Build(arenaWidth, arenaHeight, arenaDepth, debugVisuals);
        SpawnBall();
        EnsureListener();
    }

    void SetupCamera()
    {
        RenderSettings.skybox       = null;
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = debugVisuals ? new Color(0.15f, 0.15f, 0.15f) : Color.black;

        var cam = Camera.main;
        cam.backgroundColor = Color.black;
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    void SpawnBall()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Ball";
        go.transform.position   = new Vector3(0f, 0.5f, 0f);
        go.transform.localScale = Vector3.one * 0.08f;

        var renderer = go.GetComponent<Renderer>();
        if (debugVisuals)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color");
            renderer.sharedMaterial = new Material(shader) { color = new Color(1f, 0.45f, 0f) }; // orange
        }
        else
        {
            renderer.enabled = false;
        }

        // Bouncy physics material on ball collider
        var col = go.GetComponent<SphereCollider>();
        col.material = new PhysicsMaterial("Ball")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            bounceCombine   = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum,
        };

        // Rigidbody is required before BallController so Awake order is safe
        go.AddComponent<Rigidbody>();
        go.AddComponent<BallController>();

        // BallAudio requires an AudioSource; add it before BallAudio.Awake fires
        go.AddComponent<AudioSource>();
        var audio = go.AddComponent<BallAudio>();

        audio.wallHit   = ProceduralAudio.WallClick();
        audio.floorHit  = ProceduralAudio.FloorThud();
        audio.paddleHit = ProceduralAudio.PaddlePop();
        audio.StartWhoosh(ProceduralAudio.Whoosh());
    }

    void EnsureListener()
    {
        var cam = Camera.main;
        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();
        if (cam.GetComponent<DebugFlyCamera>() == null)
            cam.gameObject.AddComponent<DebugFlyCamera>();
    }
}
