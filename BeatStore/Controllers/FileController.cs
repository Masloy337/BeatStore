using BeatStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO.Compression;
using Microsoft.AspNetCore.Authorization; // Добавлено для авторизации
using Microsoft.AspNetCore.Hosting; // Добавлено для путей

namespace BeatStore.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Внедряем базу данных и окружение сервера
        public FileController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // 🎧 DEMO
        [HttpGet]
        public IActionResult Demo(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            var cleanFileName = Path.GetFileName(fileName); // Защита пути

            // Используем _env.ContentRootPath вместо GetCurrentDirectory()
            var path = Path.Combine(_env.ContentRootPath, "Storage", "demo", cleanFileName);

            if (!System.IO.File.Exists(path))
                return NotFound();

            return PhysicalFile(path, "audio/mpeg", enableRangeProcessing: true);
        }

        // 🔒 FULL
        [HttpGet]
        [Authorize] // 🔥 Автоматическая защита от неавторизованных
        public async Task<IActionResult> Full(int beatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔥 ОПТИМИЗАЦИЯ: Достаем Заказ, Бит и Лицензию за ОДИН запрос к БД
            var order = await _context.Orders
                .Include(o => o.Beat)
                .Include(o => o.License)
                .Where(o => o.UserId == userId && o.BeatId == beatId)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (order == null || order.Beat == null || order.License == null)
                return Forbid();

            var beat = order.Beat;
            var license = order.License;
            var root = _env.ContentRootPath;

            // 🔥 BASIC LICENSE -> Только MP3
            if (license.Name != null && license.Name.Trim().Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(beat.Mp3AudioPatch))
                    return NotFound("MP3 файл для этого бита не загружен.");

                var safeMp3Name = Path.GetFileName(beat.Mp3AudioPatch); // Двойная защита
                var mp3Path = Path.Combine(root, "Storage", "mp3", safeMp3Name);

                if (!System.IO.File.Exists(mp3Path))
                    return NotFound("Файл не найден на сервере.");

                return PhysicalFile(mp3Path, "audio/mpeg", beat.Title + ".mp3", enableRangeProcessing: true);
            }

            // 🔥 PREMIUM / EXCLUSIVE -> ZIP Архив (WAV + MP3)
            var memory = new MemoryStream();

            // leaveOpen: true - супер важный параметр, чтобы поток не закрылся раньше времени
            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
            {
                // Упаковываем WAV
                if (!string.IsNullOrEmpty(beat.FullAudioPath))
                {
                    var safeWavName = Path.GetFileName(beat.FullAudioPath);
                    var wavPath = Path.Combine(root, "Storage", "full", safeWavName);

                    if (System.IO.File.Exists(wavPath))
                    {
                        var wavEntry = archive.CreateEntry(beat.Title + ".wav", CompressionLevel.Fastest);
                        using (var entryStream = wavEntry.Open())
                        using (var fileStream = new FileStream(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            await fileStream.CopyToAsync(entryStream);
                        }
                    }
                }

                // Упаковываем MP3
                if (!string.IsNullOrEmpty(beat.Mp3AudioPatch))
                {
                    var safeMp3Name = Path.GetFileName(beat.Mp3AudioPatch);
                    var mp3PathZip = Path.Combine(root, "Storage", "mp3", safeMp3Name);

                    if (System.IO.File.Exists(mp3PathZip))
                    {
                        var mp3Entry = archive.CreateEntry(beat.Title + ".mp3", CompressionLevel.Fastest);
                        using (var entryStream = mp3Entry.Open())
                        using (var fileStream = new FileStream(mp3PathZip, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            await fileStream.CopyToAsync(entryStream);
                        }
                    }
                }
            }

            // Сбрасываем позицию потока на начало перед отправкой
            memory.Position = 0;

            return File(memory, "application/zip", beat.Title + ".zip");
        }
    }
}