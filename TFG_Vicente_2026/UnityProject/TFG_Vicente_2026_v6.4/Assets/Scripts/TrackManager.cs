using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the procedurally generated infinite track. Generates new chunks and delete the old ones.
/// </summary>
/// <remarks>
/// <para>
/// Maintains a limited size queue of active <see cref="ProceduralSplineGenerator"/> chunks.
/// Each frame it checks if old chunks should be destroyed (those far behind the player)
/// and if new chunks need to be spawned.
/// </para>
/// <para>
/// All chunk shape parameters (<c>knotCount</c>, <c>knotSpacing</c>, <c>maxLateralOffset</c>)
/// are defined in this script and injected into each chunk via <see cref="ProceduralSplineGenerator.Configure"/>
/// so they can be tuned in a single Inspector location.
/// </para>
/// </remarks>
public class TrackManager : MonoBehaviour {
    [Header("References")]
    [Tooltip("Chunk prefab with ProceduralSplineGenerator")]
    [SerializeField] private ProceduralSplineGenerator chunkPrefab;

    [Tooltip("Player transform TODO")]
    [SerializeField] private Transform player;

    [Header("Track Settings")]
    [Tooltip("How many chunks are kept active simultaneously")]
    [SerializeField] private int maxActiveChunks = 4;

    [Tooltip("Distance behind the player where a chunk is destroyed")]
    [SerializeField] private float destroyBehindDistance = 20f;

    [Header("Chunk Information")]
    [Tooltip("Number of Bezier knots per chunk")]
    [SerializeField] private int knotCount = 6;

    [Tooltip("Distance between consecutive knots")]
    [SerializeField] private float knotSpacing = 10f;

    [Tooltip("Maximum random lateral offset between knots")]
    [SerializeField] private float maxLateralOffset = 4f;

    /// <summary>Queue of active chunks. Front = oldest, back = newest.</summary>
    private Queue<ProceduralSplineGenerator> activeChunks = new Queue<ProceduralSplineGenerator>();

    /// <summary>World-space start position for the next chunk to be spawned.</summary>
    private Vector3 nextChunkStartPos = Vector3.zero;

    /// <summary>
    /// Incoming travel direction passed to the next spawned chunk to ensure
    /// a smooth spline junction. Initialised to <see cref="Vector3.forward"/>
    /// so the first chunk goes straight.
    /// </summary>
    private Vector3 nextChunkDirection = Vector3.forward;

    /// <summary>
    /// Spawns the initial set of chunks at game start.
    /// </summary>
    private void Start() {
        for (int i = 0; i < maxActiveChunks; i++) {
            SpawnNextChunk();
        }
    }

    /// <summary>
    /// Every frame: destroys chunks that are too far behind the player and
    /// spawns new ones at the front if the queue has less than <c>maxActiveChunks</c>.
    /// </summary>
    private void Update() {
        if (player == null) return;

        TryDestroyOldChunk();

        // Keep spawning until we have enough chunks ahead
        while (activeChunks.Count < maxActiveChunks) {
            SpawnNextChunk();
        }
    }

    /// <summary>
    /// Destroys the oldest chunk in the queue if the player has passed it
    /// by more than <c>destroyBehindDistance</c> units.
    /// </summary>
    private void TryDestroyOldChunk() {
        if (activeChunks.Count == 0) return;

        ProceduralSplineGenerator oldest = activeChunks.Peek();
        float chunkEndZ = oldest.transform.position.z + oldest.chunkLength;

        if (player.position.z - chunkEndZ > destroyBehindDistance) {
            activeChunks.Dequeue();
            Destroy(oldest.gameObject);
        }
    }

    /// <summary>
    /// Instantiates a new chunk prefab, configures it with the shared shape settings,
    /// generates its spline connected to the previous chunk, and enqueues it.
    /// </summary>
    private void SpawnNextChunk() {
        ProceduralSplineGenerator newChunk = Instantiate(chunkPrefab);

        // Configure the chunk with the settings configured in TrackManager
        newChunk.Configure(knotCount, knotSpacing, maxLateralOffset);

        // Pass start position and the incoming direction for a smooth junction
        newChunk.Generate(nextChunkStartPos, nextChunkDirection);

        // Store values for the next chunk
        nextChunkStartPos = newChunk.lastKnotWorldPosition;
        nextChunkDirection = newChunk.lastTangentDirection;

        activeChunks.Enqueue(newChunk);
    }
}
