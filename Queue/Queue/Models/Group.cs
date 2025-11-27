using System.ComponentModel.DataAnnotations;

namespace Queue.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string Name { get; set; }
    }
}
