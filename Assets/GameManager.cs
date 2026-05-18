using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if(_instance == null)
            {
                Debug.LogError("Need Gamemanager in the scene :(");
            }
            return _instance;
        }
    }

    [Header("Point Values")]
    public int HitPoints;
    public int EndTorsoPoints;
    public int EndHeadshotPoints;
    public int EndKnifePoints;

    [Header("Health Values")]
    public int ZombieHealth;
    public int PlayerHealth = 90;
    public int PlayerJugHealth = 150;
    public int PlayerHealthRegen = 40;

    [Header("Game Values")]
    public int Round;
    public float ZombieCount;
    public int PlayerCount;
    private int[] _soloLowRound = { 6, 8, 13, 18, 24, 27, 28, 28, 29, 33, 34, 36, 39, 41, 44, 47, 50, 53, 56 };
    private int[] _duoLowRound = { 7, 9, 15, 21, 27, 31, 32, 33, 34, 42, 45, 49, 54, 59, 64, 70, 76, 82, 89 };
    private int[] _trioLowRound = { 11, 14, 23, 32, 41, 47, 48, 50, 51, 62, 68, 74, 81, 89, 97, 105, 114, 123, 133 };
    private int[] _squadLowRound = { 14, 18, 30, 42, 54, 62, 64, 66, 68, 83, 91, 99, 108, 118, 129, 140, 152, 164, 178 };
    public List<GameObject> GunGameObjects = new List<GameObject>();

    [Header("UI")]
    public GameObject PointsAdditionText;
    public GameObject SlotItemPrefab;
    public List<Sprite> SlotMachineGunSprites = new List<Sprite>();

    [Header("SlotMachine")]
    public List<GameObject> SlotMachineGuns = new List<GameObject>();

    private float GetZombieCount(int playerCount, int round)
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
                    return Mathf.Round((float)zombieCount);
                }
            case 2:
                if (round < 20)
                {
                    return _duoLowRound[round - 1];
                }
                else
                {
                    double zombieCount = 0.1882f * round * round - 0.4313f * round + 29.212;
                    return Mathf.Round((float)zombieCount);
                }
            case 3:
                if (round < 20)
                {
                    return _trioLowRound[round - 1];
                }
                else
                {
                    double zombieCount = 0.2637f * round * round - 0.1802f * round + 35.015f;
                    return Mathf.Round((float)zombieCount);
                }
            case 4:
                if (round < 20)
                {
                    return _squadLowRound[round - 1];
                }
                else
                {
                    double zombieCount = 0.35714f * round * round - 0.0714f * round + 50.4286;
                    return Mathf.Round((float)zombieCount);
                }
            default:
                return 0;
        }
    }

    private int GetZombieHealth(int round)
    {
        if(round < 10)
        {
            return 50 + (100*round);
        }
        else
        {
            return Mathf.RoundToInt(ZombieHealth * 1.1f);
        }
    }

    void Awake()
    {
        _instance = this;
    }

    void Start()
    {
        Round = 1;
        PlayerCount = 1;
        ZombieCount = GetZombieCount(1, Round);
        ZombieHealth = GetZombieHealth(Round);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Round++;
            ZombieCount = GetZombieCount(1, Round);
            ZombieHealth = GetZombieHealth(Round);
        }
    }
}
