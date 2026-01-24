using UnityEngine;

public class OvershieldPad : MonoBehaviour
{
    public float overshieldAmount = 50f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();
        if (player)
        {
            player.AddOvershield(overshieldAmount);
        }
    }
}
