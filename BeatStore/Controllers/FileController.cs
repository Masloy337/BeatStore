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

            var cleanFileName = Path.GetFileName(fileName); // 🔥 FIX

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

            var order = _context.Orders
                .FirstOrDefault(o => o.UserId == userId && o.BeatId == beatId);

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

            // 🔥 BASIC → только MP3
            if (license.Name == "Basic")
            {
                var mp3Path = Path.Combine(root, "Storage/mp3", beat.Mp3AudioPatch);

                if (!System.IO.File.Exists(mp3Path))
                    return NotFound();

                return PhysicalFile(
                    mp3Path,
                    "audio/mpeg",
                    beat.Title + ".mp3",
                    enableRangeProcessing: true
                );
            }

            // 🔥 PREMIUM / EXCLUSIVE → ZIP
            var wavPath = Path.Combine(root, "Storage/full", beat.FullAudioPath);
            var mp3PathZip = Path.Combine(root, "Storage/mp3", beat.Mp3AudioPatch);

            if (!System.IO.File.Exists(wavPath))
                return NotFound();

            // 🔥 создаём ZIP в памяти
            using (var memory = new MemoryStream())
            {
                using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, true))
                {
                    // WAV
                    var wavEntry = archive.CreateEntry(beat.Title + ".wav");
                    using (var entryStream = wavEntry.Open())
                    using (var fileStream = new FileStream(wavPath, FileMode.Open))
                    {
                        await fileStream.CopyToAsync(entryStream);
                    }

                    // MP3 (если есть)
                    if (!string.IsNullOrEmpty(beat.Mp3AudioPatch) && System.IO.File.Exists(mp3PathZip))
                    {
                        var mp3Entry = archive.CreateEntry(beat.Title + ".mp3");
                        using (var entryStream = mp3Entry.Open())
                        using (var fileStream = new FileStream(mp3PathZip, FileMode.Open))
                        {
                            await fileStream.CopyToAsync(entryStream);
                        }
                    }
                }

                memory.Position = 0;

                return File(memory.ToArray(), "application/zip", beat.Title + ".zip");
            }
        }
    }
}