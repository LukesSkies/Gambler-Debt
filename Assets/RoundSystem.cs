using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundSystem : MonoBehaviour
{
    private static RoundSystem _instance;
    public static RoundSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError("Need Round System in the scene :(");
            }
            return _instance;
        }
    }

    [SerializeField] private List<GameObject> _hudUI;
    [SerializeField] private Animation _blackScreenFadeIn;
    [SerializeField] private Animation _hudFadeIn;
    [SerializeField] private TextMeshProUGUI _roundText;

    private bool _hudFadeInTrigger;

    [Header("Zombie Values")]
    [SerializeField] public float ZombieSpawnRate;
    [SerializeField] public float WalkingZombieSpeed;
    [SerializeField] public float JoggingZombieSpeed;
    [SerializeField] public float RunningZombieSpeed;
    public float ZombieCount;
    private int _highRoundZombieSpawnRateChange;
    private int[] _soloLowRound = { 6, 8, 13, 18, 24, 27, 28, 28, 29, 33, 34, 36, 39, 41, 44, 47, 50, 53, 56 };
    private int[] _duoLowRound = { 7, 9, 15, 21, 27, 31, 32, 33, 34, 42, 45, 49, 54, 59, 64, 70, 76, 82, 89 };
    private int[] _trioLowRound = { 11, 14, 23, 32, 41, 47, 48, 50, 51, 62, 68, 74, 81, 89, 97, 105, 114, 123, 133 };
    private int[] _squadLowRound = { 14, 18, 30, 42, 54, 62, 64, 66, 68, 83, 91, 99, 108, 118, 129, 140, 152, 164, 178 };
    private int[] _amountOfRunningZombies = { 0, 0, 0, 0, 8, 17, 25, 28 };
    private int[] _amountOfJoggingZombies = { 0, 0, 2, 11, 14, 9, 3, 0};
    private int[] _amountOfWalkingZombies = { 6, 8, 11, 7, 2, 1, 0 };
    public int[] MaxZombieCount = { 24, 30, 36, 42 };
    private float[] _zombieSpawnRateLowRound = { 2, 1.9f, 1.8f, 1.7f, 1.65f, 1.55f, 1.45f, 1.40f, 1.35f, 1.25f };

    public int GetZombieCount(int playerCount, int round)
    {
        switch (playerCount)
        {
            case 1:
                if (round < 20)
                {
                    return _soloLowRound[round - 1];
                }
                else
                {
                    double zombieCount = 0.09f * round * round - 0.0029 * round + 23.958;
                    return Convert.ToInt32(Mathf.Round((float)zombieCount));
                }
            case 2:
                if (round < 20)
                {
                    return _duoLowRound[round - 1];
                }
                else
                {
                    double zombieCount = 0.1882f * round * round - 0.4313f * round + 29.212;
                    return Convert.ToInt32(Mathf.Round((float)zombieCount));
                }
            case 3:
                if (round < 20)
                {
                    return _trioLowRound[round - 1];
                }
                else
                {
                    double zombieCount = 0.2637f * round * round - 0.1802f * round + 35.015f;
                    return Convert.ToInt32(Mathf.Round((float)zombieCount));
                }
            case 4:
                if (round < 20)
                {
                    return _squadLowRound[round - 1];
                }
                else
                {
                    double zombieCount = 0.35714f * round * round - 0.0714f * round + 50.4286;
                    return Convert.ToInt32(Mathf.Round((float)zombieCount));
                }
            default:
                return 0;
        }
    }

    private float GetZombieSpawnRate(int round)
    {
        if (round <= 10)
        {
            return _zombieSpawnRateLowRound[round - 1];
        }

        if (round <= 20)
        {
            return ZombieSpawnRate - 0.05f;
        }

        // round > 20
        _highRoundZombieSpawnRateChange += 1;
        if (_highRoundZombieSpawnRateChange == 3)
        {
            _highRoundZombieSpawnRateChange = 0;
            return Mathf.Max(ZombieSpawnRate - 0.05f, 0.1f);
        }

        return ZombieSpawnRate;
    }

    private int GetWalkingZombieAmount(int round)
    {
        if(round < 7)
        {
            return _amountOfWalkingZombies[round - 1];
        }
        else
        {
            return _amountOfWalkingZombies[6];
        }
    }

    private int GetJoggingZombieAmount(int round)
    {
        if (round < 8)
        {
            return _amountOfWalkingZombies[round - 1];
        }
        else
        {
            return _amountOfWalkingZombies[7];
        }
    }

    private int GetRunningZombieAmount(int round)
    {
        if (round < 9)
        {
            return _amountOfRunningZombies[round - 1];
        }
        else
        {
            return ZombieSpawner.Instance.RoundZombieCount;
        }
    }

    private void Awake()
    {
        _instance = this;
        _hudFadeInTrigger = false;
    }

    void Start()
    {
        ZombieCount = GetZombieCount(GameManager.Instance.PlayerCount, 1);
        ZombieSpawnRate = GetZombieSpawnRate(1);

        //Before Round 1 Starts
        foreach (GameObject hudUI in _hudUI)
        {
            if (hudUI.TryGetComponent<Image>(out Image uiImage))
            {
                Color tempColour = uiImage.color;
                tempColour.a = 0f;
                uiImage.color = tempColour;
            }
            else if(hudUI.TryGetComponent<TextMeshProUGUI>(out TextMeshProUGUI uiText))
            {
                Color tempColourText = uiText.color;
                tempColourText.a = 0f;
                uiText.color = tempColourText;
            }
        }
    }

    void Update()
    {
        //Before Round 1 Starts
        if(!_blackScreenFadeIn.isPlaying && !_hudFadeInTrigger)
        {
            _hudFadeInTrigger = true;
            _hudFadeIn.Play();
        }
    }

    public void NewRound()
    {
        GameManager.Instance.Round++;
        _roundText.text = GameManager.Instance.Round.ToString();
        ZombieSpawner.Instance.RoundZombieCount = GetZombieCount(GameManager.Instance.PlayerCount,
                GameManager.Instance.Round);
        ZombieSpawner.Instance.RoundZombieWalkingCount = GetWalkingZombieAmount(GameManager.Instance.Round);
        ZombieSpawner.Instance.RoundZombieJoggingCount = GetJoggingZombieAmount(GameManager.Instance.Round);
        ZombieSpawner.Instance.RoundZombieRunningCount = GetRunningZombieAmount(GameManager.Instance.Round);
        ZombieSpawnRate = GetZombieSpawnRate(GameManager.Instance.Round);
        GameManager.Instance.ZombieHealth = GameManager.Instance.GetZombieHealth(GameManager.Instance.Round);
    }
}
