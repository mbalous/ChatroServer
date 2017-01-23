using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.OrmLite;

namespace ChatroServer
{
    public class ChatHub : Hub
    {
        public static OrmLiteConnectionFactory ConnectionFactory { get; }
        private static readonly Dictionary<string, User> ConnectionIdsUsers = new Dictionary<string, User>();

        static ChatHub()
        {
            SingletonDbConnection singletonDbConnection = SingletonDbConnection.Instance;
            ConnectionFactory = singletonDbConnection.ConnectionFactory;

            using (IDbConnection db = ConnectionFactory.Open())
            {
                db.CreateTableIfNotExists<User>();
                db.CreateTableIfNotExists<Message>();
                User[] users = {new User("Karel", "123456"), new User("Varel", "123456")};
                db.SaveAll(users);
            }
        }

        public override Task OnConnected()
        {
            Debug.WriteLine($"Client connected - connection id: {this.Context.ConnectionId}", "info");
            ConnectionIdsUsers.Add(this.Context.ConnectionId, null);
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Debug.WriteLine($"Client disconnected - connection id: {this.Context.ConnectionId}", "info");
            ConnectionIdsUsers.Remove(this.Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        
        public void SendBroadcast(string message)
        {
            User username = ConnectionIdsUsers[this.Context.ConnectionId];
            if (username == null)
            {
                throw new HubException("Username not set, can't send anything.");
            }
            this.Clients.All.NewBroadcast(message, username);
        }

        public void SendMessage(string message, string recipient)
        {
            User sender = ConnectionIdsUsers[this.Context.ConnectionId];
            if (sender == null)
            {
                throw new HubException("Username not set, can't send anything.");
            }
            // find recipient
            string recipientId = ConnectionIdsUsers.FirstOrDefault(pair => pair.Value.Username == recipient).Key;
            if (recipientId == null)
            {
                // user not found
                throw new HubException($"User {recipient} not found...");
            }
            // user found sending message
            this.Clients.Client(ConnectionIdsUsers.First(pair => pair.Value.Username == recipient).Key);
        }
        

        public LoginResult Login(string username, string password)
        {
            using (IDbConnection db = ConnectionFactory.Open())
            {
                List<User> result = db.Select<User>(user => user.Username == username);
                if (result.Count != 1)
                {
                    throw new ApplicationException($"More than one user with username '{username}' found.");
                }
                if (result.Count == 0)
                {
                    return LoginResult.Failure;
                }
                else
                {
                    User userDb = result.First();
                    return userDb.Password == password ? LoginResult.Success : LoginResult.Failure;
                }
            }
        }
    }

    public enum LoginResult
    {
        Success,
        Failure
        private bool IsLogged()
        {
            return ConnectionIdsUsers.Keys.Any(s => s == this.Context.ConnectionId);
        }
    }
}