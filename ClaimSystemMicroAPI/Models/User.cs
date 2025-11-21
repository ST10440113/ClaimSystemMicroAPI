using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClaimSystemMicroAPI.Models
{
    [Table("Users")]
    public class User
    {
        [Key] public int UserId { get; set; }
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        [EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string UserName { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        public string ContactNumber { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Faculty { get; set; }
        public decimal? HourlyRate { get; set; }
        public int? MaxHours { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}

