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
        rb.collisionDetectionMode  = CollisionDetectionMode.ContinuousSpeculative;

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
            // XROrigin uses Device tracking mode, so tracking space == world space
            // (HMD initial position is world origin). Controller positions map directly.
            // If the XR Origin has a non-identity transform, go via its parent chain.
            Transform xrOrigin = cam.parent?.parent; // Camera → Camera Offset → XR Origin
            targetPos = xrOrigin != null ? xrOrigin.TransformPoint(vrPos) : vrPos;
            targetRot = xrOrigin != null ? xrOrigin.rotation * vrRot       : vrRot;
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
