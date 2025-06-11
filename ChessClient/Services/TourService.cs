using System;
using System.Threading.Tasks;

namespace ChessClient.Services
{
    public class TourService
    {
        public event Func<Task>? TourRequested;

        public async Task RequestTourAsync()
        {
            if (TourRequested != null)
            {
                await TourRequested.Invoke();
            }
        }
    }
}