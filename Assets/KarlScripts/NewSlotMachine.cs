using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class NewSlotMachine : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private int _totalItems = 40;
    [SerializeField] private int _winningIndex = 35;
    [SerializeField] private int _pointCost;

    [Header("Animation")]
    [SerializeField] private float _spinDuration = 6f;
    [SerializeField] private AnimationCurve _easeCurve;

    private List<GameObject> slots = new List<GameObject>();

    private HorizontalLayoutGroup layoutGroup;

    private float itemWidth;
    private float itemSpacing;
    private float itemStride => itemWidth + itemSpacing;

    private Coroutine _spinCoroutine;

    private bool _isSpinning;
    private bool _itemSpawned;

    [SerializeField] private string _winningItemName;

    private RectTransform _itemStrip;
    private RectTransform _viewport;

    private Transform _itemSpawn;

    private GameObject _newGun;
    private GameObject _newPlayerGunReferance;

    void Start()
    {
        _viewport = transform.Find("Mesh").transform.Find("Screen").transform.Find("Canvas").transform.Find("Background").GetComponent<RectTransform>();
        _itemStrip = _viewport.transform.Find("LayoutGroup").GetComponent<RectTransform>();
        layoutGroup = _itemStrip.GetComponent<HorizontalLayoutGroup>();
        _itemSpawn = transform.Find("Mesh").transform.Find("GunHolder").transform.Find("GunPosition");

        _easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        GenerateStrip();
    }

    public void ResetStrip()
    {
        if (_spinCoroutine != null)
        {
            StopCoroutine(_spinCoroutine);
            _spinCoroutine = null;
        }

        _isSpinning = false;

        _itemStrip.anchoredPosition = new Vector2(0, _itemStrip.anchoredPosition.y);

        foreach(Transform child in _itemStrip)
        {
            Destroy(child.gameObject);
        }

        GenerateStrip();
    }

    private void GenerateStrip()
    {
        for (int i = 0; i < _totalItems; i++)
        {
            GameObject slot = Instantiate(GameManager.Instance.SlotItemPrefab, _itemStrip);
            slots.Add(slot);
            AssignRandomItem(slot, i == _winningIndex);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(_itemStrip);

        itemSpacing = layoutGroup.spacing;

        if(itemWidth == 0)
        {
            itemWidth = slots[0].GetComponent<RectTransform>().rect.width;
        }
    }

    private void AssignRandomItem(GameObject slot, bool isWinner)
    {
        Image image = slot.GetComponentInChildren<Image>();
        int randomItem = Random.Range(0, GameManager.Instance.SlotMachineGunSprites.Count);
        image.sprite = GameManager.Instance.SlotMachineGunSprites[randomItem];
        slot.name = GameManager.Instance.SlotMachineGunSprites[randomItem].name;

        if (isWinner)
        {
            _winningItemName = slot.name;
        }
    }

    IEnumerator SpinAnimation()
    {
        _isSpinning = true;

        yield return null;

        float viewportCenter = _viewport.rect.width / 2f;
        float targetX = -(_winningIndex * itemStride) + viewportCenter - (itemWidth / 2f);

        float startX = _itemStrip.anchoredPosition.x;
        float elapsed = 0f;

        while (elapsed < _spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _spinDuration);
            float eased = _easeCurve.Evaluate(t);

            float newX = Mathf.Lerp(startX, targetX, eased);
            _itemStrip.anchoredPosition = new Vector2(newX, _itemStrip.anchoredPosition.y);

            yield return null;
        }

        _itemStrip.anchoredPosition = new Vector2(targetX, _itemStrip.anchoredPosition.y);

        _isSpinning = false;
        _spinCoroutine = null;

        switch (_winningItemName)
        {
            case "M1928":
                _newGun = Instantiate(GameManager.Instance.SlotMachineGuns[0], _itemSpawn);
                break;

            case "P90":
                _newGun = Instantiate(GameManager.Instance.SlotMachineGuns[1], _itemSpawn);
                break;
        }

        _itemSpawned = true;
    }

    public bool CanInteract(TextMeshProUGUI interactText, NewPlayerInteraction playerInteraction)
    {
        //If the slot machine isnt spinning and there is an item spawned
        if (!_isSpinning && _itemSpawned)
        {
            interactText.text = "Press F to Pickup Item";
            interactText.gameObject.SetActive(true);
            return true;
        }
        //If the slot machine isnt spinning and there isnt an item spawned
        else if (!_isSpinning && !_itemSpawned)
        {
            interactText.text = "Press F for Slot Machine [Cost: " + _pointCost + "]";
            interactText.gameObject.SetActive(true);
            return true;
        }
        interactText.gameObject.SetActive(false);
        return false;
    }

    public bool Interact(NewPlayerInteraction playerInteraction)
    {
        Points playerPoints = playerInteraction.GetComponent<Points>();

        //If the slot machine isnt spinning and there isnt an item spawned
        if (!_isSpinning && !_itemSpawned && playerPoints.Money >= _pointCost)
        {
            playerPoints.Money -= _pointCost;
            StartCoroutine(SpinAnimation());
        }
        //If the slot machine isnt spinning and there is an item spawned
        else if (!_isSpinning && _itemSpawned)
        {
            PlayerCurrentGun playerCurrentGun = playerInteraction.GetComponent<PlayerCurrentGun>();

            Transform playerGunSpawn = playerCurrentGun.GunHolder;

            switch (_winningItemName)
            {
                case "M1928":
                    _newPlayerGunReferance = Instantiate(GameManager.Instance.GunGameObjects[0], playerGunSpawn);
                    break;

                case "P90":
                    _newPlayerGunReferance = Instantiate(GameManager.Instance.GunGameObjects[1], playerGunSpawn);
                    break;
            }

            Destroy(_newGun);

            Destroy(playerCurrentGun.CurrentGun);

            playerCurrentGun.CurrentGun = _newPlayerGunReferance;
            _newPlayerGunReferance.SetActive(true);
            playerCurrentGun.GunAnimator = playerCurrentGun.CurrentGun.transform.Find("WeaponMesh").GetComponent<Animator>();

            _itemSpawned = false;

            ResetStrip();
        }
        return true;
    }
}
