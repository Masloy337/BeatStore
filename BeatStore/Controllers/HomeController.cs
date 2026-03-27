using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;

namespace BeatStore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost("/logout")]
        public async Task<IActionResult> Logout(
        [FromServices] SignInManager<IdentityUser> signInManager)
        {
            await signInManager.SignOutAsync();
            return Redirect("/");
        }
        [HttpGet("/make-admin")]
        public async Task<IActionResult> MakeAdmin(
        [FromServices] UserManager<IdentityUser> userManager,
        [FromServices] RoleManager<IdentityRole> roleManager)
        {
            var user = await userManager.GetUserAsync(User);

            if (user == null)
                return Content("User not found");

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            await userManager.AddToRoleAsync(user, "Admin");

            return Content("Ты теперь ADMIN 🔥");
        }
    }
}
