using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using UsersApp.Data;
using UsersApp.Models;
using UsersApp.ViewModels;

namespace UsersApp.Controllers
{
    [Authorize]
    public class JournalController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private const int PageSize = 10;

        public JournalController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // List view with pagination, search, and filter
        public async Task<IActionResult> List(string? search, string? tag, DateTime? dateFrom, DateTime? dateTo, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var query = _context.JournalEntries
                .Where(e => e.UserId == user.Id)
                .AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Title!.Contains(search) || e.Content.Contains(search));
            }

            // Tag filter
            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(e => e.Tags != null && e.Tags.Contains(tag));
            }

            // Date range filter
            if (dateFrom.HasValue)
            {
                query = query.Where(e => e.EntryDate >= dateFrom.Value);
            }
            if (dateTo.HasValue)
            {
                query = query.Where(e => e.EntryDate <= dateTo.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

            var entries = await query
                .OrderByDescending(e => e.EntryDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // Get all tags for filter dropdown
            var allTags = await _context.JournalEntries
                .Where(e => e.UserId == user.Id && !string.IsNullOrEmpty(e.Tags))
                .Select(e => e.Tags!)
                .ToListAsync();

            var allTagList = allTags
                .SelectMany(t => t.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            var viewModel = new JournalListViewModel
            {
                Entries = entries.Select(e => new JournalEntryViewModel
                {
                    Id = e.Id,
                    EntryDate = e.EntryDate,
                    Title = e.Title,
                    Content = e.Content.Length > 200 ? e.Content.Substring(0, 200) + "..." : e.Content,
                    Tags = e.Tags,
                    IsProtected = e.IsProtected
                }).ToList(),
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = PageSize,
                SearchQuery = search,
                TagFilter = tag,
                DateFrom = dateFrom,
                DateTo = dateTo,
                AllTags = allTagList
            };

            return View(viewModel);
        }

        // Calendar view - shows entries by date
        public async Task<IActionResult> Index(int? year, int? month)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var targetDate = DateTime.Now;
            if (year.HasValue && month.HasValue)
            {
                targetDate = new DateTime(year.Value, month.Value, 1);
            }

            var startDate = new DateTime(targetDate.Year, targetDate.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var entries = await _context.JournalEntries
                .Where(e => e.UserId == user.Id && e.EntryDate >= startDate && e.EntryDate <= endDate)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            ViewBag.Year = targetDate.Year;
            ViewBag.Month = targetDate.Month;
            ViewBag.MonthName = targetDate.ToString("MMMM yyyy");
            ViewBag.PrevMonth = startDate.AddMonths(-1);
            ViewBag.NextMonth = startDate.AddMonths(1);
            ViewBag.EntriesByDate = entries.GroupBy(e => e.EntryDate.Date).ToDictionary(g => g.Key, g => g.ToList());

            return View(entries);
        }

        // View entry for a specific date
        public async Task<IActionResult> ViewEntry(DateTime? date, int? id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            JournalEntry? entry = null;

            if (id.HasValue)
            {
                entry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Id == id.Value && e.UserId == user.Id);
            }
            else if (date.HasValue)
            {
                entry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.UserId == user.Id && e.EntryDate.Date == date.Value.Date);
            }

            if (entry == null)
            {
                if (date.HasValue)
                {
                    return RedirectToAction("Create", new { date = date.Value });
                }
                return RedirectToAction("List");
            }

            // Check if entry is protected
            if (entry.IsProtected)
            {
                var pinVerified = HttpContext.Session.GetString($"JournalPin_{entry.Id}");
                if (string.IsNullOrEmpty(pinVerified))
                {
                    ViewBag.EntryId = entry.Id;
                    ViewBag.IsProtected = true;
                    return View("VerifyPin");
                }
            }

            var viewModel = new JournalEntryViewModel
            {
                Id = entry.Id,
                EntryDate = entry.EntryDate,
                Title = entry.Title,
                Content = entry.Content,
                Tags = entry.Tags,
                IsProtected = entry.IsProtected
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPin(int entryId, string pin)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == user.Id);

            if (entry == null || !entry.IsProtected || string.IsNullOrEmpty(entry.PinHash))
            {
                return NotFound();
            }

            var hashedPin = HashPin(pin);
            if (hashedPin == entry.PinHash)
            {
                HttpContext.Session.SetString($"JournalPin_{entryId}", "verified");
                return RedirectToAction("ViewEntry", new { id = entryId });
            }
            else
            {
                ModelState.AddModelError("", "Invalid PIN. Please try again.");
                ViewBag.EntryId = entryId;
                ViewBag.IsProtected = true;
                return View("VerifyPin");
            }
        }

        // Create new entry
        public IActionResult Create(DateTime? date)
        {
            var viewModel = new JournalEntryViewModel
            {
                EntryDate = date ?? DateTime.Today
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JournalEntryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var entry = new JournalEntry
                {
                    UserId = user.Id,
                    EntryDate = viewModel.EntryDate,
                    Title = viewModel.Title,
                    Content = viewModel.Content,
                    Tags = viewModel.Tags,
                    IsProtected = viewModel.IsProtected,
                    PinHash = viewModel.IsProtected && !string.IsNullOrEmpty(viewModel.Pin) 
                        ? HashPin(viewModel.Pin) 
                        : null,
                    CreatedAt = DateTime.Now
                };

                _context.JournalEntries.Add(entry);
                await _context.SaveChangesAsync();

                // Clear PIN from session if viewing
                HttpContext.Session.Remove($"JournalPin_{entry.Id}");

                return RedirectToAction("List");
            }

            return View(viewModel);
        }

        // Edit entry
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id);

            if (entry == null)
            {
                return NotFound();
            }

            // Check PIN if protected
            if (entry.IsProtected)
            {
                var pinVerified = HttpContext.Session.GetString($"JournalPin_{entry.Id}");
                if (string.IsNullOrEmpty(pinVerified))
                {
                    ViewBag.EntryId = entry.Id;
                    ViewBag.IsProtected = true;
                    return View("VerifyPin");
                }
            }

            var viewModel = new JournalEntryViewModel
            {
                Id = entry.Id,
                EntryDate = entry.EntryDate,
                Title = entry.Title,
                Content = entry.Content,
                Tags = entry.Tags,
                IsProtected = entry.IsProtected
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(JournalEntryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                var entry = await _context.JournalEntries
                    .FirstOrDefaultAsync(e => e.Id == viewModel.Id && e.UserId == user.Id);

                if (entry == null)
                {
                    return NotFound();
                }

                entry.Title = viewModel.Title;
                entry.Content = viewModel.Content;
                entry.Tags = viewModel.Tags;
                entry.EntryDate = viewModel.EntryDate;
                entry.UpdatedAt = DateTime.Now;

                // Update protection
                if (viewModel.IsProtected)
                {
                    entry.IsProtected = true;
                    if (!string.IsNullOrEmpty(viewModel.Pin))
                    {
                        entry.PinHash = HashPin(viewModel.Pin);
                    }
                }
                else
                {
                    entry.IsProtected = false;
                    entry.PinHash = null;
                }

                _context.Update(entry);
                await _context.SaveChangesAsync();

                return RedirectToAction("List");
            }

            return View(viewModel);
        }

        // Delete entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var entry = await _context.JournalEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id);

            if (entry != null)
            {
                _context.JournalEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("List");
        }

        // Export journals
        public async Task<IActionResult> Export(string format = "json")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var entries = await _context.JournalEntries
                .Where(e => e.UserId == user.Id)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            return format.ToLower() switch
            {
                "json" => File(
                    Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(entries, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })),
                    "application/json",
                    $"journal_export_{DateTime.Now:yyyyMMdd}.json"),
                "csv" => File(
                    Encoding.UTF8.GetBytes(GenerateCsv(entries)),
                    "text/csv",
                    $"journal_export_{DateTime.Now:yyyyMMdd}.csv"),
                _ => RedirectToAction("List")
            };
        }

        // Get streak count
        [HttpGet]
        public async Task<IActionResult> GetStreak()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { streak = 0 });

            var entries = await _context.JournalEntries
                .Where(e => e.UserId == user.Id)
                .Select(e => e.EntryDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            if (!entries.Any()) return Json(new { streak = 0 });

            var streak = 0;
            var currentDate = DateTime.Today;

            foreach (var entryDate in entries)
            {
                if (entryDate == currentDate || entryDate == currentDate.AddDays(-streak))
                {
                    if (entryDate == currentDate)
                    {
                        streak = 1;
                        currentDate = currentDate.AddDays(-1);
                    }
                    else if (entryDate == currentDate)
                    {
                        streak++;
                        currentDate = currentDate.AddDays(-1);
                    }
                }
                else
                {
                    break;
                }
            }

            return Json(new { streak });
        }

        // Helper methods
        private static string HashPin(string pin)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(pin);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static string GenerateCsv(List<JournalEntry> entries)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Date,Title,Content,Tags");
            foreach (var entry in entries)
            {
                var content = entry.Content.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
                var title = (entry.Title ?? "").Replace("\"", "\"\"");
                var tags = (entry.Tags ?? "").Replace("\"", "\"\"");
                sb.AppendLine($"\"{entry.EntryDate:yyyy-MM-dd}\",\"{title}\",\"{content}\",\"{tags}\"");
            }
            return sb.ToString();
        }
    }
}
