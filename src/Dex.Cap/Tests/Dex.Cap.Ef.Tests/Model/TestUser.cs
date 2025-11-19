using System;
using System.ComponentModel.DataAnnotations;

namespace Dex.Cap.Ef.Tests.Model;

public class TestUser
{
    public Guid Id { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    public int Years { get; set; }
}