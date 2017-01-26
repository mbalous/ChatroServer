using System;
using System.Data;
using ServiceStack.DataAnnotations;

namespace ChatroServer.Entity
{
    public class User
        {
        [PrimaryKey]
        public Guid Guid { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }


        public User(string username, string password)
        {
            if (username != null)
            {
                this.Username = username;
            }
            else
            {
                throw new ArgumentNullException(nameof(username));
            }
            this.Password = password;

            this.Guid = Guid.NewGuid();
        }
        
        /// <summary>
        /// Get username.
        /// </summary>
        /// <returns>Returns username.</returns>
        public override string ToString()
        {
            return this.Username;
        }
    }
}