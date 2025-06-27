using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("default", config =>
    {
            config.Window = TimeSpan.FromSeconds(10);
            config.PermitLimit = 10;
            config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            config.QueueLimit = 2;
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

var app = builder.Build();
app.UseRateLimiter();

var sampleTodos = new Todo[] {
    new(1, "Walk the dog"),
    new(2, "Do the dishes", DateOnly.FromDateTime(DateTime.Now)),
    new(3, "Do the laundry", DateOnly.FromDateTime(DateTime.Now.AddDays(1))),
    new(4, "Clean the bathroom"),
    new(5, "Clean the car", DateOnly.FromDateTime(DateTime.Now.AddDays(2)))
};

var home = app.MapGet("/", () => "Welcome to the Todo API!");
var homePage = app.MapGet("/index.html", () => Results.Content(File.ReadAllText("htmlpage.html"), "text/html"));

var todosApi = app.MapGroup("/todos");
todosApi.MapGet("/", () => sampleTodos);
todosApi.MapGet("/{id}", (int id) =>
    sampleTodos.FirstOrDefault(a => a.Id == id) is { } todo
        ? Results.Ok(todo)
        : Results.NotFound());

var input = todosApi.MapPost("/", (Todo todo) =>
{
    if (todo.Id == 0)
    {
        return Results.BadRequest("Id must be provided.");
    }
    if (string.IsNullOrWhiteSpace(todo.Title))
    {
        return Results.BadRequest("Title must be provided.");
    }
    sampleTodos.Append(todo);
    return Results.Created($"/todos/{todo.Id}", todo);
});

app.Run();

public record Todo(int Id, string? Title, DateOnly? DueBy = null, bool IsComplete = false);

[JsonSerializable(typeof(Todo[]))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{

}
