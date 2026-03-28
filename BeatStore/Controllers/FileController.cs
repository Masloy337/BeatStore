using BeatStore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IO.Compression;

namespace BeatStore.Controllers
{
    public class FileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🎧 DEMO
        public IActionResult Demo(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return NotFound();

            var cleanFileName = Path.GetFileName(fileName); // 🔥 FIX - защита от directory traversal

            var path = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Storage",
                "demo",
                cleanFileName
            );

            if (!System.IO.File.Exists(path))
                return NotFound();

            return PhysicalFile(path, "audio/mpeg", enableRangeProcessing: true);
        }

        // 🔒 FULL
        public async Task<IActionResult> Full(int beatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            // 🔥 ИСПРАВЛЕНИЕ 1: Берем самую ПОСЛЕДНЮЮ (свежую) покупку пользователя!
            var order = await _context.Orders
                .Where(o => o.UserId == userId && o.BeatId == beatId)
                .OrderByDescending(o => o.CreatedAt) // Сортируем по дате создания
                .FirstOrDefaultAsync();

            if (order == null)
                return Forbid();

            var beat = await _context.Beats.FindAsync(beatId);
            if (beat == null)
                return NotFound();

            var license = await _context.Licenses
                .FirstOrDefaultAsync(l => l.Id == order.LicenseId);

            if (license == null)
                return Forbid();

            var root = Directory.GetCurrentDirectory();

            // 🔥 ИСПРАВЛЕНИЕ 2: Защита от пробелов в БД (сверяем строго "Basic")
            if (license.Name != null && license.Name.Trim().Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                // Защита от пустых значений в БД
                if (string.IsNullOrEmpty(beat.Mp3AudioPatch))
                    return NotFound("MP3 файл для этого бита не загружен.");
                // ... дальше твой старый код (PhysicalFile и ZIP)

                var mp3Path = Path.Combine(root, "Storage/mp3", beat.Mp3AudioPatch);

                if (!System.IO.File.Exists(mp3Path))
                    return NotFound("Файл не найден на сервере.");

                return PhysicalFile(
                    mp3Path,
                    "audio/mpeg",
                    beat.Title + ".mp3",
                    enableRangeProcessing: true
                );
            }

            // 🔥 PREMIUM / EXCLUSIVE → ZIP (WAV + MP3)

            // УБРАЛИ блок using() для MemoryStream! 
            // ASP.NET сам закроет поток после того, как отдаст файл пользователю.
            var memory = new MemoryStream();

            using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
            {
                // WAV (Проверяем, что путь не пустой перед тем, как клеить его)
                if (!string.IsNullOrEmpty(beat.FullAudioPath))
                {
                    var wavPath = Path.Combine(root, "Storage/full", beat.FullAudioPath);
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

                // MP3 (Проверяем, что путь не пустой)
                if (!string.IsNullOrEmpty(beat.Mp3AudioPatch))
                {
                    var mp3PathZip = Path.Combine(root, "Storage/mp3", beat.Mp3AudioPatch);
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

            memory.Position = 0;

            // Возвращаем сам поток ASP.NET
            return File(memory, "application/zip", beat.Title + ".zip");
        }
    }
}