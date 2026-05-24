using System.Collections.Generic;
using UnityEngine;
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

    // Read by BallController on contact
    public Vector3 Velocity { get; private set; }

    Rigidbody    rb;
    Transform    cam;
    Vector3      prevPos;
    InputDevice  rightHand;

    static readonly List<InputDevice> _deviceBuf = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic             = true;
        rb.interpolation           = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode  = CollisionDetectionMode.ContinuousKinematic;

        cam     = Camera.main.transform;
        prevPos = transform.position;
    }

    void FixedUpdate()
    {
        RefreshDevice();

        Vector3    targetPos;
        Quaternion targetRot;

        if (rightHand.isValid
            && rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 vrPos)
            && rightHand.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion vrRot))
        {
            // Controller positions are in tracking-space; transform via rig root if present.
            Transform origin = cam.parent;
            targetPos = origin != null ? origin.TransformPoint(vrPos) : vrPos;
            targetRot = origin != null ? origin.rotation * vrRot       : vrRot;
        }
        else
        {
            // Desktop: paddle hangs at arm's length, slightly below eye level.
            targetPos = cam.position
                      + cam.forward * armLength
                      + cam.up      * -0.15f;
            targetRot = cam.rotation;
        }

        Velocity = (targetPos - prevPos) / Time.fixedDeltaTime;
        prevPos  = targetPos;

        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRot);
    }

    void RefreshDevice()
    {
        if (rightHand.isValid) return;
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, _deviceBuf);
        if (_deviceBuf.Count > 0) rightHand = _deviceBuf[0];
    }
}
