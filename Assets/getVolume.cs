using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class getVolume : MonoBehaviour
{
    private BoxCollider spawnVolume;
    // Start is called before the first frame update
    void Start()
    {
        GameObject volumeControllerInstance = Instantiate(Resources.Load<GameObject>("Prefabs/VolumeController"));
        spawnVolume = volumeControllerInstance.GetComponent<BoxCollider>();
        Bounds bounds = spawnVolume.bounds;

        Debug.Log(bounds.min.x);
        Debug.Log(bounds.max.x);
        Debug.Log(bounds.min.y);
        Debug.Log(bounds.max.y);
        Debug.Log(bounds.min.z);
        Debug.Log(bounds.max.z);
    }

}

