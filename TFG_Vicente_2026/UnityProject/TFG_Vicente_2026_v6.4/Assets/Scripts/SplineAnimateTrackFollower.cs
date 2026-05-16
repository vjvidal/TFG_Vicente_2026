using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Base for objects to move inside the Spline using the Spline Animate component.
/// In addition to the base following the Spline, a child object it's generated to control the lateral movement.
/// </summary>
[RequireComponent(typeof(SplineAnimate))]
public class SplineAnimateTrackFollower : MonoBehaviour {
    [Header("Spline Object")]
    [Tooltip("Spline associated with the object (track).")]
    [SerializeField] protected TrackManager track;
    
    [Tooltip("Moment t to represent the position on the Spline.")]
    [SerializeField, Range(0f, 1f)] protected float normalizedT = 0f;
    
    [Tooltip("Lateral Offset of the Spline.")]
    [SerializeField] protected float lateralOffset = 0f;
    
    [Tooltip("Margin to evade going outbounds the Spline.")]
    [SerializeField, Min(0f)] protected float lateralEdgeMargin = 0.25f;

    [Header("Position")]
    [SerializeField] protected Transform body;
    [SerializeField] protected bool applyRotation = true;
    [SerializeField] protected float heightOffset = 0.5f;

    [Header("Spline Animate")]
    [SerializeField, Min(0f)] protected float forwardSpeed = 0f;
    [SerializeField] protected bool playOnTrackGenerated = false;

    protected SplineAnimate splineAnimate;

    public TrackManager Track => track;
    public SplineAnimate SplineAnimate => splineAnimate;
    public Transform Body => body;
    public float NormalizedT => splineAnimate != null ? Mathf.Clamp01(splineAnimate.NormalizedTime) : normalizedT;
    public float LateralOffset => lateralOffset;
    public float ForwardSpeed => forwardSpeed;
    public TrackPose CurrentPose { get; private set; }
    protected bool HasReadyTrack => track != null && track.IsReady;

    /// <summary>
    /// Get the Spline Animate, the Body if it's the Player.
    /// If not, create a new Body and assign as a enemy.
    /// </summary>
    protected void InitComponents(){
        splineAnimate = GetComponent<SplineAnimate>();
        if (body == null) {
            Transform existingBody = transform.Find("Body");
            if (existingBody != null) {
                body = existingBody;
            }
            else {
                GameObject bodyObject = new GameObject("Body");
                bodyObject.transform.SetParent(transform, false);
                body = bodyObject.transform;
            }
        }
    }

    protected virtual void Awake() {
        InitComponents();

        if (track == null)
            track = FindAnyObjectByType<TrackManager>();
    }

    /// <summary>
    /// Find the TrackManager of the game and subscribe to the event of OnTrackGenerated
    /// </summary>
    protected virtual void OnEnable() {
        if (track == null)
            track = FindAnyObjectByType<TrackManager>();

        if (track == null) return;

        track.OnTrackGenerated += HandleTrackGenerated;

        if (track.IsReady)
            ConfigureForTrack(playOnTrackGenerated);
    }

    protected virtual void OnDisable() {
        if (track != null)
            track.OnTrackGenerated -= HandleTrackGenerated;
    }

    /// <summary>
    /// Initialize the object. This is used for the Player and each enemy generated.
    /// </summary>
    /// <param name="trackManager"></param>
    /// <param name="t"></param>
    /// <param name="offset"></param>
    public virtual void Initialize(TrackManager trackManager, float t, float offset) {
        track = trackManager;
        normalizedT = Mathf.Clamp01(t);
        lateralOffset = offset;

        if (isActiveAndEnabled && track != null && track.IsReady)
            ConfigureForTrack(playOnTrackGenerated);
    }

    /// <summary>
    /// Change the forward speed of the object
    /// </summary>
    /// <param name="speed">Target speed for the object</param>
    public void SetForwardSpeed(float speed) {
        forwardSpeed = Mathf.Max(0f, speed);

        if (splineAnimate == null) return;

        splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        splineAnimate.MaxSpeed = forwardSpeed;

        if (forwardSpeed > 0f)
            splineAnimate.Play();
        else
            splineAnimate.Pause();
    }

    /// <summary>
    /// Change the lateral position of the object, limited by the lateral offset.
    /// </summary>
    /// <param name="offset">Limited lateral position of a object</param>
    public void SetLateralOffset(float offset) {
        lateralOffset = ClampLateralOffset(offset);
        ApplyLateralOffset();
    }

    public void RefreshCurrentPose() {
        ApplyLateralOffset();
    }

    protected virtual void HandleTrackGenerated(TrackManager generatedTrack) {
        if (track == null)
            track = generatedTrack;

        ConfigureForTrack(playOnTrackGenerated);
    }

    /// <summary>
    /// Connect the object to the Spline.
    /// </summary>
    /// <param name="play">Boolean to start movement or pause it</param>
    protected void ConfigureForTrack(bool play) {
        if (!HasReadyTrack) return;

        splineAnimate.Container = track.Container;
        splineAnimate.PlayOnAwake = false;
        splineAnimate.Loop = SplineAnimate.LoopMode.Once;
        splineAnimate.AnimationMethod = SplineAnimate.Method.Speed;
        splineAnimate.Easing = SplineAnimate.EasingMode.None;
        splineAnimate.Alignment = applyRotation ? SplineAnimate.AlignmentMode.SplineElement : SplineAnimate.AlignmentMode.None;
        splineAnimate.ObjectForwardAxis = SplineComponent.AlignAxis.ZAxis;
        splineAnimate.ObjectUpAxis = SplineComponent.AlignAxis.YAxis;
        splineAnimate.MaxSpeed = forwardSpeed;
        splineAnimate.NormalizedTime = normalizedT;

        if (play && forwardSpeed > 0f)
            splineAnimate.Play();
        else
            splineAnimate.Pause();

        ApplyLateralOffset();
    }

    protected float ClampLateralOffset(float offset) {
        if (!HasReadyTrack) return offset;

        float maxOffset = Mathf.Max(0f, track.HalfRoadWidth - lateralEdgeMargin);
        return Mathf.Clamp(offset, -maxOffset, maxOffset);
    }

    protected void ApplyLateralOffset() {
        if (!HasReadyTrack) return;

        lateralOffset = ClampLateralOffset(lateralOffset);
        body.localPosition = new Vector3(lateralOffset, heightOffset, 0f);

        TrackPose centerPose = track.EvaluatePose(NormalizedT, 0f, lateralEdgeMargin);
        CurrentPose = new TrackPose(
            body.position,
            transform.rotation,
            centerPose.tangent,
            centerPose.right,
            centerPose.up
        );
    }
}
