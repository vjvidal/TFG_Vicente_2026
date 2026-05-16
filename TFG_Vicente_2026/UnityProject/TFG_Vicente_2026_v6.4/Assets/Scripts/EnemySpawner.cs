using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedurally places static and moving enemies along the generated spline.
/// </summary>
public class EnemySpawner : MonoBehaviour {
    [Header("References")]
    [SerializeField] private TrackManager track;
    [SerializeField] private GameObject staticEnemyPrefab;
    [SerializeField] private GameObject movingEnemyPrefab;

    [Header("Spawn Range")]
    [SerializeField, Min(0)] private int enemyCount = 12;
    [SerializeField, Range(0f, 1f)] private float startT = 0.12f;
    [SerializeField, Range(0f, 1f)] private float endT = 0.95f;
    [SerializeField, Min(0f)] private float lateralPadding = 0.5f;

    [Header("Enemy Mix")]
    [SerializeField, Range(0f, 1f)] private float movingEnemyChance = 0.6f;
    [SerializeField, Min(0f)] private float movingEnemyAmplitude = 1.2f;
    [SerializeField, Min(0f)] private float movingEnemyFrequency = 0.5f;

    private readonly List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Awake() {
        if (track == null)
            track = FindAnyObjectByType<TrackManager>();
    }

    /// <summary>
    /// On enable subscribes to the event to check if the spline it's generated
    /// </summary>
    private void OnEnable() {
        if (track == null)
            track = FindAnyObjectByType<TrackManager>();

        if (track == null) return;

        track.OnTrackGenerated += HandleTrackGenerated;

        if (track.IsReady)
            SpawnEnemies();
    }

    private void OnDisable() {
        if (track != null)
            track.OnTrackGenerated -= HandleTrackGenerated;
    }

    private void HandleTrackGenerated(TrackManager generatedTrack) {
        track = generatedTrack;
        SpawnEnemies();
    }

    public void SpawnEnemies() {
        if (track == null || !track.IsReady) return;

        ClearSpawnedEnemies();

        float minT = Mathf.Min(startT, endT);
        float maxT = Mathf.Max(startT, endT);
        float lateralLimit = Mathf.Max(0f, track.HalfRoadWidth - lateralPadding);

        for (int i = 0; i < enemyCount; i++) {
            float t = enemyCount == 1 ? Mathf.Lerp(minT, maxT, 0.5f) : Mathf.Lerp(minT, maxT, (i + 0.5f) / enemyCount);
            float lateralOffset = Random.Range(-lateralLimit, lateralLimit);
            bool spawnMoving = Random.value < movingEnemyChance;

            GameObject enemy = spawnMoving
                ? CreateMovingEnemy(t, lateralOffset, lateralLimit)
                : CreateStaticEnemy(t, lateralOffset);

            enemy.transform.SetParent(transform, true);
            spawnedEnemies.Add(enemy);
        }

    }

    private GameObject CreateStaticEnemy(float t, float lateralOffset) {
        GameObject enemy = staticEnemyPrefab != null ? Instantiate(staticEnemyPrefab) : new GameObject("Static Enemy");
        StaticEnemy staticEnemy = enemy.GetComponent<StaticEnemy>() ?? enemy.AddComponent<StaticEnemy>();
        staticEnemy.Initialize(track, t, lateralOffset);
        return enemy;
    }

    private GameObject CreateMovingEnemy(float t, float centerOffset, float lateralLimit) {
        GameObject enemy = movingEnemyPrefab != null ? Instantiate(movingEnemyPrefab) : new GameObject("Moving Enemy");
        MovingEnemy movingEnemy = enemy.GetComponent<MovingEnemy>() ?? enemy.AddComponent<MovingEnemy>();
        float amplitude = Mathf.Min(movingEnemyAmplitude, Mathf.Max(0f, lateralLimit - Mathf.Abs(centerOffset)));
        movingEnemy.Initialize(track, t, centerOffset, amplitude, movingEnemyFrequency, Random.Range(0f, Mathf.PI * 2f));
        return enemy;
    }

    private void ClearSpawnedEnemies() {
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--) {
            if (spawnedEnemies[i] != null)
                Destroy(spawnedEnemies[i]);
        }

        spawnedEnemies.Clear();
    }
}
