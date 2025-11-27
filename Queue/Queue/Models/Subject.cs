using System.ComponentModel.DataAnnotations;

namespace Queue.Models
{
    public class Subject
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Name { get; set; }

        [Required]
        public int GroupId { get; set; }
    }
}
