using BeatStore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO.Compression;

namespace BeatStore.Controllers
{
    [Authorize] // Скачивать могут только залогиненные пользователи
    public class DownloadController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DownloadController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Beat(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.Orders
                .Include(o => o.Beat)
                .Include(o => o.License)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null || order.Beat == null || order.License == null) return Forbid();

            var beat = order.Beat;
            var root = _env.ContentRootPath;

            // --- ЛОГИКА ДЛЯ BASIC (ОТДАЕМ ПРОСТО MP3) ---
            if (order.License.Name == "Basic")
            {
                var mp3Path = Path.Combine(root, "Storage", "mp3", beat.Mp3AudioPatch ?? "");
                if (!System.IO.File.Exists(mp3Path)) return NotFound("MP3 файл не найден.");

                var fileBytes = await System.IO.File.ReadAllBytesAsync(mp3Path);
                return File(fileBytes, "audio/mpeg", $"{beat.Title}_Basic.mp3");
            }

            // --- ЛОГИКА ДЛЯ PREMIUM / EXCLUSIVE (СОЗДАЕМ ZIP) ---
            if (order.License.Name == "Premium" || order.License.Name == "Exclusive")
            {
                // Пути к обоим файлам
                var fullPath = Path.Combine(root, "Storage", "full", beat.FullAudioPath ?? "");
                var mp3Path = Path.Combine(root, "Storage", "mp3", beat.Mp3AudioPatch ?? "");

                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        // 1. Добавляем WAV (или ZIP Trackout) из папки full
                        if (System.IO.File.Exists(fullPath))
                        {
                            var wavEntry = archive.CreateEntry($"{beat.Title}_HighQuality{Path.GetExtension(beat.FullAudioPath)}");
                            using (var entryStream = wavEntry.Open())
                            using (var fileStream = new FileStream(fullPath, FileMode.Open))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }

                        // 2. Добавляем MP3 из папки mp3
                        if (System.IO.File.Exists(mp3Path))
                        {
                            var mp3Entry = archive.CreateEntry($"{beat.Title}_Preview.mp3");
                            using (var entryStream = mp3Entry.Open())
                            using (var fileStream = new FileStream(mp3Path, FileMode.Open))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                        }

                        // 3. Можно даже добавить текстовый файл с лицензией прямо в архив!
                        var licenseEntry = archive.CreateEntry("LICENSE_INFO.txt");
                        using (var writer = new StreamWriter(licenseEntry.Open()))
                        {
                            await writer.WriteLineAsync($"Beat: {beat.Title}");
                            await writer.WriteLineAsync($"License: {order.License.Name}");
                            await writer.WriteLineAsync($"Customer ID: {userId}");
                            await writer.WriteLineAsync($"Date: {DateTime.Now}");
                        }
                    }

                    // Отдаем готовый архив
                    var zipFileName = $"{beat.Title}_{order.License.Name}_Pack.zip";
                    return File(memoryStream.ToArray(), "application/zip", zipFileName);
                }
            }

            return BadRequest();
        }
    }
}
