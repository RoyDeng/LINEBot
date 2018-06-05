using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LINEBot.Models
{
    public class Bot
    {
        public int BotId { get; set; }
        public int MemberId { get; set; }
        public string ChannelToken { get; set; }
        public string ChannelSecret { get; set; }

        [ForeignKey("MemberId")]
        public Member Member { get; set; }

        public ICollection<Message> Messages { get; set; }
    }
}