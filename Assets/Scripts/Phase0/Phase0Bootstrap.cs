using UnityEngine;
using UnityEngine.XR;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

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
// FOR VR BUILD: see QUEST_BUILD.md at repo root, then tick vrMode in Inspector.
//
// VALIDATION:
//   Wear headphones. Close your eyes. Try to predict where the ball is before
//   each click. If you can track it reliably from the click alone — Phase 0 passes.

public class Phase0Bootstrap : MonoBehaviour
{
    [Header("Arena dimensions (metres)")]
    public float arenaWidth  = 1.5f;
    public float arenaHeight = 1.5f;
    [Header("Arena Z range (metres)")]
    public float arenaZMin   = -1f;
    public float arenaZMax   =  5f;

    [Header("Debug")]
    public bool debugVisuals = true;

    [Header("VR")]
    // Tick this when using Quest Link or building for Quest.
    public bool vrMode = false;

    void Awake()
    {
        // Build the static world first — no VR dependency yet.
        SetupRenderSettings();
        // Lift the arena so its floor panel sits at world y=0 (real floor in Floor tracking).
        float yOfs   = arenaHeight * 0.5f;
        float zCenter = (arenaZMin + arenaZMax) * 0.5f;
        ArenaBuilder.Build(arenaWidth, arenaHeight, arenaZMin, arenaZMax, debugVisuals, yOfs);
        if (debugVisuals) FloorGrid.Build(arenaWidth, arenaZMin, arenaZMax);
        SpawnBall(yOfs, zCenter);
        SpawnPaddle();
    }

    void Start()
    {
        // XR subsystems finish loading between Awake and Start, so detect here.
        bool xrRunning = XRSettings.enabled
                      && !string.IsNullOrEmpty(XRSettings.loadedDeviceName)
                      && XRSettings.loadedDeviceName != "None";
        bool useVR = vrMode || xrRunning;

        SetupCamera(useVR);
        EnsureListener(useVR);
    }

    void SetupRenderSettings()
    {
        RenderSettings.skybox       = null;
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = debugVisuals ? new Color(0.15f, 0.15f, 0.15f) : Color.black;
    }

    void SetupCamera(bool useVR)
    {
        var cam = Camera.main;
        cam.backgroundColor = Color.black;
        cam.clearFlags      = CameraClearFlags.SolidColor;

        if (useVR)
            SetupXROrigin(cam);
        else
            // Desktop: start at arena centre so fly-cam begins inside the room.
            cam.transform.SetPositionAndRotation(
                new Vector3(0, arenaHeight * 0.5f, (arenaZMin + arenaZMax) * 0.5f),
                Quaternion.identity);
    }

    // Creates the XR Origin hierarchy Unity 6 + OpenXR requires for correct head tracking.
    // Without it the tracking space and world space are conflated — everything appears
    // glued to the headset as you move.
    // Floor tracking mode places the world origin at the guardian floor level, so the
    // arena floor (which we shift to y=0) aligns with the real floor without recalibration.
    void SetupXROrigin(Camera cam)
    {
        var originGO = new GameObject("XR Origin");
        originGO.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var offsetGO = new GameObject("Camera Offset");
        offsetGO.transform.SetParent(originGO.transform);
        offsetGO.transform.localPosition = Vector3.zero;

        cam.transform.SetParent(offsetGO.transform);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        var xrOrigin = originGO.AddComponent<XROrigin>();
        xrOrigin.Camera                      = cam;
        xrOrigin.CameraFloorOffsetObject     = offsetGO;
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

        // TrackedPoseDriver is what actually moves the camera to match the headset.
        // Without it XROrigin exists but tracking is never applied — hence the warning.
        var tpd = cam.gameObject.AddComponent<TrackedPoseDriver>();
        tpd.positionInput = new InputActionProperty(new InputAction(
            "CamPosition", InputActionType.Value,
            "<XRHMD>/centerEyePosition", expectedControlType: "Vector3"));
        tpd.rotationInput = new InputActionProperty(new InputAction(
            "CamRotation", InputActionType.Value,
            "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion"));
    }

    void SpawnBall(float yOfs, float zCenter)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Ball";
        go.transform.position   = new Vector3(0f, yOfs, zCenter);
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

        audio.wallHit      = ProceduralAudio.WallClick();
        audio.floorHit     = ProceduralAudio.FloorThud();
        audio.paddleHit    = ProceduralAudio.PaddlePop();
        audio.frontWallHit = ProceduralAudio.FrontWallPing();
        audio.backWallHit  = ProceduralAudio.BackWallThock();
        audio.StartWhoosh(ProceduralAudio.Whoosh());
    }

    void SpawnPaddle()
    {
        var root = new GameObject("Paddle");
        root.AddComponent<Rigidbody>(); // Paddle.Awake configures kinematic
        root.AddComponent<Paddle>();

        // Racket head — flat box, this is what the ball hits
        var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "RacketHead";
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        head.transform.localScale    = new Vector3(0.26f, 0.32f, 0.025f);
        head.AddComponent<SurfaceType>().kind = SurfaceType.Kind.Paddle;

        // Handle — thin cylinder below the head
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Handle";
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0f, -0.12f, 0f);
        handle.transform.localScale    = new Vector3(0.03f, 0.13f, 0.03f);
        handle.AddComponent<SurfaceType>().kind = SurfaceType.Kind.Paddle;
        // Handle has no game-play collider — only the head should register hits
        Destroy(handle.GetComponent<Collider>());

        if (debugVisuals)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color");
            head.GetComponent<Renderer>().sharedMaterial =
                new Material(shader) { color = new Color(0.85f, 0.85f, 0.15f) }; // yellow
            handle.GetComponent<Renderer>().sharedMaterial =
                new Material(shader) { color = new Color(0.45f, 0.25f, 0.08f) }; // brown
        }
        else
        {
            head.GetComponent<Renderer>().enabled   = false;
            handle.GetComponent<Renderer>().enabled = false;
        }
    }

    void EnsureListener(bool useVR)
    {
        var cam = Camera.main;
        if (cam.GetComponent<AudioListener>() == null)
            cam.gameObject.AddComponent<AudioListener>();

        // Fly-camera is desktop-only; in VR the headset drives position/rotation.
        if (!useVR && cam.GetComponent<DebugFlyCamera>() == null)
            cam.gameObject.AddComponent<DebugFlyCamera>();
    }
}
