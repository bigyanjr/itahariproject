using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsersApp.Data;
using UsersApp.Models;
using UsersApp.ViewModels;

namespace UsersApp.Controllers
{
    [Authorize]
    public class MoodController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public MoodController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Calendar view with mood tracking
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

            var moodEntries = await _context.MoodEntries
                .Where(e => e.UserId == user.Id && e.EntryDate >= startDate && e.EntryDate <= endDate)
                .OrderByDescending(e => e.EntryDate)
                .ToListAsync();

            var moodsByDate = moodEntries.ToDictionary(e => e.EntryDate.Date, e => e.Mood);

            ViewBag.Year = targetDate.Year;
            ViewBag.Month = targetDate.Month;
            ViewBag.MonthName = targetDate.ToString("MMMM yyyy");
            ViewBag.PrevMonth = startDate.AddMonths(-1);
            ViewBag.NextMonth = startDate.AddMonths(1);
            ViewBag.MoodsByDate = moodsByDate;
            ViewBag.MoodEntries = moodEntries;

            return View(moodEntries);
        }

        // Create or edit mood entry for a specific date
        public async Task<IActionResult> Create(DateTime? date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var entryDate = date ?? DateTime.Today;
            var existingEntry = await _context.MoodEntries
                .FirstOrDefaultAsync(e => e.UserId == user.Id && e.EntryDate.Date == entryDate.Date);

            MoodEntryViewModel viewModel;
            if (existingEntry != null)
            {
                viewModel = new MoodEntryViewModel
                {
                    Id = existingEntry.Id,
                    EntryDate = existingEntry.EntryDate,
                    Mood = existingEntry.Mood,
                    Notes = existingEntry.Notes,
                    Intensity = existingEntry.Intensity
                };
            }
            else
            {
                viewModel = new MoodEntryViewModel
                {
                    EntryDate = entryDate
                };
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MoodEntryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return RedirectToAction("Login", "Account");

                MoodEntry entry;
                if (viewModel.Id.HasValue)
                {
                    entry = await _context.MoodEntries
                        .FirstOrDefaultAsync(e => e.Id == viewModel.Id.Value && e.UserId == user.Id);
                    
                    if (entry == null)
                    {
                        return NotFound();
                    }

                    entry.Mood = viewModel.Mood;
                    entry.Notes = viewModel.Notes;
                    entry.Intensity = viewModel.Intensity;
                    entry.EntryDate = viewModel.EntryDate;
                }
                else
                {
                    entry = new MoodEntry
                    {
                        UserId = user.Id,
                        EntryDate = viewModel.EntryDate,
                        Mood = viewModel.Mood,
                        Notes = viewModel.Notes,
                        Intensity = viewModel.Intensity,
                        CreatedAt = DateTime.Now
                    };

                    _context.MoodEntries.Add(entry);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { year = viewModel.EntryDate.Year, month = viewModel.EntryDate.Month });
            }

            return View(viewModel);
        }

        // Delete mood entry
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var entry = await _context.MoodEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.Id);

            if (entry != null)
            {
                _context.MoodEntries.Remove(entry);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Get mood statistics
        public async Task<IActionResult> Statistics()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var last30Days = DateTime.Today.AddDays(-30);
            var moodEntries = await _context.MoodEntries
                .Where(e => e.UserId == user.Id && e.EntryDate >= last30Days)
                .ToListAsync();

            var moodCounts = moodEntries
                .GroupBy(e => e.Mood)
                .Select(g => new { Mood = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            ViewBag.MoodCounts = moodCounts;
            ViewBag.TotalEntries = moodEntries.Count;
            ViewBag.AverageIntensity = moodEntries.Where(e => e.Intensity.HasValue).Any() 
                ? moodEntries.Where(e => e.Intensity.HasValue).Average(e => e.Intensity!.Value) 
                : 0;

            return View();
        }
    }
}

