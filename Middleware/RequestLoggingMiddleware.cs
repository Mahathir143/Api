using System.Text;

namespace Api.Middleware
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
            var startTime = DateTime.UtcNow;
            var request = context.Request;

            // Log request
            var requestBody = await GetRequestBodyAsync(request);
            _logger.LogInformation("Request: {Method} {Path} from {IP} - Body: {Body}",
                request.Method,
                request.Path,
                GetClientIP(context),
                requestBody);

            await _next(context);

            // Log response
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Response: {StatusCode} in {Duration}ms",
                context.Response.StatusCode,
                duration.TotalMilliseconds);
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            if (request.ContentLength == null || request.ContentLength == 0)
                return string.Empty;

            request.EnableBuffering();
            request.Body.Position = 0;

            using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            // Don't log sensitive information
            if (request.Path.StartsWithSegments("/api/auth"))
            {
                return "[SENSITIVE_DATA_HIDDEN]";
            }

            return body;
        }

        private string GetClientIP(HttpContext context)
        {
            if (context.Request.Headers.ContainsKey("X-Forwarded-For"))
                return context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();

            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
}
