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
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;

namespace ChatroServer
{
    public class ClientInfo
    {
        public ClientInfo(string connectionId)
        {
            if (connectionId != null)
            {
                this.ConnectionId = connectionId;
            }
            else
            {
                throw new ArgumentNullException(nameof(connectionId));
            }
        }

        public string ConnectionId { get; set; }
        public User UserEntity { get; set; }
    }

    public class User
    {
        [PrimaryKey]
        public string Username { get; set; }
        public string Password { get; set; }
    }


    public class ChatHub : Hub
    {
        public static OrmLiteConnectionFactory ConnectionFactory { get; }
        private static readonly List<ClientInfo> ConnectedClients = new List<ClientInfo>();

        static ChatHub()
        {
            SingletonDbConnection singletonDbConnection = SingletonDbConnection.Instance;
            ConnectionFactory = singletonDbConnection.ConnectionFactory;

            using (IDbConnection db = ConnectionFactory.Open())
            {
                db.CreateTableIfNotExists<User>();
                User user = new User()
                {
                    Username = "karel",
                    Password = "123456"
                };
                db.Save(user);
            }
        }


        public override Task OnConnected()
        {
            Debug.WriteLine($"Client connected - connection id: {this.Context.ConnectionId}", "info");
            ConnectedClients.Add(new ClientInfo(this.Context.ConnectionId));
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            Debug.WriteLine($"Client disconnected - connection id: {this.Context.ConnectionId}", "info");
            ClientInfo caller = GetCallerUserInfo();
            ConnectedClients.Remove(caller);
            return base.OnDisconnected(stopCalled);
        }

        private ClientInfo GetCallerUserInfo()
        {
            return ConnectedClients.Find(client => client.ConnectionId == this.Context.ConnectionId);
        }

        /*
        public void SendBroadcast(string message)
        {
            ClientInfo callerInfo = GetCallerUserInfo();
            if (callerInfo.UserName == null)
            {
                throw new HubException("Username not set, can't send anything.");
            }
            this.Clients.All.NewBroadcast(message, callerInfo.UserName);
        }
            */
        /*
        public void SendMessage(string message, string recipient)
        {
            ClientInfo callerInfo = GetCallerUserInfo();
            if (callerInfo.UserName == null)
            {
                throw new HubException("Username not set, can't send anything.");
            }
            // find recipient
            ClientInfo user = ConnectedClients.FirstOrDefault(info => info.UserName == recipient);
            if (user == null)
            {
                // user not found
                throw new HubException($"User {recipient} not found...");
            }
            // user found sending message
            this.Clients.Client(user.ConnectionId).NewMessage(message, callerInfo.UserName);
        }
        */


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
    }
}