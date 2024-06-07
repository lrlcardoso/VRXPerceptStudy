using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

[RequireComponent(typeof(AudioSource))]
public class Bubble : MonoBehaviour 
{
    AudioSource audioSource;
    private TouchDetection touchDetection;

    void Awake() 
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to the player's hand or pointer
        if (other.name == "R_IndexTip" | other.name == "R_ThumbTip")
        {
          TouchDetection touchDetector = other.GetComponent<TouchDetection>();
          Pop(touchDetector, other.name);
        }
    }

  public void Pop(TouchDetection touchDetector, string whatTouched) 
  {
      audioSource.Play();
      StartCoroutine(DestroyAfterSoundAndHaptic(touchDetector, whatTouched));
  }

  IEnumerator DestroyAfterSoundAndHaptic(TouchDetection touchDetector, string whatTouched) 
  {   
      // Wait until the sound is played
      while (audioSource.isPlaying) 
      {
          yield return null;
      }

      if (touchDetector.indexON | touchDetector.thumbON)
      {
        // Call HapticControl on the provided touchDetector after 0.5 seconds
        touchDetector.HapticControl(whatTouched);
        if (touchDetector.indexON)
        {
          touchDetector.indexON = !touchDetector.indexON;
        }
        else
        {
          touchDetector.thumbON = !touchDetector.thumbON;
        }
      }

      // Now can destroy the bubble
      Destroy(gameObject);
  }
}
