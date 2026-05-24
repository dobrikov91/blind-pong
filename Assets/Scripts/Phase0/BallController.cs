using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Speed (m/s)")]
    public float minSpeed = 1.5f;
    public float maxSpeed = 5f;

    // Very low gravity — ball drifts slowly enough to locate it by ear alone
    [Range(0f, 1f)] public float gravityScale = 0.1f;

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
        rb.linearVelocity = new Vector3(1.5f, 2f, 1.5f);
    }

    void FixedUpdate()
    {
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        ClampSpeed();
    }

    void OnCollisionEnter(Collision col)
    {
        var contact = col.contacts[0];
        Vector3 normal    = contact.normal;
        Vector3 reflected = Vector3.Reflect(rb.linearVelocity, normal);

        // Add paddle swing velocity: feel is tuned via Paddle.velocityMultiplier
        var paddle = col.gameObject.GetComponentInParent<Paddle>();
        if (paddle != null)
            reflected += paddle.Velocity * paddle.velocityMultiplier;

        // Shallow-angle hits leave the ball barely moving away from the surface.
        // Gravity can push it back in before the next frame, causing a chain of
        // re-collisions that makes the ball scrape along the wall.
        // Enforce a minimum outward speed component to break that chain.
        float outward = Vector3.Dot(reflected, normal);
        if (outward < 0.5f)
            reflected += normal * (0.5f - outward);

        rb.linearVelocity = reflected;
        ClampSpeed();

        // Move the ball clear of the surface so it can't re-collide next physics step.
        rb.position += normal * 0.005f;

        var surface = col.gameObject.GetComponent<SurfaceType>()
                   ?? col.gameObject.GetComponentInParent<SurfaceType>();
        OnBallHit?.Invoke(
            surface != null ? surface.kind : SurfaceType.Kind.Wall,
            contact.point
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
