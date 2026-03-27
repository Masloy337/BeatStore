using Microsoft.AspNetCore.Mvc;
using BeatStore.Data;
using BeatStore.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace BeatStore.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔥 модель элемента корзины
        public class CartItem
        {
            public int BeatId { get; set; }
            public int LicenseId { get; set; }
        }

        // 📦 получить корзину
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("cart");

            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItem>();

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        // 💾 сохранить корзину
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString("cart", JsonSerializer.Serialize(cart));
        }

        // ➕ добавить (🔥 теперь с лицензией)
        public IActionResult Add(int beatId, int licenseId)
        {
            var cart = GetCart();

            if (!cart.Any(x => x.BeatId == beatId && x.LicenseId == licenseId))
            {
                cart.Add(new CartItem
                {
                    BeatId = beatId,
                    LicenseId = licenseId
                });
            }

            SaveCart(cart);

            return RedirectToAction("Index", "Beats");
        }

        // 🧾 страница корзины
        public async Task<IActionResult> Index()
        {
            var cart = GetCart();

            var beatIds = cart.Select(x => x.BeatId).ToList();

            var beats = await _context.Beats
                .Include(b => b.Licenses)
                .Where(b => beatIds.Contains(b.Id))
                .ToListAsync();

            return View(beats);
        }

        // ❌ удалить
        public IActionResult Remove(int beatId, int licenseId)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.BeatId == beatId && x.LicenseId == licenseId);

            if (item != null)
                cart.Remove(item);

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        // 💰 купить все
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Redirect("/Identity/Account/Login");

            var cart = GetCart();

            foreach (var item in cart)
            {
                var beat = await _context.Beats
                    .Include(b => b.Licenses)
                    .FirstOrDefaultAsync(b => b.Id == item.BeatId);

                if (beat == null)
                    continue;

                var license = beat.Licenses?.FirstOrDefault(l => l.Id == item.LicenseId);

                if (license == null)
                    continue;

                // ❌ уже куплен
                var alreadyBought = _context.Orders
                    .Any(o => o.UserId == userId && o.BeatId == beat.Id && o.LicenseId == license.Id);

                if (alreadyBought)
                    continue;

                var order = new Order
                {
                    UserId = userId,
                    BeatId = beat.Id,
                    LicenseId = license.Id, // 🔥 КЛЮЧЕВОЕ
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);

                // 🔥 если exclusive — закрываем бит
                if (license.Name == "Exclusive")
                {
                    beat.IsSold = true;
                }
            }

            await _context.SaveChangesAsync();

            SaveCart(new List<CartItem>());

            TempData["Message"] = "Покупка успешна";

            return RedirectToAction("Index", "Beats");
        }
    }
}