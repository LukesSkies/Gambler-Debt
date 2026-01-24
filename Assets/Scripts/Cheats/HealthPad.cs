using UnityEngine;

public class HealthPad : MonoBehaviour
{
    public float healPerSecond = 10f;

    void OnTriggerStay(Collider other)
    {
        var health = other.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.Heal(healPerSecond * Time.deltaTime);
        }
    }
}
