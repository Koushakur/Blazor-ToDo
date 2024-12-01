namespace Blazor_ToDo_App.Data;

public class User {
    public Guid Id { get; set; }
    public List<string> TodoList { get; set; } = [];
}

