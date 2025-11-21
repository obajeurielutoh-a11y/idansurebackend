using Microsoft.AspNetCore.SignalR;
using SubscriptionSystem.Application.DTOs;
namespace SubscriptionSystem.Infrastructure.Hubs
{
    public class PredictionHub : Hub
    {
        public async Task UpdatePrediction(AsedeyhotPredictionDto prediction)
        {
            await Clients.All.SendAsync("PredictionUpdated", prediction);
        }
    }
}
