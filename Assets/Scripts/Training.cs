using UnityEngine;

public class Training : MonoBehaviour
{
    public GameObject bubblePrefab;
    public float spawnInterval = 1.0f;
    public Vector3 spawnAreaMin;
    public Vector3 spawnAreaMax;

    private void Start()
    {
        // Start spawning bubbles
        InvokeRepeating("SpawnBubble", 0.0f, spawnInterval);
    }

    private void SpawnBubble()
    {
        // Generate random position within the specified spawn area
        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y),
            Random.Range(spawnAreaMin.z, spawnAreaMax.z)
        );

        // Instantiate the bubble at the random position
        Instantiate(bubblePrefab, spawnPosition, Quaternion.identity);
    }
}
