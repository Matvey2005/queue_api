using System.ComponentModel.DataAnnotations;

namespace Queue.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string UserName { get; set; }

        [Required]
        [StringLength(30)]
        public string Email { get; set; }

        [Required]
        [StringLength(150)]
        public string Password { get; set; }

        public int GroupId { get; set; }
        public int SubgroupNumber { get; set; }
    }
}
