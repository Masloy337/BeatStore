using Microsoft.AspNetCore.Mvc;
using BeatStore.Data;
using BeatStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization; // 🔥 Добавили для атрибута [Authorize]

namespace BeatStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class CartItem
        {
            public int BeatId { get; set; }
            public int LicenseId { get; set; }
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("cart");
            if (string.IsNullOrEmpty(cartJson)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("cart", JsonSerializer.Serialize(cart));
        }

        // ➕ Добавить в корзину (AJAX версия)
        [HttpPost]
        public IActionResult Add(int beatId, int licenseId)
        {
            var cart = GetCart();

            var existingItem = cart.FirstOrDefault(x => x.BeatId == beatId);
            if (existingItem != null)
            {
                existingItem.LicenseId = licenseId;
            }
            else
            {
                cart.Add(new CartItem { BeatId = beatId, LicenseId = licenseId });
            }

            SaveCart(cart);

            return Json(new { success = true, message = "Бит добавлен в корзину", cartCount = cart.Count });
        }

        // 🧾 Страница корзины
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();
            var beatIds = cart.Select(x => x.BeatId).ToList();

            var beats = await _context.Beats
                .Include(b => b.Licenses)
                .Where(b => beatIds.Contains(b.Id))
                .ToListAsync();

            ViewBag.CartItems = cart;

            return View(beats);
        }

        // ❌ Удалить из корзины
        public IActionResult Remove(int beatId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.BeatId == beatId);

            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        // 🔥 1. СТРАНИЦА ОФОРМЛЕНИЯ ЗАКАЗА (GET-запрос)
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index"); // Если корзина пуста - кидаем обратно
            }

            decimal totalAmount = 0;

            // Считаем сумму прямо из базы данных для безопасности
            foreach (var item in cart)
            {
                var license = await _context.Licenses.FindAsync(item.LicenseId);
                if (license != null)
                {
                    totalAmount += license.Price;
                }
            }

            ViewBag.TotalAmount = totalAmount;
            return View(); // Отдаст страницу Checkout.cshtml, которую мы создали
        }

        // 🔥 2. СИМУЛЯЦИЯ ОПЛАТЫ И ВЫДАЧА БИТОВ (POST-запрос)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var item in cart)
            {
                var beat = await _context.Beats
                    .Include(b => b.Licenses)
                    .FirstOrDefaultAsync(b => b.Id == item.BeatId);

                if (beat == null) continue;

                var license = beat.Licenses?.FirstOrDefault(l => l.Id == item.LicenseId);
                if (license == null) continue;

                // Защита: Если бит уже купили эксклюзивно пока мы были в корзине
                if (beat.IsSold) continue;

                // Защита: Уже куплен этот бит с этой лицензией этим юзером?
                var alreadyBought = await _context.Orders
                    .AnyAsync(o => o.UserId == userId && o.BeatId == beat.Id && o.LicenseId == license.Id);

                if (alreadyBought) continue;

                // Создаем заказ
                var order = new Order
                {
                    UserId = userId,
                    BeatId = beat.Id,
                    LicenseId = license.Id,
                    // 🔥 ТЕПЕРЬ ОШИБКИ НЕ БУДЕТ, СОХРАНЯЕМ ЦЕНУ:
                    Price = license.Price,
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);

                // Если купили Exclusive — навсегда снимаем бит с продажи
                if (license.Name == "Exclusive")
                {
                    beat.IsSold = true;
                    _context.Beats.Update(beat);
                }
            }

            await _context.SaveChangesAsync();

            // Очищаем сессию корзины после успешной оплаты
            HttpContext.Session.Remove("cart");

            // Перекидываем на красивую страницу успеха
            return RedirectToAction("Success");
        }

        // 🔥 3. СТРАНИЦА УСПЕХА
        [Authorize]
        public IActionResult Success()
        {
            return View();
        }
    }
}