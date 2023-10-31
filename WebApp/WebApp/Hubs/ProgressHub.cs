using Microsoft.AspNetCore.SignalR;

public class ProgressHub : Hub
{
    public async Task UpdateProgress(double progress)
    {
        await Clients.All.SendAsync("ReceiveProgress", progress);
    }
}
