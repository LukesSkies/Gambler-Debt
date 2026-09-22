using System.Collections;
using TMPro;
using UnityEngine;

[System.Serializable]
public class DoorPart
{
    [Header("Door Part")]
    public Transform Pivot;
    public Vector3 OpenRotation;

    [HideInInspector] public Quaternion ClosedRotation;
    [HideInInspector] public Quaternion TargetRotation;
}

public class DoorTest : MonoBehaviour, IInteractable
{
    #region Inspector

    [Header("Door Settings")]
    [SerializeField] private int _pointCost = 1000;

    [Header("Door Parts")]
    [SerializeField] private DoorPart[] _doorParts;

    [Header("Linked Doors")]
    [Tooltip("Doors that should automatically open when this one is purchased.")]
    [SerializeField] private DoorTest[] _linkedDoors;

    [Header("Animation")]
    [SerializeField] private float _openTime = 1.5f;
    [SerializeField] private AnimationCurve _openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    #endregion

    #region Private Variables

    private bool _opened;
    private bool _isOpening;

    #endregion

    #region Unity Methods

    private void Start()
    {
        CacheDoorRotations();
    }

    #endregion

    #region Interaction

    public bool CanInteract(TextMeshProUGUI interactText, NewPlayerInteraction playerInteraction)
    {
        if (_opened || _isOpening)
        {
            interactText.gameObject.SetActive(false);
            return false;
        }

        interactText.text = $"Press F to Open Door [Cost: {_pointCost}]";
        interactText.gameObject.SetActive(true);

        return true;
    }

    public bool Interact(NewPlayerInteraction playerInteraction)
    {
        if (_opened || _isOpening)
            return false;

        Points points = playerInteraction.GetComponent<Points>();

        if (points == null || points.Money < _pointCost)
            return false;

        points.Money -= _pointCost;

        OpenLinkedDoors();

        StartCoroutine(OpenDoor());

        return true;
    }

    #endregion

    #region Door Logic

    private void CacheDoorRotations()
    {
        foreach (DoorPart part in _doorParts)
        {
            if (part.Pivot == null)
                continue;

            part.ClosedRotation = part.Pivot.localRotation;
            part.TargetRotation = part.ClosedRotation * Quaternion.Euler(part.OpenRotation);
        }
    }

    private IEnumerator OpenDoor()
    {
        _isOpening = true;

        float elapsed = 0f;

        while (elapsed < _openTime)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / _openTime);
            float easedProgress = _openCurve.Evaluate(progress);

            foreach (DoorPart part in _doorParts)
            {
                if (part.Pivot == null)
                    continue;

                part.Pivot.localRotation = Quaternion.Slerp(
                    part.ClosedRotation,
                    part.TargetRotation,
                    easedProgress);
            }

            yield return null;
        }

        foreach (DoorPart part in _doorParts)
        {
            if (part.Pivot == null)
                continue;

            part.Pivot.localRotation = part.TargetRotation;
        }

        _opened = true;
        _isOpening = false;
    }

    public void OpenLinkedDoor()
    {
        if (_opened || _isOpening)
            return;

        StartCoroutine(OpenDoor());
    }

    private void OpenLinkedDoors()
    {
        foreach (DoorTest door in _linkedDoors)
        {
            if (door != null)
            {
                door.OpenLinkedDoor();
            }
        }
    }

    #endregion
}