namespace RepalogicConcurrencyAssignment.Models;

public class Message
{
    public int OrderId { get; set; }
    public string Event { get; set; } = string.Empty;
}