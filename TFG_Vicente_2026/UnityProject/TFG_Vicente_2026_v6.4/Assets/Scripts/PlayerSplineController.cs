using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player forward along the generated spline and reads continuous lateral input.
/// </summary>
public class PlayerSplineController : SplineAnimateTrackFollower {
    [Header("Movement")]
    [SerializeField, Min(0f)] private float lateralSpeed = 6f;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Gameplay";
    [SerializeField] private string moveLateralActionName = "MoveLateral";

    [Header("Enemy Detection")]
    [SerializeField, Min(0.05f)] private float enemyCheckRadius = 0.45f;
    [SerializeField] private LayerMask enemyLayers = ~0;

    private InputAction moveLateralAction;
    private InputAction fallbackMoveAction;
    private EnemyBase lastDetectedEnemy;

    /// <summary>
    /// Player has to move when the spline it's generated. In addition to that, it creates a visual model if necessary, and prepare the input to move.
    /// </summary>
    protected override void Awake() {
        base.Awake();
        playOnTrackGenerated = true;
        EnsureDefaultVisual();
        ResolveMoveAction();
    }

    protected override void OnEnable() {
        base.OnEnable();
        ResolveMoveAction();
        moveLateralAction?.Enable();
    }

    protected override void OnDisable() {
        moveLateralAction?.Disable();
        base.OnDisable();
    }

    /// <summary>
    /// On every update, apply the forward speed, move the lateral position and check if a any enemy collides with the Player.
    /// </summary>
    private void Update() {
        if (!HasReadyTrack) return;

        SetForwardSpeed(forwardSpeed);

        float lateralInput = moveLateralAction != null ? moveLateralAction.ReadValue<float>() : 0f;
        SetLateralOffset(lateralOffset + lateralInput * lateralSpeed * Time.deltaTime);

        RefreshCurrentPose();
        CheckEnemyOverlap();
    }

    /// <summary>
    /// First try to use the input configured in the Action.
    /// If can't be used, create a default behaviour using A/D or Left/Right arrows
    /// </summary>
    private void ResolveMoveAction() {
        if (moveLateralAction != null) return;

        if (inputActions != null) {
            InputActionMap map = inputActions.FindActionMap(actionMapName, false);
            moveLateralAction = map?.FindAction(moveLateralActionName, false);
        }

        if (moveLateralAction == null) {
            fallbackMoveAction = new InputAction("MoveLateral", InputActionType.Value, expectedControlType: "Axis");
            fallbackMoveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            fallbackMoveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            moveLateralAction = fallbackMoveAction;
        }
    }

    /// <summary>
    /// Check in a Sphere around the Player, if exist a collide with an Enemy.
    /// In case the collide exists, save the last enemy collided to evade multiple collides, and create a debug log.
    /// </summary>
    private void CheckEnemyOverlap() {
        Vector3 checkPosition = Body != null ? Body.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(checkPosition, enemyCheckRadius, enemyLayers, QueryTriggerInteraction.Collide);

        EnemyBase detectedEnemy = null;
        for (int i = 0; i < hits.Length; i++) {
            detectedEnemy = hits[i].GetComponentInParent<EnemyBase>();
            if (detectedEnemy != null)
                break;
        }

        if (detectedEnemy == null) {
            lastDetectedEnemy = null;
            return;
        }

        if (detectedEnemy == lastDetectedEnemy) return;

        lastDetectedEnemy = detectedEnemy;
        Debug.Log($"Player hit enemy: {detectedEnemy.name}", detectedEnemy);
    }

    /// <summary>
    /// If Player don't have a model, create a capsule.
    /// </summary>
    private void EnsureDefaultVisual() {
        if (Body.GetComponentInChildren<Renderer>() != null) return;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Player Visual";
        visual.transform.SetParent(Body, false);
        visual.transform.localPosition = Vector3.up * 0.5f;
        visual.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
    }
}
