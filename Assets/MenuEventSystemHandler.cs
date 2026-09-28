using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MenuEventSystemHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<Selectable> _selectables = new List<Selectable>();
    [SerializeField] private protected Selectable _firstSelected;

    [Header("Controls")]
    [SerializeField] private protected InputActionReference _navigationReferance;

    private protected Selectable _lastSelected;

    public virtual void Awake()
    {
        foreach(Selectable selectable in _selectables)
        {
            AddSelectionListeners(selectable);
        }
    }

    public virtual void OnEnable()
    {
        _navigationReferance.action.performed += OnNavigate;

        StartCoroutine(SelectAfterDelay());
    }

    public virtual void OnDisable()
    {
        _navigationReferance.action.performed -= OnNavigate;
    }

    private protected IEnumerator SelectAfterDelay()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(_firstSelected.gameObject);
    }

    protected virtual void AddSelectionListeners(Selectable selectable)
    {
        //Add Listener
        EventTrigger trigger = selectable.gameObject.GetComponent<EventTrigger>();
        if(trigger == null)
        {
            trigger = selectable.gameObject.AddComponent<EventTrigger>();
        }

        //Add Select Event
        EventTrigger.Entry selectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Select
        };
        selectEntry.callback.AddListener(OnSelect);
        trigger.triggers.Add(selectEntry);

        //Add Deselect Event
        EventTrigger.Entry deselectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Deselect
        };
        deselectEntry.callback.AddListener(OnDeselect);
        trigger.triggers.Add(deselectEntry);

        //Add PointerEnter Event
        EventTrigger.Entry pointerEnterEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        pointerEnterEntry.callback.AddListener(OnPointerEnter);
        trigger.triggers.Add(pointerEnterEntry);

        //Add PointerExit Event
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        pointerExitEntry.callback.AddListener(OnPointerExit);
        trigger.triggers.Add(pointerExitEntry);
    }

    //If we wanted to do animations
    public void OnSelect(BaseEventData eventData)
    {
        _lastSelected = eventData.selectedObject.GetComponent<Selectable>();
    }

    public void OnDeselect(BaseEventData eventData)
    {

    }

    public void OnPointerEnter(BaseEventData eventData)
    {
        PointerEventData pointerEventData = eventData as PointerEventData;
        if(pointerEventData != null)
        {
            Selectable selectable = pointerEventData.pointerEnter.GetComponentInParent<Selectable>();
            if(selectable == null)
            {
                selectable = pointerEventData.pointerEnter.GetComponentInChildren<Selectable>();
            }
            pointerEventData.selectedObject = selectable.gameObject;
        }
    }

    public void OnPointerExit(BaseEventData eventData)
    {
        PointerEventData pointerEventData = eventData as PointerEventData;
        if (pointerEventData != null)
        {
            pointerEventData.selectedObject = null;
        }
    }

    protected private virtual void OnNavigate(InputAction.CallbackContext context)
    {
        if(EventSystem.current.currentSelectedGameObject == null && _lastSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(_lastSelected.gameObject);
        }
    }
}
