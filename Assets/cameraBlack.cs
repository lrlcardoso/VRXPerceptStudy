using UnityEngine;

public class DarknessController : MonoBehaviour
{
    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        TurnOnDarkness();
    }

    // Call this method when you want to make the screen dark
    public void TurnOnDarkness()
    {
        mainCamera.backgroundColor = Color.black;
    }

    // Call this method when you want to revert to normal view
    public void TurnOffDarkness()
    {
        // You can set any other desired background color here
        mainCamera.backgroundColor = Color.clear; // This sets the background to transparent
    }
}
