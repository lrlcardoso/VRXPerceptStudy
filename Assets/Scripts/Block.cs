using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Block : MonoBehaviour
{
    AudioSource dropBlockSound; // Sound to play on collision
    private bool hasCollided = false; // Flag to ensure the sound plays only once
    private ExperimentManager experimentManager;
    private Vector3 newPosition;

    void Awake() 
    {
        dropBlockSound = GetComponent<AudioSource>();
        experimentManager = GameObject.Find("Experiment Manager").GetComponent<ExperimentManager>();
        newPosition = new Vector3(0.15f, experimentManager.TableHeight-0.005f+0.03f, experimentManager.calibratedPos.z-0.084f); 
    }

    void Update()
    {
        // Check if the collision is with the floor
        if (transform.position.y<=0)
        {
            transform.position = newPosition;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collision is with the table surface
        if (collision.gameObject.CompareTag("TableSurface") && !hasCollided)
        {
            // Play the collision sound
            dropBlockSound.Play();
            hasCollided = true; // Set the flag to true to prevent repeated plays
        }
    }
}

