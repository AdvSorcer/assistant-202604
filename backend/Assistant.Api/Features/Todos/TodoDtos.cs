using Assistant.Api.Models;

namespace Assistant.Api.Features.Todos;

public sealed record TodoRequest(string Title, string? Description, DateOnly? DueDate, TodoStatus Status);

public sealed record TodoResponse(
    int Id,
    string Title,
    string? Description,
    DateOnly? DueDate,
    TodoStatus Status,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class TodoMapping
{
    public static TodoResponse ToTodoResponse(this TodoItem todo) => new(
        todo.Id,
        todo.Title,
        todo.Description,
        todo.DueDate,
        todo.Status,
        todo.CompletedAt,
        todo.CreatedAt,
        todo.UpdatedAt);
}
