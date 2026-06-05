namespace Dashboards_reports.CollectionTracker.Domain;

public sealed class TaskItem
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "medium";
    public string? AssignedTo { get; set; }
    public bool IsDone { get; set; }
    public DateTime? DoneAt { get; set; }
    public DateTime AddedAt { get; set; }
}
