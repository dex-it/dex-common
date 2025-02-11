namespace Dex.ResponseSigning.Tests;

public record TestEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;
}