using Microsoft.AspNetCore.Mvc;
using BeatStore.Data;
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
    }
}