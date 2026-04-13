using System.Net;
using System.Text.Json;

namespace Lunera.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // İsteği bir sonraki adıma ilet
                await _next(context);
            }
            catch (Exception ex)
            {
                // Hata olursa yakala ve logla
                _logger.LogError(ex, "Sistemde beklenmedik bir hata oluştu.");

                // Kullanıcıya düzgün bir JSON cevap dön
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Sunucu tarafında teknik bir aksaklık oluştu.",
                Detailed = exception.Message // Geliştirme ortamında hatayı görmek için
            };

            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}