using UnityEngine;

// Simple debug fly-camera. Click the Game view to lock the cursor, Escape to release.
// WASD = horizontal move, E/Q = up/down, mouse = look.
// If keys don't respond: Project Settings > Player > Active Input Handling > "Both"
public class DebugFlyCamera : MonoBehaviour
{
    public float moveSpeed  = 2f;
    public float lookSpeed  = 2f;

    float pitch = 0f;
    float yaw   = 0f;

    void Start()
    {
        var angles = transform.eulerAngles;
        pitch = angles.x;
        yaw   = angles.y;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            Cursor.lockState = CursorLockMode.Locked;
        if (Input.GetKeyDown(KeyCode.Escape))
            Cursor.lockState = CursorLockMode.None;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw   += Input.GetAxis("Mouse X") * lookSpeed;
            pitch -= Input.GetAxis("Mouse Y") * lookSpeed;
            pitch  = Mathf.Clamp(pitch, -89f, 89f);
            transform.localEulerAngles = new Vector3(pitch, yaw, 0f);
        }

        float dt = Time.deltaTime * moveSpeed;
        if (Input.GetKey(KeyCode.W)) transform.position += transform.forward * dt;
        if (Input.GetKey(KeyCode.S)) transform.position -= transform.forward * dt;
        if (Input.GetKey(KeyCode.A)) transform.position -= transform.right   * dt;
        if (Input.GetKey(KeyCode.D)) transform.position += transform.right   * dt;
        if (Input.GetKey(KeyCode.E)) transform.position += Vector3.up        * dt;
        if (Input.GetKey(KeyCode.Q)) transform.position -= Vector3.up        * dt;
    }
}
