using UnityEngine;
using UnityEngine.EventSystems;

public class SelectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler
{
    [SerializeField] private HandleMenuUI _handleMenuUI;

    public void OnPointerEnter(PointerEventData eventData)
    {
        eventData.selectedObject = gameObject;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        eventData.selectedObject = null;
    }

    public void OnSelect(BaseEventData eventData)
    {
        _handleMenuUI.LastSelectedButton = gameObject;

        for (int i = 0; i < _handleMenuUI.Buttons.Count; i++)
        {
            if (_handleMenuUI.Buttons[i] == gameObject)
            {
                _handleMenuUI.LastSelectedIndex = i;
                return;
            }
        }
    }
}