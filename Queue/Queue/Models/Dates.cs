using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Queue.Models
{
    public class Dates
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }

        public string Date {  get; set; }

        public int ForSubgroup { get; set; }

        [Required]
        public int GroupId { get; set;}

        [JsonIgnore] 
        public List<Exchange> Exchanges { get; set; }
    }
}
