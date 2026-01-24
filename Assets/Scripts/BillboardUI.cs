using UnityEngine;

public class BillboardUI : MonoBehaviour
{
    public Camera targetCamera;

    void LateUpdate()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        transform.LookAt(transform.position + targetCamera.transform.forward);
    }
}