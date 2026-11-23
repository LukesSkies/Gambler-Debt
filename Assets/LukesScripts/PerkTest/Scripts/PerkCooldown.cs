using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PerkCooldown : MonoBehaviour
{
    [Header("Testing")]
    [SerializeField] private bool enableTestKey = false;
    [SerializeField] private KeyCode testKey = KeyCode.P;

    private Image cooldownImage;

    private float cooldownDuration;
    private Coroutine cooldownRoutine;

    private bool isOnCooldown;

    public bool IsOnCooldown => isOnCooldown;

    private void Update()
    {
        if (enableTestKey && testKey != KeyCode.None)
        {
            if (Input.GetKeyDown(testKey))
                ActivatePerk();
        }
    }

    public void Configure(
        PerkDefinition perk,
        int tier,
        Image targetCooldownImage)
    {
        // Stop an old cooldown when the perk changes or upgrades.
        if (cooldownRoutine != null)
        {
            StopCoroutine(cooldownRoutine);
            cooldownRoutine = null;
        }

        isOnCooldown = false;

        cooldownImage = targetCooldownImage;

        cooldownDuration = perk != null
            ? perk.CooldownDuration
            : 0f;

        if (cooldownImage == null)
            return;

        Sprite tierSprite = perk != null
            ? perk.GetTierSprite(tier)
            : null;

        cooldownImage.sprite = tierSprite;

        // Configure the image for a radial cooldown.
        cooldownImage.type = Image.Type.Filled;
        cooldownImage.fillMethod = Image.FillMethod.Radial360;
        cooldownImage.fillOrigin = (int)Image.Origin360.Top;
        cooldownImage.fillClockwise = true;

        // A completed cooldown is displayed as fully filled.
        cooldownImage.fillAmount = 1f;

        cooldownImage.enabled =
            perk != null && cooldownDuration > 0f;

        cooldownImage.raycastTarget = false;
    }

    public void ActivatePerk()
    {
        if (isOnCooldown)
            return;

        if (cooldownDuration <= 0f)
            return;

        if (cooldownImage == null)
            return;

        if (cooldownRoutine != null)
            StopCoroutine(cooldownRoutine);

        cooldownRoutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;

        // Empty the radial image immediately.
        cooldownImage.fillAmount = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < cooldownDuration)
        {
            elapsedTime += Time.deltaTime;

            cooldownImage.fillAmount = Mathf.Clamp01(
                elapsedTime / cooldownDuration
            );

            yield return null;
        }

        // Ensure the image is completely full.
        cooldownImage.fillAmount = 1f;

        isOnCooldown = false;
        cooldownRoutine = null;
    }

    private void OnDisable()
    {
        if (cooldownRoutine != null)
        {
            StopCoroutine(cooldownRoutine);
            cooldownRoutine = null;
        }

        isOnCooldown = false;

        if (cooldownImage != null)
            cooldownImage.fillAmount = 1f;
    }
}