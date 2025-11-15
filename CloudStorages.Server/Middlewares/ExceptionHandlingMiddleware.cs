using System.Net;
using System.Text.Json;
using Amazon.S3;
using Microsoft.AspNetCore.Mvc;

namespace CloudStorages.Server.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            var problem = CreateProblemDetails(context, ex);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
        }

        private static ProblemDetails CreateProblemDetails(HttpContext context, Exception ex)
        {
            var status = (int)HttpStatusCode.InternalServerError;
            var title = "An unexpected error occurred";
            var type = "https://httpstatuses.com/500";

            switch (ex)
            {

                case KeyNotFoundException:
                    status = (int)HttpStatusCode.NotFound;
                    title = "Resource not found";
                    type = "https://httpstatuses.com/404";
                    break;

                case ArgumentException:
                    status = (int)HttpStatusCode.BadRequest;
                    title = "Invalid request";
                    type = "https://httpstatuses.com/400";
                    break;

                case AmazonS3Exception s3Ex:
                    status = (int)s3Ex.StatusCode;
                    title = "AWS S3 error";
                    type = $"https://httpstatuses.com/{(int)s3Ex.StatusCode}";
                    break;
            }

            return new ProblemDetails
            {
                Type = type,
                Title = title,
                Status = status,
                Detail = ex.Message,
                Instance = context.Request.Path
            };
        }
    }
}
