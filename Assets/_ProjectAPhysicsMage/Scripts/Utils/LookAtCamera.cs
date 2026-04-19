using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private Camera mainCamera;
    void Start()
    {
        mainCamera = Camera.main;
    }
    void LateUpdate()
    {
        transform.LookAt(mainCamera.transform.position);
        transform.Rotate(0, 180, 0);
    }
}
