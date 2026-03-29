using Resend;

namespace BeatStore.Services
{
    public interface IEmailService
    {
        Task SendOrderReceiptAsync(string userEmail, string trackTitle, string licenseName, int orderId);
    }

    public class EmailService : IEmailService
    {
        private readonly IResend _resend;

        public EmailService(IResend resend)
        {
            _resend = resend;
        }

        public async Task SendOrderReceiptAsync(string userEmail, string trackTitle, string licenseName, int orderId)
        {
            // Красивый HTML-шаблон для письма
            string htmlContent = $@"
                <div style='font-family: Arial, sans-serif; background-color: #0a0a0a; color: #fff; padding: 40px; border-radius: 10px; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #1db954; text-align: center;'>Спасибо за покупку! 🎧</h2>
                    <p style='font-size: 16px; color: #ccc;'>Твой заказ успешно оформлен.</p>
                    
                    <div style='background-color: #141414; padding: 20px; border-radius: 8px; border: 1px solid #333; margin: 20px 0;'>
                        <h3 style='margin-top: 0; color: #fff;'>Детали заказа:</h3>
                        <p style='margin: 5px 0; color: #aaa;'><strong>Бит:</strong> {trackTitle}</p>
                        <p style='margin: 5px 0; color: #aaa;'><strong>Лицензия:</strong> {licenseName}</p>
                    </div>

                    <p style='text-align: center; margin-top: 30px;'>
                        <a href='https://твоя_ссылка_ngrok.ngrok-free.app/Download/Beat?orderId={orderId}' 
                           style='background-color: #1db954; color: #000; padding: 15px 30px; text-decoration: none; border-radius: 25px; font-weight: bold; font-size: 16px;'>
                           📥 Скачать файлы
                        </a>
                    </p>
                    <p style='text-align: center; color: #666; font-size: 12px; margin-top: 30px;'>
                        С уважением, BeatStore Team
                    </p>
                </div>";

            var message = new EmailMessage
            {
                From = "BeatStore <onboarding@resend.dev>", // Обязательно так для тестов без домена!
                To = { userEmail }, // ⚠️ Во время тестов тут должен быть email твоего аккаунта Resend
                Subject = $"Твой бит {trackTitle} ({licenseName}) готов к скачиванию 🔥",
                HtmlBody = htmlContent
            };

            // Отправляем письмо!
            await _resend.EmailSendAsync(message);
        }
    }
}