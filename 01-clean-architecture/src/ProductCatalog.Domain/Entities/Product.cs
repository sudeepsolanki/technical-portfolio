using ProductCatalog.Domain.Common;

namespace ProductCatalog.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public Category Category { get; set; } = null!;
}
