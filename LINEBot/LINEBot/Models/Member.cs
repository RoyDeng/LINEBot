using System.Collections.Generic;

namespace LINEBot.Models
{
    public class Member
    {
        public int MemberId { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public ICollection<Bot> Bots { get; set; }
    }
}