using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UsersApp.Data;
using UsersApp.Models;

namespace UsersApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public DashboardController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var now = DateTime.Now;
            var last30Days = now.AddDays(-30);
            var last7Days = now.AddDays(-7);

            // Journal Statistics
            var totalEntries = await _context.JournalEntries
                .Where(e => e.UserId == user.Id)
                .CountAsync();

            var entriesLast30Days = await _context.JournalEntries
                .Where(e => e.UserId == user.Id && e.EntryDate >= last30Days)
                .CountAsync();

            var entriesLast7Days = await _context.JournalEntries
                .Where(e => e.UserId == user.Id && e.EntryDate >= last7Days)
                .CountAsync();

            // Calculate streak
            var entryDates = await _context.JournalEntries
                .Where(e => e.UserId == user.Id)
                .Select(e => e.EntryDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            var streak = CalculateStreak(entryDates);

            // Mood Statistics
            var moodEntries = await _context.MoodEntries
                .Where(e => e.UserId == user.Id && e.EntryDate >= last30Days)
                .ToListAsync();

            var moodCounts = moodEntries
                .GroupBy(e => e.Mood)
                .Select(g => new { Mood = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var averageIntensity = moodEntries.Where(e => e.Intensity.HasValue).Any()
                ? moodEntries.Where(e => e.Intensity.HasValue).Average(e => e.Intensity!.Value)
                : 0;

            // Recent entries
            var recentEntries = await _context.JournalEntries
                .Where(e => e.UserId == user.Id)
                .OrderByDescending(e => e.EntryDate)
                .Take(5)
                .ToListAsync();

            // Mood trend (last 7 days)
            var moodTrend = await _context.MoodEntries
                .Where(e => e.UserId == user.Id && e.EntryDate >= last7Days)
                .OrderBy(e => e.EntryDate)
                .Select(e => new { Date = e.EntryDate.Date, Mood = e.Mood, Intensity = e.Intensity ?? 5 })
                .ToListAsync();

            ViewBag.TotalEntries = totalEntries;
            ViewBag.EntriesLast30Days = entriesLast30Days;
            ViewBag.EntriesLast7Days = entriesLast7Days;
            ViewBag.Streak = streak;
            ViewBag.MoodCounts = moodCounts;
            ViewBag.AverageIntensity = Math.Round(averageIntensity, 1);
            ViewBag.RecentEntries = recentEntries;
            ViewBag.MoodTrend = moodTrend;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStreak()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(new { streak = 0 });

            var entryDates = await _context.JournalEntries
                .Where(e => e.UserId == user.Id)
                .Select(e => e.EntryDate.Date)
                .Distinct()
                .OrderByDescending(d => d)
                .ToListAsync();

            var streak = CalculateStreak(entryDates);
            return Json(new { streak });
        }

        private static int CalculateStreak(List<DateTime> entryDates)
        {
            if (!entryDates.Any()) return 0;

            var streak = 0;
            var currentDate = DateTime.Today;

            // Check if today has an entry
            if (entryDates.Contains(currentDate))
            {
                streak = 1;
                currentDate = currentDate.AddDays(-1);
            }

            // Count consecutive days
            foreach (var entryDate in entryDates)
            {
                if (entryDate == currentDate)
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else if (entryDate < currentDate)
                {
                    break;
                }
            }

            return streak;
        }
    }
}

