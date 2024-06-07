using System.Collections;
using UnityEngine;
using UnityEngine.Events;

  /**
 * A pinchable ball for the ball pinch game.
 * Popping triggers an effect and lets the game know.
 */
  [RequireComponent(typeof(AudioSource))]
  public class Bubble : MonoBehaviour {

    public float floatSpeed = 1.0f;
    public float maxLifetime = 10.0f;

    AudioSource audioSource;
    bool popped = false;

    void Awake() {
      audioSource = GetComponent<AudioSource>();
    }

    public void Pop() {

      popped = true;

      audioSource.Play();
      StartCoroutine(DestroyAfterAudioPlays());
    }

    IEnumerator DestroyAfterAudioPlays() {
      while (audioSource.isPlaying) {
        yield return null;
      }

      Destroy(gameObject);
    }

    private void Start()
    {
        // Destroy the bubble after a certain time to prevent memory leaks
        //Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        // Make the bubble float upwards
        //transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to the player's hand or pointer
        if (other.name == "R_IndexTip")
        {
            Pop();
        }
    }

    //private void Pop()
    //{
        // Add pop effect or sound here if needed
    //    Destroy(gameObject);
    //}
}
