using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChatroServer.Entity;
using Microsoft.AspNet.SignalR;
using ServiceStack.OrmLite;

namespace ChatroServer
{
    public sealed class ChatHub : Hub
    {
        public static OrmLiteConnectionFactory ConnectionFactory { get; }
        private static readonly Dictionary<string, User> ConnectionIdsUsers = new Dictionary<string, User>();

        static ChatHub()
        {
            SingletonDbConnection singletonDbConnection = SingletonDbConnection.Instance;
            ConnectionFactory = singletonDbConnection.ConnectionFactory;

            using (IDbConnection db = ConnectionFactory.Open())
            {
#if DEBUG
                db.DropAndCreateTable<User>();
                db.DropAndCreateTable<Message>();
                User[] users = { new User("Karel", "123456"), new User("Varel", "123456") };
                db.SaveAll(users);
#else
                db.CreateTableIfNotExists<User>();
                db.CreateTableIfNotExists<Message>();
#endif

            }
        }

        /// <inheritdoc />
        public override Task OnConnected()
        {
            Debug.WriteLine($"Client connected - connection id: {this.Context.ConnectionId}", "info");
            ConnectionIdsUsers.Add(this.Context.ConnectionId, null);
            return base.OnConnected();
        }

        /// <inheritdoc />
        public override Task OnDisconnected(bool stopCalled)
        {
            Debug.WriteLine($"Client disconnected - connection id: {this.Context.ConnectionId}", "info");
            ConnectionIdsUsers.Remove(this.Context.ConnectionId);
            return base.OnDisconnected(stopCalled);
        }

        public void SendBroadcast(string content)
        {
            User username = ConnectionIdsUsers[this.Context.ConnectionId];
            if (username == null)
            {
                throw new HubException("Username not set, can't send anything.");
            }
            Message message = new Message(DateTime.Now, username, content);
            using (IDbConnection db = ConnectionFactory.Open())
            {
                db.Save(message);
            }
            this.Clients.All.NewBroadcast(content, username);
        }

        public void SendMessage(string message, string recipient)
        {
            if (!IsLogged())
            {
                // TODO
                return;
            }

            User sender = ConnectionIdsUsers[this.Context.ConnectionId];
            User userRecipient;
            using (IDbConnection db = ConnectionFactory.Open())
            {
                userRecipient = db.Select<User>().FirstOrDefault(user => user.Username == recipient);
            }
            if (userRecipient == null)
            {
                // TODO: Figure out how to notify the client about this...
                throw new Exception($"User {recipient} not found in database...");
            }

            Message msg = new Message(DateTime.Now, sender, recipient);

            // Is the user online?
            string connectionId;
            bool isConnected = TryGetUserConnectionIdFromUsername(recipient, out connectionId);
            if (isConnected)
            {
                this.Clients.Client(connectionId).NewMessage(message, sender.Username);
            }
        }

        private bool TryGetUserConnectionIdFromUsername(string username, out string connectionId)
        {
            string result = ConnectionIdsUsers.FirstOrDefault(pair => pair.Value.Username == username).Key;
            if (result != null)
            {
                connectionId = result;
                return true;
            }
            else
            {
                connectionId = null;
                return false;
            }
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
                User userDb = result.First();
                if (userDb.Password == password)
                {
                    ConnectionIdsUsers[this.Context.ConnectionId] = userDb;
                    return LoginResult.Success;
                }
                return LoginResult.Failure;
            }
        }

        public List<Message> DownloadMessages(DateTime from)
        {
            if (!IsLogged())
            {
                return null;
            }

            using (IDbConnection db = ConnectionFactory.Open())
            {
                return db.Select<Message>().Where(message => message.TimeStamp > @from).ToList();
            }
        }

        private bool IsLogged()
        {
            return ConnectionIdsUsers[this.Context.ConnectionId] != null;
        }
    }
}