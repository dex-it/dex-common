using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Dex.Specifications.EntityFramework;

namespace Dex.Specifications.TestProject
{
    public static class Sp<T>
    {
        public static Specification<T> Like(Expression<Func<T, string>> expression, string pattern)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));

            return new CaseSensitiveLikeSpecification<T>(expression, pattern);
        }

        public static Specification<T> Equal<TProperty>(Expression<Func<T, TProperty>> expression, TProperty property)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (property == null) throw new ArgumentNullException(nameof(property));

            return new EqualSpecification<T, TProperty>(expression, property);
        }

        public static Specification<T> In<TProperty>(Expression<Func<T, TProperty>> expression, IEnumerable<TProperty> elements)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            return new InSpecification<T, TProperty>(expression, elements);
        }
    }
}