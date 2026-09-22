using System;
using TMPro;
using UnityEngine;

public class NewPlayerInteraction : MonoBehaviour
{
    [SerializeField] private float _raycastDistance;

    public TextMeshProUGUI InteractText;
    private Transform _raycast;

    public bool CanInteract;
    public bool InteractQueued;

    private void Start()
    {
        InteractText = GameObject.Find("HUD").transform.Find("InteractionText").GetComponentInChildren<TextMeshProUGUI>();

        _raycast = transform.Find("CameraHolder");
    }

    void Update()
    {
        if(InteractionTest(out IInteractable interactable))
        {
            CanInteract = true;
            if (interactable.CanInteract(InteractText, this))
            {
                InteractText.gameObject.SetActive(true);

                if (InteractQueued)
                {
                    interactable.Interact(this);
                    InteractQueued = false;
                }
            }
            else
            {
                InteractText.gameObject.SetActive(false);
            }
        }
        else
        {
            CanInteract = false;
            InteractText.gameObject.SetActive(false);
        }
    }

    public bool InteractionTest(out IInteractable interactable)
    {
        interactable = null;

        Ray ray = new Ray(_raycast.position, _raycast.forward);

        Debug.DrawRay(ray.origin, ray.direction, Color.red, 3f);

        if(Physics.Raycast(ray, out RaycastHit hit, _raycastDistance))
        {
            interactable = hit.transform.GetComponent<IInteractable>();

            if(interactable != null)
            {
                return true;
            }
        }

        return false;
    }
}
