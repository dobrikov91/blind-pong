using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

// Tracks the right-hand XR controller in VR, or floats in front of the
// camera on desktop. Move the Rigidbody kinematically so physics can
// detect collisions with the ball correctly.
[RequireComponent(typeof(Rigidbody))]
public class Paddle : MonoBehaviour
{
    [Header("Hit feel")]
    public float velocityMultiplier = 1.5f;

    [Header("Desktop fallback")]
    public float armLength = 0.6f;

    [Header("Hit flash")]
    public Color flashColor    = Color.white;
    public float flashDuration = 0.12f;

    [Header("Proximity haptics")]
    public float hapticMaxDistance  = 2.0f; // buzz starts at this distance (metres)
    public float hapticMinDistance  = 0.15f; // full intensity at this distance
    public float hapticMaxAmplitude = 0.6f;  // amplitude at closest (0–1)

    [Header("Mode")]
    // Space-invaders mode: paddle is always parallel to the X axis (face perpendicular
    // to Z). Hand controls Y and Z only; X is locked to 0. Rotation is fixed.
    public bool spaceInvadersMode = true;

    // Assigned by Phase0Bootstrap — toggled when mode changes
    [HideInInspector] public GameObject normalGeometry;
    [HideInInspector] public GameObject siGeometry;

    // Read by BallController on contact
    public Vector3 Velocity { get; private set; }

    Rigidbody  rb;
    Transform  cam;
    Vector3    prevPos;
    bool       prevSIMode;

    InputAction posAction;
    InputAction rotAction;
    InputAction respawnAction;
    InputAction toggleBallAction;

    BallController ball;
    Renderer       ballRenderer;
    Coroutine      flashCoroutine;

    Renderer[] paddleRenderers;
    Color[]    originalColors;
    bool[]     originalEnabled;

    readonly List<UnityEngine.XR.InputDevice> hapticDevices = new List<UnityEngine.XR.InputDevice>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic            = true;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        cam     = Camera.main.transform;
        prevPos = transform.position;

        // Use the same Input System pipeline as TrackedPoseDriver so controller
        // and camera positions are always in the same reference space.
        posAction = new InputAction("RHandPos", InputActionType.Value,
            "<XRController>{RightHand}/devicePosition", expectedControlType: "Vector3");
        rotAction = new InputAction("RHandRot", InputActionType.Value,
            "<XRController>{RightHand}/deviceRotation", expectedControlType: "Quaternion");
        posAction.Enable();
        rotAction.Enable();

        // A button on Quest controller; R key on desktop.
        respawnAction = new InputAction("Respawn", InputActionType.Button);
        respawnAction.AddBinding("<XRController>{RightHand}/primaryButton");
        respawnAction.AddBinding("<Keyboard>/r");
        respawnAction.Enable();

        // B button on Quest controller; V key on desktop.
        toggleBallAction = new InputAction("ToggleBall", InputActionType.Button);
        toggleBallAction.AddBinding("<XRController>{RightHand}/secondaryButton");
        toggleBallAction.AddBinding("<Keyboard>/v");
        toggleBallAction.Enable();
    }

    void Start()
    {
        ball = Object.FindFirstObjectByType<BallController>();
        if (ball != null)
        {
            ballRenderer = ball.GetComponent<Renderer>();
            ball.OnBallHit += OnBallHit;
        }
        prevSIMode = spaceInvadersMode;
        SyncGeometry();

        paddleRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        originalColors  = new Color[paddleRenderers.Length];
        originalEnabled = new bool[paddleRenderers.Length];
        for (int i = 0; i < paddleRenderers.Length; i++)
        {
            originalColors[i]  = GetColor(paddleRenderers[i].material);
            originalEnabled[i] = paddleRenderers[i].enabled;
        }
    }

    void SyncGeometry()
    {
        normalGeometry?.SetActive(!spaceInvadersMode);
        siGeometry?.SetActive(spaceInvadersMode);
    }

    void Update()
    {
        if (spaceInvadersMode != prevSIMode)
        {
            SyncGeometry();
            prevSIMode = spaceInvadersMode;
        }

        if (respawnAction.WasPressedThisFrame())
        {
            if (ball == null) ball = Object.FindFirstObjectByType<BallController>();
            if (ball != null)
                ball.Respawn(transform.position + transform.up * 0.4f);
        }

        if (toggleBallAction.WasPressedThisFrame())
        {
            if (ballRenderer == null && ball != null)
                ballRenderer = ball.GetComponent<Renderer>();
            if (ballRenderer != null)
                ballRenderer.enabled = !ballRenderer.enabled;
        }
    }

    void OnDestroy()
    {
        if (ball != null) ball.OnBallHit -= OnBallHit;
        posAction?.Dispose();
        rotAction?.Dispose();
        respawnAction?.Dispose();
        toggleBallAction?.Dispose();
    }

    void OnBallHit(SurfaceType.Kind kind, Vector3 _)
    {
        if (kind != SurfaceType.Kind.Paddle) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());

        // Strong one-shot buzz on contact
        hapticDevices.Clear();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.RightHand, hapticDevices);
        foreach (var dev in hapticDevices)
            dev.SendHapticImpulse(0, 1.0f, 0.18f);
    }

    // URP Unlit uses _BaseColor; Built-in Unlit/Color uses _Color.
    static readonly int BaseProp = Shader.PropertyToID("_BaseColor");
    static readonly int ColProp  = Shader.PropertyToID("_Color");

    static Color GetColor(Material m)
        => m.HasProperty(BaseProp) ? m.GetColor(BaseProp) : m.GetColor(ColProp);

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty(BaseProp)) m.SetColor(BaseProp, c);
        if (m.HasProperty(ColProp))  m.SetColor(ColProp,  c);
    }

    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < paddleRenderers.Length; i++)
        {
            paddleRenderers[i].enabled = true;
            SetColor(paddleRenderers[i].material, flashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < paddleRenderers.Length; i++)
        {
            SetColor(paddleRenderers[i].material, originalColors[i]);
            paddleRenderers[i].enabled = originalEnabled[i];
        }

        flashCoroutine = null;
    }

    void FixedUpdate()
    {
        Vector3    targetPos;
        Quaternion targetRot;

        bool inVR = posAction.activeControl != null;
        if (inVR)
        {
            Vector3 vrPos = posAction.ReadValue<Vector3>();
            Transform cameraOffset = cam.parent;
            Vector3 worldPos = cameraOffset != null
                ? cameraOffset.TransformPoint(vrPos) : vrPos;

            if (spaceInvadersMode)
            {
                // Lock X to 0; hand drives Y and Z only.
                // Rotation is fixed: face perpendicular to Z, parallel to X axis.
                targetPos = new Vector3(worldPos.x, worldPos.y, worldPos.z);
                targetRot = Quaternion.identity;
            }
            else
            {
                Quaternion vrRot = rotAction.ReadValue<Quaternion>();
                var grip = Quaternion.Euler(0f, 0f, 180f);
                targetPos = worldPos;
                targetRot = (cameraOffset != null ? cameraOffset.rotation : Quaternion.identity)
                            * vrRot * grip;
            }
        }
        else
        {
            if (spaceInvadersMode)
            {
                // Mouse cursor controls paddle X/Y: project mouse screen position
                // into world space at armLength depth from the camera.
                var mouse = Mouse.current;
                Vector2 screenPos = mouse != null
                    ? mouse.position.ReadValue()
                    : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Ray ray = Camera.main.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
                targetPos = ray.GetPoint(armLength);
                targetRot = Quaternion.identity;
            }
            else
            {
                // Free-look mode: paddle hangs at arm's length, rotates with camera.
                targetPos = cam.position + cam.forward * armLength + cam.up * -0.15f;
                targetRot = cam.rotation;
            }
        }

        Velocity = (targetPos - prevPos) / Time.fixedDeltaTime;
        prevPos  = targetPos;

        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRot);

        if (inVR) SendProximityHaptics(targetPos);
    }

    void SendProximityHaptics(Vector3 paddlePos)
    {
        if (ball == null) return;

        float dist = Vector3.Distance(ball.transform.position, paddlePos);
        if (dist >= hapticMaxDistance) return;

        float t         = 1f - Mathf.InverseLerp(hapticMinDistance, hapticMaxDistance, dist);
        float amplitude = t * hapticMaxAmplitude;

        hapticDevices.Clear();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.RightHand, hapticDevices);
        foreach (var dev in hapticDevices)
            dev.SendHapticImpulse(0, amplitude, Time.fixedDeltaTime);
    }
}
