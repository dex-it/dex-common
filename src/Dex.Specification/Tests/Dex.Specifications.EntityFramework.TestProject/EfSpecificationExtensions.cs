using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Dex.Specifications.EntityFramework.TestProject
{
    public static class EfSpecificationExtensions
    {
        public static Specification<T> AndLike<T>(this Specification<T> current, Expression<Func<T, string>> expression, string pattern)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));

            return current & new EfLikeSpecification<T>(expression, pattern);
        }

        public static Specification<T> AndEqual<T, TProperty>(this Specification<T> current, Expression<Func<T, TProperty>> expression, TProperty property)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (property == null) throw new ArgumentNullException(nameof(property));

            return current & new EfEqualSpecification<T, TProperty>(expression, property);
        }

        public static Specification<T> AndIn<T, TProperty>(this Specification<T> current, Expression<Func<T, TProperty>> expression, IEnumerable<TProperty> elements)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            return current & new EfInSpecification<T, TProperty>(expression, elements);
        }
    }
}