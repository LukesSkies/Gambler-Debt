
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class PerkCooldown : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image cooldownImage;
    [SerializeField] private KeyCode testKey = KeyCode.Space;

    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownDuration = 10f;

    private Coroutine cooldownRoutine;
    private bool isOnCooldown;

    private void Start()
    {
        if (cooldownImage != null)
            cooldownImage.fillAmount = 1f;
    }

    private void Update()
    {
        // Temporary testing input
        if (Input.GetKeyDown(testKey))
        {
            ActivatePerk();
        }
    }

    public void ActivatePerk()
    {
        if (isOnCooldown)
            return;

        if (cooldownRoutine != null)
            StopCoroutine(cooldownRoutine);

        cooldownRoutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isOnCooldown = true;

        // Empty the radial image immediately
        cooldownImage.fillAmount = 0f;

        float elapsedTime = 0f;

        while (elapsedTime < cooldownDuration)
        {
            elapsedTime += Time.deltaTime;

            cooldownImage.fillAmount =
                Mathf.Clamp01(elapsedTime / cooldownDuration);

            yield return null;
        }

        // Ensure completely full
        cooldownImage.fillAmount = 1f;

        isOnCooldown = false;
        cooldownRoutine = null;
    }
}
