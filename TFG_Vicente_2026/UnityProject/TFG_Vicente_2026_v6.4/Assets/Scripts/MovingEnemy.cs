using UnityEngine;

/// <summary>
/// Enemy that moves in the Spline. By default, only moves horizontally, but it's prepared to configure a vertical movement too.
/// </summary>
public class MovingEnemy : EnemyBase {
    [Header("Lateral Movement")]
    [SerializeField] private float centerOffset = 0f;
    [SerializeField, Min(0f)] private float amplitude = 1f;
    [SerializeField, Min(0f)] private float lateralFrequency = 1f;

    [Header("Forward Movement")]
    [SerializeField] private bool moveForward = false;

    private float phase;

    public void Initialize(TrackManager trackManager, float t, float center, float movementAmplitude, float frequency, float initialPhase) {
        centerOffset = center;
        amplitude = movementAmplitude;
        lateralFrequency = frequency;
        phase = initialPhase;
        base.Initialize(trackManager, t, centerOffset);
        SetForwardSpeed(moveForward ? forwardSpeed : 0f);
    }

    /// <summary>
    /// On update modifies the position of the moving Enemy.
    /// Using the sin calc, the lateral movement oscilates between the whole road constructed in the Spline using the Spline Extruder
    /// With the lateralFrequency, it controls how many oscilations per seconds the enemy does (lateral speed)
    /// It is multiplied by 2*PI because in rads, it's a complete sin wave.
    /// With this formula, the enemy movement its: center > right > center > left > center > right > ...
    /// Phase it's a parameter to have multiple moving enemies with different movements.
    /// </summary>
    private void Update() {
        if (!HasReadyTrack) return;

        SetForwardSpeed(moveForward ? forwardSpeed : 0f);

        float angle = phase + Time.time * lateralFrequency * Mathf.PI * 2f;
        SetLateralOffset(centerOffset + Mathf.Sin(angle) * amplitude);
        RefreshCurrentPose();
    }
}
