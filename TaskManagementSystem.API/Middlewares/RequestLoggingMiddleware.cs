using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TaskManagementSystem.API.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestTime = DateTime.UtcNow;
            var requestId = Guid.NewGuid().ToString();

            // Log the request information
            _logger.LogInformation(
                "Request {RequestId} {RequestMethod} {RequestPath} started at {RequestTime}",
                requestId, context.Request.Method, context.Request.Path, requestTime);

            // Store the original body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                // Create a new memory stream
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Continue down the middleware pipeline
                await _next(context);

                // Log the response information
                _logger.LogInformation(
                    "Request {RequestId} {RequestMethod} {RequestPath} completed with status code {StatusCode} in {ElapsedMilliseconds}ms",
                    requestId, context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);

                // Copy the response body to the original stream
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, 
                    "Request {RequestId} {RequestMethod} {RequestPath} failed with exception after {ElapsedMilliseconds}ms",
                    requestId, context.Request.Method, context.Request.Path, stopwatch.ElapsedMilliseconds);
                throw;
            }
            finally
            {
                // Always restore the original body stream
                context.Response.Body = originalBodyStream;
            }
        }
    }
}