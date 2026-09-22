using TMPro;

public interface IInteractable
{
    public bool CanInteract(TextMeshProUGUI interactText, NewPlayerInteraction playerInteraction);
    public bool Interact(NewPlayerInteraction playerInteraction);
}
