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
    }

    void OnDestroy()
    {
        posAction?.Dispose();
        rotAction?.Dispose();
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
            if (cameraOffset != null)
            {
                targetPos = cameraOffset.TransformPoint(vrPos);
                targetRot = cameraOffset.rotation * vrRot;
            }
            else
            {
                targetPos = vrPos;
                targetRot = vrRot;
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
