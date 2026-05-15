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

    // Check if it's the final of the Spline on other classes 
    public float TrackLength => generatedLength;

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
    /// Build the Spline and extrude it using Spline Extrude
    /// </summary>
    public void GenerateTrack() {
        Spline spline = BuildFlatRandomSpline();
        spline.SetTangentMode(TangentMode.AutoSmooth);
        spline.Closed = false;

        splineContainer.Spline = spline;
        generatedLength = spline.GetLength();

        ConfigureExtrude();
        splineExtrude.Rebuild();
    }

    /// <summary>
    /// Returns the actual position on the generated spline at normalized t [0, 1].
    /// </summary>
    public Vector3 GetPositionAt(float t) {
        if (!HasValidSpline()) return transform.position;

        SplineUtility.Evaluate(splineContainer.Spline, Mathf.Clamp01(t), out float3 position, out _, out _);
        return transform.TransformPoint((Vector3)position);
    }

    /// <summary>
    /// Returns the actual tangent direction on the generated spline at normalized t [0, 1].
    /// </summary>
    public Vector3 GetTangentAt(float t) {
        if (!HasValidSpline()) return transform.forward;

        SplineUtility.Evaluate(splineContainer.Spline, Mathf.Clamp01(t), out _, out float3 tangent, out _);
        return transform.TransformDirection((Vector3)tangent).normalized;
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
