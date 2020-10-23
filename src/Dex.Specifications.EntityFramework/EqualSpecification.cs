using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Specifications.EntityFramework
{
    public sealed class EqualSpecification<T, TProperty> : Specification<T>
    {
        public EqualSpecification(Expression<Func<T, TProperty>> expression, TProperty property)
        {
            Predicate = e => EF.Property<TProperty>(e, expression.GetMemberName()).Equals(property);
        }
    }
}