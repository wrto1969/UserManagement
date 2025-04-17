var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Add the middleware to the pipeline
app.UseMiddleware<RequestResponseLoggingMiddleware>();
app.UseMiddleware<RequestLoggingAndExceptionHandlingMiddleware>();
app.UseMiddleware<TokenValidationMiddleware>();
app.UseHttpsRedirection();

var users = new List<User>();
/*
  This section sets up a route for when someone sends data to "/users".
  It's specifically designed to add a new user.
*/
app.MapPost("/users", (User user) =>
{    // Check if the new user's information is valid (e.g., the username isn't already taken).
    var validationResult = ValidateUser(user, users);
    // If there's a problem with the user's information, stop here and return a message explaining the issue.
    if (validationResult is not null)
    {
        return validationResult;
    }    
    // Add the new user to our list of users.
    users.Add(user);
    /*
        Send a response saying the user was successfully created.
        Also provide a link to where details about the new user can be found.
    */
    return Results.Created($"/users/{user.UserName}", user);
});

/*  blogs section */
var blogs = new List<Blog>
{
    new Blog { Title = "First Blog", Content = "This is the content of the first blog." },
    new Blog { Title = "Second Blog", Content = "This is the content of the second blog." }
};

app.MapGet("/", () => "Welcome to the User Management API!");

/*  MAPPING */
app.MapGet("/blogs", () => Results.Ok(blogs));
// This route retrieves a blog post by its title.
// It checks if a blog with the given title exists and returns it if found, or a 404 Not Found response if not.
app.MapGet("/blogs/{title}", (string title) =>
{
    var blog = blogs.FirstOrDefault(b => b.Title == title);
    return blog is not null ? Results.Ok(blog) : Results.NotFound();
});

app.MapPost("/blogs", (Blog blog) =>
{
    if (string.IsNullOrWhiteSpace(blog.Title) || string.IsNullOrWhiteSpace(blog.Content))
    {
        return Results.BadRequest("Invalid blog data. Ensure Title and Content are provided.");
    }

    if (blogs.Any(b => b.Title == blog.Title))
    {
        return Results.Conflict($"A blog with the title '{blog.Title}' already exists.");
    }

    blogs.Add(blog);
    return Results.Created($"/blogs/{blog.Title}", blog);
});
app.MapPut("/blogs/{title}", (string title, Blog updatedBlog) =>
{
    var blog = blogs.FirstOrDefault(b => b.Title == title);
    if (blog is null)
    {
        return Results.NotFound();
    }

    blog.Content = updatedBlog.Content;
    return Results.Ok(blog);
});
app.MapDelete("/blogs/{title}", (string title) =>
{
    var blog = blogs.FirstOrDefault(b => b.Title == title);
    if (blog is null)
    {
        return Results.NotFound();
    }

    blogs.Remove(blog);
    return Results.NoContent();
});
/* USERS */
app.MapGet("/users", () => Results.Ok(users));

app.MapGet("/users/{username}", (string username) =>
{
    var user = users.FirstOrDefault(u => u.UserName == username);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

app.MapPut("/users/{username}", (string username, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.UserName == username);
    if (user is null)
    {
        return Results.NotFound();
    }

    var validationResult = ValidateUser(updatedUser, users.Where(u => u.UserName != username).ToList());
    if (validationResult is not null)
    {
        return validationResult;
    }

    user.Age = updatedUser.Age;
    user.Email = updatedUser.Email;
    return Results.Ok(user);
});

app.MapDelete("/users/{username}", (string username) =>
{
    var user = users.FirstOrDefault(u => u.UserName == username);
    if (user is null)
    {
        return Results.NotFound();
    }

    users.Remove(user);
    return Results.NoContent();
});

app.Run();

static IResult? ValidateUser(User user, List<User> users)
{
    if (string.IsNullOrWhiteSpace(user.UserName) || user.Age <= 0 || string.IsNullOrWhiteSpace(user.Email))
    {
        return Results.BadRequest("Invalid user data. Ensure UserName, Age, and Email are provided and valid.");
    }

    var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    if (!System.Text.RegularExpressions.Regex.IsMatch(user.Email, emailRegex))
    {
        return Results.BadRequest("Please provide a valid email address.");
    }

    if (users.Any(u => u.UserName == user.UserName))
    {
        return Results.Conflict($"A user with the username '{user.UserName}' already exists.");
    }

    if (users.Any(u => u.Email == user.Email))
    {
        return Results.Conflict($"A user with the email '{user.Email}' already exists.");
    }

    return null;
} 
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Log the HTTP Request
        _logger.LogInformation("HTTP Request: {method} {url} {headers}",
            context.Request.Method,
            context.Request.Path,
            context.Request.Headers);

        // Copy the original response body stream
        var originalResponseBodyStream = context.Response.Body;

        using (var responseBody = new MemoryStream())
        {
            context.Response.Body = responseBody;

            // Call the next middleware in the pipeline
            await _next(context);

            // Log the HTTP Response
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            _logger.LogInformation("HTTP Response: {statusCode} {body}",
                context.Response.StatusCode,
                responseText);

            // Copy the response back to the original stream
            await responseBody.CopyToAsync(originalResponseBodyStream);
        }
    }
}

public class RequestLoggingAndExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingAndExceptionHandlingMiddleware> _logger;

    public RequestLoggingAndExceptionHandlingMiddleware(RequestDelegate next, ILogger<RequestLoggingAndExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Log the HTTP Request
            _logger.LogInformation("HTTP Request: {method} {url} {headers}",
                context.Request.Method,
                context.Request.Path,
                context.Request.Headers);

            // Call the next middleware in the pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception
            _logger.LogError(ex, "An unhandled exception occurred while processing the request.");

            // Return a consistent error response
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var errorResponse = new
        {
            StatusCode = context.Response.StatusCode,
            Message = "An unexpected error occurred. Please try again later.",
            Details = exception.Message // You can remove this in production for security reasons
        };

        return context.Response.WriteAsJsonAsync(errorResponse);
    }
}

public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenValidationMiddleware> _logger;

    public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the Authorization header is present
        if (!context.Request.Headers.TryGetValue("Authorization", out var token))
        {
            _logger.LogWarning("Authorization header is missing.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Authorization header is missing."
            });
            return;
        }

        // Validate the token (replace this with your actual token validation logic)
        if (!IsValidToken(token))
        {
            _logger.LogWarning("Invalid token: {Token}", token);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new
            {
                StatusCode = StatusCodes.Status401Unauthorized,
                Message = "Invalid token."
            });
            return;
        }

        // Token is valid, proceed to the next middleware
        await _next(context);
    }

    private bool IsValidToken(string token)
    {
        // Replace this with your actual token validation logic
        // For example, check against a database, validate a JWT, etc.
        return token == "valid-token"; // Example: Replace "valid-token" with your logic
    }
}

class User
{
    public string UserName { get; set; }
    public int Age { get; set; }
    public string Email { get; set; }

    public override string ToString()
    {
        return $"UserName: {UserName}, Age: {Age}, Email: {Email}";
    }
}
class Blog
{
    public string Title { get; set; }
    public string Content { get; set; }

    public override string ToString()
    {
        return $"Title: {Title}, Content: {Content}";
    }
}