using System.Data.Entity;

namespace LINEBot.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext() : base("name=DbConnection")
        {
        }

        public DbSet<Member> Members { get; set; }

        public DbSet<Bot> Bots { get; set; }

        public DbSet<Message> Messages { get; set; }
    }
}