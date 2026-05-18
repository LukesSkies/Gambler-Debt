using TMPro;

public interface IInteractable
{
    public bool CanInteract(TextMeshProUGUI interactText);
    public bool Interact(NewPlayerInteraction playerInteraction);
}
