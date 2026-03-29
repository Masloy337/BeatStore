using Microsoft.AspNetCore.Mvc;
using BeatStore.Data;
using BeatStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using BeatStore.Services; // 🔥 1. Добавили пространство имен нашего сервиса писем

namespace BeatStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService; // 🔥 2. Добавили поле для сервиса

        // 🔥 3. Внедрили сервис в конструктор
        public CartController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCart();
            if (!cart.Any())
            {
                return RedirectToAction("Index");
            }

            decimal totalAmount = 0;

            foreach (var item in cart)
            {
                var license = await _context.Licenses.FindAsync(item.LicenseId);
                if (license != null)
                {
                    totalAmount += license.Price;
                }
            }

            ViewBag.TotalAmount = totalAmount;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment()
        {
            var cart = GetCart();
            if (!cart.Any()) return RedirectToAction("Index");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔥 Получаем email текущего пользователя
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity.Name;

            // Временный список для хранения успешных заказов, чтобы отправить по ним письма
            var successfulOrders = new List<(Order order, string trackTitle, string licenseName)>();

            foreach (var item in cart)
            {
                var beat = await _context.Beats
                    .Include(b => b.Licenses)
                    .FirstOrDefaultAsync(b => b.Id == item.BeatId);

                if (beat == null) continue;

                var license = beat.Licenses?.FirstOrDefault(l => l.Id == item.LicenseId);
                if (license == null) continue;

                if (beat.IsSold) continue;

                var alreadyBought = await _context.Orders
                    .AnyAsync(o => o.UserId == userId && o.BeatId == beat.Id && o.LicenseId == license.Id);

                if (alreadyBought) continue;

                var order = new Order
                {
                    UserId = userId,
                    BeatId = beat.Id,
                    LicenseId = license.Id,
                    Price = license.Price,
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);

                // 🔥 Сохраняем информацию для письма
                successfulOrders.Add((order, beat.Title, license.Name));

                if (license.Name == "Exclusive")
                {
                    beat.IsSold = true;
                    _context.Beats.Update(beat);
                }
            }

            // 🔥 Сохраняем в БД. Только ПОСЛЕ этого у order.Id появится реальный номер (например, 15)
            await _context.SaveChangesAsync();

            // 🔥 4. РАССЫЛКА ПИСЕМ
            if (!string.IsNullOrEmpty(userEmail))
            {
                foreach (var record in successfulOrders)
                {
                    try
                    {
                        // Отправляем письмо с реальным ID заказа
                        await _emailService.SendOrderReceiptAsync(userEmail, record.trackTitle, record.licenseName, record.order.Id);
                    }
                    catch (Exception ex)
                    {
                        // Если Resend выдаст ошибку (например, не тот email для тестов), 
                        // мы ловим её здесь, чтобы пользователь всё равно попал на страницу Success!
                        Console.WriteLine($"Ошибка отправки письма: {ex.Message}");
                    }
                }
            }

            HttpContext.Session.Remove("cart");

            return RedirectToAction("Success");
        }

        [Authorize]
        public IActionResult Success()
        {
            return View();
        }
    }
}