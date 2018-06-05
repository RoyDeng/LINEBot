using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LINEBot.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public int BotId { get; set; }
        public int Event { get; set; }
        public int Type { get; set; }
        public string KeyWord { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public string Yes { get; set; }
        public string No { get; set; }
        public string Msg { get; set; }
        public string PostBack { get; set; }
        public string Title { get; set; }
        public string STKId { get; set; }
        public string STKPKGId { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [DataType(DataType.MultilineText)]
        public string Text { get; set; }

        [ForeignKey("BotId")]
        public virtual Bot Bot { get; set; }
    }
}