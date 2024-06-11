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
    public bool popped = false;
    private GameObject block; 
    private string blockPrefabPath = "Prefabs/block";

    void Awake() 
    {
        audioSource = GetComponent<AudioSource>();
        vrPractice = GameObject.Find("VRPractice").GetComponent<VRPractice>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name == gameObject.tag & vrPractice.isAble2pinch)
        {
          popped = true;
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

      // Get the position of the bubble
      Vector3 blockPosition = transform.position;

      // Destroy the bubble
      Destroy(gameObject);

      block = Resources.Load<GameObject>(blockPrefabPath);

      // Instantiate the block at the position of the bubble
      Instantiate(block, blockPosition, Quaternion.Euler(30.0f,30.0f,50.0f));
  }
}


