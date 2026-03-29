using BeatStore.Models;

namespace BeatStore.ViewModels
{
    // Эта модель будет содержать ВСЁ, что нужно странице каталога
    public class BeatsIndexViewModel
    {
        public List<Beat> Beats { get; set; } = new();      // Основной список битов
        public List<Beat> TopBeats { get; set; } = new();   // Топ 4 бита
        public List<string> Genres { get; set; } = new();   // Список жанров для кнопок

        public string? CurrentSearch { get; set; }          // Что сейчас ищем
        public string? CurrentGenre { get; set; }           // Какой жанр сейчас выбран
    }
}