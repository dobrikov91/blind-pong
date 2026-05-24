using UnityEngine;

// Marker component placed on every arena panel so BallController can classify
// collision surfaces without relying on pre-defined tags.
public class SurfaceType : MonoBehaviour
{
    public enum Kind { Wall, FloorCeiling, Paddle, WallFront, WallBack }
    public Kind kind = Kind.Wall;
}
