using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dex.Specifications.EntityFramework
{
    public static class SpecificationExtensions
    {
        public static Specification<T> Like<T>(this Specification<T> current, Expression<Func<T, string>> expression, string pattern)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));

            return current & new CaseSensitiveLikeSpecification<T>(expression, pattern);
        }

        public static Specification<T> Equal<T, TProperty>(this Specification<T> current, Expression<Func<T, TProperty>> expression, TProperty property)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (property == null) throw new ArgumentNullException(nameof(property));

            return current & new EqualSpecification<T, TProperty>(expression, property);
        }

        public static Specification<T> In<T, TProperty>(this Specification<T> current, Expression<Func<T, TProperty>> expression, IEnumerable<TProperty> elements)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            return current & new InSpecification<T, TProperty>(expression, elements);
        }
    }
}