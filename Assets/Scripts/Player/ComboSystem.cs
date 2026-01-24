using UnityEngine;
using System;

public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance;

    [Header("Combo Settings")]
    public float comboDecayRate = 1f;       // how fast combo drains per second
    public float comboGainPerKill = 5f;    // combo added per kill
    public float maxCombo = 100f;
    public float combo = 0f;
    public float noKillResetTime = 3f;     // reset combo if no kills in this time

    [Header("Damage Scaling")]
    public float minDamageMultiplier = 1f;
    public float maxDamageMultiplier = 2.5f;

    [Header("Drop Rate Boost")]
    public float maxDropBoost = 0.5f; // +50% better drop chance at max combo

    [Header("Multikill Settings")]
    public float multikillWindow = 1.0f;
    private float multikillTimer = 0f;
    private int multikillCount = 0;

    public event Action OnComboChanged;
    public event Action<int> OnMultikill;

    private float timeSinceLastKill = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        // decay combo over time
        if (combo > 0)
            OnComboChanged?.Invoke();
        {
            combo -= comboDecayRate * Time.deltaTime;
            combo = Mathf.Clamp(combo, 0, maxCombo);

            timeSinceLastKill += Time.deltaTime;
            if (timeSinceLastKill >= noKillResetTime)
                combo = 0;

            OnComboChanged?.Invoke();
        }

        // multikill timer
        if (multikillCount > 0)
        {
            multikillTimer += Time.deltaTime;

            if (multikillTimer > multikillWindow)
            {
                if (multikillCount > 1)
                    OnMultikill?.Invoke(multikillCount);

                multikillCount = 0;
                multikillTimer = 0f;
            }
        }
    }

    public void RegisterKill()
    {
        combo += comboGainPerKill;
        combo = Mathf.Clamp(combo, 0, maxCombo);

        timeSinceLastKill = 0f;

        multikillCount++;
        multikillTimer = 0f;

        OnComboChanged?.Invoke();
    }

    public float GetDamageMultiplier()
    {
        float t = combo / maxCombo;
        return Mathf.Lerp(minDamageMultiplier, maxDamageMultiplier, t);
    }

    public float GetDropRateBonus()
    {
        return Mathf.Lerp(0f, maxDropBoost, combo / maxCombo);
    }
}
