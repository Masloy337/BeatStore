using Microsoft.AspNetCore.Mvc;

namespace BeatStore.Controllers
{
    public class ErrorsController : Controller
    {
        // Метод для обработки всех статус-кодов (404, 403 и т.д.)
        [Route("Errors/{statusCode}")]
        public IActionResult HandleErrorCode(int statusCode)
        {
            var viewName = statusCode switch
            {
                404 => "NotFound",
                500 => "ServerError",
                _ => "GenericError"
            };

            return View(viewName);
        }

        // Метод для критических исключений в коде
        [Route("Errors/500")]
        public IActionResult ServerError()
        {
            return View("ServerError");
        }
    }
}
