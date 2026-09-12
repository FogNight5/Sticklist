namespace Sticklist.Models;

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool LimitToOncePerDay { get; set; }
    public List<Entry> Entries { get; set; } = new();
}
