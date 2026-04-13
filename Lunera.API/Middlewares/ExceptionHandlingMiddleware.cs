using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Lunera.Application.Exceptions;

namespace Lunera.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/problem+json";
            var problemDetails = new ProblemDetails { Instance = context.Request.Path };

            switch (exception)
            {
                case NotFoundException e:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    problemDetails.Title = "Not Found";
                    problemDetails.Status = StatusCodes.Status404NotFound;
                    problemDetails.Detail = e.Message;
                    break;

                case InvalidOperationException e:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    problemDetails.Title = "Business Rule Violation";
                    problemDetails.Status = StatusCodes.Status400BadRequest;
                    problemDetails.Detail = e.Message;
                    break;

                case DbUpdateConcurrencyException:
                    context.Response.StatusCode = StatusCodes.Status409Conflict;
                    problemDetails.Title = "Concurrency Conflict";
                    problemDetails.Status = StatusCodes.Status409Conflict;
                    problemDetails.Detail = "Bu işlem başka bir cihaz/sekme tarafından zaten gerçekleştirildi. Lütfen sayfayı yenileyin.";
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    problemDetails.Title = "Internal Server Error";
                    problemDetails.Status = StatusCodes.Status500InternalServerError;
                    problemDetails.Detail = "Sunucu tarafında beklenmeyen bir hata oluştu.";
                    break;
            }

            await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
        }
    }
}