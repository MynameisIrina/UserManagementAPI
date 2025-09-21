using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddLogging();

var app = builder.Build();


app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TokenValidationMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapControllers();

app.Run();



// Simple middleware to log HTTP requests and responses
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Log the incoming request
        context.Request.EnableBuffering();
        var requestBody = "";
        if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        {
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
            {
                requestBody = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
            }
        }
        _logger.LogInformation($"Request: {context.Request.Method} {context.Request.Path} {requestBody}");

        // Log the outgoing response
        var originalBodyStream = context.Response.Body;
        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            await _next(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation($"Response: {context.Response.StatusCode} {text}");

            await responseBody.CopyToAsync(originalBodyStream);
        }
    }
}

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    // This method runs for every HTTP request
    public async Task Invoke(HttpContext context)
    {
        try
        {
            // Continue processing the request
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "An unhandled exception occurred.");

            // Set response status code and content type
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            // Write a simple JSON error response
            string errorJson = "{ \"error\": \"Internal server error.\" }";
            await context.Response.WriteAsync(errorJson);
        }
    }
}

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenValidationMiddleware> _logger;

    // Change this to your secure token or use configuration in a real app!
    private const string VALID_TOKEN = "your-valid-token";

    public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Check for Authorization header with Bearer token
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            // Missing or invalid header
            context.Response.StatusCode = 401; // Unauthorized
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{ \"error\": \"Unauthorized. Missing or invalid token.\" }");
            _logger.LogWarning("Unauthorized access attempt: missing or invalid Authorization header.");
            return;
        }

        // Extract the token from the header
        var token = authHeader.Substring("Bearer ".Length).Trim();

        if (token != VALID_TOKEN)
        {
            // Invalid token
            context.Response.StatusCode = 401; // Unauthorized
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{ \"error\": \"Unauthorized. Invalid token.\" }");
            _logger.LogWarning($"Unauthorized access attempt: invalid token '{token}'.");
            return;
        }

        // Token is valid, continue to the next middleware or controller
        await _next(context);
    }
}