using System.ComponentModel.DataAnnotations;

namespace UsersApp.ViewModels
{
    public class JournalEntryViewModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; } = DateTime.Today;

        [StringLength(200)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = string.Empty;

        public string? Tags { get; set; }

        public bool IsProtected { get; set; } = false;

        [StringLength(20, MinimumLength = 4, ErrorMessage = "PIN must be between 4 and 20 characters")]
        public string? Pin { get; set; }

        public string? VerifyPin { get; set; }

        public List<string> TagList => string.IsNullOrEmpty(Tags) 
            ? new List<string>() 
            : Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList();
    }

    public class JournalListViewModel
    {
        public List<JournalEntryViewModel> Entries { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
        public string? SearchQuery { get; set; }
        public string? TagFilter { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public List<string> AllTags { get; set; } = new();
    }
}

