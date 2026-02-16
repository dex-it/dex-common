using System.Collections.Generic;


namespace Dex.Pagination.Dto;

public record PageResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];

    public int? Count { get; set; }
}