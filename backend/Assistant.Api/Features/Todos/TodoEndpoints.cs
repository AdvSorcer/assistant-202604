using Assistant.Api.Infrastructure;
using Assistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Assistant.Api.Features.Todos;

public static class TodoEndpoints
{
    public static RouteGroupBuilder MapTodoEndpoints(this RouteGroupBuilder api)
    {
        var todoApi = api.MapGroup("/todos");

        todoApi.MapGet("/", async (AssistantDbContext db) =>
            await db.TodoItems
                .AsNoTracking()
                .OrderBy(todo => todo.Status)
                .ThenBy(todo => todo.DueDate)
                .Select(todo => todo.ToTodoResponse())
                .ToListAsync());

        todoApi.MapPost("/", async (TodoRequest request, AssistantDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { message = "請輸入代辦事項" });
            }

            var todo = new TodoItem
            {
                Title = request.Title.Trim(),
                Description = request.Description,
                DueDate = request.DueDate,
                Status = request.Status
            };
            db.TodoItems.Add(todo);
            await db.SaveChangesAsync();
            return Results.Created($"/api/todos/{todo.Id}", todo.ToTodoResponse());
        });

        todoApi.MapPut("/{id:int}", async (int id, TodoRequest request, AssistantDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                return Results.BadRequest(new { message = "請輸入代辦事項" });
            }

            var todo = await db.TodoItems.FindAsync(id);
            if (todo is null)
            {
                return Results.NotFound();
            }

            todo.Title = request.Title.Trim();
            todo.Description = request.Description;
            todo.DueDate = request.DueDate;
            todo.Status = request.Status;
            todo.CompletedAt = request.Status == TodoStatus.Done ? DateTimeOffset.UtcNow : null;
            todo.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            return Results.Ok(todo.ToTodoResponse());
        });

        todoApi.MapDelete("/{id:int}", async (int id, AssistantDbContext db) =>
        {
            var deleted = await db.TodoItems.Where(todo => todo.Id == id).ExecuteDeleteAsync();
            return deleted == 0 ? Results.NotFound() : Results.NoContent();
        });

        return api;
    }
}
