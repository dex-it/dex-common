// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Dex.Pagination.Conditions;

public sealed record OrderCondition : IOrderCondition
{
    public OrderCondition(string fieldName, bool isDesc = false)
    {
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        Validate(fieldName);
        IsDesc = isDesc;
    }

    public string FieldName { get; }

    public bool IsDesc { get; }

    private static void Validate(string fieldName)
    {
        if (!Regex.IsMatch(fieldName, "^[\\w|-|\\.|#|$]{1,256}$"))
        {
            throw new InvalidDataException("fieldName contains restricted symbols");
        }
    }
}