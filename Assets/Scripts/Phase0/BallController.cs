using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Speed (m/s)")]
    public float minSpeed = 2f;
    public float maxSpeed = 8f;

    // 50% earth gravity — ball stays in the air long enough to track by ear
    [Range(0f, 1f)] public float gravityScale = 0.5f;

    // Surface kind + world-space contact point
    public event Action<SurfaceType.Kind, Vector3> OnBallHit;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.mass = 0.05f;
    }

    void Start()
    {
        rb.linearVelocity = new Vector3(2.5f, 3f, 2f);
    }

    void FixedUpdate()
    {
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        ClampSpeed();
    }

    void OnCollisionEnter(Collision col)
    {
        Vector3 normal    = col.contacts[0].normal;
        Vector3 reflected = Vector3.Reflect(rb.linearVelocity, normal);

        // Add paddle swing velocity: feel is tuned via Paddle.velocityMultiplier
        var paddle = col.gameObject.GetComponentInParent<Paddle>();
        if (paddle != null)
            reflected += paddle.Velocity * paddle.velocityMultiplier;

        rb.linearVelocity = reflected;
        ClampSpeed();

        var surface = col.gameObject.GetComponent<SurfaceType>()
                   ?? col.gameObject.GetComponentInParent<SurfaceType>();
        OnBallHit?.Invoke(
            surface != null ? surface.kind : SurfaceType.Kind.Wall,
            col.contacts[0].point
        );
    }

    void ClampSpeed()
    {
        float speed = rb.linearVelocity.magnitude;
        if (speed > 0.01f && speed < minSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * minSpeed;
        else if (speed > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    public float Speed => rb.linearVelocity.magnitude;
}
