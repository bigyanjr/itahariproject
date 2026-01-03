using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsersApp.Models
{
    public class JournalEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; }

        [Required]
        public string Content { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        public string? Tags { get; set; } // Comma-separated tags

        public bool IsProtected { get; set; } = false;

        public string? PinHash { get; set; } // Hashed PIN for protection

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }

        [NotMapped]
        public List<string> TagList => string.IsNullOrEmpty(Tags) 
            ? new List<string>() 
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
    }
}

