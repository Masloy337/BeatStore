using BeatStore.Data;
using BeatStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeatStore.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public IActionResult Stats()
        {
            var beats = _context.Beats
                .Include(b => b.Orders)
                .ToList();

            var model = new AdminStatsViewModel();

            model.TotalSales = beats.Sum(b => b.Orders?.Count ?? 0);

            model.TotalRevenue = beats.Sum(b =>
                (b.Orders?.Count ?? 0) * b.Price
            );

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

            // 🔥 ГРАФИК
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
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Create()
        {

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            string Title,
            string Genre,
            string ProducerName,
            int Bpm,
            int BasicPrice,
            int PremiumPrice,
            int ExclusivePrice,
            decimal Price,
            IFormFile? demoFile,
            IFormFile? fullFile,
            IFormFile? mp3File,
            IFormFile? imageFile)

        {
            // 🔥 НОВОЕ: убрали wwwroot для аудио
            var root = Directory.GetCurrentDirectory();

            var demoFolder = Path.Combine(root, "Storage/demo");
            var fullFolder = Path.Combine(root, "Storage/full");

            // ❗ изображения оставляем в wwwroot (чтобы отображались)
            var imageFolder = Path.Combine(root, "wwwroot/uploads/images");

            Directory.CreateDirectory(demoFolder);
            Directory.CreateDirectory(fullFolder);
            Directory.CreateDirectory(imageFolder);

            string? demoPath = null;
            string? fullPath = null;
            string? imagePath = null;

            // --- DEMO (теперь защищённый)
            if (demoFile != null)
            {
                var name = Guid.NewGuid() + Path.GetExtension(demoFile.FileName);
                var path = Path.Combine(demoFolder, name);

                using var stream = new FileStream(path, FileMode.Create);
                await demoFile.CopyToAsync(stream);

                // 🔥 сохраняем только имя
                demoPath = name;
            }

            string? mp3Path = null;

            if (mp3File != null)
            {
                var name = Guid.NewGuid() + Path.GetExtension(mp3File.FileName);
                var path = Path.Combine(root, "Storage/mp3", name);

                Directory.CreateDirectory(Path.Combine(root, "Storage/mp3"));

                using var stream = new FileStream(path, FileMode.Create);
                await mp3File.CopyToAsync(stream);

                mp3Path = name;
            }

            // --- FULL (теперь защищённый)
            if (fullFile != null)
            {
                var name = Guid.NewGuid() + Path.GetExtension(fullFile.FileName);
                var path = Path.Combine(fullFolder, name);

                using var stream = new FileStream(path, FileMode.Create);
                await fullFile.CopyToAsync(stream);

                // 🔥 сохраняем только имя
                fullPath = name;
            }

            // --- IMAGE (оставляем как было)
            if (imageFile != null)
            {
                var name = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                var path = Path.Combine(imageFolder, name);

                using var stream = new FileStream(path, FileMode.Create);
                await imageFile.CopyToAsync(stream);

                imagePath = "/uploads/images/" + name;
            }

            var beat = new Beat
            {
                Title = Title,
                Genre = Genre,
                Bpm = Bpm,
                Price = Price,
                ProducerName = ProducerName,
                CreatedAt = DateTime.Now,
                DemoAudioPath = demoPath,   // 🔥 теперь имя файла
                FullAudioPath = fullPath,   // 🔥 теперь имя файла
                CoverImagePath = imagePath

            };

            _context.Beats.Add(beat);
            await _context.SaveChangesAsync();

            // 🔥 СОЗДАЕМ ЛИЦЕНЗИИ
            var licenses = new List<License>
{
            new License
            {
            Name = "Basic",
            Price = BasicPrice,
            Description = "MP3 lease (basic license)",
            BeatId = beat.Id
            },
            new License
            {
            Name = "Premium",
            Price = PremiumPrice,
            Description = "WAV lease (premium license)",
            BeatId = beat.Id
            },
            new License
            {
            Name = "Exclusive",
            Price = ExclusivePrice,
            Description = "Exclusive rights (beat removed after purchase)",
            BeatId = beat.Id
            }
            };

            _context.Licenses.AddRange(licenses);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Beats");
        }

        public IActionResult List()
        {
            var beats = _context.Beats.OrderByDescending(x => x.CreatedAt).ToList();
            return View(beats);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var beat = _context.Beats.Find(id);

            if (beat != null)
            {
                // 🔥 УДАЛЯЕМ ФАЙЛЫ С ДИСКА

                var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                if (!string.IsNullOrEmpty(beat.DemoAudioPath))
                {
                    var demoPath = Path.Combine(root, "Storage/demo", Path.GetFileName(beat.DemoAudioPath));
                    if (System.IO.File.Exists(demoPath))
                        System.IO.File.Delete(demoPath);
                }

                if (!string.IsNullOrEmpty(beat.FullAudioPath))
                {
                    var fullPath = Path.Combine(root, "Storage/full", Path.GetFileName(beat.FullAudioPath));
                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }

                if (!string.IsNullOrEmpty(beat.CoverImagePath))
                {
                    var imagePath = Path.Combine(root, "uploads/images", Path.GetFileName(beat.CoverImagePath));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                // 🔥 УДАЛЯЕМ ИЗ БД
                _context.Beats.Remove(beat);
                _context.SaveChanges();
            }

            return RedirectToAction("List");
        }

        // ВРЕМЕННО — назначить админа
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
    }
}