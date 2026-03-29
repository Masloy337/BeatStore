using BeatStore.Data;
using BeatStore.Models;
using BeatStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
        // Не забудь добавить этот using в самом верху контроллера:
        // using BeatStore.ViewModels;

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string genre)
        {
            // 1. Создаем нашу пустую "коробку"
            var viewModel = new BeatsIndexViewModel
            {
                CurrentSearch = searchString,
                CurrentGenre = genre
            };

            // 2. Кладем туда Жанры
            viewModel.Genres = await _context.Beats
                .Where(b => !string.IsNullOrEmpty(b.Genre))
                .Select(b => b.Genre)
                .Distinct()
                .AsNoTracking()
                .ToListAsync();

            // 3. Кладем туда ТОП-биты
            viewModel.TopBeats = await _context.Beats
                .Include(b => b.Likes)
                .OrderByDescending(b => b.PlayCount)
                .Take(4)
                .AsNoTracking()
                .ToListAsync();

            // 4. Собираем основной запрос
            var beatsQuery = _context.Beats
                .Include(b => b.Likes)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                beatsQuery = beatsQuery.Where(b =>
                    b.Title.Contains(searchString) ||
                    b.ProducerName.Contains(searchString) ||
                    (b.Genre != null && b.Genre.Contains(searchString)) ||
                    (b.Tags != null && b.Tags.Contains(searchString))
                );
            }

            if (!string.IsNullOrEmpty(genre))
            {
                beatsQuery = beatsQuery.Where(b => b.Genre == genre);
            }

            // 5. Кладем отфильтрованные биты в модель
            viewModel.Beats = await beatsQuery.ToListAsync();

            // 6. Отправляем ОДНУ коробку на страницу вместо кучи ViewBag
            return View(viewModel);
        }

        // 🔥 ОБНОВЛЕННЫЙ BUY (поддержка лицензий)
        [HttpPost]
        [Authorize] // 🔒 Защита: только для авторизованных
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Buy(int id, int? licenseId)
        {
            // ⚠️ Здесь мы меняем данные (IsSold), поэтому AsNoTracking НЕ используем
            var beat = await _context.Beats
                .Include(b => b.Licenses)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (beat == null)
                return NotFound();

            if (beat.IsSold)
                return BadRequest("Уже продано");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            License? license = null;

            if (licenseId.HasValue)
            {
                license = beat.Licenses?.FirstOrDefault(x => x.Id == licenseId.Value);

                if (license == null)
                    return BadRequest("Лицензия не найдена");
            }

            // 🔥 СОЗДАЕМ ЗАКАЗ (ИСПРАВЛЕНИЕ: ДОБАВЛЕНА ЦЕНА)
            var order = new Order
            {
                UserId = userId,
                BeatId = beat.Id,
                LicenseId = license?.Id,
                Price = license != null ? license.Price : beat.Price, // 🔥 Фиксируем цену покупки!
                CreatedAt = DateTime.Now
            };

            _context.Orders.Add(order);

            // Если Exclusive — продаём полностью
            if (license != null && license.Name == "Exclusive")
            {
                beat.IsSold = true;
            }
            // Если лицензии нет (старый способ)
            else if (license == null)
            {
                beat.IsSold = true;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // 🔥 УВЕЛИЧЕНИЕ ПРОСЛУШИВАНИЙ
        [HttpPost]
        public async Task<IActionResult> AddPlay(int id)
        {
            // ⚠️ Изменяем PlayCount, AsNoTracking НЕ используем
            var beat = await _context.Beats.FindAsync(id);

            if (beat == null)
                return NotFound();

            beat.PlayCount++;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // 🔥 ПОЛУЧЕНИЕ ЛИЦЕНЗИЙ ДЛЯ МОДАЛКИ
        [HttpGet]
        public async Task<IActionResult> GetLicenses(int beatId) // 🚀 ИСПРАВЛЕНИЕ: Сделали асинхронным
        {
            var licenses = await _context.Licenses
                .Where(l => l.BeatId == beatId)
                .Select(l => new {
                    l.Id,
                    l.Name,
                    l.Price
                })
                .AsNoTracking() // 🚀 Оптимизация
                .ToListAsync();

            return Json(licenses);
        }

        // GET: /Beats/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var beat = await _context.Beats
                .Include(b => b.Licenses)
                .AsNoTracking() // 🚀 Оптимизация
                .FirstOrDefaultAsync(m => m.Id == id);

            if (beat == null) return NotFound();

            return View(beat);
        }
    }
}