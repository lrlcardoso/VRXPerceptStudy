using UnityEngine;

public class VRPractice : MonoBehaviour
{
    private GameObject bubblePrefab;
    private PinchControl pinchControl;
    private Vector3 spawnAreaMin = new Vector3(-0.5f, 0.8f, 0.3f);
    private Vector3 spawnAreaMax = new Vector3(0.5f, 1.3f, 0.5f);
    private Material material1;
    private Material material2;
    private GameObject indexSphere; // Reference to the index finger sphere
    private GameObject thumbSphere; // Reference to the thumb sphere
    public bool isAble2pinch = false;

    private void Start()
    {
        pinchControl = GameObject.Find("Rig/Camera Offset/RightHand").GetComponent<PinchControl>();
        bubblePrefab = Instantiate(Resources.Load<GameObject>("Prefabs/Bubble"), Vector3.zero, Quaternion.identity);
        
        // Load materials from Resources folder
        material1 = Resources.Load<Material>("Materials/BubbleFinger");
        material2 = Resources.Load<Material>("Materials/BubbleThumb");

        // Find the spheres in the hierarchy
        indexSphere = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_IndexMetacarpal/R_IndexProximal/R_IndexIntermediate/R_IndexDistal/R_IndexTip/IndexSphere");
        thumbSphere = GameObject.Find("Rig/Camera Offset/RightHand/R_Wrist/R_ThumbMetacarpal/R_ThumbProximal/R_ThumbDistal/R_ThumbTip/ThumbSphere");

        // Instantiate 16 objects with alternating tags and materials
        for (int i = 0; i < 16; ++i)
        {
            SpawnBubble(i);
        }
    }

    void Update()
    {
        if (pinchControl.x > 0.6f)
        {
            // Turn on the spheres
            indexSphere.SetActive(true);
            thumbSphere.SetActive(true);
            isAble2pinch = true;
        }
        else
        {
            // Turn off the spheres
            indexSphere.SetActive(false);
            thumbSphere.SetActive(false);
            isAble2pinch = false;
        }
    }

    private void SpawnBubble(int index)
    {
        // Generate random position within the specified spawn area
        Vector3 spawnPosition = new Vector3(
            Random.Range(spawnAreaMin.x, spawnAreaMax.x),
            Random.Range(spawnAreaMin.y, spawnAreaMax.y),
            Random.Range(spawnAreaMin.z, spawnAreaMax.z)
        );

        // Instantiate the bubble at the random position
        GameObject bubble = Instantiate(bubblePrefab, spawnPosition, Quaternion.identity);

        // Assign tag and material based on the index
        if (index % 2 == 0)
        {
            bubble.tag = "R_IndexTip";
            bubble.GetComponent<Renderer>().material = material1;
        }
        else
        {
            bubble.tag = "R_ThumbTip";
            bubble.GetComponent<Renderer>().material = material2;
        }
    }
}
