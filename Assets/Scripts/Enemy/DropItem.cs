using UnityEngine;
using System.Collections;

public class DropItem : MonoBehaviour
{
    public enum DropType { Coin, Health, Shield, Ammo }
    public DropType dropType;

    [Header("Visual Settings")]
    public float floatHeight = 0.25f;
    public float floatSpeed = 2f;
    public float rotationSpeed = 50f;
    public float despawnTime = 60f;
    public float baseScale = 0.3f;

    [Header("Pickup Settings")]
    public float pickupRange = 2.5f;

    private Vector3 startPos;
    private bool collected = false;
    private GameObject player;
    private PlayerInventory playerInventory;
    private PlayerHealth playerHealth;
    private PlayerGunSystem playerGunSystem;
    private PlayerUI playerUI;

    private int value;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        playerInventory = player.GetComponent<PlayerInventory>();
        playerHealth = player.GetComponent<PlayerHealth>();
        playerGunSystem = player.GetComponent<PlayerGunSystem>();
        playerUI = Object.FindFirstObjectByType<PlayerUI>();

        startPos = transform.position;

        switch (dropType)
        {
            case DropType.Coin:
                value = Random.Range(3, 11);
                break;
            case DropType.Health:
                value = Random.Range(5, 26);
                break;
            case DropType.Shield:
                value = Random.value > 0.5f ? 25 : 50;
                break;
            case DropType.Ammo:
                if (playerGunSystem != null && playerGunSystem.currentGun != null)
                    value = Random.Range(5, playerGunSystem.currentGun.maxAmmo + 1);
                else
                    value = 5;
                break;
        }

        float scaleVariance = Random.Range(0.9f, 1.1f);
        transform.localScale = Vector3.one * baseScale * scaleVariance;

        StartCoroutine(FloatAndSpin());
        StartCoroutine(AutoDespawn());
    }

    IEnumerator FloatAndSpin()
    {
        float offset = Random.Range(0f, Mathf.PI * 2);
        while (!collected)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed + offset) * floatHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    IEnumerator AutoDespawn()
    {
        yield return new WaitForSeconds(despawnTime);
        if (!collected)
            Destroy(gameObject);
    }

    void Update()
    {
        if (collected || player == null) return;

        if (Vector3.Distance(transform.position, player.transform.position) <= pickupRange)
            TryCollect();
    }

    void TryCollect()
    {
        if (dropType == DropType.Health && playerHealth.currentHealth >= playerHealth.maxHealth)
            return;

        if (dropType == DropType.Shield && playerHealth.currentOvershield >= playerHealth.maxOvershield)
            return;

        Collect();
    }

    void Collect()
    {
        if (collected) return;
        collected = true;

        switch (dropType)
        {
            case DropType.Coin:
                playerInventory.AddCoins(value);
                Debug.Log($"💰 Collected {value} coins!");
                break;

            case DropType.Health:
                playerHealth.Heal(value);
                Debug.Log($"❤️ Healed {value} HP!");
                break;

            case DropType.Shield:
                playerHealth.AddOvershield(value);
                Debug.Log($"🛡 Gained {value} overshield!");
                break;

            case DropType.Ammo:
                if (playerGunSystem != null && playerGunSystem.currentGun != null)
                {
                    playerGunSystem.ammo += value;
                    playerGunSystem.ammo = Mathf.Clamp(playerGunSystem.ammo, 0, playerGunSystem.currentGun.maxAmmo);
                    playerUI?.UpdateAmmo(playerGunSystem.ammo, playerGunSystem.currentGun.maxAmmo);
                    Debug.Log($"🔫 Picked up {value} ammo!");
                }
                else
                {
                    playerInventory.AddCoins(5);
                    Debug.Log("🎲 No gun — converted ammo drop to 5 coins!");
                }
                break;
        }

        Destroy(gameObject);
    }
}
