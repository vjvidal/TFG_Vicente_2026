using UnityEngine;

/// <summary>
/// Base class for the Enemies.
/// </summary>
public abstract class EnemyBase : SplineAnimateTrackFollower {
    [Header("Enemy Visual")]
    [SerializeField] private PrimitiveType enemyForm = PrimitiveType.Cube;
    [SerializeField] private Vector3 fallbackScale = Vector3.one;

    protected override void Awake() {
        base.Awake();
        EnsureDefaultVisual();
    }

    /// <summary>
    /// Same as player. If the enemy don't have any model, create a primitive as a enemy defined in enemyForm.
    /// In addition to that, activate the trigger to avoid be a "physic wall" to the player. Using this way, the player
    /// can go through the enemies.
    /// </summary>
    protected void EnsureDefaultVisual() {
        if (Body.GetComponentInChildren<Renderer>() != null) return;

        GameObject visual = GameObject.CreatePrimitive(enemyForm);
        visual.name = "Enemy Visual";
        visual.transform.SetParent(Body, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = fallbackScale;

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            visualCollider.isTrigger = true;
    }
}
