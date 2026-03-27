using Microsoft.AspNetCore.Mvc;
using BeatStore.Data;
using System.Security.Claims;

namespace BeatStore.Controllers
{
    public class LikeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LikeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Toggle(int beatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            var like = _context.Likes
                .FirstOrDefault(x => x.UserId == userId && x.BeatId == beatId);

            if (like != null)
            {
                _context.Likes.Remove(like);
            }
            else
            {
                _context.Likes.Add(new Models.Like
                {
                    UserId = userId,
                    BeatId = beatId
                });
            }

            _context.SaveChanges();

            return Ok();
        }
    }
}