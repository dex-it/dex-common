using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Dex.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Specifications.EntityFramework
{
    public sealed class EfInSpecification<T, TProperty> : Specification<T>
    {
        public EfInSpecification(Expression<Func<T, TProperty>> expression, IEnumerable<TProperty> elements)
        {
            Predicate = e => elements.Contains(EF.Property<TProperty>(e, expression.GetMemberName()));
        }
    }
}