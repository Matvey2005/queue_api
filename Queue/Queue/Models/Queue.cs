using System.ComponentModel.DataAnnotations;

namespace Queue.Models
{
    public class Queue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public int DateId { get; set; }

        public int NumberUser { get; set; }
    }
}
