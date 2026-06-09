using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Rally survival game loop:
//   Idle → (A / R press) → Serving (3 countdown beeps) → BallInPlay
//   Paddle hit   = return: score +1, jingle, ball speeds up
//   WallBack hit = miss: rally over, miss tone, score counted in beeps, back to Idle
//
// All state feedback is audio (procedural beeps) on a non-spatial source —
// game-state sounds should not be positional.
public class RallyGame : MonoBehaviour
{
    public enum State { Idle, Serving, BallInPlay, RallyOver }

    [Header("Serve")]
    public float   baseServeSpeed     = 2.5f;
    public float   speedRampPerReturn = 0.08f;
    public Vector3 servePoint;                              // set by Phase0Bootstrap
    public Vector2 targetXRange = new Vector2(-0.4f, 0.4f); // aim window at player end
    public Vector2 targetYRange = new Vector2(0.8f, 1.6f);
    public float   targetZ      = 0f;                       // set by Phase0Bootstrap

    [Header("Timing")]
    public float countdownInterval = 0.7f;
    public float returnDebounce    = 0.5f; // ignore double-taps on the paddle

    public State CurrentState { get; private set; } = State.Idle;
    public int   ReturnCount  { get; private set; }
    public int   BestScore    { get; private set; }

    BallController ball;
    AudioSource    uiSource;
    InputAction    serveAction;
    float          lastReturnTime;

    AudioClip beepLow, beepMid, beepHigh, scoreBeep, returnJingle, missTone;

    void Awake()
    {
        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.spatialBlend = 0f;
        uiSource.playOnAwake  = false;

        beepLow      = ProceduralAudio.Beep(600f);
        beepMid      = ProceduralAudio.Beep(800f);
        beepHigh     = ProceduralAudio.Beep(1200f);
        scoreBeep    = ProceduralAudio.Beep(880f);
        returnJingle = ProceduralAudio.ReturnJingle();
        missTone     = ProceduralAudio.MissTone();

        // Same Input System pipeline as Paddle so VR and desktop both work.
        serveAction = new InputAction("Serve", InputActionType.Button);
        serveAction.AddBinding("<XRController>{RightHand}/primaryButton");
        serveAction.AddBinding("<Keyboard>/r");
        serveAction.Enable();
    }

    void Start()
    {
        ball = Object.FindFirstObjectByType<BallController>();
        if (ball == null)
        {
            Debug.LogError("RallyGame: no BallController in scene — disabling");
            enabled = false;
            return;
        }
        ball.OnBallHit += OnBallHit;
        ball.Hold(servePoint);
        Debug.Log("RallyGame: Idle — press A (VR) or R (desktop) to serve");
    }

    void OnDestroy()
    {
        if (ball != null) ball.OnBallHit -= OnBallHit;
        serveAction?.Dispose();
    }

    void Update()
    {
        if (CurrentState == State.Idle && serveAction.WasPressedThisFrame())
            StartCoroutine(ServeRoutine());
    }

    IEnumerator ServeRoutine()
    {
        CurrentState = State.Serving;
        ReturnCount  = 0;
        Debug.Log("RallyGame: Serving");

        ball.Hold(servePoint);
        uiSource.PlayOneShot(beepLow);
        yield return new WaitForSeconds(countdownInterval);
        uiSource.PlayOneShot(beepMid);
        yield return new WaitForSeconds(countdownInterval);
        uiSource.PlayOneShot(beepHigh);
        yield return new WaitForSeconds(countdownInterval);

        var target = new Vector3(
            Random.Range(targetXRange.x, targetXRange.y),
            Random.Range(targetYRange.x, targetYRange.y),
            targetZ);
        Vector3 dir = (target - servePoint).normalized;
        ball.Launch(dir * CurrentRallySpeed());

        lastReturnTime = -999f;
        CurrentState   = State.BallInPlay;
        Debug.Log($"RallyGame: BallInPlay at {CurrentRallySpeed():0.0} m/s");
    }

    float CurrentRallySpeed()
        => Mathf.Min(baseServeSpeed * (1f + speedRampPerReturn * ReturnCount), ball.maxSpeed);

    void OnBallHit(SurfaceType.Kind kind, Vector3 _)
    {
        if (CurrentState != State.BallInPlay) return;

        if (kind == SurfaceType.Kind.Paddle)
        {
            if (Time.time - lastReturnTime < returnDebounce) return;
            lastReturnTime = Time.time;
            ReturnCount++;
            // OnBallHit fires after BallController has set the reflected velocity,
            // so rescaling here applies the rally speed ramp to the outgoing ball.
            ball.SetSpeed(CurrentRallySpeed());
            uiSource.PlayOneShot(returnJingle);
            Debug.Log($"RallyGame: return #{ReturnCount}, speed {CurrentRallySpeed():0.0} m/s");
        }
        else if (kind == SurfaceType.Kind.WallBack)
        {
            StartCoroutine(RallyOverRoutine());
        }
    }

    IEnumerator RallyOverRoutine()
    {
        CurrentState = State.RallyOver;
        BestScore    = Mathf.Max(BestScore, ReturnCount);
        Debug.Log($"RallyGame: rally over — score {ReturnCount}, best {BestScore}");

        ball.Hold(servePoint);
        uiSource.PlayOneShot(missTone);
        yield return new WaitForSeconds(0.8f);

        // Score = one beep per successful return (zero returns = silence)
        for (int i = 0; i < ReturnCount; i++)
        {
            uiSource.PlayOneShot(scoreBeep);
            yield return new WaitForSeconds(0.15f);
        }

        CurrentState = State.Idle;
        Debug.Log("RallyGame: Idle — press A (VR) or R (desktop) to serve");
    }
}
