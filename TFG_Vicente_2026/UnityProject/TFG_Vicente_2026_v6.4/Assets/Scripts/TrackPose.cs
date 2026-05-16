using UnityEngine;

/// <summary>
/// Structure that defines the position, direction and movement of the spline in a specific point (t).
/// </summary>
public struct TrackPose {
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 tangent;
    public Vector3 right;
    public Vector3 up;

    public TrackPose(Vector3 position, Quaternion rotation, Vector3 tangent, Vector3 right, Vector3 up) {
        this.position = position;
        this.rotation = rotation;
        this.tangent = tangent;
        this.right = right;
        this.up = up;
    }
}
