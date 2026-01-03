using System.ComponentModel.DataAnnotations;

namespace UsersApp.ViewModels
{
    public class MoodEntryViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Please select a mood")]
        [StringLength(50)]
        public string Mood { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string? Notes { get; set; }

        [Range(1, 10, ErrorMessage = "Intensity must be between 1 and 10")]
        public int? Intensity { get; set; }
    }

    public class MoodCalendarViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public Dictionary<DateTime, string> MoodsByDate { get; set; } = new();
        public List<MoodEntryViewModel> MoodEntries { get; set; } = new();
    }
}

