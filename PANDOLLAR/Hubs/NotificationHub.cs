using Microsoft.AspNetCore.SignalR;

namespace PANDOLLAR.Hubs
{
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"[NotificationHub] Client connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"[NotificationHub] Client disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendNotification(string type, string message)
        {
            Console.WriteLine($"[NotificationHub] Sending notification - Type: {type}, Message: {message}");
            await Clients.All.SendAsync("ReceiveNotification", type, message);
        }
    }
}
