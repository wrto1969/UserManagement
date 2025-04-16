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
/* blog mapping */





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