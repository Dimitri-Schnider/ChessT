using System;
using System.Threading.Tasks;

namespace ChessClient.Services
{
    // Ein Dienst, der die interaktive Tour durch die Anwendung steuert.
    public class TourService
    {
        // Event, das ausgelöst wird, wenn eine Tour angefordert wird.
        public event Func<Task>? TourRequested;

        // Methode, um die Anforderung zum Starten einer Tour auszulösen.
        public async Task RequestTourAsync()
        {
            if (TourRequested != null)
            {
                await TourRequested.Invoke();
            }
        }
    }
}