
using UnityEngine;

public class MouseDragRotate : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private bool invertRotation = false;

    private void OnMouseDrag()
    {
        float mouseX = Input.GetAxisRaw("Mouse X");

        if (invertRotation)
            mouseX = -mouseX;

        transform.Rotate(
            Vector3.up,
            mouseX * rotationSpeed,
            Space.World
        );
    }
}