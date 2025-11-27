using System.Text.Json.Serialization;

namespace Queue.Models
{
    public class Exchange
    {
        public int Id { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public string CreatedAt { get; set; }
        public int DateId { get; set; }

        // Навигационные свойства
        [JsonIgnore]
        public User FromUser { get; set; }

        [JsonIgnore]
        public User ToUser { get; set; }

        [JsonIgnore]
        public virtual Dates DateNavigation { get; set; } // Навигационное свойство к дате обмена
    }
}
