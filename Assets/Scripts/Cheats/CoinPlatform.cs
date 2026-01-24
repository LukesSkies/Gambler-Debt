using UnityEngine;
using System.Collections;

public class CoinPlatform : MonoBehaviour
{
    public int coinAmount = 1;
    public float giveInterval = 1f;

    private bool giving;
    private PlayerInventory player;

    void Start()
    {
        player = Object.FindFirstObjectByType<PlayerInventory>();

        if (player == null)
            Debug.LogWarning("No PlayerInventory found in the scene!");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerInventory>() != null)
        {
            giving = true;
            StartCoroutine(GiveCoins());
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerInventory>() != null)
        {
            giving = false;
        }
    }

    IEnumerator GiveCoins()
    {
        while (giving)
        {
            if (player != null)
                player.AddCoins(coinAmount);

            yield return new WaitForSeconds(giveInterval);
        }
    }
}
