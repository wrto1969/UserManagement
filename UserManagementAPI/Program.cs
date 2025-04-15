var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

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