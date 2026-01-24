using UnityEngine;

public class GunRewardPad : MonoBehaviour
{
    [Header("Gun Reward Settings")]
    [Tooltip("List of possible guns this pad can give.")]
    public GunData[] possibleGuns;

    [Tooltip("Cooldown time between uses.")]
    public float cooldownTime = 5f;

    [Header("Pad Feedback")]
    public Renderer padRenderer;
    public Color readyColor = Color.green;
    public Color cooldownColor = Color.red;
    public float flashTime = 0.3f;

    private bool canUse = true;

    void Start()
    {
        if (padRenderer == null)
            padRenderer = GetComponentInChildren<Renderer>();

        if (padRenderer)
            padRenderer.material.color = readyColor;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!canUse) return;
        if (!other.CompareTag("Player")) return;

        PlayerGunSystem gunSystem = other.GetComponent<PlayerGunSystem>();
        if (gunSystem == null) return;

        if (possibleGuns.Length == 0)
        {
            Debug.LogWarning("No guns assigned to GunRewardPad!");
            return;
        }

        // Pick a random gun
        GunData randomGun = possibleGuns[Random.Range(0, possibleGuns.Length)];

        // Give the gun
        gunSystem.GiveGun(randomGun);
        Debug.Log($"Player received random gun: {randomGun.gunName}");

        // Trigger cooldown + feedback
        StartCoroutine(CooldownRoutine());
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        canUse = false;

        if (padRenderer)
            padRenderer.material.color = cooldownColor;

        yield return new WaitForSeconds(cooldownTime);

        if (padRenderer)
        {
            padRenderer.material.color = readyColor;

            // Optional quick flash effect
            padRenderer.material.color = Color.white;
            yield return new WaitForSeconds(flashTime);
            padRenderer.material.color = readyColor;
        }

        canUse = true;
        Debug.Log("GunRewardPad ready again.");
    }
}
