using System;
using System.Diagnostics.CodeAnalysis;

namespace Dex.Pagination.Conditions;

public record PageCondition : IPageCondition
{
    private readonly int _page = 1;
    private readonly int _pageSize = 10;

    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "External usage")]
    public PageCondition()
    {
    }

    public PageCondition(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    //todo После обновления до .net10 использовать ключевое слово field
    public int Page
    {
        get => _page;
        init
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(Page), "must be greater 0");

            _page = value;
        }
    }

    //todo После обновления до .net10 использовать ключевое слово field
    public int PageSize
    {
        get => _pageSize;
        init
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(nameof(PageSize), "must be greater 0");

            _pageSize = value;
        }
    }
}