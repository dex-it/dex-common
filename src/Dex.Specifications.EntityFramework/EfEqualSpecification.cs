using System;
using System.Linq.Expressions;
using Dex.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Specifications.EntityFramework
{
    public sealed class EfEqualSpecification<T, TProperty> : Specification<T>
    {
        public EfEqualSpecification(Expression<Func<T, TProperty>> expression, TProperty property)
        {
            Predicate = e => EF.Property<TProperty>(e, expression.GetMemberName()).Equals(property);
        }
    }
}