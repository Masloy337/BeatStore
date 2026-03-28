using BeatStore.Data;
using BeatStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BeatStore.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> MyBeats()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Redirect("/Identity/Account/Login");

            var orders = _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Beat)
            .Include(o => o.License) // 🔥 ВАЖНО
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

            return View(orders);
        }
        // 🔥 ГЕНЕРАЦИЯ ЛИЦЕНЗИИ
        [Authorize]
        public async Task<IActionResult> License(int orderId)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Находим заказ, проверяем что он принадлежит текущему пользователю
            var order = await _context.Orders
                .Include(o => o.Beat)
                .Include(o => o.License)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Beat == null || order.License == null)
            {
                return NotFound("Заказ или лицензия не найдены.");
            }

            // Передаем имя покупателя (email или никнейм)
            ViewBag.BuyerName = User.Identity.Name;

            return View(order); // Отдаем во View
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int beatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Json(new { success = false, message = "Необходимо войти в аккаунт" });

            // Ищем, лайкал ли уже этот юзер этот бит
            var existingFavorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BeatId == beatId);

            bool isLiked = false;

            if (existingFavorite != null)
            {
                // Если лайк уже стоит — убираем его
                _context.Favorites.Remove(existingFavorite);
            }
            else
            {
                // Если лайка нет — ставим
                _context.Favorites.Add(new Favorite { UserId = userId, BeatId = beatId });
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            // Возвращаем результат в JS, чтобы перекрасить сердечко
            return Json(new { success = true, isLiked = isLiked });
        }

        // 🔥 2. СТРАНИЦА "МОИ ЛАЙКИ"
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Favorites()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Получаем все лайкнутые биты пользователя
            var favoriteBeats = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Beat)
                    .ThenInclude(b => b.Licenses) // Подтягиваем лицензии, чтобы показывать цены
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.Beat)
                .ToListAsync();

            return View(favoriteBeats);
        }
    }
}