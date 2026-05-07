using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Procedurally generates a flat road mesh in triangles along the <see cref="SplineContainer"/>
/// attached to the same GameObject.
/// </summary>
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplineRoadMesh : MonoBehaviour {

    /// <summary>Width of the road in world units.</summary>
    [Tooltip("Width of the road in world units")]
    [SerializeField] private float roadWidth = 4f;

    /// <summary>
    /// Number of quad segments used to approximate the spline curve.
    /// Higher values produce smoother curves at the cost of more vertices.
    /// </summary>
    [Tooltip("Number of quad segments along the spline (higher = smoother curves)")]
    [SerializeField] private int resolution = 30;

    private SplineContainer splineContainer;
    private MeshFilter meshFilter;

    private void Awake() {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();
    }

    /// <summary>
    /// Builds (or rebuilds) the road mesh by sampling the attached spline.
    /// Called automatically by <see cref="ProceduralSplineGenerator.Generate"/> after
    /// the spline is updated.
    /// </summary>
    public void BuildMesh() {
        Spline spline = splineContainer.Spline;
        if (spline == null || spline.Count < 2) return;

        Mesh mesh = new Mesh { name = "RoadMesh" };

        // Two vertices (left + right) per sample point
        Vector3[] vertices = new Vector3[resolution * 2];

        // Two triangles (6 indices) per quad between consecutive sample points
        int[] triangles = new int[(resolution - 1) * 6];

        for (int i = 0; i < resolution; i++) {
            // t values between [0, 1]: normalised position along the spline
            float t = (float) i / (float)(resolution - 1);

            SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tangent, out float3 upVec);

            // Cross(up, tangent) yields the right-hand side direction with normals facing +Y.
            Vector3 right = Vector3.Cross((Vector3)upVec, (Vector3)tangent).normalized;

            float halfWidth = roadWidth * 0.5f;
            vertices[i * 2] = (Vector3)pos - right * halfWidth; // left edge
            vertices[i * 2 + 1] = (Vector3)pos + right * halfWidth; // right edge
        }

        // Build quads between consecutive sample pairs
        int triangleIndex = 0;
        for (int i = 0; i < resolution - 1; i++) {
            int leftCurrent  = i * 2;
            int rightCurrent = i * 2 + 1;
            int leftNext     = (i + 1) * 2;
            int rightNext    = (i + 1) * 2 + 1;

            // First triangle of the quad (upper-left half)
            triangles[triangleIndex++] = leftCurrent;
            triangles[triangleIndex++] = leftNext;
            triangles[triangleIndex++] = rightCurrent;

            // Second triangle of the quad (lower-right half)
            triangles[triangleIndex++] = rightCurrent;
            triangles[triangleIndex++] = leftNext;
            triangles[triangleIndex++] = rightNext;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;

        // Recalculate light direction foreach triangle and collision mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }
}
