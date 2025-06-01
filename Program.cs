using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<RequestLoggingService>();

var app = builder.Build();

// use https only in prod
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// add swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// middleware
app.UseErrorHandling();
app.UseTokenAuthentication();
app.UseRequestLogging();

var users = new List<User>();

// GET /users - Get all users
app.MapGet("/users", () =>
{
    try
    {
        return Results.Ok(users);
    }
    catch (Exception ex)
    {
        return HandleException(ex);
    }
})
.WithName("GetUsers")
.WithTags("Users");

// GET /users/{id} - Get user by ID
app.MapGet("/users/{id:int}", (int id) =>
{
    try
    {
        if (id <= 0)
        {
            return Results.BadRequest(new { error = "ID must be greater than 0" });
        }

        var user = users.FirstOrDefault(u => u.Id == id);
        return user is not null ? Results.Ok(user) : Results.NotFound(new { error = "User not found" });
    }
    catch (Exception ex)
    {
        return HandleException(ex);
    }
})
.WithName("GetUser")
.WithTags("Users");

// POST /users - Create new user
app.MapPost("/users", (CreateUserRequest request) =>
{
    try
    {
        // Manual validation first
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { error = "Email is required" });
        }

        // Validate using Data Annotations
        var validationResults = ValidateModel(request);
        if (validationResults.Any())
        {
            return Results.BadRequest(new { errors = validationResults });
        }

        if (!IsValidEmail(request.Email))
        {
            return Results.BadRequest(new { error = "Invalid email format" });
        }

        if (string.IsNullOrEmpty(request.Phone) )
        {
            return Results.BadRequest(new { error = "Invalid phone format" });
        }

        var emailExists = users.Any(u => u.Email == request.Email);
        if (emailExists)
        {
            return Results.BadRequest(new { error = "Email already exists" });
        }

        var newId = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;

        var user = new User
        {
            Id = newId,
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAt = DateTime.UtcNow
        };

        users.Add(user);

        return Results.Created($"/users/{user.Id}", user);
    }
    catch (Exception ex)
    {
        return HandleException(ex);
    }
})
.WithName("CreateUser")
.WithTags("Users");

// PUT /users/{id} - Update user
app.MapPut("/users/{id:int}", (int id, UpdateUserRequest request) =>
{
    try
    {
        if (id <= 0)
        {
            return Results.BadRequest(new { error = "ID must be greater than 0" });
        }

        // Manual validation first
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Name is required" });
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return Results.BadRequest(new { error = "Email is required" });
        }

        // Validate using Data Annotations
        var validationResults = ValidateModel(request);
        if (validationResults.Any())
        {
            return Results.BadRequest(new { errors = validationResults });
        }

        var user = users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        var emailExists = users.Any(u => u.Email == request.Email && u.Id != id);
        if (emailExists)
        {
            return Results.BadRequest(new { error = "Email already exists" });
        }

        user.Name = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        return Results.Ok(user);
    }
    catch (Exception ex)
    {
        return HandleException(ex);
    }
})
.WithName("UpdateUser")
.WithTags("Users");

// DELETE /users/{id} - Delete user
app.MapDelete("/users/{id:int}", (int id) =>
{
    try
    {
        if (id <= 0)
        {
            return Results.BadRequest(new { error = "ID must be greater than 0" });
        }

        var user = users.FirstOrDefault(u => u.Id == id);
        if (user is null)
        {
            return Results.NotFound(new { error = "User not found" });
        }

        users.Remove(user);

        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return HandleException(ex);
    }
})
.WithName("DeleteUser")
.WithTags("Users");

// get all logs
app.MapGet("/logs", (RequestLoggingService loggingService) =>
{
    return loggingService.GetLogs();
});

app.Run();

// Exception handling helper method
static IResult HandleException(Exception ex)
{
    // Log the exception here (you can use ILogger)
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");

    return ex switch
    {
        ArgumentException => Results.BadRequest(new { error = "Invalid argument provided", details = ex.Message }),
        InvalidOperationException => Results.BadRequest(new { error = "Invalid operation", details = ex.Message }),
        UnauthorizedAccessException => Results.Unauthorized(),
        NotImplementedException => Results.StatusCode(501), // Not Implemented
        _ => Results.StatusCode(500) // Internal Server Error
    };
}

// Validation helper method
static List<string> ValidateModel<T>(T model)
{
    var context = new ValidationContext(model!);
    var results = new List<ValidationResult>();
    var errors = new List<string>();

    bool isValid = Validator.TryValidateObject(model!, context, results, validateAllProperties: true);

    if (!isValid)
    {
        errors.AddRange(results.Select(r => r.ErrorMessage ?? "Validation error"));
    }

    return errors;
}

// Email validation helper
static bool IsValidEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return false;

    try
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch
    {
        return false;
    }
}

// Models
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public record CreateUserRequest(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    string Name,

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    string Email,

    [Phone(ErrorMessage = "Invalid phone format")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    string? Phone
);

public record UpdateUserRequest(
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    string Name,

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    string Email,

    [Phone(ErrorMessage = "Invalid phone format")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    string? Phone
);

// log
public class RequestLog
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RequestLoggingService
{
    private readonly ConcurrentQueue<RequestLog> _logs = new();

    public void AddLog(string method, string path, int statusCode)
    {
        _logs.Enqueue(new RequestLog
        {
            Method = method,
            Path = path,
            StatusCode = statusCode,
            Timestamp = DateTime.UtcNow
        });
    }

    public IEnumerable<RequestLog> GetLogs()
    {
        return _logs.ToArray();
    }
}

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestLoggingService _loggingService;

    public RequestLoggingMiddleware(RequestDelegate next, RequestLoggingService loggingService)
    {
        _next = next;
        _loggingService = loggingService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;

        await _next(context);

        var statusCode = context.Response.StatusCode;

        _loggingService.AddLog(method, path, statusCode);
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}

// error handling
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            message = "An error occurred while processing your request.",
            statusCode = context.Response.StatusCode
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}

// authentication
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = ExtractToken(context);

        if (string.IsNullOrEmpty(token) || !IsValidToken(token))
        {
            await HandleUnauthorizedAsync(context);
            return;
        }

        await _next(context);
    }

    private static string? ExtractToken(HttpContext context)
    {
        return context.Request.Headers["x-auth"].FirstOrDefault();
    }

    private static bool IsValidToken(string token)
    {
        return token == "auth_token";
    }

    private static async Task HandleUnauthorizedAsync(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var response = new
        {
            message = "Unauthorized",
            statusCode = 401
        };

        var jsonResponse = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(jsonResponse);
    }
}

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseTokenAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}