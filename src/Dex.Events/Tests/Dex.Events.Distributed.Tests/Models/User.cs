using System;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace Dex.Events.Distributed.Tests.Models;

public class User
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Years { get; set; }
}