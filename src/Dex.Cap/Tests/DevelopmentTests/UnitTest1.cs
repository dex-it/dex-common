using System.Diagnostics;
using NUnit.Framework;

namespace DevelopmentTests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void SetParentActivityTest()
    {
        var a1 = new Activity("oper-1");
        a1.Start();

        var a2 = new Activity("oper-2");
        Debug.Assert(a1.Id != null, "a1.Id != null");
        
        a2.SetParentId(a1.Id);
        a2.Start();
        
        TestContext.WriteLine(a1.TraceId);
        TestContext.WriteLine(a2.TraceId);

        TestContext.WriteLine(a1.SpanId);
        TestContext.WriteLine(a2.SpanId);
        
        TestContext.WriteLine(a1.Id);
        TestContext.WriteLine(a2.Id);
        
        Assert.Pass();
    }
}