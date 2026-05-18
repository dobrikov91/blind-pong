using UnityEngine;

// Builds a 6-panel box arena entirely at runtime.
// No meshes are rendered — panels are invisible colliders only.
public static class ArenaBuilder
{
    public static void Build(float width = 6f, float height = 4f, float depth = 6f)
    {
        var root = new GameObject("Arena");
        var mat  = MakeBouncyMaterial();

        float hw = width  * 0.5f;
        float hh = height * 0.5f;
        float hd = depth  * 0.5f;

        AddPanel(root, "Floor",     new Vector3( 0,  -hh,  0), new Vector3(width, 0.1f, depth),  SurfaceType.Kind.FloorCeiling, mat);
        AddPanel(root, "Ceiling",   new Vector3( 0,   hh,  0), new Vector3(width, 0.1f, depth),  SurfaceType.Kind.FloorCeiling, mat);
        AddPanel(root, "WallLeft",  new Vector3(-hw,  0,   0), new Vector3(0.1f, height, depth), SurfaceType.Kind.Wall, mat);
        AddPanel(root, "WallRight", new Vector3( hw,  0,   0), new Vector3(0.1f, height, depth), SurfaceType.Kind.Wall, mat);
        AddPanel(root, "WallFront", new Vector3( 0,   0,   hd), new Vector3(width, height, 0.1f), SurfaceType.Kind.Wall, mat);
        AddPanel(root, "WallBack",  new Vector3( 0,   0,  -hd), new Vector3(width, height, 0.1f), SurfaceType.Kind.Wall, mat);
    }

    static void AddPanel(GameObject root, string panelName, Vector3 localPos, Vector3 colSize,
                         SurfaceType.Kind kind, PhysicsMaterial mat)
    {
        var go = new GameObject(panelName);
        go.transform.SetParent(root.transform);
        go.transform.localPosition = localPos;

        var col = go.AddComponent<BoxCollider>();
        col.size     = colSize;
        col.material = mat;

        go.AddComponent<SurfaceType>().kind = kind;
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
