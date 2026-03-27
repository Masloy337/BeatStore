using Microsoft.AspNetCore.Mvc;
using BeatStore.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BeatStore.Models;

namespace BeatStore.Controllers
{
    public class BeatsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BeatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, int? minBpm, int? maxBpm, string genre)
        {
            var query = _context.Beats
                .Include(b => b.Orders)
                .Include(b => b.Licenses)
                .Include(b => b.Likes)
                .AsQueryable();

            // 🔍 фильтры (оставляем твою логику)
            if (!string.IsNullOrEmpty(search))
                query = query.Where(b => b.Title.Contains(search));

            if (minBpm.HasValue)
                query = query.Where(b => b.Bpm >= minBpm);

            if (maxBpm.HasValue)
                query = query.Where(b => b.Bpm <= maxBpm);

            if (!string.IsNullOrEmpty(genre))
                query = query.Where(b => b.Genre.Contains(genre));

            var beats = await query
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            // 🔥 ТОП БИТЫ
            var topBeats = beats
                .OrderByDescending(b => b.PlayCount + (b.Likes.Count * 3))
                .Take(4)
                .ToList();

            ViewBag.TopBeats = topBeats;

            return View(beats);
        }

        // 🔥 ОБНОВЛЕННЫЙ BUY (поддержка лицензий)
        [HttpPost]
        public async Task<IActionResult> Buy(int id, int? licenseId)
        {
            var beat = await _context.Beats
                .Include(b => b.Licenses)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (beat == null)
                return NotFound();

            if (beat.IsSold)
                return BadRequest("Уже продано");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            License? license = null;

            // 🔥 если передали лицензию — используем её
            if (licenseId.HasValue)
            {
                license = beat.Licenses?.FirstOrDefault(x => x.Id == licenseId.Value);

                if (license == null)
                    return BadRequest("Лицензия не найдена");
            }

            // 🔥 СОЗДАЕМ ЗАКАЗ
            var order = new Order
            {
                UserId = userId,
                BeatId = beat.Id,
                LicenseId = license?.Id, // 🔥 НОВОЕ
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);

            // 🔥 ВАЖНО
            // если Exclusive — продаём полностью
            if (license != null && license.Name == "Exclusive")
            {
                beat.IsSold = true;
            }

            // 🔥 если лицензии нет (старый способ) — оставляем старую логику
            if (license == null)
            {
                beat.IsSold = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> AddPlay(int id)
        {
            var beat = await _context.Beats.FindAsync(id);

            if (beat == null)
                return NotFound();

            beat.PlayCount++;

            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpGet]
        public IActionResult GetLicenses(int beatId)
        {
            var licenses = _context.Licenses
                .Where(l => l.BeatId == beatId)
                .Select(l => new {
                    l.Id,
                    l.Name,
                    l.Price
                })
                .ToList();

            return Json(licenses);
        }
    }
}
