using Microsoft.AspNetCore.SignalR;

namespace WebApp.Hubs
{
    public class jsCodeHub : Hub
    {
        public async Task ExecuteJavaScript(string jsCode)
        {
            await Clients.Caller.SendAsync("ExecuteJavaScript", jsCode);
        }
    }
}
