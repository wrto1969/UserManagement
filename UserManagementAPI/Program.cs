var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

// In-memory list to store users
var users = new List<User>();

// Create (POST) - Add a new user
app.MapPost("/users", (User user) =>
{
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

    user.Age = updatedUser.Age; // This works now because User is a class
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
}
