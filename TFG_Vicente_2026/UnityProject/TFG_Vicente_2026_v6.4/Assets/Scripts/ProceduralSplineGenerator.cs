using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Procedurally generates a single spline chunk at runtime.
/// Each chunk is a segment of the infinite track, defined by a series of
/// Bezier knots with randomised lateral offsets.
/// </summary>
/// <remarks>
/// This component is designed to be instantiated and configured exclusively
/// by <see cref="TrackManager"/>.
/// </remarks>
[RequireComponent(typeof(SplineContainer))]
public class ProceduralSplineGenerator : MonoBehaviour {

    // Configured at runtime by TrackManager
    private int knotCount = 6;
    private float knotSpacing = 10f;
    private float maxLateralOffset = 4f;

    /// <summary>
    /// Injects the chunk configuration from <see cref="TrackManager"/> before calling <see cref="Generate"/>.
    /// </summary>
    /// <param name="knots">Number of Bezier knots in this chunk.</param>
    /// <param name="spacing">Distance between consecutive knots.</param>
    /// <param name="lateralOffset">Maximum random lateral offset between knots.</param>
    public void Configure(int knots, float spacing, float lateralOffset) {
        knotCount = knots;
        knotSpacing = spacing;
        maxLateralOffset = lateralOffset;
    }

    /// <summary>
    /// Gets the world-space position of this chunk's last knot.
    /// <see cref="TrackManager"/> uses this as the start position for the next chunk.
    /// </summary>
    public Vector3 lastKnotWorldPosition { get; private set; }

    /// <summary>
    /// Gets the normalised outgoing direction at this chunk's last knot.
    /// Used in the next chunk to ensure a smooth junction.
    /// </summary>
    public Vector3 lastTangentDirection { get; private set; } = Vector3.forward;

    /// <summary>
    /// Gets the total length of a chunk due the knotCount and knotSpacing.
    /// </summary>
    public float chunkLength => (knotCount - 1) * knotSpacing;

    private SplineContainer splineContainer;
    private SplineRoadMesh roadMesh;

    private void Awake() {
        splineContainer = GetComponent<SplineContainer>();
        roadMesh = GetComponent<SplineRoadMesh>();
    }

    /// <summary>
    /// Builds the spline for this chunk and positions the GameObject in the world.
    /// Knot 0 is placed at <paramref name="startWorldPos"/>, knot 1 continues in
    /// <paramref name="incomingDirection"/> for a smooth junction with the previous chunk,
    /// and subsequent knots drift randomly within <c>maxLateralOffset</c>.
    /// </summary>
    /// <param name="startWorldPos">
    /// World position of this chunk's first knot (equal to the previous chunk's
    /// <see cref="lastKnotWorldPosition"/>).
    /// </param>
    /// <param name="incomingDirection">
    /// Normalised direction the previous chunk was travelling at its last knot.
    /// Used to align knot 1 so the spline junction appears smooth.
    /// </param>
    public void Generate(Vector3 startWorldPos, Vector3 incomingDirection) {

        // Place this GameObject at the chunk start
        transform.position = startWorldPos;

        Spline spline = new Spline();
        float currentX = 0f;

        for (int i = 0; i < knotCount; i++) {
            float z = i * knotSpacing;
            float x;

            if (i == 0) {
                // First knot: always at local origin (world pos = startWorldPos)
                x = 0f;
            }
            else if (i == 1) {
                // Second knot: continue in the incoming direction so the junction is smooth.
                // Project the incoming XZ direction onto X, clamped to maxLateralOffset.
                x = Mathf.Clamp(incomingDirection.x * knotSpacing, -maxLateralOffset, maxLateralOffset);
                currentX = x;
            }
            else {
                // Remaining knots: random lateral drift from the current X position
                float delta = UnityEngine.Random.Range(-maxLateralOffset, maxLateralOffset);
                x = Mathf.Clamp(currentX + delta, -maxLateralOffset * 2f, maxLateralOffset * 2f);
                currentX = x;
            }

            spline.Add(new BezierKnot(new float3(x, 0f, z)));
        }

        spline.SetTangentMode(TangentMode.AutoSmooth);
        spline.Closed = false;
        splineContainer.Spline = spline;

        // --- Calculate last positions to use in the next chunk ---
        float3 lastPos = spline[knotCount - 1].Position;
        float3 secondToLast = spline[knotCount - 2].Position;

        // Convert last local knot position to world space
        lastKnotWorldPosition = transform.TransformPoint(lastPos);

        // Calculate last direction to use in the next chunk
        Vector3 outDir = new Vector3(lastPos.x - secondToLast.x, 0f, lastPos.z - secondToLast.z);
        lastTangentDirection = outDir.normalized;

        // Rebuild the road mesh if the component is present on this GameObject
        roadMesh?.BuildMesh();
    }
}