using BeatStore.Data;
using BeatStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace BeatStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        // 🔥 1. ДОБАВЛЕН СЕРВИС ОКРУЖЕНИЯ ДЛЯ ПРАВИЛЬНЫХ ПУТЕЙ
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env; // Сохраняем окружение
        }

        public IActionResult Stats()
        {
            var beats = _context.Beats
                .Include(b => b.Orders)
                .ToList();

            var model = new AdminStatsViewModel();

            model.TotalSales = beats.Sum(b => b.Orders?.Count ?? 0);
            model.TotalRevenue = beats.Sum(b => (b.Orders?.Count ?? 0) * b.Price);

            var topBeat = beats
                .OrderByDescending(b => b.Orders?.Count ?? 0)
                .FirstOrDefault();

            if (topBeat != null)
            {
                model.TopBeatTitle = topBeat.Title;
                model.TopBeatSales = topBeat.Orders?.Count ?? 0;
            }

            model.Beats = beats.Select(b => new BeatStats
            {
                Title = b.Title,
                SalesCount = b.Orders?.Count ?? 0,
                Revenue = (b.Orders?.Count ?? 0) * b.Price
            }).ToList();

            model.SalesByDay = _context.Orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new SalesPoint
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        public IActionResult List()
        {
            var beats = _context.Beats.OrderByDescending(x => x.CreatedAt).ToList();
            return View(beats);
        }

        // 🔥 2. ОБНОВЛЕННЫЙ AJAX-МЕТОД СОЗДАНИЯ БИТА С ЛИЦЕНЗИЯМИ И ТВОИМИ ПАПКАМИ
        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 1073741824)]
        public async Task<IActionResult> CreateBeatAjax(
            string Title, string Genre, string ProducerName, int Bpm,
            int BasicPrice, int PremiumPrice, int ExclusivePrice, decimal Price,
            string? Tags,
            IFormFile? demoFile, IFormFile? fullFile, IFormFile? mp3File, IFormFile? imageFile)
        {
            try
            {
                // ContentRootPath - корень проекта (защищенно)
                // WebRootPath - папка wwwroot (публично)
                var demoFolder = Path.Combine(_env.ContentRootPath, "Storage", "demo");
                var mp3Folder = Path.Combine(_env.ContentRootPath, "Storage", "mp3");
                var fullFolder = Path.Combine(_env.ContentRootPath, "Storage", "full");
                var imageFolder = Path.Combine(_env.WebRootPath, "uploads", "images");

                // Создаем папки, если их еще нет
                Directory.CreateDirectory(demoFolder);
                Directory.CreateDirectory(mp3Folder);
                Directory.CreateDirectory(fullFolder);
                Directory.CreateDirectory(imageFolder);

                string? demoPath = null;
                string? mp3Path = null;
                string? fullPath = null;
                string? imagePath = null;

                // --- DEMO (Защищенный)
                if (demoFile != null)
                {
                    var name = Guid.NewGuid() + Path.GetExtension(demoFile.FileName);
                    using var stream = new FileStream(Path.Combine(demoFolder, name), FileMode.Create);
                    await demoFile.CopyToAsync(stream);
                    demoPath = name;
                }

                // --- MP3 (Защищенный)
                if (mp3File != null)
                {
                    var name = Guid.NewGuid() + Path.GetExtension(mp3File.FileName);
                    using var stream = new FileStream(Path.Combine(mp3Folder, name), FileMode.Create);
                    await mp3File.CopyToAsync(stream);
                    mp3Path = name;
                }

                // --- FULL (Защищенный)
                if (fullFile != null)
                {
                    var name = Guid.NewGuid() + Path.GetExtension(fullFile.FileName);
                    using var stream = new FileStream(Path.Combine(fullFolder, name), FileMode.Create);
                    await fullFile.CopyToAsync(stream);
                    fullPath = name;
                }

                // --- IMAGE (Публичный)
                if (imageFile != null)
                {
                    var name = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    using var stream = new FileStream(Path.Combine(imageFolder, name), FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    imagePath = "/uploads/images/" + name; // Путь для браузера
                }

                var beat = new Beat
                {
                    Title = Title,
                    Genre = Genre,
                    Bpm = Bpm,
                    Price = Price,
                    ProducerName = ProducerName,
                    CreatedAt = DateTime.Now,
                    DemoAudioPath = demoPath,
                    FullAudioPath = fullPath,
                    CoverImagePath = imagePath,
                    Mp3AudioPatch = mp3Path,
                    Tags = Tags
                };

                _context.Beats.Add(beat);
                await _context.SaveChangesAsync();

                // 🔥 СОЗДАЕМ ЛИЦЕНЗИИ
                var licenses = new List<License>
                {
                    new License { Name = "Basic", Price = BasicPrice, Description = "MP3 lease", BeatId = beat.Id },
                    new License { Name = "Premium", Price = PremiumPrice, Description = "WAV lease", BeatId = beat.Id },
                    new License { Name = "Exclusive", Price = ExclusivePrice, Description = "Exclusive rights", BeatId = beat.Id }
                };

                _context.Licenses.AddRange(licenses);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Бит успешно загружен" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // 🔥 3. ИСПРАВЛЕННЫЙ МЕТОД DELETE (Решен баг с путями)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var beat = _context.Beats.Find(id);

            if (beat != null)
            {
                var contentRoot = _env.ContentRootPath; // Корень проекта для Storage
                var webRoot = _env.WebRootPath;         // Корень сайта для wwwroot

                // Удаляем Demo
                if (!string.IsNullOrEmpty(beat.DemoAudioPath))
                {
                    var demoPath = Path.Combine(contentRoot, "Storage", "demo", Path.GetFileName(beat.DemoAudioPath));
                    if (System.IO.File.Exists(demoPath)) System.IO.File.Delete(demoPath);
                }

                // Удаляем MP3
                if (!string.IsNullOrEmpty(beat.Mp3AudioPatch))
                {
                    var mp3Path = Path.Combine(contentRoot, "Storage", "mp3", Path.GetFileName(beat.Mp3AudioPatch));
                    if (System.IO.File.Exists(mp3Path)) System.IO.File.Delete(mp3Path);
                }

                // Удаляем Full (WAV/ZIP)
                if (!string.IsNullOrEmpty(beat.FullAudioPath))
                {
                    var fullPath = Path.Combine(contentRoot, "Storage", "full", Path.GetFileName(beat.FullAudioPath));
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                }

                // Удаляем Обложку
                if (!string.IsNullOrEmpty(beat.CoverImagePath))
                {
                    var imagePath = Path.Combine(webRoot, "uploads", "images", Path.GetFileName(beat.CoverImagePath));
                    if (System.IO.File.Exists(imagePath)) System.IO.File.Delete(imagePath);
                }

                _context.Beats.Remove(beat);
                _context.SaveChanges();
            }

            return RedirectToAction("List");
        }

        public async Task<IActionResult> MakeAdmin()
        {
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByNameAsync("test2");

            if (user != null)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }

            return Content("test2 теперь Admin");
        }
        // 1. GET: Выводим страницу редактирования с заполненными данными
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var beat = await _context.Beats
                .Include(b => b.Licenses)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (beat == null) return NotFound();

            return View(beat);
        }

        // 2. POST: Сохраняем изменения (через AJAX)
        [HttpPost]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> EditBeatAjax(
            int Id, string Title, string Genre, string ProducerName, int Bpm,
            int BasicPrice, int PremiumPrice, int ExclusivePrice, decimal Price,
            string? Tags,
            IFormFile? demoFile, IFormFile? fullFile, IFormFile? mp3File, IFormFile? imageFile)
        {
            try
            {
                var beat = await _context.Beats.Include(b => b.Licenses).FirstOrDefaultAsync(x => x.Id == Id);
                if (beat == null) return NotFound();

                // Обновляем текстовые поля
                beat.Title = Title;
                beat.Genre = Genre;
                beat.ProducerName = ProducerName;
                beat.Bpm = Bpm;
                beat.Price = Price;
                beat.Tags = Tags;

                var root = _env.ContentRootPath;
                var webRoot = _env.WebRootPath;

                // 🔥 ЛОГИКА ОБНОВЛЕНИЯ ФАЙЛОВ: Если пришел новый файл -> удаляем старый -> сохраняем новый

                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(beat.CoverImagePath))
                    {
                        var oldPath = Path.Combine(webRoot, beat.CoverImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    var name = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    using var stream = new FileStream(Path.Combine(webRoot, "uploads/images", name), FileMode.Create);
                    await imageFile.CopyToAsync(stream);
                    beat.CoverImagePath = "/uploads/images/" + name;
                }

                if (demoFile != null)
                {
                    if (!string.IsNullOrEmpty(beat.DemoAudioPath))
                    {
                        var oldPath = Path.Combine(root, "Storage/demo", beat.DemoAudioPath);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }
                    var name = Guid.NewGuid() + Path.GetExtension(demoFile.FileName);
                    using var stream = new FileStream(Path.Combine(root, "Storage/demo", name), FileMode.Create);
                    await demoFile.CopyToAsync(stream);
                    beat.DemoAudioPath = name;
                }

                // ... (аналогично для mp3File и fullFile, если нужно обновлять)

                // 🔥 ОБНОВЛЯЕМ ЦЕНЫ ЛИЦЕНЗИЙ
                var basic = beat.Licenses.FirstOrDefault(l => l.Name == "Basic");
                if (basic != null) basic.Price = BasicPrice;

                var premium = beat.Licenses.FirstOrDefault(l => l.Name == "Premium");
                if (premium != null) premium.Price = PremiumPrice;

                var exclusive = beat.Licenses.FirstOrDefault(l => l.Name == "Exclusive");
                if (exclusive != null) exclusive.Price = ExclusivePrice;

                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}