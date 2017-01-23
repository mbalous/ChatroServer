using System;
using ServiceStack.DataAnnotations;

namespace ChatroServer.Entity
{
    public class User
    {
        [PrimaryKey]
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