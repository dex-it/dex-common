using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Specifications.EntityFramework
{
    public sealed class InSpecification<T, TProperty> : Specification<T>
    {
        public InSpecification(Expression<Func<T, TProperty>> expression, IEnumerable<TProperty> elements)
        {
            Predicate = e => elements.Contains(EF.Property<TProperty>(e, expression.GetMemberName()));
        }
    }
}