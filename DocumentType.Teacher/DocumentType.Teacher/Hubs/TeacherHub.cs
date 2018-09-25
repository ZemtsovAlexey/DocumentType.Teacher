using DocumentType.Teacher.Nets;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DocumentType.Teacher
{
    public class TeacherHub : Hub
    {
        public TeacherHub()
        {
        }

        public override Task OnConnectedAsync()
        {
            var clients = Clients;

            NeuralNetwork.IterationChange += (s, r) =>
            {
                clients.All.SendAsync("IterationChange", r);
            };

            return base.OnConnectedAsync();
        }
    }
}