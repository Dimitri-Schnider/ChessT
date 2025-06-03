using System;

namespace ChessClient.Services
{
    // Dienst zur Steuerung der Anzeige von Modal-Dialogen.
    public class ModalService
    {
        // Ereignis, das ausgelöst wird, wenn das "Spiel erstellen"-Modal angezeigt werden soll.
        public event Action? ShowCreateGameModalRequested;
        // Ereignis, das ausgelöst wird, wenn das "Spiel beitreten"-Modal angezeigt werden soll.
        public event Action? ShowJoinGameModalRequested;

        // Löst das Ereignis zur Anzeige des "Spiel erstellen"-Modals aus.
        public void RequestShowCreateGameModal()
        {
            ShowCreateGameModalRequested?.Invoke();
        }

        // Löst das Ereignis zur Anzeige des "Spiel beitreten"-Modals aus.
        public void RequestShowJoinGameModal()
        {
            ShowJoinGameModalRequested?.Invoke();
        }
    }
}