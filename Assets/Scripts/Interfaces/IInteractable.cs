using UnityEngine;

namespace Interfaces
{
    public interface IInteractable
    {
        string InteractPrompt { get; }
        void Interact(IInteractor interactor);
    }
}
