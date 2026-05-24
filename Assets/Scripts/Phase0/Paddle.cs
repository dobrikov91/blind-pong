using UnityEngine;
using UnityEngine.InputSystem;

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

    // Read by BallController on contact
    public Vector3 Velocity { get; private set; }

    Rigidbody  rb;
    Transform  cam;
    Vector3    prevPos;

    InputAction posAction;
    InputAction rotAction;
    InputAction respawnAction;

    BallController ball;

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
    }

    void Start()
    {
        ball = Object.FindFirstObjectByType<BallController>();
    }

    void Update()
    {
        if (!respawnAction.WasPressedThisFrame()) return;
        if (ball == null)
            ball = Object.FindFirstObjectByType<BallController>();
        if (ball == null) return;

        // Spawn at the tip of the racket head so the player can serve immediately.
        ball.Respawn(transform.position + transform.up * 0.4f);
    }

    void OnDestroy()
    {
        posAction?.Dispose();
        rotAction?.Dispose();
        respawnAction?.Dispose();
    }

    void FixedUpdate()
    {
        Vector3    targetPos;
        Quaternion targetRot;

        bool inVR = posAction.activeControl != null;
        if (inVR)
        {
            Vector3    vrPos = posAction.ReadValue<Vector3>();
            Quaternion vrRot = rotAction.ReadValue<Quaternion>();

            // Camera Offset is the XROrigin child whose local transform absorbs any
            // floor-height compensation. Transforming through it keeps the controller
            // in the same world space as the camera driven by TrackedPoseDriver.
            Transform cameraOffset = cam.parent;
            // 180° around Z flips the paddle's local Y axis so the head faces outward
            // (toward the ball) and the handle is in the player's grip, not the reverse.
            // Adjust if the face still feels twisted after testing.
            var grip = Quaternion.Euler(0f, 0f, 180f);
            if (cameraOffset != null)
            {
                targetPos = cameraOffset.TransformPoint(vrPos);
                targetRot = cameraOffset.rotation * vrRot * grip;
            }
            else
            {
                targetPos = vrPos;
                targetRot = vrRot * grip;
            }
        }
        else
        {
            // Desktop: paddle hangs at arm's length, slightly below eye level.
            targetPos = cam.position
                      + cam.forward * armLength
                      + cam.up * -0.15f;
            targetRot = cam.rotation;
        }

        Velocity = (targetPos - prevPos) / Time.fixedDeltaTime;
        prevPos  = targetPos;

        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRot);
    }
}
