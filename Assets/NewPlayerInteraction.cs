using TMPro;
using UnityEngine;

public class NewPlayerInteraction : MonoBehaviour
{
    [SerializeField] private float _raycastDistance;

    private TextMeshProUGUI _interactText;
    private Transform _raycast;

    private void Start()
    {
        _interactText = GameObject.Find("HUD").transform.Find("InteractionText").GetComponentInChildren<TextMeshProUGUI>();

        _raycast = transform.Find("CameraHolder");
    }

    void Update()
    {
        if(InteractionTest(out IInteractable interactable))
        {
            if (interactable.CanInteract(_interactText))
            {
                _interactText.gameObject.SetActive(true);

                if (Input.GetKeyDown(KeyCode.F))
                {
                    interactable.Interact(this);
                }
            }
            else
            {
                _interactText.gameObject.SetActive(false);
            }
        }
        else
        {
            _interactText.gameObject.SetActive(false);
        }
    }

    private bool InteractionTest(out IInteractable interactable)
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
