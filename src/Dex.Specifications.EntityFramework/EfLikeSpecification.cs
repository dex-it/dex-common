using System;
using System.Linq.Expressions;
using Dex.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Specifications.EntityFramework
{
    public sealed class EfLikeSpecification<T> : Specification<T>
    {
        public EfLikeSpecification(Expression<Func<T, string>> expression, string pattern)
        {
            Predicate = e => EF.Functions.Like(EF.Property<string>(e, expression.GetMemberName()), $"%{pattern}%");
        }
    }
}