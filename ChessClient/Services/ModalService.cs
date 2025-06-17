using System;

namespace ChessClient.Services
{
    // Ein einfacher Dienst zur Entkopplung, der es beliebigen Komponenten ermöglicht,
    // das Öffnen von globalen Modal-Dialogen anzufordern.
    public class ModalService
    {
        // Event, das ausgelöst wird, um das "Spiel erstellen"-Modal anzufordern.
        public event Action? ShowCreateGameModalRequested;
        // Event, das ausgelöst wird, um das "Spiel beitreten"-Modal anzufordern.
        public event Action? ShowJoinGameModalRequested;

        // Methode zum Auslösen der "Spiel erstellen"-Anforderung.
        public void RequestShowCreateGameModal()
        {
            ShowCreateGameModalRequested?.Invoke();
        }

        // Methode zum Auslösen der "Spiel beitreten"-Anforderung.
        public void RequestShowJoinGameModal()
        {
            ShowJoinGameModalRequested?.Invoke();
        }
    }
}