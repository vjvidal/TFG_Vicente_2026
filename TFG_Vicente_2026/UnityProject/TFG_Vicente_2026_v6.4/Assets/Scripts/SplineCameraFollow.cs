using UnityEngine;

/// <summary>
/// Smooth camera follow that looks ahead along the player's current spline direction.
/// </summary>
public class SplineCameraFollow : MonoBehaviour {
    [SerializeField] private PlayerSplineController target;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 5f, -8f);
    [SerializeField, Min(0f)] private float lookAheadDistance = 12f;
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.25f;
    [SerializeField, Min(0.01f)] private float lookTargetSmoothTime = 0.15f;
    [SerializeField, Min(0f)] private float followYawSmoothSpeed = 5f;
    [SerializeField, Min(0f)] private float rotationLerpSpeed = 5f;

    private Vector3 positionVelocity;
    private Vector3 lookTargetVelocity;
    private Vector3 smoothedLookTarget;
    private Quaternion smoothedFollowRotation = Quaternion.identity;
    private bool hasSmoothedState;

    private void Awake() {
        if (target == null)
            target = FindAnyObjectByType<PlayerSplineController>();
    }

    private void LateUpdate() {
        if (target == null) {
            target = FindAnyObjectByType<PlayerSplineController>();
            if (target == null) return;
        }

        target.RefreshCurrentPose();

        Transform targetBody = target.Body != null ? target.Body : target.transform;
        Vector3 desiredForward = Vector3.ProjectOnPlane(target.transform.forward, Vector3.up);
        if (desiredForward.sqrMagnitude <= Mathf.Epsilon)
            desiredForward = Vector3.forward;

        desiredForward.Normalize();

        Quaternion desiredFollowRotation = Quaternion.LookRotation(desiredForward, Vector3.up);

        // Smooth the rotation of camera
        if (!hasSmoothedState) {
            smoothedFollowRotation = desiredFollowRotation;
            smoothedLookTarget = targetBody.position + desiredForward * lookAheadDistance;
            hasSmoothedState = true;
        }

        float yawSmoothFactor = 1f - Mathf.Exp(-followYawSmoothSpeed * Time.deltaTime);
        smoothedFollowRotation = Quaternion.Slerp(smoothedFollowRotation, desiredFollowRotation, yawSmoothFactor);

        Vector3 smoothedForward = smoothedFollowRotation * Vector3.forward;
        Vector3 desiredPosition = targetBody.position + smoothedFollowRotation * localOffset;
        Vector3 desiredLookTarget = targetBody.position + smoothedForward * lookAheadDistance;

        smoothedLookTarget = Vector3.SmoothDamp(
            smoothedLookTarget,
            desiredLookTarget,
            ref lookTargetVelocity,
            lookTargetSmoothTime
        );

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        Vector3 lookDirection = smoothedLookTarget - transform.position;
        if (lookDirection.sqrMagnitude <= Mathf.Epsilon) return;

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationLerpSpeed * Time.deltaTime);
    }
}
