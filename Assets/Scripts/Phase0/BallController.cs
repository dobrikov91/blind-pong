using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallController : MonoBehaviour
{
    [Header("Speed (m/s)")]
    public float minSpeed = 0.5f;
    public float maxSpeed = 15f;

    // Very low gravity — ball drifts slowly enough to locate it by ear alone
    [Range(0f, 1f)] public float gravityScale = 0.3f;

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
        // Free-play kick-off; in rally mode the ball is held at the serve point instead.
        if (!rb.isKinematic)
            rb.linearVelocity = new Vector3(1.5f, 2f, 1.5f);
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return; // held by RallyGame during serve countdown
        rb.AddForce(Physics.gravity * gravityScale, ForceMode.Acceleration);
        ClampSpeed();
    }

    void OnCollisionEnter(Collision col)
    {
        var contact = col.contacts[0];
        Vector3 normal    = contact.normal;
        Vector3 reflected = Vector3.Reflect(rb.linearVelocity, normal);

        // Add paddle swing velocity: feel is tuned via Paddle.velocityMultiplier
        var paddle = col.collider.gameObject.GetComponentInParent<Paddle>();
        if (paddle != null)
            reflected += paddle.Velocity * paddle.velocityMultiplier;

        // Shallow-angle hits leave the ball barely moving away from the surface.
        // Gravity can push it back in before the next frame, causing a chain of
        // re-collisions that makes the ball scrape along the wall.
        // Enforce a minimum outward speed component to break that chain.
        float outward = Vector3.Dot(reflected, normal);
        if (outward < 0.5f)
            reflected += normal * (0.5f - outward) * 1.2f;

        rb.linearVelocity = reflected;
        ClampSpeed();

        // Move the ball clear of the surface so it can't re-collide next physics step.
        rb.position += normal * 0.005f;

        var surface = col.collider.gameObject.GetComponent<SurfaceType>()
                   ?? col.collider.gameObject.GetComponentInParent<SurfaceType>();
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

    public void Respawn(Vector3 pos)
    {
        rb.position        = pos;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // Freeze the ball at a position (e.g. held at the serve point during countdown).
    // Kinematic bodies only support Speculative continuous detection, so switch modes.
    public void Hold(Vector3 pos)
    {
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.isKinematic            = true;
        rb.position               = pos;
    }

    // Release a held ball with the given velocity
    public void Launch(Vector3 velocity)
    {
        rb.isKinematic            = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity         = velocity;
        rb.angularVelocity        = Vector3.zero;
    }

    // Rescale current velocity to the given speed, keeping direction
    public void SetSpeed(float speed)
    {
        if (rb.linearVelocity.sqrMagnitude < 0.0001f) return;
        rb.linearVelocity = rb.linearVelocity.normalized * Mathf.Min(speed, maxSpeed);
    }
}
