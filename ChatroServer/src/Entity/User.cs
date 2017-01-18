using ServiceStack.DataAnnotations;

namespace ChatroServer
{
    public class User
    {
        [PrimaryKey]
        public string Username { get; set; }
        public string Password { get; set; }
    }
}