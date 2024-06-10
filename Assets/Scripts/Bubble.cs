using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Ports;

[RequireComponent(typeof(AudioSource))]
public class Bubble : MonoBehaviour 
{
    AudioSource audioSource;
    private VRPractice vrPractice;

    void Awake() 
    {
        audioSource = GetComponent<AudioSource>();
        //vrPractice = Resources.Load<GameObject>("Prefabs/VR Practice").GetComponent<VRPractice>();
        vrPractice = GameObject.Find("VRPractice").GetComponent<VRPractice>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == gameObject.tag & vrPractice.isAble2pinch)
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
        touchDetector.HapticControl(whatTouched);
      }

      // Now can destroy the bubble
      Destroy(gameObject);
  }
}


