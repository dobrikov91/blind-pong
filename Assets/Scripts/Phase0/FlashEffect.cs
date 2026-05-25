using System.Collections;
using UnityEngine;

// Standalone visual flash. Add to any GameObject whose renderers should blink.
// With autoTrigger = true it self-subscribes to BallController.OnBallHit and
// flashes whenever the surface kind matches triggerOn.
// Call Flash() directly to trigger from code.
public class FlashEffect : MonoBehaviour
{
    [Header("Visual")]
    public Color flashColor    = Color.white;
    public float flashDuration = 0.12f;

    [Header("Auto-trigger")]
    public bool             autoTrigger = true;
    public SurfaceType.Kind triggerOn   = SurfaceType.Kind.Paddle;

    Renderer[]     renderers;
    Color[]        originalColors;
    bool[]         originalEnabled;
    Coroutine      flashCoroutine;
    BallController ball;

    static readonly int BaseProp = Shader.PropertyToID("_BaseColor");
    static readonly int ColProp  = Shader.PropertyToID("_Color");

    void Start()
    {
        renderers       = GetComponentsInChildren<Renderer>(includeInactive: true);
        originalColors  = new Color[renderers.Length];
        originalEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i]  = GetColor(renderers[i].material);
            originalEnabled[i] = renderers[i].enabled;
        }

        if (autoTrigger)
        {
            ball = Object.FindFirstObjectByType<BallController>();
            if (ball != null) ball.OnBallHit += OnBallHit;
        }
    }

    void OnDestroy()
    {
        if (ball != null) ball.OnBallHit -= OnBallHit;
    }

    void OnBallHit(SurfaceType.Kind kind, Vector3 _)
    {
        if (kind == triggerOn) Flash();
    }

    public void Flash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = true;
            SetColor(renderers[i].material, flashColor);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            SetColor(renderers[i].material, originalColors[i]);
            renderers[i].enabled = originalEnabled[i];
        }

        flashCoroutine = null;
    }

    static Color GetColor(Material m)
        => m.HasProperty(BaseProp) ? m.GetColor(BaseProp) : m.GetColor(ColProp);

    static void SetColor(Material m, Color c)
    {
        if (m.HasProperty(BaseProp)) m.SetColor(BaseProp, c);
        if (m.HasProperty(ColProp))  m.SetColor(ColProp,  c);
    }
}
