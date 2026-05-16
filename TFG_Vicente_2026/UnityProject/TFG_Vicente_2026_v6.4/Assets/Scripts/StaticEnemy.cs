/// <summary>
/// Static enemy. This enemy doesn't move in the Spline, it's only generated.
/// </summary>
public class StaticEnemy : EnemyBase {
    /// <summary>
    /// When the spline it's generated, updates the speed to 0. With that this Enemy doesn't move on the Spline.
    /// </summary>
    /// <param name="generatedTrack"></param>
    protected override void HandleTrackGenerated(TrackManager generatedTrack) {
        base.HandleTrackGenerated(generatedTrack);
        SetForwardSpeed(0f);
        RefreshCurrentPose();
    }
}
