using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Dex.Specifications.EntityFramework
{
    public sealed class CaseSensitiveLikeSpecification<T> : Specification<T>
    {
        public CaseSensitiveLikeSpecification(Expression<Func<T, string>> expression, string pattern)
        {
            Predicate = e => EF.Functions.Like(EF.Property<string>(e, expression.GetMemberName()), $"%{pattern}%");
        }
    }
}