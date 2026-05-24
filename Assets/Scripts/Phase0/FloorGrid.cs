using UnityEngine;

// Draws a 1-metre square grid on the floor at world y=0 with axis labels.
// Labels face upward — readable when looking down in VR or from the fly-cam.
public static class FloorGrid
{
    public static void Build(float width, float zMin, float zMax)
    {
        var root = new GameObject("FloorGrid");
        var mat  = GridMaterial();

        float hw     = width * 0.5f;
        float depth  = zMax - zMin;
        float zCenter = (zMin + zMax) * 0.5f;
        float y  = 0.003f + 0.5f + 0.05f; // 3 mm above floor to avoid z-fighting

        // Strips parallel to X axis, one per integer Z value
        for (int iz = Mathf.CeilToInt(zMin); iz <= Mathf.FloorToInt(zMax); iz++)
            Line(root, new Vector3(0, y, iz), new Vector3(width, 0.004f, 0.010f), mat);

        // Strips parallel to Z axis, one per integer X value, spanning full depth
        for (int ix = Mathf.CeilToInt(-hw); ix <= Mathf.FloorToInt(hw); ix++)
            Line(root, new Vector3(ix, y, zCenter), new Vector3(0.010f, 0.004f, depth), mat);

        // X-coordinate labels along the near-Z edge
        for (int ix = Mathf.CeilToInt(-hw); ix <= Mathf.FloorToInt(hw); ix++)
            Label(root, $"X:{ix}", new Vector3(ix, y, zMin + 0.12f));

        // Z-coordinate labels along the near-X edge
        for (int iz = Mathf.CeilToInt(zMin); iz <= Mathf.FloorToInt(zMax); iz++)
            Label(root, $"Z:{iz}", new Vector3(-hw + 0.12f, y, iz));
    }

    static void Line(GameObject root, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "GridLine";
        go.transform.SetParent(root.transform);
        go.transform.position   = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Object.Destroy(go.GetComponent<Collider>());
    }

    static void Label(GameObject root, string text, Vector3 pos)
    {
        var go = new GameObject($"Label_{text}");
        go.transform.SetParent(root.transform);
        go.transform.position      = pos;
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // face upward

        var tm           = go.AddComponent<TextMesh>();
        tm.text          = text;
        tm.fontSize      = 36;
        tm.characterSize = 0.010f;
        tm.color         = new Color(0.85f, 0.85f, 0.85f);
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
    }

    static Material GridMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        return new Material(shader) { color = new Color(0.38f, 0.38f, 0.38f) };
    }
}
