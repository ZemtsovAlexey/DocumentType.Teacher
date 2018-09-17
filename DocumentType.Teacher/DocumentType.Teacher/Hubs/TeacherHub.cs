using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DocumentType.Teacher
{
    public class TeacherHub : Hub
    {
        public Task SendToAll(string user, string message)
        {
            return Clients.All.SendAsync("sendToAll", user, message);
        }
    }
}