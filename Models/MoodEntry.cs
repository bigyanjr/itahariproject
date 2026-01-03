using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsersApp.Models
{
    public class MoodEntry
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Mood { get; set; } // e.g., "Happy", "Sad", "Anxious", "Calm", "Excited", "Tired", "Energetic"

        [StringLength(500)]
        public string? Notes { get; set; }

        [Range(1, 10)]
        public int? Intensity { get; set; } // 1-10 scale

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public virtual Users? User { get; set; }
    }
}

