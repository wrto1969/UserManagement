var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

// In-memory list to store users
var users = new List<User>();

// Create (POST) - Add a new user
app.MapPost("/users", (User user) =>
{
    // Validate input
    if (string.IsNullOrWhiteSpace(user.UserName) || user.Age <= 0 || string.IsNullOrWhiteSpace(user.Email))
    {
        return Results.BadRequest("Please enter a correct age.");
    }
    // Validate email format
    var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    if (!System.Text.RegularExpressions.Regex.IsMatch(user.Email, emailRegex))
    {
        return Results.BadRequest("Please provide a valid email address.");
    }
    // Check for duplicate usernames
    if (users.Any(u => u.UserName == user.UserName))
    {
        return Results.Conflict($"A user with the username '{user.UserName}' already exists.");
    }

    users.Add(user);
    return Results.Created($"/users/{user.UserName}", user);
});

// Read (GET) - Get all users
app.MapGet("/users", () =>
{
    return Results.Ok(users);
});

// Read (GET) - Get a user by username
app.MapGet("/users/{username}", (string username) =>
{
    var user = users.FirstOrDefault(u => u.UserName == username);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});

// Update (PUT) - Update a user by username
app.MapPut("/users/{username}", (string username, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.UserName == username);
    if (user is null)
    {
        return Results.NotFound();
    }

    user.Age = updatedUser.Age;
    user.Email = updatedUser.Email; // Update email
    return Results.Ok(user);
});

// Delete (DELETE) - Remove a user by username
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

// Change from record to class to allow property mutation
class User
{
    public string UserName { get; set; }
    public int Age { get; set; }
    public string Email { get; set; } // New property
}
