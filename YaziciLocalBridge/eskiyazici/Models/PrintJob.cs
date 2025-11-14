using System.Text.Json.Nodes;

namespace MenuBuPrinterAgent.Models;

internal sealed class PrintJob
{
    public int Id { get; init; }
    public string JobType { get; init; } = string.Empty;
    public JsonObject Payload { get; init; } = new();
    public DateTime CreatedAt { get; init; }
}

