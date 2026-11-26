using UnityEngine;

public class PlayerInsideBuilding : MonoBehaviour
{
    public bool PlayerInBuilding = false;

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == "InsideTrigger")
        {
            PlayerInBuilding = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "InsideTrigger")
        {
            PlayerInBuilding = false;
        }
    }
}
