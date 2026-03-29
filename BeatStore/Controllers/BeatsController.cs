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

        // 🔥 Обновленный метод Index с поиском и фильтрами
        public async Task<IActionResult> Index(string searchString, string genre)
        {
            // 1. Получаем уникальные жанры из базы данных для кнопок-фильтров
            var genres = await _context.Beats
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .Select(b => b.Genre)
                .Distinct()
                .ToListAsync();

            ViewBag.Genres = genres;
            ViewBag.CurrentGenre = genre;
            ViewBag.CurrentSearch = searchString;

            // 2. Получаем ТОП-биты (например, 4 самых прослушиваемых)
            ViewBag.TopBeats = await _context.Beats
                .Include(b => b.Likes)
                .OrderByDescending(b => b.PlayCount)
                .Take(4)
                .ToListAsync();

            // 3. Базовый запрос для списка ВСЕХ битов
            var beatsQuery = _context.Beats
                .Include(b => b.Likes)
                .AsQueryable();

            // 🔥 ФИЛЬТР 1: Поиск по названию, продюсеру, ЖАНРУ и ТЕГАМ!
            if (!string.IsNullOrEmpty(searchString))
            {
                beatsQuery = beatsQuery.Where(b =>
                    b.Title.Contains(searchString) ||
                    b.ProducerName.Contains(searchString) ||
                    (b.Genre != null && b.Genre.Contains(searchString)) ||
                    (b.Tags != null && b.Tags.Contains(searchString))
                );
            }

            // 🔥 ФИЛЬТР 2: По клику на кнопку жанра
            if (!string.IsNullOrEmpty(genre))
            {
                beatsQuery = beatsQuery.Where(b => b.Genre == genre);
            }

            // Выполняем запрос и отправляем отфильтрованные биты на страницу
            var beats = await beatsQuery.ToListAsync();
            return View(beats);
        }

        // 🔥 ОБНОВЛЕННЫЙ BUY (поддержка лицензий)
        [HttpPost]
        [ValidateAntiForgeryToken]
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
        // GET: /Beats/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var beat = await _context.Beats
                .Include(b => b.Licenses)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (beat == null) return NotFound();

            return View(beat);
        }
    }
}
