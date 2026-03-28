using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace BeatStore.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Свойство для сохранения ссылки возврата
        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Поле Email обязательно.")]
            [EmailAddress(ErrorMessage = "Введите корректный Email.")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Поле Пароль обязательно.")]
            [StringLength(100, ErrorMessage = "Пароль должен содержать не менее {2} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            // 🔥 ВОТ ОНО! Добавили поле подтверждения пароля с проверкой на совпадение
            [DataType(DataType.Password)]
            [Display(Name = "Подтвердите пароль")]
            [Compare("Password", ErrorMessage = "Пароли не совпадают.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        // Метод для получения страницы
        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/Beats");
            ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
                return Page();

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, false);
                return LocalRedirect(returnUrl); // Возвращаем туда, куда собирался юзер (или в каталог)
            }

            // Если при регистрации возникла ошибка (например, слишком простой пароль или email уже занят)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}