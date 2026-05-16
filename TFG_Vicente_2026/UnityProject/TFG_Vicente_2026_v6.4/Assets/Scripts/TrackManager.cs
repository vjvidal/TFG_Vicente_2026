using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// Generates one finite procedural road spline when the scene starts.
/// The spline remains flat on Y = 0 and is rendered with Unity's SplineExtrude component.
/// </summary>
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(SplineExtrude))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class TrackManager : MonoBehaviour {
    private const float RoadProfileWidthMultiplier = 1.2f;

    // Spline configuration
    [Header("Spline Configuration")]
    [Tooltip("Approximate total track length.")]
    [SerializeField, Min(1f)] private float trackLength = 120f;

    [Tooltip("Distance between consecutive spline knots.")]
    [SerializeField, Min(0.1f)] private float knotSpacing = 8f;

    [Tooltip("Maximum lateral change between consecutive knots.")]
    [SerializeField, Min(0f)] private float maxLateralStep = 4f;

    [Tooltip("Maximum absolute lateral offset from the track center.")]
    [SerializeField, Min(0f)] private float maxLateralOffset = 15f;

    // Road mesh configuration
    [Header("Road Mesh")]
    [Tooltip("Approximate visual road width.")]
    [SerializeField, Min(0.1f)] private float roadWidth = 4f;

    [Tooltip("SplineExtrude mesh resolution. Higher values create smoother curves.")]
    [SerializeField, Min(0.01f)] private float segmentsPerUnit = 4f;

    private SplineContainer splineContainer;
    private SplineExtrude splineExtrude;
    private float generatedLength;
    private bool isReady;

    // Event fired when the Spline is created to start Spline Animate Track Followers movement (Player and Enemies).
    public event Action<TrackManager> OnTrackGenerated;

    // Make public variables from Spline to control Player and Enemies
    public SplineContainer Container => splineContainer;
    public float RoadWidth => roadWidth;
    public float HalfRoadWidth => roadWidth * 0.5f;
    public float TrackLength => generatedLength;
    public bool IsReady => isReady;

    /// <summary>
    /// Get the components needed by the Spline.
    /// </summary>
    private void InitComponents(){
        splineContainer = GetComponent<SplineContainer>();
        splineExtrude = GetComponent<SplineExtrude>();
    }

    private void Awake() {
        InitComponents();
        ConfigureExtrude();
    }

    private void Start() {
        GenerateTrack();
    }

    /// <summary>
    /// Build the Spline and extrude it using Spline Extrude.
    /// Added a flag to check if the Spline it's created before start the player movement
    /// </summary>
    public void GenerateTrack() {
        isReady = false;

        Spline spline = BuildFlatRandomSpline();
        spline.SetTangentMode(TangentMode.AutoSmooth);
        spline.Closed = false;

        splineContainer.Spline = spline;
        generatedLength = spline.GetLength();

        ConfigureExtrude();
        splineExtrude.Rebuild();

        isReady = true;
        OnTrackGenerated?.Invoke(this);
    }

    /// <summary>
    /// Returns the actual position on the generated spline at normalized t [0, 1].
    /// </summary>
    public Vector3 GetPositionAt(float t) {
        if (!HasValidSpline()) return transform.position;

        return EvaluatePose(t, 0f).position;
    }

    /// <summary>
    /// Returns the actual tangent direction on the generated spline at normalized t [0, 1].
    /// </summary>
    public Vector3 GetTangentAt(float t) {
        if (!HasValidSpline()) return transform.forward;

        return EvaluatePose(t, 0f).tangent;
    }

    /// <summary>
    /// Most important function on the code. Evaluates the Spline for a specific moment (t) and lateral offset, and return the TrackPose of this moment.
    /// This is because, the Spline has a mesh and it's wider than the Spline itself.
    /// </summary>
    /// <param name="t">The exact point of the Spline that has to be evaluated</param>
    /// <param name="lateralOffset">The lateral position to evaluate in the Spline</param>
    /// <param name="edgeMargin">Margin of the edge to avoid compare on the Spline outbounds</param>
    /// <returns>TrackPose structure with all the information about that specific moment and offset</returns>
    public TrackPose EvaluatePose(float t, float lateralOffset, float edgeMargin = 0f) {
        if (!HasValidSpline()) {
            return new TrackPose(
                transform.position,
                transform.rotation,
                transform.forward,
                transform.right,
                transform.up
            );
        }

        float clampedT = Mathf.Clamp01(t);
        float maxOffset = Mathf.Max(0f, HalfRoadWidth - Mathf.Max(0f, edgeMargin));
        float clampedLateralOffset = Mathf.Clamp(lateralOffset, -maxOffset, maxOffset);

        SplineUtility.Evaluate(splineContainer.Spline, clampedT, out float3 localPosition, out float3 localTangent, out float3 localUp);

        Vector3 tangent = transform.TransformDirection((Vector3)localTangent).normalized;
        if (tangent.sqrMagnitude <= Mathf.Epsilon)
            tangent = transform.forward;

        Vector3 up = transform.TransformDirection((Vector3)localUp).normalized;
        if (up.sqrMagnitude <= Mathf.Epsilon)
            up = Vector3.up;

        Vector3 right = Vector3.Cross(up, tangent).normalized;
        if (right.sqrMagnitude <= Mathf.Epsilon)
            right = transform.right;

        Vector3 position = transform.TransformPoint((Vector3)localPosition) + right * clampedLateralOffset;
        Quaternion rotation = Quaternion.LookRotation(tangent, up);

        return new TrackPose(position, rotation, tangent, right, up);
    }

    /// <summary>
    /// Generate the Spline with curves using the parameters in the config
    /// </summary>
    /// <returns>Spline created</returns>
    private Spline BuildFlatRandomSpline() {
        Spline spline = new Spline();
        int knotCount = Mathf.Max(2, Mathf.CeilToInt(trackLength / knotSpacing) + 1);
        float currentX = 0f;

        for (int i = 0; i < knotCount; i++) {
            float z = Mathf.Min(i * knotSpacing, trackLength);

            if (i == 0) {
                currentX = 0f;
            } else {
                float lateralStep = UnityEngine.Random.Range(-maxLateralStep, maxLateralStep);
                currentX = Mathf.Clamp(currentX + lateralStep, -maxLateralOffset, maxLateralOffset);
            }

            spline.Add(new BezierKnot(new float3(currentX, 0f, z)));
        }

        return spline;
    }

    /// <summary>
    /// Configures the Splines Extrude used to generate the mesh
    /// </summary>
    private void ConfigureExtrude() {
        if (splineContainer == null || splineExtrude == null) return;

        splineExtrude.Container = splineContainer;
        splineExtrude.Radius = roadWidth / RoadProfileWidthMultiplier;
        splineExtrude.SegmentsPerUnit = segmentsPerUnit;
        splineExtrude.Capped = true;
        splineExtrude.Range = new Vector2(0f, 1f);
    }

    private bool HasValidSpline() {
        return splineContainer != null && splineContainer.Spline != null && splineContainer.Spline.Count >= 2;
    }
}
