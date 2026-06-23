using TenderDocs.Domain.Common;

namespace TenderDocs.Domain.Entities;

public class Organization : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public bool DemoMode { get; set; } = true; // "Demo Mode" badge on the dashboard

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<StorageConnection> StorageConnections { get; set; } = new List<StorageConnection>();
}
