using System.Net;
using System.Text.Json;

namespace TheSocialMediaV2.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // 1. İsteği bir sonraki adıma (Controller'a) gönder
                await _next(context);
            }
            catch (Exception ex)
            {
                // 2. Eğer bir hata patlarsa (Exception), onu burada yakala
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Cevap tipini JSON olarak ayarla
            context.Response.ContentType = "application/json";

            // Durum kodunu 500 (Sunucu Hatası) olarak ayarla
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // Hata mesajını hazırla
            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Sunucu tarafında beklenmeyen bir hata oluştu. (Global Handler)",
                Detailed = exception.Message // Geliştirme aşamasında hatayı görmek için ekledik. Canlıya çıkarken bunu sileceğiz.
            };

            // JSON'a çevirip gönder
            var jsonResponse = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(jsonResponse);
        }
    }
}