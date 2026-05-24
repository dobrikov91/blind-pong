using UnityEngine;

// Builds a 6-panel box arena entirely at runtime.
// debugVisible = true renders each panel as a tinted cube so you can see the room.
public static class ArenaBuilder
{
    public static void Build(float width = 6f, float height = 4f, float depth = 6f,
                             bool debugVisible = false, float yOffset = 0f)
    {
        var root = new GameObject("Arena");
        // yOffset lifts the whole room so its floor panel lands at world y=0 (real floor).
        root.transform.position = new Vector3(0, yOffset, 0);
        var mat  = MakeBouncyMaterial();

        float hw = width  * 0.5f;
        float hh = height * 0.5f;
        float hd = depth  * 0.5f;

        AddPanel(root, "Floor",     new Vector3( 0,  -hh,  0), new Vector3(width, 0.1f, depth),  SurfaceType.Kind.FloorCeiling, mat, debugVisible);
        AddPanel(root, "Ceiling",   new Vector3( 0,   hh,  0), new Vector3(width, 0.1f, depth),  SurfaceType.Kind.FloorCeiling, mat, debugVisible);
        AddPanel(root, "WallLeft",  new Vector3(-hw,  0,   0), new Vector3(0.1f, height, depth), SurfaceType.Kind.Wall,         mat, debugVisible);
        AddPanel(root, "WallRight", new Vector3( hw,  0,   0), new Vector3(0.1f, height, depth), SurfaceType.Kind.Wall,         mat, debugVisible);
        AddPanel(root, "WallFront", new Vector3( 0,   0,   hd), new Vector3(width, height, 0.1f), SurfaceType.Kind.Wall,        mat, debugVisible);
        AddPanel(root, "WallBack",  new Vector3( 0,   0,  -hd), new Vector3(width, height, 0.1f), SurfaceType.Kind.Wall,        mat, debugVisible);
    }

    static void AddPanel(GameObject root, string panelName, Vector3 localPos, Vector3 colSize,
                         SurfaceType.Kind kind, PhysicsMaterial mat, bool visible)
    {
        GameObject go;
        if (visible)
        {
            // CreatePrimitive gives us a mesh + default BoxCollider (size 1,1,1).
            // Scaling the transform to colSize makes the physical and visual sizes match.
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = panelName;
            go.GetComponent<BoxCollider>().sharedMaterial = mat;
            go.GetComponent<Renderer>().sharedMaterial    = DebugMaterial(kind);
        }
        else
        {
            go = new GameObject(panelName);
            var col = go.AddComponent<BoxCollider>();
            col.size     = colSize;
            col.material = mat;
        }

        go.transform.SetParent(root.transform);
        go.transform.localPosition = localPos;
        if (visible) go.transform.localScale = colSize;

        go.AddComponent<SurfaceType>().kind = kind;
    }

    // Dark tints — visible but not glaring; distinguishes floor/ceiling from walls.
    static Material DebugMaterial(SurfaceType.Kind kind)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        mat.color = kind == SurfaceType.Kind.FloorCeiling
            ? new Color(0.10f, 0.10f, 0.30f)   // dark blue
            : new Color(0.08f, 0.22f, 0.08f);   // dark green
        return mat;
    }

    static PhysicsMaterial MakeBouncyMaterial()
    {
        return new PhysicsMaterial("ArenaSurface")
        {
            bounciness      = 1f,
            dynamicFriction = 0f,
            staticFriction  = 0f,
            bounceCombine   = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum,
        };
    }
}
