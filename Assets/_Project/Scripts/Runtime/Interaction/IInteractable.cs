namespace LotoSim.Interaction
{
    /// <summary>Objeto del mundo que responde a una interacción.</summary>
    public interface IInteractable
    {
        /// <summary>Si la acción está disponible ahora para este interactor.</summary>
        bool CanInteract(IInteractor interactor);

        /// <summary>Texto de la acción si está disponible, o el motivo por el que no lo está.</summary>
        string GetPrompt(IInteractor interactor);

        void Interact(IInteractor interactor);
    }
}
