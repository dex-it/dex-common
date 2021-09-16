using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;

namespace Dex.Extensions.Test
{
    public class ExpressionsTest
    {
        private string _field1;
        private string Prop1 { get; set; }
        private ExpressionsTest Exp2 { get; set; }

        [Test]
        public void ValidAccessTest()
        {
            TestExpressionFunc<ExpressionsTest>(x => x.Prop1);
        }

        [Test]
        public void InvalidAccessTest()
        {
            Assert.Catch<ArgumentException>(() => { TestExpressionFunc<ExpressionsTest>(x => x._field1); });
            Assert.Catch<ArgumentException>(() => { TestExpressionFunc<ExpressionsTest>(x => x.ToString()); });
            Assert.Catch<ArgumentException>(() => { TestExpressionFunc<ExpressionsTest>(x => x.Exp2.Prop1); });
        }

        private static void TestExpressionFunc<T>(Expression<Func<T, string>> selector)
        {
            var propertyInfo = selector.GetPropertyInfo();
            TestContext.WriteLine(propertyInfo.Name);
        }
    }
}